// Author:
//
// T. Freyburger (t.freyburger@gmail.com)
//
// Copyright (c)  Thierry Freyburger
//
// Licensed under the GPLV3 license.
////
using AspectDN.Common;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AspectDN.Aspect.Weaving.IJoinpoints;
using Foundation.Common.Error;

namespace AspectDN.Aspect.Weaving
{
    internal abstract class BaseMethodBodyBuilder
    {
        protected List<ILWorkItem> _ILWorkItems;
        protected Dictionary<VariableDefinition, VariableDefinition> _VariableTranslations;
        protected Dictionary<string, VariableDefinition> _VariableIndexNames;
        protected List<ConvertedIL> _ConvertedILs;
        protected MethodDefinition _SourceMethod;
        protected WeaveItemMember _WeaveItemMember;
        protected MethodDefinition _TargetMethod;
        protected MethodBody _TargetMethodBody;
        internal BaseMethodBodyBuilder()
        {
            _VariableIndexNames = new Dictionary<string, VariableDefinition>();
            _VariableTranslations = new Dictionary<VariableDefinition, VariableDefinition>();
            _ILWorkItems = new List<ILWorkItem>();
        }

        internal BaseMethodBodyBuilder(MethodDefinition sourceMethod, WeaveItemMember weaveItemMember) : this()
        {
            _WeaveItemMember = weaveItemMember;
            _SourceMethod = sourceMethod;
            _TargetMethod = (MethodDefinition)_WeaveItemMember.Resolve(_SourceMethod);
            _TargetMethodBody = _TargetMethod.Body;
        }

        internal BaseMethodBodyBuilder(MethodDefinition sourceMethod, WeaveItemMember weaveItemMember, MethodBody targetMethodBody) : this()
        {
            _WeaveItemMember = weaveItemMember;
            _SourceMethod = sourceMethod;
            if (weaveItemMember.WeaveItem.Joinpoint is IInstructionJoinpoint)
                _TargetMethod = ((IInstructionJoinpoint)weaveItemMember.WeaveItem.Joinpoint).CallingMethod;
            else
                _TargetMethod = targetMethodBody.Method;
            _TargetMethodBody = targetMethodBody;
        }

        protected void _CloneVariable(VariableDefinition sourceVariable, MethodBody targetMethodBody)
        {
            var targetVariable = new VariableDefinition(_WeaveItemMember.Resolve(sourceVariable.VariableType));
            string targetVariableName = null;
            if (_SourceMethod.DebugInformation != null && _SourceMethod.DebugInformation.Scope != null)
            {
                targetVariableName = CecilHelper.GetVariableName(_SourceMethod, sourceVariable);
                if (!string.IsNullOrEmpty(targetVariableName))
                {
                    if (_VariableIndexNames.ContainsKey(targetVariableName))
                        targetVariableName = $"{targetVariableName}{targetVariable.Index}";
                }
            }

            targetVariableName = _AddTargetBodyVariable(targetMethodBody, targetVariable, targetVariableName);
            _VariableTranslations.Add(sourceVariable, targetVariable);
        }

        protected string _AddTargetBodyVariable(MethodBody targetMethodBody, VariableDefinition variable, string targetVariableName)
        {
            targetMethodBody.Variables.Add(variable);
            if (string.IsNullOrEmpty(targetVariableName))
                targetVariableName = $"V_{variable.Index}";
            _VariableIndexNames.Add(targetVariableName, variable);
            return targetVariableName;
        }

        protected int _CloneIL(int atIndex, Instruction sourceIL, ConvertedIL iLChange, ILLocalisations ilLocalisation)
        {
            (int from, int to) intervalOffset = (sourceIL.Offset, sourceIL.Offset);
            if (iLChange != null)
            {
                intervalOffset = (iLChange.SourceILBlock.From.Offset, iLChange.SourceILBlock.To.Offset);
                foreach (var il in iLChange.TargetILs)
                    _ILWorkItems.Insert(atIndex++, new ILWorkItem(intervalOffset, il, ilLocalisation));
                return atIndex;
            }

            var targetInstruction = _CloneInstruction(sourceIL);
            _ILWorkItems.Insert(atIndex++, new ILWorkItem(intervalOffset, targetInstruction, ilLocalisation));
            return atIndex;
        }

        protected Instruction _CloneInstruction(Instruction sourceIL)
        {
            var targetInstruction = sourceIL;
            if (sourceIL.OpCode.OperandType == OperandType.InlineNone)
            {
                switch (OpCodeDatas.Get(sourceIL.OpCode).OpCodeType)
                {
                    case OpCodeTypes.LocVar | OpCodeTypes.Ld:
                    case OpCodeTypes.LocVar | OpCodeTypes.St:
                        var oldVariable = CecilHelper.GetVariable(_SourceMethod.Body, OpCodeDatas.Get(sourceIL.OpCode), sourceIL.Operand);
                        var newVariable = _VariableTranslations[oldVariable];
                        targetInstruction = CecilHelper.GetVariableIL(OpCodeDatas.Get(sourceIL.OpCode), newVariable);
                        break;
                    default:
                        targetInstruction = Instruction.Create(sourceIL.OpCode);
                        break;
                }
            }
            else
            {
                switch (sourceIL.Operand)
                {
                    case TypeReference typeReference:
                        var newTypeReference = _WeaveItemMember.Resolve(typeReference);
                        targetInstruction = Instruction.Create(sourceIL.OpCode, newTypeReference);
                        break;
                    case CallSite callSite:
                        targetInstruction = Instruction.Create(sourceIL.OpCode, _WeaveItemMember.Resolve(callSite));
                        break;
                    case MethodReference methodReference:
                        var newMethodReference = _WeaveItemMember.Resolve(methodReference);
                        var opCode = sourceIL.OpCode;
                        if (newMethodReference is IError)
                        {
                            _WeaveItemMember.AddError((IError)newMethodReference);
                            newMethodReference = methodReference;
                        }
                        else
                        {
                            opCode = _ResolveCallOpCode(sourceIL, methodReference.Resolve());
                        }

                        targetInstruction = Instruction.Create(opCode, (MethodReference)newMethodReference);
                        break;
                    case FieldReference fieldReference:
                        if (fieldReference.Name == "&this")
                            targetInstruction = Instruction.Create(OpCodes.Ldarg_0);
                        else
                        {
                            var newFieldReference = _WeaveItemMember.Resolve(fieldReference);
                            if (newFieldReference is IError)
                            {
                                _WeaveItemMember.AddError((IError)newFieldReference);
                                newFieldReference = fieldReference;
                            }

                            if (newFieldReference is string && "This" == (string)newFieldReference)
                            {
                                throw new NotSupportedException("This encounter");
                            }

                            targetInstruction = Instruction.Create(sourceIL.OpCode, (FieldReference)newFieldReference);
                        }

                        break;
                    case string @string:
                        targetInstruction = Instruction.Create(sourceIL.OpCode, @string);
                        break;
                    case sbyte @sbyte:
                        targetInstruction = Instruction.Create(sourceIL.OpCode, @sbyte);
                        break;
                    case byte @byte:
                        targetInstruction = Instruction.Create(sourceIL.OpCode, @byte);
                        break;
                    case int @int:
                        targetInstruction = Instruction.Create(sourceIL.OpCode, @int);
                        break;
                    case long @long:
                        targetInstruction = Instruction.Create(sourceIL.OpCode, @long);
                        break;
                    case float @float:
                        targetInstruction = Instruction.Create(sourceIL.OpCode, @float);
                        break;
                    case double @double:
                        targetInstruction = Instruction.Create(sourceIL.OpCode, @double);
                        break;
                    case VariableDefinition variable:
                        var newVariable = _VariableTranslations[variable];
                        targetInstruction = CecilHelper.GetVariableIL(OpCodeDatas.Get(sourceIL.OpCode), newVariable);
                        break;
                    case ParameterDefinition parameter:
                        targetInstruction = Instruction.Create(sourceIL.OpCode, _TargetMethod.Parameters[parameter.Index]);
                        break;
                    case Instruction instruction:
                        targetInstruction = Instruction.Create(sourceIL.OpCode, instruction);
                        break;
                    case Instruction[] instructions:
                        targetInstruction = Instruction.Create(sourceIL.OpCode, instructions);
                        break;
                    default:
                        throw new NotSupportedException($"unable to clone this instruction as it is unknown");
                }
            }

            return targetInstruction;
        }

        protected Instruction _GetInstruction(int offset)
        {
            var ilWorkItem = _GetILWorkInstruction(offset);
            if (ilWorkItem != null)
                return ilWorkItem.Instruction;
            return null;
        }

        protected ILWorkItem _GetILWorkInstruction(int offset)
        {
            ILWorkItem label = null;
            var ils = _ILWorkItems.Where(t => WeaverHelper.AreILLocalisationEqual(t.ILLocalisation, ILLocalisations.Source) && t.Offsets.from <= offset && offset <= t.Offsets.to);
            if (ils.Any())
            {
                label = ils.OrderBy(t => t.Instruction.Offset).FirstOrDefault(t => t.Instruction.Offset >= offset);
                if (label == null)
                    label = ils.First();
            }

            return label;
        }

        protected void _OffsetRecalculation()
        {
            var offset = 0;
            foreach (var il in _ILWorkItems)
            {
                if (il.ILLocalisation == ILLocalisations.Target)
                    il.OldTargetOffset = il.Instruction.Offset;
                il.Instruction.Offset = offset;
                offset += il.Instruction.GetSize();
            }
        }

        protected void _WriteNewILInstructions(MethodBody targetMethodBody)
        {
            targetMethodBody.Instructions.Clear();
            foreach (var ilWorkItem in _ILWorkItems)
            {
                targetMethodBody.Instructions.Add(ilWorkItem.Instruction);
            }
        }

        protected IEnumerable<ConvertedIL> _BuildIlConversions(MethodBody targetMethodBody)
        {
            _ConvertedILs = new List<ConvertedIL>();
            if (!_WeaveItemMember.PrototypeItemMappings.Any())
                return _ConvertedILs;
            var iLBlockToConverts = new ILBlockToConverts();
            foreach (var prototypeMemberMapping in _WeaveItemMember.PrototypeItemMappings.Where(t => t.PrototypeItem is FieldDefinition))
            {
                var sourceILs = _SourceMethod.Body.Instructions.Where(t => t.Operand is FieldReference && ((FieldReference)t.Operand).Name == prototypeMemberMapping.PrototypeItemMember.Name && ((FieldReference)t.Operand).DeclaringType.Resolve() == prototypeMemberMapping.PrototypeItemMember.DeclaringType.Resolve());
                foreach (var sourceIL in sourceILs)
                    iLBlockToConverts.Add(prototypeMemberMapping, sourceIL, ILTree.Create(_SourceMethod).GetDataBlock(sourceIL));
            }

            foreach (var prototypeMemberMapping in _WeaveItemMember.PrototypeItemMappings.Where(t => t.PrototypeItem is MethodDefinition))
            {
                var sourceILs = _SourceMethod.Body.Instructions.Where(t => t.Operand is MethodReference && ((MethodReference)t.Operand).GetElementMethod().Resolve().FullName == prototypeMemberMapping.FullPrototypeItemName);
                foreach (var sourceIL in sourceILs)
                    iLBlockToConverts.Add(prototypeMemberMapping, sourceIL, ILTree.Create(_SourceMethod).GetDataBlock(sourceIL));
            }

            iLBlockToConverts.Complete();
            foreach (var ilBlockToConvert in iLBlockToConverts.Where(t => t.Parent == null))
            {
                var targetIls = _GetTargetILs(ilBlockToConvert, targetMethodBody);
                if (targetIls.Any())
                    _ConvertedILs.Add(new ConvertedIL(ilBlockToConvert.SourceIL, ilBlockToConvert.ILBlock, targetIls));
            }

            return _ConvertedILs;
        }

        internal IEnumerable<Instruction> _GetTargetILs(ILBlockToConvert iLBlockToConvert, MethodBody targetMethodBody)
        {
            var sourceILBlock = iLBlockToConvert.ILBlock;
            var instruction = iLBlockToConvert.SourceIL;
            switch (instruction.Operand)
            {
                case FieldReference sourceFieldReference:
                    var targetIls = _GetTargetILsFromPropertyItemField(iLBlockToConvert, sourceFieldReference, targetMethodBody);
                    return targetIls;
                case MethodReference sourceMethodReference:
                    targetIls = _GetTargetILsFromPropertyItemMethod(iLBlockToConvert, sourceMethodReference, targetMethodBody);
                    return targetIls;
                default:
                    throw new NotSupportedException();
            }
        }

        IEnumerable<Instruction> _GetTargetILsFromPropertyItemField(ILBlockToConvert ilBlockToConvert, FieldReference sourceFieldReference, MethodBody targetMethodBody)
        {
            var instruction = ilBlockToConvert.SourceIL;
            var opCodeData = OpCodeDatas.Get(instruction.OpCode);
            bool isLoading = (opCodeData.OpCodeType & OpCodeTypes.Ld) == OpCodeTypes.Ld;
            var targetDefinition = _WeaveItemMember.Resolve(sourceFieldReference);
            var sourceDefinition = sourceFieldReference.Resolve();
            if (targetDefinition is IError)
            {
                _WeaveItemMember.AddError((IError)targetDefinition);
                return ilBlockToConvert.ILBlock.Instructions;
            }

            switch (targetDefinition)
            {
                case ParameterDefinition parameter:
                    var newInstruction = CecilHelper.GetLoadOrStoreILArgInstruction(isLoading, _TargetMethod.Parameters[parameter.Index], _TargetMethod.IsStatic);
                    newInstruction.Offset = ilBlockToConvert.SourceIL.Offset;
                    var targetILs = _GetConvertedILs(instruction, ilBlockToConvert, newInstruction, false, true, targetMethodBody);
                    return targetILs;
                case VariableDefinition variableDefinition:
                    var newVariable = this._TargetMethod.Body.Variables.First(t => t == variableDefinition);
                    newInstruction = CecilHelper.GetVariableIL(OpCodeDatas.Get(instruction.OpCode), newVariable);
                    newInstruction.Offset = ilBlockToConvert.SourceIL.Offset;
                    targetILs = _GetConvertedILs(instruction, ilBlockToConvert, newInstruction, false, true, targetMethodBody);
                    return targetILs;
                case PropertyDefinition propertyDefinition:
                    targetILs = _GetTargetILsFromPropertyItemFieldToProperty(propertyDefinition, isLoading, opCodeData, ilBlockToConvert, sourceFieldReference, targetMethodBody);
                    return targetILs;
                    ;
                case FieldReference fieldReference:
                    var opCode = instruction.OpCode;
                    var fieldDefinition = WeaverHelper.ResolveDefinition(fieldReference, _WeaveItemMember.WeaveItem);
                    if (fieldDefinition.IsStatic)
                    {
                        if (opCode == OpCodes.Ldfld)
                            opCode = OpCodes.Ldsfld;
                        else if (opCode == OpCodes.Stfld)
                            opCode = OpCodes.Stsfld;
                        else if (opCode == OpCodes.Ldflda)
                            opCode = OpCodes.Ldsflda;
                    }

                    newInstruction = Instruction.Create(opCode, fieldReference);
                    newInstruction.Offset = ilBlockToConvert.SourceIL.Offset;
                    targetILs = _GetConvertedILs(instruction, ilBlockToConvert, newInstruction, sourceDefinition.IsStatic, fieldDefinition.IsStatic, targetMethodBody);
                    return targetILs;
                case EventDefinition eventDefinition:
                    _WeaveItemMember.AddError(WeaverHelper.GetError(_WeaveItemMember.WeaveItem, "PrototypeEventPropertyMistake", _WeaveItemMember.WeaveItem.Aspect.FullAspectDeclarationName, eventDefinition.FullName));
                    return ilBlockToConvert.ILBlock.Instructions;
                case string target:
                    if (target == "This")
                    {
                        if (targetMethodBody.Method.IsStatic)
                        {
                            _WeaveItemMember.AddError(WeaverHelper.GetError(_WeaveItemMember.WeaveItem, "ThisWithTargetStaticJoinpoint", _WeaveItemMember.WeaveItem.Aspect.FullAspectDeclarationName, targetMethodBody.Method.FullName));
                            return new Instruction[0];
                        }
                        else
                        {
                            newInstruction = Instruction.Create(OpCodes.Ldarg_0);
                            newInstruction.Offset = ilBlockToConvert.SourceIL.Offset;
                            targetILs = _GetConvertedILs(instruction, ilBlockToConvert, newInstruction, sourceDefinition.IsStatic, false, targetMethodBody);
                            return targetILs;
                        }
                    }
                    else
                        throw new NotImplementedException();
                default:
                    throw new NotImplementedException();
            }
        }

        IEnumerable<Instruction> _GetTargetILsFromPropertyItemFieldToProperty(PropertyDefinition propertyDefinition, bool isLoading, OpCodeData opCodeData, ILBlockToConvert ilBlockToConvert, FieldReference sourceFieldReference, MethodBody targetMethodBody)
        {
            var targetILs = new List<Instruction>();
            var sourceILBlock = ilBlockToConvert.ILBlock;
            var instruction = ilBlockToConvert.SourceIL;
            var sourceIl = sourceILBlock.FirstIL;
            while (sourceIl != null && sourceIl.OpCode == OpCodes.Nop)
            {
                var item = _GetChildTargetILs(sourceIl, ilBlockToConvert, targetMethodBody);
                targetILs.AddRange(item.targetILs);
                sourceIl = sourceILBlock.GetNextInstruction(item.lastInstruction);
            }

            if (((propertyDefinition.GetMethod != null && propertyDefinition.GetMethod.IsStatic) || (propertyDefinition.SetMethod != null && propertyDefinition.SetMethod.IsStatic)) && !sourceFieldReference.Resolve().IsStatic)
            {
                sourceIl = sourceIl.Next;
            }

            while (sourceIl != null && sourceIl != instruction)
            {
                var item = _GetChildTargetILs(sourceIl, ilBlockToConvert, targetMethodBody);
                targetILs.AddRange(item.targetILs);
                sourceIl = sourceILBlock.GetNextInstruction(item.lastInstruction);
            }

            var opCode = OpCodes.Call;
            var propertyMethod = isLoading ? propertyDefinition.GetMethod : propertyDefinition.SetMethod;
            var baseTargetTypes = WeaverHelper.GetBaseTypes(_WeaveItemMember.WeaveItem.Joinpoint.DeclaringType, _WeaveItemMember.WeaveItem.Weaver.SafeWeaveItemMembers);
            if (!WeaverHelper.IsMemberModifierCompatible(_WeaveItemMember.WeaveItem, baseTargetTypes, propertyMethod))
            {
                _WeaveItemMember.AddError(AspectDNErrorFactory.GetError("PrototypememberAccessModifierError", ((FieldDefinition)instruction.Operand).FullName, _WeaveItemMember.WeaveItem.Aspect.AspectDeclarationName, propertyMethod.FullName));
            }

            MethodReference propertyMethodReference = propertyMethod;
            if (propertyMethod.DeclaringType.HasGenericParameters)
            {
                var genericDeclaringType = new GenericInstanceType(propertyMethod.DeclaringType);
                foreach (var genericParameter in propertyDefinition.DeclaringType.GenericParameters.Cast<TypeReference>())
                    genericDeclaringType.GenericArguments.Add(genericParameter);
                propertyMethodReference = new MethodReference(propertyMethod.Name, propertyMethod.ReturnType, genericDeclaringType)
                {HasThis = true};
            }

            var newInstruction = Instruction.Create(opCode, propertyMethodReference);
            newInstruction.Offset = ilBlockToConvert.SourceIL.Offset;
            targetILs.Add(newInstruction);
            if (opCodeData.IsAddress)
            {
                var newVariable = new VariableDefinition(propertyMethod.ReturnType);
                _AddTargetBodyVariable(targetMethodBody, newVariable, null);
                var offset = targetILs.Any() ? targetILs.Last().Offset : 0;
                targetILs.Add(CecilHelper.GetVariableIL(OpCodeDatas.Get(OpCodes.Stloc), newVariable));
                targetILs.Last().Offset = offset;
                targetILs.Add(Instruction.Create(OpCodes.Ldloca_S, newVariable));
                targetILs.Last().Offset = offset;
            }

            return targetILs;
        }

        IEnumerable<Instruction> _GetTargetILsFromPropertyItemMethod(ILBlockToConvert ilBlockToConvert, MethodReference sourceMethodReference, MethodBody targetMethodBody)
        {
            var resolvedMethodReference = _WeaveItemMember.Resolve(sourceMethodReference);
            var sourceDefinition = CecilHelper.Resolve(sourceMethodReference);
            var instruction = ilBlockToConvert.SourceIL;
            var sourceILBlock = ilBlockToConvert.ILBlock;
            var prototypeItemMapping = ilBlockToConvert.PrototypeItemMapping;
            if (resolvedMethodReference is IError)
            {
                _WeaveItemMember.AddError((IError)resolvedMethodReference);
                return ilBlockToConvert.ILBlock.Instructions;
            }

            switch (resolvedMethodReference)
            {
                case MethodReference targetMethodReference:
                    var targetMethodDefinition = targetMethodReference.Resolve();
                    if (targetMethodDefinition == null && prototypeItemMapping.Target is MethodDefinition)
                        targetMethodDefinition = (MethodDefinition)prototypeItemMapping.Target;
                    var opCode = _ResolveCallOpCode(instruction, targetMethodDefinition);
                    if (targetMethodDefinition.IsStatic && !sourceMethodReference.Resolve().IsStatic)
                        targetMethodReference.HasThis = false;
                    var newInstruction = Instruction.Create(instruction.OpCode, targetMethodReference);
                    newInstruction.Offset = ilBlockToConvert.SourceIL.Offset;
                    var targetILs = _GetConvertedILs(instruction, ilBlockToConvert, newInstruction, sourceDefinition.IsStatic, targetMethodDefinition.IsStatic, targetMethodBody);
                    return targetILs;
                case FieldDefinition targetFieldDefinition:
                    var targetILList = new List<Instruction>();
                    var sourceIl = sourceILBlock.FirstIL;
                    while (sourceIl != null && sourceIl.OpCode == OpCodes.Nop)
                    {
                        var item = _GetChildTargetILs(sourceIl, ilBlockToConvert, targetMethodBody);
                        targetILList.AddRange(item.targetILs);
                        sourceIl = sourceILBlock.GetNextInstruction(item.lastInstruction);
                    }

                    var offset = targetILList.Any() ? targetILList.Last().Offset : 0;
                    if (targetFieldDefinition.IsStatic && !sourceMethodReference.Resolve().IsStatic)
                    {
                        sourceIl = sourceIl.Next;
                        targetILList.Add(Instruction.Create(OpCodes.Ldsfld, targetFieldDefinition));
                    }
                    else
                    {
                        var item = _GetChildTargetILs(sourceIl, ilBlockToConvert, targetMethodBody);
                        targetILList.AddRange(item.targetILs);
                        sourceIl = sourceILBlock.GetNextInstruction(item.lastInstruction);
                        targetILList.Add(Instruction.Create(OpCodes.Ldfld, targetFieldDefinition));
                    }

                    targetILList.Last().Offset = offset;
                    while (sourceIl != null && sourceIl != instruction)
                    {
                        var item = _GetChildTargetILs(sourceIl, ilBlockToConvert, targetMethodBody);
                        targetILList.AddRange(item.targetILs);
                        sourceIl = sourceILBlock.GetNextInstruction(item.lastInstruction);
                    }

                    var callMethod = targetFieldDefinition.FieldType.Resolve().Methods.First(t => t.Name == "Invoke");
                    newInstruction = Instruction.Create(OpCodes.Callvirt, callMethod);
                    newInstruction.Offset = ilBlockToConvert.SourceIL.Offset;
                    targetILList.Add(newInstruction);
                    return targetILList;
                default:
                    throw new NotImplementedException();
            }
        }

        IEnumerable<Instruction> _GetConvertedILs(Instruction sourceInstruction, ILBlockToConvert ilBlockToConvert, Instruction newInstruction, bool isSourceStatic, bool isTargetStatic, MethodBody targetMethodBody)
        {
            List<Instruction> targetILs = new List<Instruction>();
            var sourceILBlock = ilBlockToConvert.ILBlock;
            var il = sourceILBlock.FirstIL;
            while (il != null && il.OpCode == OpCodes.Nop)
            {
                var item = _GetChildTargetILs(il, ilBlockToConvert, targetMethodBody);
                targetILs.AddRange(item.targetILs);
                il = sourceILBlock.GetNextInstruction(item.lastInstruction);
            }

            if (isTargetStatic && !isSourceStatic)
                il = il.Next;
            while (il != null && il != sourceInstruction)
            {
                var item = _GetChildTargetILs(il, ilBlockToConvert, targetMethodBody);
                targetILs.AddRange(item.targetILs);
                il = sourceILBlock.GetNextInstruction(item.lastInstruction);
            }

            targetILs.Add(newInstruction);
            return targetILs;
        }

        (IEnumerable<Instruction> targetILs, Instruction lastInstruction) _GetChildTargetILs(Instruction sourceInstruction, ILBlockToConvert ilBlockToConvert, MethodBody targetMethodBody)
        {
            (IEnumerable<Instruction> targetILs, Instruction lastInstruction) item = (null, null);
            var child = ilBlockToConvert.Children.FirstOrDefault(t => t.ILBlock.FirstIL.Offset <= sourceInstruction.Offset && sourceInstruction.Offset <= t.ILBlock.LastIL.Offset);
            if (child != null)
                item = (_GetTargetILs(child, targetMethodBody), child.ILBlock.LastIL);
            else
            {
                var targetInstuction = _CloneInstruction(sourceInstruction);
                targetInstuction.Offset = sourceInstruction.Offset;
                item = ((IEnumerable<Instruction>)new Instruction[]{targetInstuction}, sourceInstruction);
            }

            return item;
        }

        internal ConvertedIL _CreateConvertedILs(Instruction instruction, MethodBody targetMethodBody, PrototypeItemMapping prototypeItemMapping)
        {
            var sourceILBlock = ILTree.Create(_SourceMethod).GetDataBlock(instruction);
            List<Instruction> targetILs = new List<Instruction>();
            switch (instruction.Operand)
            {
                case FieldReference sourceFieldReference:
                    var opCodeData = OpCodeDatas.Get(instruction.OpCode);
                    bool isLoading = (opCodeData.OpCodeType & OpCodeTypes.Ld) == OpCodeTypes.Ld;
                    var targetReference = _WeaveItemMember.Resolve(sourceFieldReference);
                    if (targetReference is IError)
                    {
                        _WeaveItemMember.AddError((IError)targetReference);
                        targetReference = sourceFieldReference;
                    }

                    switch (targetReference)
                    {
                        case ParameterDefinition parameter:
                            var newInstruction = CecilHelper.GetLoadOrStoreILArgInstruction(isLoading, _TargetMethod.Parameters[parameter.Index], _TargetMethod.IsStatic);
                            targetILs = _GetConvertedILs(instruction, sourceILBlock, newInstruction, true);
                            break;
                        case VariableDefinition variableDefinition:
                            var newVariable = _VariableTranslations[variableDefinition];
                            newInstruction = CecilHelper.GetVariableIL(OpCodeDatas.Get(instruction.OpCode), newVariable);
                            targetILs = _GetConvertedILs(instruction, sourceILBlock, newInstruction, true);
                            break;
                        case PropertyDefinition propertyDefinition:
                            var sourceIl = sourceILBlock.FirstIL;
                            while (sourceIl != null && sourceIl.OpCode == OpCodes.Nop)
                            {
                                targetILs.Add(sourceIl);
                                sourceIl = sourceILBlock.GetNextInstruction(sourceIl);
                            }

                            if ((propertyDefinition.GetMethod != null && propertyDefinition.GetMethod.IsStatic) || (propertyDefinition.SetMethod != null && propertyDefinition.SetMethod.IsStatic))
                            {
                                sourceIl = sourceIl.Next;
                            }

                            while (sourceIl != null && sourceIl != instruction)
                            {
                                var convertedIl = _ConvertedILs.FirstOrDefault(t => t.SourceILBlock.FirstIL.Offset <= sourceIl.Offset && t.SourceILBlock.LastIL.Offset >= sourceIl.Offset);
                                if (convertedIl != null)
                                {
                                    targetILs.AddRange(convertedIl.TargetILs);
                                    _ConvertedILs.Remove(convertedIl);
                                    sourceIl = targetILs.Last().Next;
                                }
                                else
                                {
                                    var targetIL = _CloneInstruction(sourceIl);
                                    targetILs.Add(targetIL);
                                    sourceIl = sourceILBlock.GetNextInstruction(sourceIl);
                                }
                            }

                            var opCode = OpCodes.Call;
                            var propertyMethod = isLoading ? propertyDefinition.GetMethod : propertyDefinition.SetMethod;
                            var baseTargetTypes = WeaverHelper.GetBaseTypes(_WeaveItemMember.WeaveItem.Joinpoint.DeclaringType, _WeaveItemMember.WeaveItem.Weaver.SafeWeaveItemMembers);
                            if (!WeaverHelper.IsMemberModifierCompatible(_WeaveItemMember.WeaveItem, baseTargetTypes, propertyMethod))
                            {
                                _WeaveItemMember.AddError(AspectDNErrorFactory.GetError("PrototypememberAccessModifierError", ((FieldDefinition)instruction.Operand).FullName, _WeaveItemMember.WeaveItem.Aspect.AspectDeclarationName, propertyMethod.FullName));
                            }

                            targetILs.Add(Instruction.Create(opCode, propertyMethod));
                            if (opCodeData.IsAddress)
                            {
                                newVariable = new VariableDefinition(propertyMethod.ReturnType);
                                _AddTargetBodyVariable(targetMethodBody, newVariable, null);
                                targetILs.Add(CecilHelper.GetVariableIL(OpCodeDatas.Get(OpCodes.Stloc), newVariable));
                                targetILs.Add(Instruction.Create(OpCodes.Ldloca_S, newVariable));
                            }

                            break;
                        case FieldReference fieldReference:
                            newInstruction = Instruction.Create(isLoading ? OpCodes.Ldfld : OpCodes.Stfld, fieldReference);
                            targetILs = _GetConvertedILs(instruction, sourceILBlock, newInstruction, fieldReference.Resolve().IsStatic);
                            break;
                        case EventDefinition eventDefinition:
                            _WeaveItemMember.AddError(WeaverHelper.GetError(_WeaveItemMember.WeaveItem, "PrototypeEventPropertyMistake", _WeaveItemMember.WeaveItem.Aspect.FullAspectDeclarationName, eventDefinition.FullName));
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    return new ConvertedIL(instruction, sourceILBlock, targetILs);
                case MethodReference sourceMethodReference:
                    var resolvedMethodReference = _WeaveItemMember.Resolve(sourceMethodReference);
                    if (resolvedMethodReference is IError)
                    {
                        _WeaveItemMember.AddError((IError)resolvedMethodReference);
                        targetReference = sourceMethodReference;
                    }

                    switch (resolvedMethodReference)
                    {
                        case MethodReference targetMethodReference:
                            var targetMethodDefinition = targetMethodReference.Resolve();
                            if (targetMethodDefinition == null && prototypeItemMapping.Target is MethodDefinition)
                                targetMethodDefinition = (MethodDefinition)prototypeItemMapping.Target;
                            var opCode = _ResolveCallOpCode(instruction, targetMethodDefinition);
                            if (targetMethodDefinition.IsStatic)
                                targetMethodReference.HasThis = false;
                            var newInstruction = Instruction.Create(instruction.OpCode, targetMethodReference);
                            targetILs = _GetConvertedILs(instruction, sourceILBlock, newInstruction, targetMethodDefinition.IsStatic);
                            return new ConvertedIL(instruction, sourceILBlock, targetILs);
                        case FieldDefinition targetFieldDefinition:
                            var sourceIl = sourceILBlock.FirstIL;
                            while (sourceIl != null && sourceIl.OpCode == OpCodes.Nop)
                            {
                                targetILs.Add(sourceIl);
                                sourceIl = sourceILBlock.GetNextInstruction(sourceIl);
                            }

                            if (targetFieldDefinition.IsStatic)
                            {
                                sourceIl = sourceIl.Next;
                            }
                            else
                            {
                                targetILs.Add(sourceIl);
                                sourceIl = sourceILBlock.GetNextInstruction(sourceIl);
                            }

                            targetILs.Add(Instruction.Create(OpCodes.Ldfld, targetFieldDefinition));
                            while (sourceIl != null && sourceIl != instruction)
                            {
                                var convertedIl = _ConvertedILs.FirstOrDefault(t => t.SourceILBlock.FirstIL.Offset <= sourceIl.Offset && t.SourceILBlock.LastIL.Offset >= sourceIl.Offset);
                                if (convertedIl != null)
                                {
                                    targetILs.AddRange(convertedIl.TargetILs);
                                    _ConvertedILs.Remove(convertedIl);
                                    sourceIl = targetILs.Last().Next;
                                }
                                else
                                {
                                    var targetIL = _CloneInstruction(sourceIl);
                                    targetILs.Add(targetIL);
                                    sourceIl = sourceILBlock.GetNextInstruction(sourceIl);
                                }
                            }

                            var callMethod = targetFieldDefinition.FieldType.Resolve().Methods.First(t => t.Name == "Invoke");
                            targetILs.Add(Instruction.Create(OpCodes.Callvirt, callMethod));
                            return new ConvertedIL(instruction, sourceILBlock, targetILs);
                        default:
                            throw new NotImplementedException();
                    }

                default:
                    throw new NotSupportedException();
            }
        }

        OpCode _ResolveCallOpCode(Instruction instruction, MethodDefinition targetMethodDefinition)
        {
            var opCode = instruction.OpCode;
            if (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt)
            {
                if (!targetMethodDefinition.IsNewSlot && !targetMethodDefinition.IsVirtual && !targetMethodDefinition.IsReuseSlot)
                    opCode = OpCodes.Call;
                else
                {
                    var prototypeItemMapping = _WeaveItemMember.PrototypeItemMappings.FirstOrDefault(t => t.Target is MethodDefinition && ((MethodDefinition)t.Target).FullName == targetMethodDefinition.FullName);
                    if (prototypeItemMapping != null && prototypeItemMapping.TargetKind == IConcerns.PrototypeItemMappingTargetKinds.BaseMember)
                        opCode = OpCodes.Call;
                    else
                        opCode = OpCodes.Callvirt;
                }

                if (targetMethodDefinition.IsStatic)
                    opCode = OpCodes.Call;
            }

            return opCode;
        }

        List<Instruction> _GetConvertedILs(Instruction sourceInstruction, ILBlockNode sourceILBlock, Instruction newInstruction, bool isTargetStatic)
        {
            List<Instruction> targetILs = new List<Instruction>();
            var il = sourceILBlock.FirstIL;
            while (il != null && il.OpCode == OpCodes.Nop)
            {
                targetILs.Add(il);
                il = sourceILBlock.GetNextInstruction(il);
            }

            if (isTargetStatic)
                il = il.Next;
            while (il != null && il != sourceInstruction)
            {
                var convertedIl = _ConvertedILs.FirstOrDefault(t => t.SourceILBlock.FirstIL.Offset <= il.Offset && t.SourceILBlock.LastIL.Offset >= il.Offset);
                if (convertedIl != null)
                {
                    targetILs.AddRange(convertedIl.TargetILs);
                    _ConvertedILs.Remove(convertedIl);
                    il = convertedIl.SourceILBlock.LastIL.Next;
                }
                else
                {
                    var targetIL = _CloneInstruction(il);
                    targetILs.Add(targetIL);
                    il = sourceILBlock.GetNextInstruction(il);
                }
            }

            targetILs.Add(newInstruction);
            return targetILs;
        }

        protected void _ChangeBranchAccordingOffsetInterval()
        {
            foreach (var gotoInstruction in _ILWorkItems.Where(t => t.Instruction.Operand is Instruction || t.Instruction.Operand is Instruction[]))
                _ChangeOpCodeBranchAccordingOffsetInterval(gotoInstruction);
        }

        void _ChangeOpCodeBranchAccordingOffsetInterval(ILWorkItem iLWorkItem)
        {
            var newOpCode = iLWorkItem.Instruction.OpCode;
            var size = ((Instruction)iLWorkItem.Instruction.Operand).Offset - iLWorkItem.Instruction.Offset;
            if (Math.Abs(size) > 127)
            {
                switch (OpCodeDatas.Get(newOpCode).OpCodeValue)
                {
                    case OpCodeValues.Beq_S:
                        newOpCode = OpCodes.Beq;
                        break;
                    case OpCodeValues.Bge_S:
                        newOpCode = OpCodes.Bge;
                        break;
                    case OpCodeValues.Bge_Un_S:
                        newOpCode = OpCodes.Bge_Un;
                        break;
                    case OpCodeValues.Bgt_S:
                        newOpCode = OpCodes.Bgt;
                        break;
                    case OpCodeValues.Bgt_Un_S:
                        newOpCode = OpCodes.Bgt_Un;
                        break;
                    case OpCodeValues.Ble_S:
                        newOpCode = OpCodes.Ble;
                        break;
                    case OpCodeValues.Ble_Un_S:
                        newOpCode = OpCodes.Ble_Un;
                        break;
                    case OpCodeValues.Blt_S:
                        newOpCode = OpCodes.Blt;
                        break;
                    case OpCodeValues.Blt_Un_S:
                        newOpCode = OpCodes.Blt_Un;
                        break;
                    case OpCodeValues.Bne_Un_S:
                        newOpCode = OpCodes.Bne_Un;
                        break;
                    case OpCodeValues.Brfalse_S:
                        newOpCode = OpCodes.Brfalse;
                        break;
                    case OpCodeValues.Brtrue_S:
                        newOpCode = OpCodes.Brtrue;
                        break;
                    case OpCodeValues.Br_S:
                        newOpCode = OpCodes.Br;
                        break;
                    case OpCodeValues.Leave_S:
                        newOpCode = OpCodes.Leave;
                        break;
                }
            }
            else
            {
                switch (OpCodeDatas.Get(newOpCode).OpCodeValue)
                {
                    case OpCodeValues.Beq:
                        newOpCode = OpCodes.Beq_S;
                        break;
                    case OpCodeValues.Bge:
                        newOpCode = OpCodes.Bge_S;
                        break;
                    case OpCodeValues.Bge_Un:
                        newOpCode = OpCodes.Bge_Un_S;
                        break;
                    case OpCodeValues.Bgt:
                        newOpCode = OpCodes.Bgt_S;
                        break;
                    case OpCodeValues.Bgt_Un:
                        newOpCode = OpCodes.Bgt_Un_S;
                        break;
                    case OpCodeValues.Ble:
                        newOpCode = OpCodes.Ble_S;
                        break;
                    case OpCodeValues.Ble_Un:
                        newOpCode = OpCodes.Ble_Un_S;
                        break;
                    case OpCodeValues.Blt:
                        newOpCode = OpCodes.Blt_S;
                        break;
                    case OpCodeValues.Blt_Un:
                        newOpCode = OpCodes.Blt_Un_S;
                        break;
                    case OpCodeValues.Bne_Un:
                        newOpCode = OpCodes.Bne_Un_S;
                        break;
                    case OpCodeValues.Brfalse:
                        newOpCode = OpCodes.Brfalse_S;
                        break;
                    case OpCodeValues.Brtrue:
                        newOpCode = OpCodes.Brtrue_S;
                        break;
                    case OpCodeValues.Br:
                        newOpCode = OpCodes.Br_S;
                        break;
                    case OpCodeValues.Leave:
                        newOpCode = OpCodes.Leave_S;
                        break;
                }
            }

            if (newOpCode != iLWorkItem.Instruction.OpCode)
                iLWorkItem.Instruction.OpCode = newOpCode;
        }

        internal string GetILWorkItemsString(IEnumerable<ILWorkItem> ilWorkItems)
        {
            var sb = new StringBuilder();
            int i = 0;
            foreach (var t in _ILWorkItems)
            {
                sb.Append(i++).Append("\t\t").Append(t.Instruction).Append("\t\t").Append(t.Offsets.from).Append("-").Append(t.Offsets.to);
                if (t.Instruction.Operand is Instruction)
                {
                    sb.Append("\t\t").Append(t.Instruction.Operand);
                    var @goto = _ILWorkItems.FirstOrDefault(x => x.Instruction.Operand == t.Instruction);
                    int index = _ILWorkItems.IndexOf(@goto);
                    sb.Append("\t\t").Append(index);
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        protected bool _IsReturnInstruction(Instruction instruction)
        {
            if (instruction.OpCode == OpCodes.Ret)
                return true;
            if (instruction.Operand is MethodReference)
            {
                var declaringType = ((MethodReference)instruction.Operand).DeclaringType;
                if (CecilHelper.IsTypeException(declaringType))
                    return true;
            }

            return false;
        }

        protected ILWorkItem _GetFirstEndTargetIlWorkItem()
        {
            var lasttEndSourceIlWorkItem = _ILWorkItems.OrderByDescending(t => _ILWorkItems.IndexOf(t)).First(t => (t.ILLocalisation & ILLocalisations.Source) == ILLocalisations.Source);
            var firstEndTargetIlWorkItem = _ILWorkItems[_ILWorkItems.IndexOf(lasttEndSourceIlWorkItem) + 1];
            return firstEndTargetIlWorkItem;
        }
    }
}