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
using AspectDN.Aspect.Weaving.IJoinpoints;

namespace AspectDN.Aspect.Weaving
{
    internal class ILBlockToConverts : IEnumerable<ILBlockToConvert>
    {
        List<ILBlockToConvert> _ILBlocks;
        internal ILBlockToConverts()
        {
            _ILBlocks = new List<ILBlockToConvert>();
        }

        internal void Add(PrototypeItemMapping prototypeItemMapping, Instruction sourceIL, ILBlockNode iLBlock)
        {
            Add(new ILBlockToConvert(prototypeItemMapping, sourceIL, iLBlock));
        }

        internal void Add(ILBlockToConvert ilBlock)
        {
            _ILBlocks.Add(ilBlock);
        }

        internal void Complete()
        {
            foreach (var child in _ILBlocks)
            {
                var parents = _ILBlocks.Where(p => p != child && child.ILBlock.FirstIL.Offset >= p.ILBlock.FirstIL.Offset && child.ILBlock.To.Offset <= p.ILBlock.To.Offset);
                if (parents.Any())
                {
                    var minOffsetSize = parents.Min(p => p.ILBlock.OffsetSize);
                    var parent = parents.FirstOrDefault(p => p.ILBlock.OffsetSize == minOffsetSize);
                    child.SetParent(parent);
                }
            }
        }

        IEnumerator<ILBlockToConvert> IEnumerable<ILBlockToConvert>.GetEnumerator()
        {
            return (System.Collections.Generic.IEnumerator<ILBlockToConvert>)_ILBlocks.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return (System.Collections.IEnumerator)_ILBlocks;
        }
    }

    internal class ILBlockToConvert
    {
        ILBlockToConvert _Parent;
        internal PrototypeItemMapping PrototypeItemMapping { get; }

        internal Instruction SourceIL { get; }

        internal ILBlockNode ILBlock { get; }

        internal ILBlockToConvert Parent { get => _Parent; }

        internal List<ILBlockToConvert> Children { get; }

        internal ILBlockToConvert(PrototypeItemMapping prototypeItemMapping, Instruction sourceIL, ILBlockNode iLBlock)
        {
            PrototypeItemMapping = prototypeItemMapping;
            SourceIL = sourceIL;
            ILBlock = iLBlock;
            _Parent = null;
            Children = new List<ILBlockToConvert>();
        }

        internal void SetParent(ILBlockToConvert parent)
        {
            _Parent = parent;
            _Parent.Children.Add(this);
        }
    }

    internal class Instructions
    {
        List<Instruction> _Instructions;
        public Instructions(IEnumerable<Instruction> instructions)
        {
            _Instructions = new List<Instruction>(instructions);
        }
    }
}