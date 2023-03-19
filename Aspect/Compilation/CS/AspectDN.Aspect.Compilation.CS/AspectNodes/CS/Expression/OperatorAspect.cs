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
using TokenizerDN.Common.SourceAnalysis;
using Microsoft.CodeAnalysis;
using AspectDN.Common;
using Microsoft.CodeAnalysis.CSharp;

namespace AspectDN.Aspect.Compilation.CS
{
    internal class OperatorAspect : CSAspectNode
    {
        internal OperatorAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            throw AspectDNErrorFactory.GetException("NotImplementedException");
        }
    }
}