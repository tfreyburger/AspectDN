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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AspectDN.Aspect.Compilation.CS
{
    internal class AttributeSectionAspect : CSAspectNode
    {
        internal List<AttributeAspect> Attributes { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeAspect>(this).ToList(); }

        internal AttributeTargetSpecifierAspect TargetSpecifier { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeTargetSpecifierAspect>(this, false).FirstOrDefault(); }

        internal AttributeSectionAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AttributeSection(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AttributeTargetSpecifierAspect : CSAspectNode
    {
        internal IdentifierNameAspect Identifier => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this, false).FirstOrDefault();
        internal AttributeTargetSpecifierAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AttributeTargetSpecifier(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AttributeAspect : CSAspectNode
    {
        internal TypeAspect Type => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeAspect>(this, false).FirstOrDefault();
        internal AttributeArgumentListAspect Arguments => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeArgumentListAspect>(this, false).FirstOrDefault();
        internal AttributeAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.Attribute(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AttributeArgumentListAspect : CSAspectNode
    {
        internal IEnumerable<AttributeArgumentAspect> AttributeArguments { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeArgumentAspect>(this); }

        internal AttributeArgumentListAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AttributeArgumentList(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal abstract class AttributeArgumentAspect : CSAspectNode
    {
        internal AttributeArgumentAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class AttributeArgumentPositionalAspect : AttributeArgumentAspect
    {
        internal IdentifierNameAspect IdentifierName => (IdentifierNameAspect)(ChildCSAspectNodes.Count() == 1 ? null : ChildCSAspectNodes.First());
        internal ExpressionAspect Expression => (ExpressionAspect)ChildCSAspectNodes.Last();
        internal AttributeArgumentPositionalAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PositionalAttributeArgument(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AttributeArgumentNamedAspect : AttributeArgumentAspect
    {
        internal IdentifierNameAspect IdentifierName => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this, false).FirstOrDefault();
        internal ExpressionAspect Expression => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false, IdentifierName).FirstOrDefault();
        internal AttributeArgumentNamedAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.NamedAttributeArgument(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }
}