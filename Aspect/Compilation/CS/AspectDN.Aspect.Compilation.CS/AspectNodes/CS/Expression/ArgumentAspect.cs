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
using AspectDN.Aspect.Compilation.Foundation;
using TokenizerDN.Common.SourceAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AspectDN.Aspect.Compilation.CS
{
    internal class ArgumentAspect : CSAspectNode
    {
        internal ArgumentNameAspect Name { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ArgumentNameAspect>(this, false).FirstOrDefault(); }

        internal KeywordAspect Modifier { get => CSAspectCompilerHelper.GetDescendingNodesOfType<KeywordAspect>(this, false).FirstOrDefault(); }

        internal ExpressionAspect Expression { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false).FirstOrDefault(); }

        internal ArgumentAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.Argument(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class ArgumentNameAspect : CSAspectNode
    {
        internal IdentifierNameAspect Identifier { get => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this, false).FirstOrDefault(); }

        internal ArgumentNameAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ArgumentName(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class VariableReferenceAspect : CSAspectNode
    {
        internal ExpressionAspect Expression { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this).FirstOrDefault(); }

        internal VariableReferenceAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.VariableReference(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class ArgumentListAspect : CSAspectNode
    {
        internal IEnumerable<ArgumentAspect> Arguments => CSAspectCompilerHelper.GetDescendingNodesOfType<ArgumentAspect>(this);
        internal ArgumentListAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ArgumentList(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }
}