// Author:
//
// T. Freyburger (t.freyburger@gmail.com)
//
// Copyright (c)  Thierry Freyburger
//
// Licensed under the GPLV3 license.
////
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Mono.Cecil.Cil;
using AspectDN.Common;
using AspectDN.Aspect.Weaving.IConcerns;
using System.Runtime.InteropServices;

namespace AspectDN.Aspect.Weaving
{
    internal interface IILWorker
    {
        MethodBody Merge();
    }

    internal class MethodBodyMerger : BaseMethodBodyBuilder, IILWorker
    {
        internal static MethodBodyMerger Create(ILMergekinds ilMergerKind, MethodDefinition fromMethod, WeaveItemMember weaveItemMember, MethodBody targetMethodBody)
        {
            if (fromMethod.Body.Instructions.Where(t => t.OpCode == OpCodes.Ret).Count() > 1)
                throw new NotImplementedException("Advice code can not be applied in a method target containing more than one return IL instruction");
            return new MethodBodyMerger(ilMergerKind, fromMethod, weaveItemMember, targetMethodBody);
        }

        ILMergekinds _ILMergeKind;
        MethodBody _NewMethodBody;
        Instruction _TargetReturnLabel;
        Instruction _NewTargetReturnLabel;
        Instruction _SourceReturnLabel;
        internal MethodBody NewMethodBody => _NewMethodBody;
        internal MethodBodyMerger(ILMergekinds ilMergeKind, MethodDefinition fromMethod, WeaveItemMember weaveItemMember, MethodBody targetMethodBody) : base(fromMethod, weaveItemMember, targetMethodBody)
        {
            _ILMergeKind = ilMergeKind;
            _SetupILWorkItems();
        }

        internal void InsertBefore(Instruction from, Instruction to)
        {
            var atIndex = 0;
            _Insert(from, to, atIndex, ILLocalisations.Before | ILLocalisations.Source);
        }

        internal void InsertBefore(Instruction fromIL, Instruction toIL, Instruction beforeIL)
        {
            var atIndex = _ILWorkItems.IndexOf(_ILWorkItems.FirstOrDefault(t => t.Instruction == beforeIL));
            _Insert(fromIL, toIL, atIndex, ILLocalisations.Before | ILLocalisations.Source);
        }

        internal void InsertAfter(Instruction from, Instruction to, Instruction afterIL)
        {
            int atIndex = 0;
            if (_IsReturnInstruction(afterIL))
                atIndex = _ILWorkItems.IndexOf(_ILWorkItems.FirstOrDefault(t => t.Instruction == _TargetReturnLabel));
            else
            {
                if (afterIL.Next != null)
                    atIndex = _ILWorkItems.IndexOf(_ILWorkItems.FirstOrDefault(t => t.Instruction == afterIL.Next));
                else
                    atIndex = _ILWorkItems.Count;
            }

            _Insert(from, to, atIndex, ILLocalisations.Source | ILLocalisations.After);
        }

        internal void InsertEndAfter(Instruction from, Instruction to)
        {
            var targetReturnLabelIndex = _ILWorkItems.IndexOf(_ILWorkItems.Where(t => t.Instruction == _TargetReturnLabel).FirstOrDefault());
            if (targetReturnLabelIndex == -1)
            {
                targetReturnLabelIndex = _ILWorkItems.Count;
                _Insert(from, to, targetReturnLabelIndex, ILLocalisations.Source | ILLocalisations.AfterEnd);
            }
            else
            {
                if (_TargetMethod.ReturnType.ToString() == typeof(void).ToString() && _TargetReturnLabel.Previous != null && _TargetReturnLabel.Previous.OpCode == OpCodes.Nop)
                    targetReturnLabelIndex--;
                _Insert(from, to, targetReturnLabelIndex, ILLocalisations.Source | ILLocalisations.AfterEnd);
            }
        }

        internal MethodBody Merge()
        {
            _LabelRecalculation();
            _OffsetRecalculation();
            _CopyNewHandlers();
            _ChangeBranchAccordingOffsetInterval();
            if (_TargetMethod.DebugInformation != null)
                _UpdateDebugInformation();
            _WriteNewILInstructions(_NewMethodBody);
            return _NewMethodBody;
        }

        void _UpdateDebugInformation()
        {
        }

        void _Insert(Instruction fromIL, Instruction toIL, int atIndex, ILLocalisations ilLocationsation)
        {
            while (fromIL != null && fromIL.Offset <= toIL.Offset)
                fromIL = _AddIL(ref atIndex, fromIL, ilLocationsation);
        }

        Instruction _AddIL(ref int atIndex, Instruction sourceIL, ILLocalisations ilLocalisation)
        {
            var ilChange = _ConvertedILs.FirstOrDefault(t => t.SourceILBlock.FirstIL.Offset <= sourceIL.Offset && t.SourceILBlock.LastIL.Offset >= sourceIL.Offset);
            atIndex = _CloneIL(atIndex, sourceIL, ilChange, ilLocalisation);
            if (ilChange != null)
                sourceIL = ilChange.SourceILBlock.LastIL.Next;
            else
                sourceIL = sourceIL.Next;
            return sourceIL;
        }

        VariableDefinition _GetReturnedVariable(MethodBody methodBody)
        {
            VariableDefinition variable = null;
            var returnLabel = CecilHelper.GetReturnLabel(methodBody);
            if (returnLabel != null && !_IsReturnInstruction(returnLabel))
                variable = CecilHelper.GetVariable(methodBody, OpCodeDatas.Get(returnLabel.OpCode), returnLabel.Operand);
            return variable;
        }

        void _SetupILWorkItems()
        {
            _CopyTargetBodyToNewMethod();
            _SourceReturnLabel = CecilHelper.GetReturnLabel(_SourceMethod.Body);
            if (_SourceReturnLabel != null && _SourceReturnLabel.Operand is MethodReference && ((MethodReference)_SourceReturnLabel.Operand).DeclaringType.FullName == typeof(EndCodeException).FullName)
                _SourceReturnLabel = null;
            _NewTargetReturnLabel = _TargetReturnLabel = CecilHelper.GetReturnLabel(_NewMethodBody);
            if (_SourceMethod.DebugInformation != null && _SourceMethod.DebugInformation.Scope != null)
            {
                foreach (var sourceVariable in _SourceMethod.Body.Variables)
                    _VariableIndexNames.Add(CecilHelper.GetVariableName(_SourceMethod, sourceVariable), sourceVariable);
            }

            var ils = _SourceMethod.Body.Instructions.Where(t => (OpCodeDatas.Get(t.OpCode).OpCodeType & OpCodeTypes.LocVar) == OpCodeTypes.LocVar);
            foreach (var il in ils)
            {
                var sourceVariable = CecilHelper.GetVariable(_SourceMethod.Body, OpCodeDatas.Get(il.OpCode), il.Operand);
                if (!_VariableTranslations.ContainsKey(sourceVariable))
                {
                    var returnedVariable = _GetReturnedVariable(_SourceMethod.Body);
                    var newReturnedVariable = _GetReturnedVariable(_NewMethodBody);
                    if (_NewMethodBody.Instructions.Any() && sourceVariable == returnedVariable && newReturnedVariable != null)
                        _VariableTranslations.Add(sourceVariable, newReturnedVariable);
                    else
                        _CloneVariable(sourceVariable, _NewMethodBody);
                }
            }

            _BuildIlConversions(_NewMethodBody);
            if (_WeaveItemMember.OnError)
                return;
            foreach (var il in _NewMethodBody.Instructions)
                _ILWorkItems.Add(new ILWorkItem((il.Offset, il.Offset), il, ILLocalisations.Target));
        }

        void _CopyTargetBodyToNewMethod()
        {
            _NewMethodBody = new MethodBody(_TargetMethod);
            _NewMethodBody.InitLocals = _TargetMethodBody.InitLocals;
            _NewMethodBody.LocalVarToken = _TargetMethodBody.LocalVarToken;
            _NewMethodBody.MaxStackSize = _TargetMethodBody.MaxStackSize;
            var sourceReturnLabel = CecilHelper.GetReturnLabel(_SourceMethod.Body);
            var targetReturnLabel = CecilHelper.GetReturnLabel(_TargetMethod.Body);
            var addRetIL = false;
            var addVarRet = false;
            var addSaveAndGoRet = false;
            if (targetReturnLabel == null || (targetReturnLabel.Next != null && targetReturnLabel.Next.OpCode == OpCodes.Throw))
            {
                if (sourceReturnLabel != null)
                {
                    addRetIL = true;
                    if (_TargetMethod.ReturnType.FullName != typeof(void).FullName)
                        addVarRet = true;
                }
            }

            if (targetReturnLabel == _TargetMethodBody.Instructions.First() && _TargetMethod.ReturnType.FullName != typeof(void).FullName)
            {
                addSaveAndGoRet = true;
            }

            if (addVarRet || addSaveAndGoRet)
                _NewMethodBody.Variables.Add(new VariableDefinition(_TargetMethod.ReturnType));
            foreach (VariableDefinition variable in _TargetMethodBody.Variables)
                _NewMethodBody.Variables.Add(new VariableDefinition(variable.VariableType));
            foreach (var ilToCopy in _TargetMethodBody.Instructions)
            {
                Instruction newInstruction = null;
                if (ilToCopy.OpCode.OperandType == OperandType.InlineNone)
                {
                    var opCodeData = OpCodeDatas.Get(ilToCopy.OpCode);
                    if ((addVarRet || addSaveAndGoRet) && opCodeData.OpCodeType == OpCodeTypes.LocVar && opCodeData.Index != -1)
                        newInstruction = CecilHelper.GetVariableIL(opCodeData, _NewMethodBody.Variables[opCodeData.Index + 1]);
                    else
                        newInstruction = Instruction.Create(ilToCopy.OpCode);
                }
                else
                {
                    switch (ilToCopy.Operand)
                    {
                        case TypeReference typeReference:
                            newInstruction = Instruction.Create(ilToCopy.OpCode, typeReference);
                            break;
                        case CallSite callSite:
                            newInstruction = Instruction.Create(ilToCopy.OpCode, callSite);
                            break;
                        case MethodReference methodReference:
                            newInstruction = Instruction.Create(ilToCopy.OpCode, methodReference);
                            break;
                        case FieldReference fieldReference:
                            newInstruction = Instruction.Create(ilToCopy.OpCode, fieldReference);
                            break;
                        case string @string:
                            newInstruction = Instruction.Create(ilToCopy.OpCode, @string);
                            break;
                        case sbyte @sbyte:
                            newInstruction = Instruction.Create(ilToCopy.OpCode, @sbyte);
                            break;
                        case byte @byte:
                            newInstruction = Instruction.Create(ilToCopy.OpCode, @byte);
                            break;
                        case int @int:
                            newInstruction = Instruction.Create(ilToCopy.OpCode, @int);
                            break;
                        case long @long:
                            newInstruction = Instruction.Create(ilToCopy.OpCode, @long);
                            break;
                        case float @float:
                            newInstruction = Instruction.Create(ilToCopy.OpCode, @float);
                            break;
                        case double @double:
                            newInstruction = Instruction.Create(ilToCopy.OpCode, @double);
                            break;
                        case VariableDefinition variable:
                            newInstruction = Instruction.Create(ilToCopy.OpCode, variable);
                            break;
                        case ParameterDefinition parameter:
                            newInstruction = Instruction.Create(ilToCopy.OpCode, parameter);
                            break;
                        case Instruction instruction:
                            newInstruction = Instruction.Create(ilToCopy.OpCode, instruction);
                            break;
                        case Instruction[] instructions:
                            newInstruction = Instruction.Create(ilToCopy.OpCode, instructions);
                            break;
                        default:
                            throw new NotSupportedException($"unable to copy this instruction as it is unknown");
                    }
                }

                newInstruction.Offset = ilToCopy.Offset;
                _NewMethodBody.Instructions.Add(newInstruction);
            }

            foreach (var newInstruction in _NewMethodBody.Instructions.Where(t => t.Operand is Instruction || t.Operand is Instruction[]))
            {
                switch (newInstruction.Operand)
                {
                    case Instruction[] instructions:
                        newInstruction.Operand = new Instruction[instructions.Length];
                        for (int i = 0; i < instructions.Length; i++)
                            ((Instruction[])newInstruction.Operand)[i] = _NewMethodBody.Instructions.First(t => t.Offset == instructions[i].Offset);
                        break;
                    case Instruction instruction:
                        newInstruction.Operand = _NewMethodBody.Instructions.First(t => t.Offset == instruction.Offset);
                        break;
                }
            }

            foreach (var handler in _TargetMethodBody.ExceptionHandlers)
            {
                ExceptionHandler newHandler = new ExceptionHandler(handler.HandlerType);
                if (handler.CatchType != null)
                    newHandler.CatchType = handler.CatchType;
                if (handler.HandlerStart != null)
                    newHandler.HandlerStart = _NewMethodBody.Instructions.First(t => t.Offset == handler.HandlerStart.Offset);
                if (handler.HandlerEnd != null)
                    newHandler.HandlerEnd = _NewMethodBody.Instructions.First(t => t.Offset == handler.HandlerEnd.Offset);
                if (handler.TryStart != null)
                    newHandler.TryStart = _NewMethodBody.Instructions.First(t => t.Offset == handler.TryStart.Offset);
                if (handler.TryEnd != null)
                    newHandler.TryEnd = _NewMethodBody.Instructions.First(t => t.Offset == handler.TryEnd.Offset);
                if (handler.FilterStart != null)
                    newHandler.FilterStart = _NewMethodBody.Instructions.First(t => t.Offset == handler.FilterStart.Offset);
                _NewMethodBody.ExceptionHandlers.Add(newHandler);
            }

            if (addRetIL)
            {
                if (addVarRet)
                    _NewMethodBody.Instructions.Insert(_NewMethodBody.Instructions.Count, Instruction.Create(OpCodes.Ldloc_0));
                _NewMethodBody.Instructions.Insert(_NewMethodBody.Instructions.Count, Instruction.Create(OpCodes.Ret));
            }
            else
            {
                if (addSaveAndGoRet)
                {
                    _NewMethodBody.Instructions.Insert(_NewMethodBody.Instructions.Count - 1, Instruction.Create(OpCodes.Stloc_0));
                    _NewMethodBody.Instructions.Insert(_NewMethodBody.Instructions.Count - 1, Instruction.Create(OpCodes.Ldloc_0));
                    _NewMethodBody.Instructions.Insert(_NewMethodBody.Instructions.Count - 2, Instruction.Create(OpCodes.Br_S, _NewMethodBody.Instructions.Last().Previous));
                }
            }
        }

        void _LabelRecalculation()
        {
            foreach (var gotoInstruction in _ILWorkItems.Where(t => t.Instruction.Operand is Instruction || t.Instruction.Operand is Instruction[]))
            {
                switch (gotoInstruction.ILLocalisation & ILLocalisations.Target)
                {
                    case ILLocalisations.Target:
                        switch (gotoInstruction.Instruction.Operand)
                        {
                            case Instruction instruction:
                                gotoInstruction.Instruction.Operand = _GetNewTargetLabel(instruction);
                                break;
                            case Instruction[] instructions:
                                for (int i = 0; i < instructions.Length; i++)
                                    instructions[i] = _GetNewTargetLabel(instructions[i]);
                                break;
                        }

                        break;
                    default:
                        switch (gotoInstruction.Instruction.Operand)
                        {
                            case Instruction instruction:
                                gotoInstruction.Instruction.Operand = _GetNewSourceLabel(instruction, WeaverHelper.AreILLocalisationEqual(gotoInstruction.ILLocalisation, ILLocalisations.Source | ILLocalisations.AfterEnd));
                                break;
                            case Instruction[] instructions:
                                for (int i = 0; i < instructions.Length; i++)
                                    instructions[i] = _GetNewSourceLabel(instructions[i], WeaverHelper.AreILLocalisationEqual(gotoInstruction.ILLocalisation, ILLocalisations.Source | ILLocalisations.AfterEnd));
                                break;
                        }

                        break;
                }
            }

            if (_ILMergeKind == ILMergekinds.Body && _ILWorkItems.Any(t => t.ILLocalisation == (t.ILLocalisation & (ILLocalisations.AfterEnd | ILLocalisations.Source))))
            {
                var firstIlWrkItemAfterEnd = _ILWorkItems.First(t => t.ILLocalisation == (t.ILLocalisation & (ILLocalisations.Source | ILLocalisations.AfterEnd)));
                var firstILWorkAfterEndindex = _ILWorkItems.IndexOf(firstIlWrkItemAfterEnd);
                var firstIlWrkItemAfterEndBranchOperand = firstIlWrkItemAfterEnd;
                var firstIlWrkItemAfterEndBranchOperandIndex = firstILWorkAfterEndindex;
                while (firstIlWrkItemAfterEndBranchOperand.Instruction.OpCode == OpCodes.Nop)
                {
                    firstIlWrkItemAfterEndBranchOperandIndex += 1;
                    firstIlWrkItemAfterEndBranchOperand = _ILWorkItems[firstIlWrkItemAfterEndBranchOperandIndex];
                }

                foreach (var ilWrkItemReturn in _ILWorkItems.Where(t => t.ILLocalisation == ILLocalisations.Target && _ILWorkItems.IndexOf(t) < firstILWorkAfterEndindex && t.Instruction.Operand == _TargetReturnLabel))
                {
                    ilWrkItemReturn.Instruction.Operand = firstIlWrkItemAfterEndBranchOperand.Instruction;
                }

                var prvIlWrkItem = _ILWorkItems[firstILWorkAfterEndindex - 1];
                if (OpCodeDatas.Get(prvIlWrkItem.Instruction.OpCode).IsBranch && prvIlWrkItem.Instruction.Operand == firstIlWrkItemAfterEndBranchOperand.Instruction)
                {
                    var ilWrkItemRefInstructions = _ILWorkItems.Where(t => t.Instruction.Operand == prvIlWrkItem.Instruction || (t.Instruction.Operand is Instruction[] && ((Instruction[])t.Instruction.Operand).Any(i => i == prvIlWrkItem.Instruction)));
                    foreach (var ilWrkItemRefInstruction in ilWrkItemRefInstructions)
                    {
                        switch (ilWrkItemRefInstruction.Instruction.Operand)
                        {
                            case Instruction instruction:
                                ilWrkItemRefInstruction.Instruction.Operand = firstIlWrkItemAfterEndBranchOperand.Instruction;
                                break;
                            case Instruction[] instructions:
                                for (int i = 0; i < ((Instruction[])ilWrkItemRefInstruction.Instruction.Operand).Length; i++)
                                    ((Instruction[])ilWrkItemRefInstruction.Instruction.Operand)[i] = firstIlWrkItemAfterEndBranchOperand.Instruction;
                                break;
                        }
                    }

                    _ILWorkItems.Remove(prvIlWrkItem);
                }
            }

            if (_TargetMethod.ReturnType.FullName != typeof(void).FullName)
            {
                var atIndex = _ILWorkItems.IndexOf(_ILWorkItems.Where(t => t.Instruction == _TargetReturnLabel).FirstOrDefault());
                if (atIndex > 0 && (!OpCodeDatas.Get(_ILWorkItems[atIndex - 1].Instruction.OpCode).IsBranch || (OpCodeDatas.Get(_ILWorkItems[atIndex - 1].Instruction.OpCode).IsBranch && _ILWorkItems[atIndex - 1].Instruction.Operand != _TargetReturnLabel)))
                {
                    _ILWorkItems.Insert(atIndex, new ILWorkItem((-1, -1), Instruction.Create(OpCodes.Br_S, _TargetReturnLabel), ILLocalisations.Source | ILLocalisations.AfterEnd));
                }
            }
        }

        Instruction _GetNewTargetLabel(Instruction oldLabel)
        {
            if (oldLabel == _TargetReturnLabel)
                return _NewTargetReturnLabel;
            var index = _ILWorkItems.IndexOf(_ILWorkItems.First(t => t.Instruction == oldLabel));
            if (index > 0)
            {
                if (WeaverHelper.AreILLocalisationEqual(_ILWorkItems[index - 1].ILLocalisation, ILLocalisations.Source))
                {
                    while (index >= 0 && WeaverHelper.AreILLocalisationEqual(_ILWorkItems[index - 1].ILLocalisation, ILLocalisations.Source))
                        --index;
                }

                return _ILWorkItems[index].Instruction;
            }

            var newLabel = _ILWorkItems.First(t => WeaverHelper.AreILLocalisationEqual(t.ILLocalisation, ILLocalisations.Target) && t.Instruction.Offset == oldLabel.Offset).Instruction;
            return newLabel;
        }

        Instruction _GetNewSourceLabel(Instruction oldLabel, bool isAfterEndLocalisation)
        {
            if (oldLabel == _SourceReturnLabel)
                return isAfterEndLocalisation ? _TargetReturnLabel : _NewTargetReturnLabel;
            else
            {
                if (oldLabel.Operand is MethodReference && ((MethodReference)oldLabel.Operand).DeclaringType.FullName == typeof(EndCodeException).FullName)
                {
                    var firstEndTargetIlWorkItem = _GetFirstEndTargetIlWorkItem();
                    return firstEndTargetIlWorkItem.Instruction;
                }
                else
                {
                    var newLabel = _GetInstruction(oldLabel.Offset);
                    if (newLabel == null)
                    {
                        var previous = _GetILWorkInstruction(oldLabel.Previous.Offset);
                        newLabel = _ILWorkItems[_ILWorkItems.IndexOf(previous) + 1].Instruction;
                    }

                    return newLabel;
                }
            }
        }

        void _CopyNewHandlers()
        {
            bool between = _ILWorkItems.Any(t => WeaverHelper.AreILLocalisationEqual(t.ILLocalisation, ILLocalisations.Source | ILLocalisations.Before)) && _ILWorkItems.Any(t => WeaverHelper.AreILLocalisationEqual(t.ILLocalisation, ILLocalisations.Source | ILLocalisations.After) || WeaverHelper.AreILLocalisationEqual(t.ILLocalisation, ILLocalisations.Source | ILLocalisations.AfterEnd));
            foreach (ExceptionHandler handler in _SourceMethod.Body.ExceptionHandlers)
            {
                ExceptionHandler newHandler = new ExceptionHandler(handler.HandlerType);
                if (handler.CatchType != null)
                    newHandler.CatchType = _WeaveItemMember.Resolve(handler.CatchType);
                if (handler.HandlerStart != null)
                    newHandler.HandlerStart = _GetNewHandlerLimit(between, handler.HandlerStart, true);
                if (handler.HandlerEnd != null)
                    newHandler.HandlerEnd = _GetNewHandlerLimit(between, handler.HandlerEnd, false);
                if (handler.TryStart != null)
                    newHandler.TryStart = _GetNewHandlerLimit(between, handler.TryStart, true);
                if (handler.TryEnd != null)
                    newHandler.TryEnd = _GetNewHandlerLimit(between, handler.TryEnd, false);
                if (handler.FilterStart != null)
                    newHandler.FilterStart = _GetNewHandlerLimit(between, handler.FilterStart, true);
                _NewMethodBody.ExceptionHandlers.Add(newHandler);
                if (newHandler.HandlerStart != null)
                {
                    var start = _ILWorkItems.IndexOf(_ILWorkItems.Where(t => t.Instruction == newHandler.HandlerStart).First());
                    var end = _ILWorkItems.IndexOf(_ILWorkItems.Where(t => t.Instruction == newHandler.HandlerEnd).First()) - 1;
                    _CleanExceptionHandlerLeaveLabel(start, end);
                }

                if (newHandler.TryStart != null)
                {
                    var start = _ILWorkItems.IndexOf(_ILWorkItems.Where(t => t.Instruction == newHandler.TryStart).First());
                    var end = _ILWorkItems.IndexOf(_ILWorkItems.Where(t => t.Instruction == newHandler.TryEnd).First()) - 1;
                    _CleanExceptionHandlerLeaveLabel(start, end);
                }
            }

            var handlers = new List<(int start, int end, int endhandler)>();
            foreach (ExceptionHandler handler in _NewMethodBody.ExceptionHandlers)
            {
                var start = _ILWorkItems.IndexOf(_ILWorkItems.Where(t => t.Instruction == handler.TryStart).First());
                var end = _ILWorkItems.IndexOf(_ILWorkItems.Where(t => t.Instruction == handler.TryEnd).First()) - 1;
                var endHandler = _ILWorkItems.IndexOf(_ILWorkItems.Where(t => t.Instruction == handler.HandlerEnd).First()) - 1;
                if (!handlers.Any(t => t.start == start && t.end == end))
                    handlers.Add((start, end, endHandler));
                else
                {
                    var existingHandler = handlers.Where(t => t.start == start && t.end == end).FirstOrDefault();
                    if (existingHandler.endhandler < endHandler)
                    {
                        handlers.Remove(existingHandler);
                        existingHandler.endhandler = endHandler;
                        handlers.Add(existingHandler);
                    }
                }
            }

            if (_ILMergeKind == ILMergekinds.None)
                return;
            foreach (var handlerLimit in handlers)
            {
                if (_IsReturnLeave(_TargetMethod.ReturnType, handlerLimit.end))
                {
                    if (WeaverHelper.AreILLocalisationEqual(_ILWorkItems[handlerLimit.end].ILLocalisation, ILLocalisations.Source) || _ILMergeKind == ILMergekinds.Instruction)
                    {
                        var firstOut = _ILWorkItems[handlerLimit.endhandler + 1].Instruction;
                        if (firstOut != _TargetReturnLabel)
                            _ILWorkItems[handlerLimit.end].Instruction.Operand = firstOut;
                    }
                }

                var tryStart = _ILWorkItems[handlerLimit.start].Instruction;
                var tryEnd = _ILWorkItems[handlerLimit.end + 1].Instruction;
                foreach (ExceptionHandler handler in _NewMethodBody.ExceptionHandlers.Where(t => t.TryStart == tryStart && t.TryEnd == tryEnd))
                {
                    var handlerEnd = _ILWorkItems.IndexOf(_ILWorkItems.Where(t => t.Instruction == handler.HandlerEnd).First()) - 1;
                    if (_IsReturnLeave(_TargetMethod.ReturnType, handlerEnd))
                    {
                        if (WeaverHelper.AreILLocalisationEqual(_ILWorkItems[handlerEnd].ILLocalisation, ILLocalisations.Source) || _ILMergeKind == ILMergekinds.Instruction)
                        {
                            var firstOut = _ILWorkItems[handlerLimit.endhandler + 1].Instruction;
                            if (firstOut != _TargetReturnLabel)
                                _ILWorkItems[handlerEnd].Instruction.Operand = firstOut;
                        }
                    }
                }
            }
        }

        bool _IsReturnLeave(TypeReference returnType, int leaveIndex)
        {
            var leaves = _ILWorkItems[leaveIndex];
            if (leaves.Instruction.Operand != _TargetReturnLabel)
                return false;
            if (returnType.ToString() != typeof(void).ToString())
            {
                var previous = _ILWorkItems[leaveIndex - 1].Instruction;
                if ((OpCodeDatas.Get(previous.OpCode).OpCodeType & OpCodeTypes.LocVar) == OpCodeTypes.LocVar)
                {
                    var fromVar = CecilHelper.GetVariable(_SourceMethod.Body, OpCodeDatas.Get(previous.OpCode), previous.Operand);
                    if (fromVar != _GetReturnedVariable(_SourceMethod.Body))
                        return false;
                }
            }

            return true;
        }

        void _CleanExceptionHandlerLeaveLabel(int from, int to)
        {
            var gotoInstructions = _ILWorkItems.Where(t => WeaverHelper.AreILLocalisationEqual(t.ILLocalisation, ILLocalisations.Target) && (OpCodeDatas.Get(t.Instruction.OpCode).OpCodeType & OpCodeTypes.Branch) == OpCodeTypes.Branch && from >= _ILWorkItems.IndexOf(t) && _ILWorkItems.IndexOf(t) <= to).ToList();
            foreach (var gotoInstruction in gotoInstructions)
            {
                if (_ILWorkItems.IndexOf(_ILWorkItems.Where(t => t.Instruction == gotoInstruction.Instruction.Operand).First()) <= to)
                    continue;
                if (gotoInstruction.Instruction.Operand == _TargetReturnLabel)
                {
                    if (_ILWorkItems.IndexOf(gotoInstruction) == to)
                    {
                    }
                    else
                    {
                    }
                }

                int i = from;
                while (i < to)
                {
                    if (_ILWorkItems[i].Instruction.OpCode != OpCodes.Nop)
                        break;
                    i++;
                }

                if (i != to)
                {
                    if (gotoInstruction.Instruction.OpCode == OpCodes.Br)
                        gotoInstruction.Instruction.OpCode = OpCodes.Leave;
                    else if (gotoInstruction.Instruction.OpCode == OpCodes.Br_S)
                        gotoInstruction.Instruction.OpCode = OpCodes.Leave_S;
                }
                else
                {
                    i = _ILWorkItems.IndexOf(gotoInstruction);
                    var leave = _ILWorkItems[to];
                    while (_ILWorkItems[i] != leave)
                        _ILWorkItems.RemoveAt(i);
                }
            }
        }

        Instruction _GetNewHandlerLimit(bool between, Instruction oldLimit, bool startLimit)
        {
            if (startLimit)
            {
                var ilWorkItem = _GetILWorkInstruction(oldLimit.Offset);
                if (ilWorkItem == null && between)
                    ilWorkItem = _ILWorkItems[_ILWorkItems.IndexOf(_ILWorkItems.Where(t => WeaverHelper.AreILLocalisationEqual(t.ILLocalisation, ILLocalisations.Source | ILLocalisations.Before)).Last())];
                return ilWorkItem.Instruction;
            }
            else
            {
                var ilWorkItem = _GetILWorkInstruction(oldLimit.Offset);
                if ((ilWorkItem == null && between) || _SourceReturnLabel == null || (_SourceReturnLabel != null && _SourceReturnLabel.Offset >= oldLimit.Offset))
                {
                    var previous = _GetILWorkInstruction(oldLimit.Previous.Offset);
                    ilWorkItem = _ILWorkItems[_ILWorkItems.IndexOf(previous) + 1];
                }

                return ilWorkItem.Instruction;
            }
        }

        MethodBody IILWorker.Merge() => Merge();
    }
}