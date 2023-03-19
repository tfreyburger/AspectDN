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
using System.Runtime.InteropServices;

namespace AspectDN.Aspect.Weaving
{
    internal class ILBodyMerger : IILWorker
    {
        MethodBody _TargetMethodBody;
        WeaveItemMember _WeaveItemMember;
        IEnumerable<Instruction> _NewIls;
        internal ILBodyMerger(WeaveItemMember weaveItemMember, MethodBody targetMethodBody)
        {
            _WeaveItemMember = weaveItemMember;
            _TargetMethodBody = targetMethodBody;
        }

        internal void InsertBeforeBody(IEnumerable<Instruction> sourceIls)
        {
            var ils = sourceIls.ToList();
            var newIls = new List<Instruction>();
            foreach (var il in ils)
            {
                Instruction newIl = null;
                if (il.OpCode.OperandType == OperandType.InlineNone)
                {
                    newIl = Instruction.Create(il.OpCode);
                }
                else
                {
                    switch (il.Operand)
                    {
                        case TypeReference typeReference:
                            newIl = Instruction.Create(il.OpCode, _WeaveItemMember.Resolve(typeReference));
                            break;
                        case Mono.Cecil.CallSite callSite:
                            newIl = Instruction.Create(il.OpCode, _WeaveItemMember.Resolve(callSite));
                            break;
                        case MethodReference methodReference:
                            newIl = Instruction.Create(il.OpCode, (MethodReference)_WeaveItemMember.Resolve(methodReference));
                            break;
                        case FieldReference fieldReference:
                            newIl = Instruction.Create(il.OpCode, (FieldReference)_WeaveItemMember.Resolve(fieldReference));
                            break;
                        case string @string:
                            newIl = Instruction.Create(il.OpCode, @string);
                            break;
                        case sbyte @sbyte:
                            newIl = Instruction.Create(il.OpCode, @sbyte);
                            break;
                        case byte @byte:
                            newIl = Instruction.Create(il.OpCode, @byte);
                            break;
                        case int @int:
                            newIl = Instruction.Create(il.OpCode, @int);
                            break;
                        case long @long:
                            newIl = Instruction.Create(il.OpCode, @long);
                            break;
                        case float @float:
                            newIl = Instruction.Create(il.OpCode, @float);
                            break;
                        case double @double:
                            newIl = Instruction.Create(il.OpCode, @double);
                            break;
                        case VariableDefinition variable:
                            newIl = Instruction.Create(il.OpCode, variable);
                            break;
                        case ParameterDefinition parameter:
                            newIl = Instruction.Create(il.OpCode, parameter);
                            break;
                        case Instruction instruction:
                            newIl = Instruction.Create(il.OpCode, instruction);
                            break;
                        case Instruction[] instructions:
                            newIl = Instruction.Create(il.OpCode, instructions);
                            break;
                        default:
                            throw new NotSupportedException($"unable to clone this instruction as it is unknown");
                    }
                }

                newIls.Add(newIl);
            }

            foreach (var newIl in newIls.Where(t => t.Operand != null && (t.Operand is Instruction || t.Operand is Instruction[])))
            {
                switch (newIl.Operand)
                {
                    case Instruction instruction:
                        newIl.Operand = newIls[ils.IndexOf((Instruction)newIl.Operand)];
                        break;
                    case Instruction[] instructions:
                        var newInstructions = new Instruction[instructions.Length];
                        for (int i = 0; i < instructions.Length; i++)
                            newInstructions[i] = newIls[ils.IndexOf((Instruction)instructions[i].Operand)];
                        newIl.Operand = newInstructions;
                        break;
                }
            }

            _NewIls = newIls;
        }

        internal MethodBody Merge()
        {
            foreach (var il in _NewIls.Reverse())
                _TargetMethodBody.Instructions.Insert(0, il);
            CecilHelper.OffsetRecalculation(_TargetMethodBody.Instructions);
            return _TargetMethodBody;
        }

        MethodBody IILWorker.Merge() => Merge();
    }
}