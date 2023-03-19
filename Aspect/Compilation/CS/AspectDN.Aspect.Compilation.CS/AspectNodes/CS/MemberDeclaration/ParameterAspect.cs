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
using AspectDN.Aspect.Compilation.Foundation;
using Microsoft.CodeAnalysis;
using AspectDN.Common;

namespace AspectDN.Aspect.Compilation.CS
{
    internal class FormalParameterListAspect : CSAspectNode
    {
        internal IEnumerable<ParameterAspect> Parameters { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ParameterAspect>(this); }

        internal FormalParameterListAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.FormalParameterList(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal abstract class ParameterAspect : CSAspectNode
    {
        internal ParameterAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class FixedParameterAspect : ParameterAspect
    {
        internal IEnumerable<AttributeSectionAspect> AttributeSections { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeSectionAspect>(this); }

        internal IEnumerable<ParameterModifierAspect> Modifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ParameterModifierAspect>(this).ToList(); }

        internal TypeAspect Type { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeAspect>(this).FirstOrDefault(); }

        internal IdentifierNameAspect Identifier { get => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this).FirstOrDefault(); }

        internal ExpressionAspect DefaultArgument { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false, Identifier).FirstOrDefault(); }

        internal FixedParameterAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.FixedParameter(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class ParameterModifierAspect : CSAspectNode
    {
        internal ParameterModifierAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ParameterModifier(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class ParameterArrayAspect : ParameterAspect
    {
        internal IEnumerable<AttributeSectionAspect> AttributeSections { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeSectionAspect>(this); }

        internal ArrayTypeAspect ArrayType { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ArrayTypeAspect>(this).FirstOrDefault(); }

        internal IdentifierNameAspect Identifier { get => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this).FirstOrDefault(); }

        internal ParameterArrayAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ArrayParameter(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }
}