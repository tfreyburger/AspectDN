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

namespace AspectDN.Aspect.Weaving
{
    internal class MethodBodyCloner : BaseMethodBodyBuilder
    {
        internal static MethodBodyCloner Create(MethodDefinition fromMethod, WeaveItemMember weaveItemMember)
        {
            return new MethodBodyCloner(fromMethod, weaveItemMember);
        }

        internal MethodBodyCloner(MethodDefinition fromMethod, WeaveItemMember weaveItemMember) : base(fromMethod, weaveItemMember)
        {
        }

        internal MethodBody CloneBody()
        {
            var clonedMethodBody = new MethodBody(_TargetMethod);
            clonedMethodBody.InitLocals = _SourceMethod.Body.InitLocals;
            clonedMethodBody.LocalVarToken = _SourceMethod.Body.LocalVarToken;
            clonedMethodBody.MaxStackSize = _SourceMethod.Body.MaxStackSize;
            foreach (VariableDefinition variable in _SourceMethod.Body.Variables)
                _CloneVariable(variable, clonedMethodBody);
            _BuildIlConversions(clonedMethodBody);
            if (_WeaveItemMember.OnError)
                return null;
            int atIndex = _ILWorkItems.Count;
            var il = _SourceMethod.Body.Instructions.FirstOrDefault();
            while (il != null)
            {
                var ilChange = _ConvertedILs.FirstOrDefault(t => t.SourceILBlock.FirstIL.Offset <= il.Offset && t.SourceILBlock.LastIL.Offset >= il.Offset);
                atIndex = _CloneIL(atIndex, il, ilChange, ILLocalisations.Source);
                if (ilChange != null)
                    il = ilChange.SourceILBlock.LastIL.Next;
                else
                    il = il.Next;
            }

            _OffsetRecalculation();
            foreach (var ilworkItem in _ILWorkItems.Where(t => t.Instruction.Operand is Instruction || t.Instruction.Operand is Instruction))
            {
                switch (ilworkItem.Instruction.Operand)
                {
                    case Instruction[] instructions:
                        for (int i = 0; i < instructions.Length; i++)
                            ((Instruction[])ilworkItem.Instruction.Operand)[i] = _GetInstruction(instructions[i].Offset);
                        break;
                    case Instruction instruction:
                        ilworkItem.Instruction.Operand = _GetInstruction(instruction.Offset);
                        break;
                }
            }

            foreach (var handler in _SourceMethod.Body.ExceptionHandlers)
            {
                ExceptionHandler newHandler = new ExceptionHandler(handler.HandlerType);
                if (handler.CatchType != null)
                    newHandler.CatchType = _WeaveItemMember.Resolve(handler.CatchType);
                if (handler.HandlerStart != null)
                    newHandler.HandlerStart = _GetInstruction(handler.HandlerStart.Offset);
                if (handler.HandlerEnd != null)
                    newHandler.HandlerEnd = _GetInstruction(handler.HandlerEnd.Offset);
                if (handler.TryStart != null)
                    newHandler.TryStart = _GetInstruction(handler.TryStart.Offset);
                if (handler.TryEnd != null)
                    newHandler.TryEnd = _GetInstruction(handler.TryEnd.Offset);
                if (handler.FilterStart != null)
                    newHandler.FilterStart = _GetInstruction(handler.FilterStart.Offset);
                clonedMethodBody.ExceptionHandlers.Add(newHandler);
            }

            _ChangeBranchAccordingOffsetInterval();
            _WriteNewILInstructions(clonedMethodBody);
            return clonedMethodBody;
        }

        internal Instruction CloneIL(Instruction instruction)
        {
            return _CloneInstruction(instruction);
        }
    }
}