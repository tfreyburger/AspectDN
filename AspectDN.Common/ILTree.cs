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
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Dynamic;
using Foundation.Common.Error;

namespace AspectDN.Common
{
    internal class ILTree
    {
        internal static ILTree Create(MethodDefinition method)
        {
            return Create(method.Body);
        }

        internal static ILTree Create(MethodBody methodBody)
        {
            var ilTree = _CreateTree(methodBody);
            _BuildILNodeTree(ilTree);
            _EvaluateILNodeStack(ilTree);
            return ilTree;
        }

        static ILTree _CreateTree(MethodBody methodBody)
        {
            var ilTree = new ILTree(methodBody.Method);
            var instruction = methodBody.Instructions.First();
            while (instruction != null)
            {
                var stackEffect = CecilHelper.GetStackEffect(methodBody, instruction);
                ilTree.AddIlNode(instruction, OpCodeDatas.Get(instruction.OpCode), stackEffect.Pop, stackEffect.Push, stackEffect.handler);
                instruction = instruction.Next;
            }

            return ilTree;
        }

        static void _BuildILNodeTree(ILTree ilTree)
        {
            foreach (var ilNode in ilTree.ILNodes)
            {
                if (ilNode.PreviousNodeSeq == null)
                    continue;
                if (!ilNode.PreviousNodeSeq.OpCodeData.IsEnd && (!ilNode.PreviousNodeSeq.OpCodeData.IsBranch || ilNode.PreviousNodeSeq.OpCodeData.BranchType != OpCodeTypes.UnCond))
                    ilNode.PreviousNodeSeq.AddNext(ilNode);
                var previousList = ilTree.GetILNodes(t => t.Instruction.Operand == ilNode.Instruction || (t.Instruction.Operand is Instruction[] && ((Instruction[])t.Instruction.Operand).Count(i => i == ilNode.Instruction) > 0));
                foreach (var previous in previousList)
                    previous.AddNext(ilNode);
            }
        }

        static void _EvaluateILNodeStack(ILTree ilTree)
        {
            var ilNode = ilTree.FirstILNode;
            while (ilNode != null)
            {
                var previous = ilNode.PreviousNodes.Where(p => p.IsEvaluate).FirstOrDefault();
                _EvaluateILNodeStack(previous, ilNode);
                ilNode = ilTree.GetILNodes(t => !t.IsEvaluate && (t.PreviousNodes.Count() == 0 || t.PreviousNodes.Where(p => p.IsEvaluate).Count() > 0)).FirstOrDefault();
            }

            if (ilTree.ILNodes.Count(t => t.InStackBalance < 0) > 0)
                throw ErrorFactory.GetException("StackError", ilTree.Method.FullName);
        }

        static void _EvaluateILNodeStack(ILTree.ILNode previousILnode, ILTree.ILNode ilNode)
        {
            ilNode.SetInStackBalance(previousILnode == null ? 0 : previousILnode.OutStackBalance);
            if (ilNode.OutStackBalance < 0)
                throw ErrorFactory.GetException("StackError", ilNode.ILTree.Method.FullName);
            if (ilNode.NextNodes.Count() != 0)
            {
                foreach (var nextILNode in ilNode.NextNodes)
                {
                    if (nextILNode.IsEvaluate)
                    {
                        if (ilNode.OutStackBalance != nextILNode.InStackBalance)
                            throw ErrorFactory.GetException("StackError", ilNode.ILTree.Method.FullName);
                        continue;
                    }
                    else
                        _EvaluateILNodeStack(ilNode, nextILNode);
                }
            }
            else
            {
                if (ilNode.OutStackBalance != 0)
                    throw ErrorFactory.GetException("StackError", ilNode.ILTree.Method.FullName);
            }
        }

        protected List<ILNode> _ILNodes;
        internal IReadOnlyList<ILNode> ILNodes => _ILNodes;
        internal MethodDefinition Method { get; }

        internal MethodBody MethodBody => Method.Body;
        internal ILNode FirstILNode => _ILNodes.First();
        internal ILNode LastILNode => _ILNodes.Last();
        internal ILTree(MethodDefinition method)
        {
            Method = method;
            _ILNodes = new List<ILNode>();
        }

        internal ILNode AddIlNode(Instruction instruction, OpCodeData opCodeData, int pop, int push, ExceptionHandler handler)
        {
            var ilInstr = new ILTree.ILNode(this, instruction, opCodeData, pop, push, handler);
            return ilInstr;
        }

        internal ILNode GetILNode(Instruction instruction)
        {
            return _ILNodes.Where(t => t.Instruction == instruction).FirstOrDefault();
        }

        internal ILTree.ILNode GetILNode(Func<ILTree.ILNode, bool> whereClause)
        {
            return GetILNodes(whereClause).FirstOrDefault();
        }

        internal IEnumerable<ILTree.ILNode> GetILNodes(Func<ILTree.ILNode, bool> whereClause)
        {
            return _ILNodes.Where(whereClause);
        }

        internal ILBlockNode GetDataBlock(Instruction instruction)
        {
            return GetDataBlock(GetILNode(instruction));
        }

        internal ILBlockNode GetDataBlock(ILNode ilNode)
        {
            var from = ilNode;
            if (ilNode.Pop != 0)
            {
                int stackLevel = ilNode.InStackBalance - (ilNode.StartHandler == null ? ilNode.Pop : ilNode.Pop - 1);
                from = GetFromILNode(ilNode, stackLevel);
            }

            return new ILBlockNode(this, from, ilNode);
        }

        internal ILBlockNode GetFullDataBlock(Instruction instruction)
        {
            return GetFullDataBlock(GetILNode(instruction));
        }

        internal ILBlockNode GetFullDataBlock(ILNode ilNode)
        {
            var from = ilNode;
            var to = ilNode;
            int targetStackLevel = 0;
            if (CecilHelper.GetExceptionHandler(ilNode.ILTree.MethodBody, ilNode.Instruction, ExceptionHandlerType.Catch, ExceptionHandlerType.Fault, ExceptionHandlerType.Filter, ExceptionHandlerType.Finally) != null)
                targetStackLevel = 1;
            if (ilNode.OutStackBalance > targetStackLevel)
                to = GetToILNode(ilNode, targetStackLevel);
            if (to.InStackBalance > targetStackLevel)
            {
                from = GetFromILNode(to, targetStackLevel);
            }

            return new ILBlockNode(this, from, to);
        }

        internal IEnumerable<ILBlockNode> GetFullDataBlocks()
        {
            var ilBlocks = new List<ILBlockNode>();
            var ilNode = _ILNodes.FirstOrDefault();
            while (ilNode != null)
            {
                ilBlocks.Add(GetFullDataBlock(ilNode));
                ilNode = ilBlocks.Last().ILNodes.Last();
                ilNode = ilNode.NextNodeSeq;
            }

            return ilBlocks;
        }

        internal string GetSource()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var ilInstr in _ILNodes)
                sb.AppendLine(ilInstr.ToString());
            return sb.ToString();
        }

        internal void AcceptVisitor(IILTreeVisitor iLTreeVisitor) => iLTreeVisitor.Visit(this);
        internal ILTree.ILNode GetFromILNode(ILTree.ILNode ilNode, int stackLevel)
        {
            bool stackLevelReach = false;
            while (ilNode != null && !stackLevelReach)
            {
                stackLevelReach = ilNode.InStackBalance <= stackLevel && ilNode.Push > 0;
                if (stackLevelReach)
                    break;
                ilNode = ilNode.GetLowestPreviousNodes().FirstOrDefault();
            }

            if (ilNode != null && ilNode.PreviousNodes.Count() == 1 && ilNode.PreviousNodes.First().OpCode == OpCodes.Nop)
                ilNode = ilNode.PreviousNodes.First();
            return ilNode;
        }

        internal ILTree.ILNode GetToILNode(ILTree.ILNode ilNode, int stackLevel)
        {
            bool stackLevelReach = false;
            while (ilNode != null && !stackLevelReach)
            {
                stackLevelReach = ilNode.OutStackBalance <= stackLevel && ilNode.Pop > 0;
                if (stackLevelReach)
                    break;
                var nextNodes = ilNode.NextNodes;
                switch (nextNodes.Count())
                {
                    case 0:
                        ilNode = null;
                        break;
                    case 1:
                        ilNode = nextNodes.First();
                        break;
                    default:
                        ilNode = ilNode.NextNodes.Where(t => t.Instruction.Offset == ilNode.NextNodes.Max(p => p.Instruction.Offset)).FirstOrDefault();
                        break;
                }
            }

            if (ilNode != null && ilNode.PreviousNodes.Count() == 1 && ilNode.PreviousNodes.First().OpCode == OpCodes.Nop)
                ilNode = ilNode.PreviousNodes.First();
            return ilNode;
        }

        ILTree.ILNode _GetToILNode(ILTree.ILNode ilNode, IEnumerable<ILTree.ILNode> nextNodes, int staclLevel)
        {
            ILTree.ILNode to = ilNode;
            foreach (var next in nextNodes)
            {
                var found = GetToILNode(next, staclLevel);
                if (found.NodeIndex > to.NodeIndex)
                    to = found;
            }

            return to;
        }

        internal string GetILTreeDocument()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var ilNode in ILNodes)
                sb.AppendLine(ilNode.ToString());
            return sb.ToString();
        }

        internal class ILNode
        {
            List<ILNode> _NextNodes;
            List<ILNode> _PreviousNodes;
            int _InStackBalance;
            bool _IsInTree;
            bool _IsEvaluate = false;
            int _Pop;
            internal ILTree ILTree { get; }

            internal Instruction Instruction { get; }

            internal OpCodeData OpCodeData { get; }

            internal int Pop { get => _Pop; set => _Pop = value; }

            internal int Push { get; }

            internal int InStackBalance => _InStackBalance;
            internal int OutStackBalance => !IsEvaluate ? 0 : InStackBalance - Pop + Push;
            internal ILNode PreviousNodeSeq => NodeIndex == 0 ? null : ILTree._ILNodes[NodeIndex - 1];
            internal ILNode NextNodeSeq => NodeIndex == ILTree._ILNodes.Count() - 1 ? null : ILTree._ILNodes[NodeIndex + 1];
            internal int NodeIndex => ILTree._ILNodes.IndexOf(this);
            internal IReadOnlyList<ILNode> NextNodes => _NextNodes;
            internal IReadOnlyList<ILNode> PreviousNodes => _PreviousNodes;
            internal bool IsEvaluate => _IsEvaluate;
            internal bool IsIntree { get => _IsInTree; set => _IsInTree = value; }

            internal ExceptionHandler StartHandler { get; }

            internal MethodBody MethodBody => ILTree.MethodBody;
            internal MethodDefinition Method => ILTree.Method;
            internal ILBlockNode DataBlock => ILTree.GetDataBlock(this);
            internal ILBlockNode FullDataBlock => ILTree.GetFullDataBlock(this);
            internal int Offset => Instruction.Offset;
            internal object Operand => Instruction.Operand;
            internal OpCode OpCode => Instruction.OpCode;
            internal ILNode(ILTree ilStrucMethod, Instruction instruction, OpCodeData opCodeData, int pop, int push, ExceptionHandler handler)
            {
                ILTree = ilStrucMethod;
                Instruction = instruction;
                OpCodeData = opCodeData;
                Pop = pop;
                Push = push;
                ilStrucMethod._ILNodes.Add(this);
                StartHandler = handler;
                _NextNodes = new List<ILNode>();
                _PreviousNodes = new List<ILNode>();
            }

            internal void AddNext(ILTree.ILNode ilInstr)
            {
                _NextNodes.Add(ilInstr);
                ilInstr._PreviousNodes.Add(this);
            }

            internal void AddPrevious(ILTree.ILNode ilInstr)
            {
                _PreviousNodes.Add(ilInstr);
                ilInstr._NextNodes.Add(this);
            }

            internal void SetInStackBalance(int inStackBalance)
            {
                _InStackBalance = inStackBalance;
                if (Pop == int.MaxValue)
                    Pop = _InStackBalance + Push;
                _IsEvaluate = true;
            }

            internal IEnumerable<ILTree.ILNode> GetLowestPreviousNodes()
            {
                return PreviousNodes.Where(t => t.Instruction.Offset == PreviousNodes.Min(p => p.Instruction.Offset));
            }

            internal ILTree.ILNode GetHighestNextNodes()
            {
                return NextNodes.Where(t => t.Instruction.Offset == NextNodes.Max(p => p.Instruction.Offset)).FirstOrDefault();
            }

            public override string ToString()
            {
                StringBuilder sbprevious = new StringBuilder();
                foreach (var ilNode in PreviousNodes)
                    sbprevious.Append(sbprevious.Length != 0 ? "," : "").Append(ilNode.Offset.ToString("x4"));
                StringBuilder sbNext = new StringBuilder();
                foreach (var ilNode in NextNodes)
                    sbNext.Append(sbNext.Length != 0 ? "," : "").Append(ilNode.Offset.ToString("x4"));
                return $"({IsEvaluate});{InStackBalance};{Pop};{Push};{OutStackBalance};{Instruction.ToString()};{sbprevious.ToString()};{sbNext.ToString()};";
            }
        }
    }

    internal class ILBlockNode
    {
        internal MethodDefinition Method => ILTree.Method;
        internal ILTree ILTree { get; }

        internal ILTree.ILNode From { get; }

        internal ILTree.ILNode To { get; }

        internal IEnumerable<ILTree.ILNode> ILNodes => _GetILNodes();
        internal IEnumerable<Instruction> Instructions => _GetInstructions();
        internal Instruction FirstIL => From.Instruction;
        internal Instruction LastIL => To.Instruction;
        internal int OffsetSize => To.Instruction.Offset - From.Instruction.Offset;
        internal ILBlockNode(ILTree ilTree, ILTree.ILNode from, ILTree.ILNode to)
        {
            ILTree = ilTree;
            From = from;
            To = to;
        }

        IEnumerable<ILTree.ILNode> _GetILNodes()
        {
            var ilNode = From;
            while (ilNode != null && ilNode.Offset <= To.Offset)
            {
                yield return ilNode;
                ilNode = ilNode.NextNodeSeq;
            }
        }

        IEnumerable<Instruction> _GetInstructions()
        {
            foreach (var iLNode in _GetILNodes())
                yield return iLNode.Instruction;
        }

        internal Instruction GetNextInstruction(Instruction from)
        {
            if (from == To.Instruction)
                return null;
            return from.Next;
        }
    }

    internal class ILBlockTreeNode
    {
        List<ILBlockTreeNode> _ILBlockTreeNodes;
        internal ILTree ILtree { get; }

        internal ILBlockTreeNode ParentILBlockTreeNode { get; }

        internal IEnumerable<ILTree.ILNode> IlNodes => ILtree.GetILNodes(t => t.Offset >= FromILNode.Offset && t.Offset <= ToILNode.Offset).OrderBy(t => t.Offset);
        internal IEnumerable<ILBlockTreeNode> Children => _ILBlockTreeNodes;
        internal ILTree.ILNode MainILNode => ToILNode;
        internal ILTree.ILNode FromILNode { get; }

        internal ILTree.ILNode ToILNode { get; }

        internal ILBlockTreeNode(ILBlockTreeNode parent, ILTree iLTree, ILTree.ILNode from, ILTree.ILNode to)
        {
            _ILBlockTreeNodes = new List<ILBlockTreeNode>();
            FromILNode = from;
            ToILNode = to;
            ParentILBlockTreeNode = parent;
            ILtree = iLTree;
        }

        internal void Add(IEnumerable<ILBlockTreeNode> children) => _ILBlockTreeNodes.AddRange(children);
    }

    internal class ILTreeBlockVisitor : IILTreeVisitor
    {
        IEnumerable<ILBlockTreeNode> _IlBlockTreeNodes;
        ILTree _ILTree;
        internal IEnumerable<ILBlockTreeNode> IlBlockTreeNodes { get => _IlBlockTreeNodes; }

        void _Visit(ILTree iLTree)
        {
            _ILTree = iLTree;
            _IlBlockTreeNodes = _Visit(null, _ILTree.FirstILNode, _ILTree.LastILNode);
        }

        IEnumerable<ILBlockTreeNode> _Visit(ILBlockTreeNode parent, ILTree.ILNode from, ILTree.ILNode to)
        {
            var ilBlocks = new List<ILBlockTreeNode>();
            var ilNode = to;
            while (ilNode != null && ilNode.Offset >= from.Offset)
            {
                var blockTo = ilNode;
                if (ilNode == from)
                {
                    var ilBlock = new ILBlockTreeNode(parent, _ILTree, blockTo, blockTo);
                    ilNode = null;
                }
                else
                {
                    var blockFrom = _GetFromILNode(blockTo);
                    var ilBlock = new ILBlockTreeNode(parent, _ILTree, blockFrom, blockTo);
                    if (blockFrom.Offset < from.Offset)
                        throw new NotSupportedException("ILTreeBlockNode with a start node out of ranger");
                    ilBlocks.Insert(0, ilBlock);
                    if (blockFrom.Pop > 0)
                        ilNode = blockFrom;
                    else
                        ilNode = blockFrom.PreviousNodeSeq;
                }
            }

            foreach (var ilBlock in ilBlocks.Where(t => t.IlNodes.Count() > 1))
            {
                if (ilBlock.IlNodes.Count() == 2)
                    ilBlock.Add(new ILBlockTreeNode[]{new ILBlockTreeNode(ilBlock, ilBlock.ILtree, ilBlock.FromILNode, ilBlock.FromILNode)});
                else
                {
                    var children = _Visit(ilBlock, ilBlock.FromILNode, ilBlock.ToILNode.PreviousNodeSeq);
                    ilBlock.Add(children);
                }
            }

            return ilBlocks;
        }

        ILTree.ILNode _GetFromILNode(ILTree.ILNode ilNode)
        {
            var delta = -ilNode.Pop;
            ILTree.ILNode fromIlNode = ilNode;
            while (fromIlNode != null && delta < 0)
            {
                fromIlNode = fromIlNode.GetLowestPreviousNodes().FirstOrDefault();
                if (fromIlNode != null)
                    delta += fromIlNode.Push - fromIlNode.Pop;
            }

            if (delta < 0)
                throw new NotSupportedException("");
            return fromIlNode;
        }

        void IILTreeVisitor.Visit(ILTree iLTree) => _Visit(iLTree);
    }

    internal interface IILTreeVisitor
    {
        void Visit(ILTree iLTree);
    }
}