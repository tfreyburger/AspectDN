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
using Microsoft.CodeAnalysis;
using TokenizerDN.Common.SourceAnalysis;

namespace AspectDN.Aspect.Compilation.Foundation
{
    internal abstract class AspectNode
    {
        static ulong _IdCounter = 1;
        static readonly object idCounterLock = new object ();
        protected string _Id;
        List<TokenErrorAspect> _Errors;
        bool _OnError = false;
        internal string Id
        {
            get
            {
                if (string.IsNullOrEmpty(_Id))
                    _Id = _GetId().ToString();
                return _Id;
            }
        }

        internal ISynToken SynToken { get; }

        internal AspectNode ParentAspectNode { get; set; }

        internal AspectNode AspectRoot => ParentAspectNode != null ? ParentAspectNode.AspectRoot : null;
        internal virtual AspectTree AspectTree => ParentAspectNode != null ? ParentAspectNode.AspectTree : null;
        List<AspectNode> _ChildAspectNodes;
        internal List<AspectNode> ChildAspectNodes => _ChildAspectNodes;
        internal IEnumerable<TokenErrorAspect> Errors => _Errors;
        internal bool OnError => _IsOnError();
        internal string TokenValue => SynToken.Value;
        internal AspectNode(ISynToken token)
        {
            SynToken = token;
            _ChildAspectNodes = new List<AspectNode>();
            _Errors = new List<TokenErrorAspect>();
        }

        static ulong _GetId()
        {
            lock (idCounterLock)
            {
                return _IdCounter++;
            }
        }

        internal abstract SyntaxNodeOrToken? GetSyntaxNode();
        internal virtual AspectNode AddNode(AspectNode node)
        {
            if (node is TokenErrorAspect)
            {
                if (_ChildAspectNodes.Count() != 0)
                    _ChildAspectNodes.Last()._Errors.Add((TokenErrorAspect)node);
                else
                    _Errors.Add((TokenErrorAspect)node);
            }
            else
            {
                _ChildAspectNodes.Add(node);
                node.ParentAspectNode = this;
            }

            return this;
        }

        internal virtual void RemovetNode(AspectNode node)
        {
            node.ParentAspectNode = null;
            _ChildAspectNodes.Remove(node);
        }

        internal void SetOnError() => _OnError = true;
        bool _IsOnError()
        {
            if (_OnError || _Errors.Count() != 0)
                return true;
            foreach (var child in ChildAspectNodes)
                if (child.OnError)
                    return true;
            return false;
        }
    }
}