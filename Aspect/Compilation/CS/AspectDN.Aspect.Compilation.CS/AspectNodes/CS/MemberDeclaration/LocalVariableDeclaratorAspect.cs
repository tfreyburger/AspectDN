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
using AspectDN.Aspect.Compilation.Foundation;

namespace AspectDN.Aspect.Compilation.CS
{
    internal class AbstractVariableDeclaratorAspect : CSAspectNode
    {
        internal IdentifierNameAspect Identifier => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this, false).FirstOrDefault();
        internal CSAspectNode VariableInitializer => CSAspectCompilerHelper.GetDescendingNodesOfType<CSAspectNode>(this, false, Identifier).FirstOrDefault();
        internal AbstractVariableDeclaratorAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.VariableDeclarator(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class LocalVariableDeclaratorAspect : AbstractVariableDeclaratorAspect
    {
        internal LocalVariableDeclaratorAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class VariableDeclaratorAspect : AbstractVariableDeclaratorAspect
    {
        internal VariableDeclaratorAspect(ISynToken token) : base(token)
        {
        }
    }
}