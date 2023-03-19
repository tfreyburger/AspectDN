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
using Microsoft.CodeAnalysis.CSharp;

namespace AspectDN.Aspect.Compilation.CS
{
    internal class LiteralExpressionAspect : ExpressionAspect
    {
        internal LiteralExpressionTypes LiteralExpressionType { get; }

        internal LiteralExpressionAspect(ISynToken token, LiteralExpressionTypes literalExpressionType) : base(token)
        {
            LiteralExpressionType = literalExpressionType;
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.LiteralExpression(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal enum LiteralExpressionTypes
    {
        Boolean,
        Integer,
        Decimal,
        Hexadecimal,
        Real,
        Character,
        String,
        Null
    }
}