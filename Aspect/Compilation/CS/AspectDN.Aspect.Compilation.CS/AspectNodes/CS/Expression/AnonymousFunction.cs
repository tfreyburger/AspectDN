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

namespace AspectDN.Aspect.Compilation.CS
{
    internal class AnonymousMethodExpressionAspect : CSAspectNode
    {
        internal List<ExplicitAnonymousFunctionParameterAspect> Parameters { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ExplicitAnonymousFunctionParameterAspect>(this, false).ToList(); }

        internal AnonymousFunctionBodyAspect Body { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AnonymousFunctionBodyAspect>(this, false).FirstOrDefault(); }

        internal AnonymousMethodExpressionAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AnonymousMethodExpression(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal abstract class LambdaExpressionAspect : ExpressionAspect
    {
        internal AnonymousFunctionBodyAspect Body => CSAspectCompilerHelper.GetDescendingNodesOfType<AnonymousFunctionBodyAspect>(this, false).FirstOrDefault();
        internal LambdaExpressionAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class ParenthesisLambdaExpressionAspect : LambdaExpressionAspect
    {
        internal List<AnonymousFunctionParameterAspect> Parameters => CSAspectCompilerHelper.GetDescendingNodesOfType<AnonymousFunctionParameterAspect>(this, false).ToList();
        internal ParenthesisLambdaExpressionAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ParenthesisLambdaExpression(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class SimpleLambdaExpressionAspect : LambdaExpressionAspect
    {
        internal AnonymousFunctionParameterAspect Parameter => CSAspectCompilerHelper.GetDescendingNodesOfType<AnonymousFunctionParameterAspect>(this, false).FirstOrDefault();
        internal SimpleLambdaExpressionAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.SimpleLambdaExpression(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AnonymousFunctionBodyAspect : CSAspectNode
    {
        internal CSAspectNode Expression => ChildCSAspectNodes.FirstOrDefault();
        internal AnonymousFunctionBodyAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AnonymousFunctionBody(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal abstract class AnonymousFunctionParameterAspect : CSAspectNode
    {
        internal AnonymousFunctionParameterAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class ExplicitAnonymousFunctionParameterAspect : AnonymousFunctionParameterAspect
    {
        internal IdentifierNameAspect Identifier { get => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this, false).FirstOrDefault(); }

        internal KeywordAspect Modifier => CSAspectCompilerHelper.GetDescendingNodesOfType<KeywordAspect>(this, false).FirstOrDefault();
        internal TypeAspect Type { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeAspect>(this, false).FirstOrDefault(); }

        internal ExplicitAnonymousFunctionParameterAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ExplicitAnonymousFunctionParameter(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class ImplicitAnonymousFunctionParameterAspect : AnonymousFunctionParameterAspect
    {
        internal IdentifierNameAspect Identifier { get => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this, false).FirstOrDefault(); }

        internal ImplicitAnonymousFunctionParameterAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ImplicitAnonymousFunctionParameter(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }
}