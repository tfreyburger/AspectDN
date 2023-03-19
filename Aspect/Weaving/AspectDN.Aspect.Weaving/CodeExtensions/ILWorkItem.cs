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

namespace AspectDN.Aspect.Weaving
{
    internal class ILWorkItem
    {
        internal (int from, int to) Offsets { get; }

        internal Instruction Instruction { get; }

        internal ILLocalisations ILLocalisation { get; }

        internal int OldTargetOffset { get; set; }

        internal ILWorkItem((int from, int to) offsets, Instruction instruction, ILLocalisations ilLocalisation)
        {
            Offsets = (offsets.from, offsets.to);
            Instruction = instruction;
            ILLocalisation = ilLocalisation;
        }

        public override string ToString()
        {
            return Instruction.ToString();
        }
    }

    [Flags]
    internal enum ILLocalisations : uint
    {
        Source = 1,
        Target = 2,
        Before = 4,
        After = 8,
        AfterEnd = 16,
        Beteween = 32
    }

    internal enum ILMergekinds
    {
        None,
        Body,
        Instruction
    }

    internal class ConvertedIL
    {
        internal Instruction SourceIL { get; }

        internal ILBlockNode SourceILBlock { get; }

        internal IEnumerable<Instruction> TargetILs { get; }

        internal ConvertedIL(Instruction sourceIL, ILBlockNode sourceILBlock, IEnumerable<Instruction> targetILs)
        {
            SourceIL = sourceIL;
            SourceILBlock = sourceILBlock;
            TargetILs = targetILs;
        }
    }
}