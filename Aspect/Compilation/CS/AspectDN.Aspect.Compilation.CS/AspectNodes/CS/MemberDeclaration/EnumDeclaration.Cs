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

namespace AspectDN.Aspect.Compilation.CS
{
    internal class EnumDeclarationAspect : TypeMemberDeclarationAspect
    {
        internal IEnumerable<AttributeSectionAspect> AttributeSections { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeSectionAspect>(this); }

        internal IEnumerable<ModifierAspect> Modifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ModifierAspect>(this, false).ToList(); }

        internal IdentifierNameAspect Identifier { get => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this).FirstOrDefault(); }

        internal EnumBaseAspect EnumBase { get => CSAspectCompilerHelper.GetDescendingNodesOfType<EnumBaseAspect>(this).FirstOrDefault(); }

        internal IEnumerable<EnumMemberDeclarationApsect> EnumMembers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<EnumMemberDeclarationApsect>(this, false).ToList(); }

        internal EnumDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.EnumDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class EnumBaseAspect : CSAspectNode
    {
        internal TypeAspect Type => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeAspect>(this, false).FirstOrDefault();
        internal EnumBaseAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.EnumBase(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class EnumMemberDeclarationApsect : CSAspectNode
    {
        internal IEnumerable<AttributeSectionAspect> AttributeSections { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeSectionAspect>(this); }

        internal IdentifierNameAspect Identifier { get => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this).FirstOrDefault(); }

        internal ExpressionAspect Expression { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false, Identifier).FirstOrDefault(); }

        internal EnumMemberDeclarationApsect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.EnumMemberDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }
}