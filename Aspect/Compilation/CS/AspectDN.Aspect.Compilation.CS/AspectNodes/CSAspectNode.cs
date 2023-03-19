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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using AspectDN.Aspect.Compilation.Foundation;
using TokenizerDN.Common.SourceAnalysis;

namespace AspectDN.Aspect.Compilation.CS
{
    internal abstract class CSAspectNode : AspectNode
    {
        internal virtual CSAspectNodeTypes CSAspectNodeType { get => CSAspectNodeTypes.Misc; }

        internal List<CSAspectNode> ChildCSAspectNodes => ChildAspectNodes.Cast<CSAspectNode>().ToList();
        internal CSAspectNode this[int i]
        {
            get
            {
                if (ChildAspectNodes.Count >= i + 1)
                    return ChildCSAspectNodes[i];
                return null;
            }
        }

        internal CSAspectNode(ISynToken syntoken) : base(syntoken)
        {
        }
    }
}