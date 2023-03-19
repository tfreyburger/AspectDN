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
using AspectDN.Aspect.Weaving.IJoinpoints;
using AspectDN.Aspect.Weaving.IConcerns;
using System.Runtime.InteropServices;

namespace AspectDN.Aspect.Weaving
{
    internal class CodeMethodExtender
    {
        internal static CodeMethodExtender Create(IEnumerable<NewCodeMember> weaveItemMembers)
        {
            return new CodeMethodExtender(weaveItemMembers, weaveItemMembers.First().TargetMethod);
        }

        IEnumerable<NewCodeMember> _NewCodeMembers;
        MethodDefinition _TargetMethod;
        MethodBody _NewTargetMethodBody;
        List<(Instruction oldInstruction, Instruction newInstruction)> _TargetInstructions;
        internal CodeMethodExtender(IEnumerable<NewCodeMember> newCodeMembers, MethodDefinition targetMethod)
        {
            _NewCodeMembers = newCodeMembers;
            _TargetMethod = targetMethod;
            _NewTargetMethodBody = targetMethod.Body;
            _TargetInstructions = newCodeMembers.Where(t => t.WeaveItem.Joinpoint is IInstructionJoinpoint).Select(t => (((IInstructionJoinpoint)t.WeaveItem.Joinpoint).Instruction, ((IInstructionJoinpoint)t.WeaveItem.Joinpoint).Instruction)).ToList();
        }

        internal void Merge()
        {
            _MergeCodes();
            foreach (var newCodeMember in _NewCodeMembers)
                newCodeMember.NewMethodBody = _NewTargetMethodBody;
        }

        void _MergeCodes()
        {
            foreach (var newCode in _NewCodeMembers.OfType<NewCode>().Where(t => t.Joinpoint is IInstructionJoinpoint))
                _MergeNewCodeMember(newCode);
            foreach (var newChangeValue in _NewCodeMembers.OfType<NewChangeValue>())
                _MergeNewCodeMember(newChangeValue);
            foreach (var newCode in _NewCodeMembers.OfType<NewCode>().Where(t => t.Joinpoint is IMemberJoinpoint))
                _MergeNewCodeMember(newCode);
            foreach (var newCode in _NewCodeMembers.OfType<NewFieldInitCode>())
                _MergeNewCodeMember(newCode);
        }

        void _MergeNewCodeMember(NewCodeMember newCodeMember)
        {
            IILWorker methodBodyMerger = null;
            if (newCodeMember is NewCode)
            {
                var newCode = (NewCode)newCodeMember;
                switch (newCode.ExecutionTime)
                {
                    case ExecutionTimes.before:
                        switch (newCode.Joinpoint)
                        {
                            case IInstructionJoinpoint instructionJoinpoint:
                                methodBodyMerger = _MergeNewCodeBeforeInstruction(newCode);
                                break;
                            case IMemberJoinpoint methodJoinpoint:
                                methodBodyMerger = _MergeNewCodeBeforeBody(newCode);
                                break;
                            default:
                                throw new NotImplementedException();
                        }

                        break;
                    case ExecutionTimes.after:
                        switch (newCode.Joinpoint)
                        {
                            case IInstructionJoinpoint instructionJoinpoint:
                                methodBodyMerger = _MergeNewCodeAfterInstruction(newCode);
                                break;
                            case IMemberJoinpoint methodJoinpoint:
                                methodBodyMerger = _MergeNewCodeAfterBody(newCode);
                                break;
                            default:
                                throw new NotImplementedException();
                        }

                        break;
                    case ExecutionTimes.around:
                        switch (newCode.Joinpoint)
                        {
                            case IInstructionJoinpoint instructionJoinpoint:
                                methodBodyMerger = _MergeNewCodeAroundInstruction(newCode);
                                break;
                            case IMemberJoinpoint methodJoinpoint:
                                methodBodyMerger = _MergeNewCodeAroundBody(newCode);
                                break;
                            default:
                                throw new NotImplementedException();
                        }

                        break;
                    default:
                        break;
                }
            }
            else
            {
                if (newCodeMember is NewChangeValue)
                    methodBodyMerger = _MergeChangeValueCode((NewChangeValue)newCodeMember);
                else
                {
                    methodBodyMerger = _MergeFieldInitILCode((NewFieldInitCode)newCodeMember);
                }
            }

            if (methodBodyMerger != null)
                _NewTargetMethodBody = methodBodyMerger.Merge();
        }

        MethodBodyMerger _MergeNewCodeBeforeInstruction(NewCode newCode)
        {
            var methodBodyMerger = MethodBodyMerger.Create(ILMergekinds.Instruction, newCode.SourceMethod, newCode, _NewTargetMethodBody);
            if (newCode.OnError)
                return null;
            _TargetInstructions = _TargetInstructions.Select(t => (t.oldInstruction, methodBodyMerger.NewMethodBody.Instructions.First(n => n.Offset == t.newInstruction.Offset))).ToList();
            var targetInstruction = _TargetInstructions.First(t => t.oldInstruction == ((IInstructionJoinpoint)newCode.Joinpoint).Instruction).newInstruction;
            var targetInstructionILBlck = ILTree.Create(methodBodyMerger.NewMethodBody).GetFullDataBlock(targetInstruction);
            var bodyILInterval = _GetBodyILInterval(newCode.SourceMethod);
            methodBodyMerger.InsertBefore(bodyILInterval.from, bodyILInterval.to, targetInstructionILBlck.FirstIL);
            return methodBodyMerger;
        }

        MethodBodyMerger _MergeNewCodeAfterInstruction(NewCode newCode)
        {
            var methodBodyMerger = MethodBodyMerger.Create(ILMergekinds.Instruction, newCode.SourceMethod, newCode, _NewTargetMethodBody);
            if (newCode.OnError)
                return null;
            _TargetInstructions = _TargetInstructions.Select(t => (t.oldInstruction, methodBodyMerger.NewMethodBody.Instructions.First(n => n.Offset == t.newInstruction.Offset))).ToList();
            var targetInstruction = _TargetInstructions.First(t => t.oldInstruction == ((IInstructionJoinpoint)newCode.Joinpoint).Instruction).newInstruction;
            var targetPivotILBlock = ILTree.Create(methodBodyMerger.NewMethodBody).GetFullDataBlock(targetInstruction);
            var afterIL = targetPivotILBlock.LastIL;
            var codeBodyInstructions = _GetBodyILInterval(newCode.SourceMethod);
            methodBodyMerger.InsertAfter(codeBodyInstructions.from, codeBodyInstructions.to, targetPivotILBlock.LastIL);
            return methodBodyMerger;
        }

        MethodBodyMerger _MergeNewCodeAroundInstruction(NewCode newCode)
        {
            var methodBodyMerger = MethodBodyMerger.Create(ILMergekinds.Instruction, newCode.SourceMethod, newCode, _NewTargetMethodBody);
            if (newCode.OnError)
                return null;
            _TargetInstructions = _TargetInstructions.Select(t => (t.oldInstruction, methodBodyMerger.NewMethodBody.Instructions.First(n => n.Offset == t.newInstruction.Offset))).ToList();
            var targetInstruction = _TargetInstructions.First(t => t.oldInstruction == ((IInstructionJoinpoint)newCode.Joinpoint).Instruction).newInstruction;
            var targetPivotILBlock = ILTree.Create(methodBodyMerger.NewMethodBody).GetFullDataBlock(targetInstruction);
            var aroundILs = CecilHelper.GetAroundILs(newCode.SourceMethod);
            if (aroundILs.Count() != 1)
            {
                newCode.AddError(WeaverHelper.GetError(newCode, "NoAroundAnchorFound"));
                return null;
            }

            var aroundILBlock = ILTree.Create(newCode.SourceMethod).GetFullDataBlock(aroundILs.First());
            var codeBodyInstructions = _GetBodyILInterval(newCode.SourceMethod);
            methodBodyMerger.InsertBefore(codeBodyInstructions.from, aroundILBlock.FirstIL.Previous, targetPivotILBlock.FirstIL);
            methodBodyMerger.InsertAfter(aroundILBlock.LastIL.Next, codeBodyInstructions.to, targetPivotILBlock.LastIL);
            return methodBodyMerger;
        }

        MethodBodyMerger _MergeNewCodeBeforeBody(NewCode newCode)
        {
            var bodyBuilder = MethodBodyMerger.Create(ILMergekinds.Body, newCode.SourceMethod, newCode, _NewTargetMethodBody);
            if (newCode.OnError)
                return null;
            var sourceBodyInstructions = _GetBodyILInterval(newCode.SourceMethod);
            bodyBuilder.InsertBefore(sourceBodyInstructions.from, sourceBodyInstructions.to);
            return bodyBuilder;
            ;
        }

        MethodBodyMerger _MergeNewCodeAfterBody(NewCode newCode)
        {
            var bodyBuilder = MethodBodyMerger.Create(ILMergekinds.Body, newCode.SourceMethod, newCode, _NewTargetMethodBody);
            if (newCode.OnError)
                return null;
            var codeBodyInstructions = _GetBodyILInterval(newCode.SourceMethod);
            bodyBuilder.InsertEndAfter(codeBodyInstructions.from, codeBodyInstructions.to);
            return bodyBuilder;
        }

        MethodBodyMerger _MergeNewCodeAroundBody(NewCode newCode)
        {
            var bodyBuilder = MethodBodyMerger.Create(ILMergekinds.Body, newCode.SourceMethod, newCode, _NewTargetMethodBody);
            if (newCode.OnError)
                return null;
            var codeBodyInstructions = _GetBodyILInterval(newCode.SourceMethod);
            var aroundILs = CecilHelper.GetAroundILs(newCode.SourceMethod);
            if (aroundILs.Count() != 1)
            {
                newCode.AddError(WeaverHelper.GetError(newCode, "NoAroundAnchorFound"));
                return null;
            }

            var aroundILBlock = ILTree.Create(newCode.SourceMethod).GetFullDataBlock(aroundILs.First());
            bodyBuilder.InsertBefore(codeBodyInstructions.from, aroundILBlock.FirstIL.Previous);
            bodyBuilder.InsertEndAfter(aroundILBlock.LastIL.Next, codeBodyInstructions.to);
            return bodyBuilder;
        }

        (Instruction from, Instruction to) _GetBodyILInterval(MethodDefinition method)
        {
            var to = method.Body.Instructions.Last().OpCode == OpCodes.Throw ? method.Body.Instructions.Last() : null;
            if (to == null)
            {
                to = CecilHelper.GetReturnLabel(method.Body).Previous;
                if (to.OpCode == OpCodes.Throw && to.Previous.Operand is MethodReference && ((MethodReference)to.Previous.Operand).DeclaringType.FullName == typeof(EndCodeException).FullName)
                {
                    to = to.Previous.Previous;
                }
            }
            else
            {
                var dummerReturn = method.Body.Instructions.FirstOrDefault(t => t.Operand is MethodReference && ((MethodReference)t.Operand).DeclaringType.FullName == typeof(EndCodeException).FullName);
                if (dummerReturn != null)
                    to = ILTree.Create(method).GetFullDataBlock(dummerReturn).FirstIL.Previous;
            }

            if (to != null && to.OpCode == OpCodes.Nop && to.Previous != null)
                to = to.Previous;
            if (to == null)
                to = method.Body.Instructions.First();
            return (method.Body.Instructions.First(), to);
        }

        MethodBodyMerger _MergeChangeValueCode(NewChangeValue newChangeValue)
        {
            var methodBodyMerger = MethodBodyMerger.Create(ILMergekinds.None, newChangeValue.SourceMethod, newChangeValue, _NewTargetMethodBody);
            _TargetInstructions = _TargetInstructions.Select(t => (t.oldInstruction, methodBodyMerger.NewMethodBody.Instructions.First(n => n.Offset == t.newInstruction.Offset))).ToList();
            var targetInstruction = _TargetInstructions.First(t => t.oldInstruction == ((IInstructionJoinpoint)newChangeValue.Joinpoint).Instruction).newInstruction;
            var joinpointILBlock = ILTree.Create(methodBodyMerger.NewMethodBody).GetDataBlock(targetInstruction);
            var from = newChangeValue.SourceMethod.Body.Instructions.Where(t => t.OpCode == OpCodes.Stloc_0).First();
            var to = newChangeValue.SourceMethod.Body.Instructions.Where(t => t.OpCode == OpCodes.Ldloc_0).Last();
            methodBodyMerger.InsertAfter(from, to, joinpointILBlock.LastIL);
            return methodBodyMerger;
        }

        IILWorker _MergeFieldInitILCode(NewFieldInitCode newFieldInitCode)
        {
            var bodyBuilder = new ILBodyMerger(newFieldInitCode, _NewTargetMethodBody);
            bodyBuilder.InsertBeforeBody(newFieldInitCode.FieldInitILCode.Instructions);
            return bodyBuilder;
        }
    }
}