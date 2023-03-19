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
    internal abstract class TypeMemberDeclarationAspect : CSAspectNode
    {
        internal TypeMemberDeclarationAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class ConstantDeclarationAspect : TypeMemberDeclarationAspect
    {
        internal IEnumerable<AttributeSectionAspect> AttributeSections { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeSectionAspect>(this); }

        internal IEnumerable<ModifierAspect> Modifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ModifierAspect>(this, false).ToList(); }

        internal TypeAspect Type { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeAspect>(this, false).FirstOrDefault(); }

        internal IEnumerable<ConstantDeclaratorAspect> Declarators { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ConstantDeclaratorAspect>(this); }

        internal ConstantDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ConstantDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class ConstantDeclaratorAspect : CSAspectNode
    {
        internal IdentifierNameAspect Identifier => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this, false).FirstOrDefault();
        internal ExpressionAspect Expression => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false, Identifier).FirstOrDefault();
        internal ConstantDeclaratorAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ConstantDeclarator(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class FieldDeclarationAspect : TypeMemberDeclarationAspect
    {
        internal IEnumerable<AttributeSectionAspect> AttributeSections { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeSectionAspect>(this); }

        internal IEnumerable<ModifierAspect> Modifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ModifierAspect>(this, false).ToList(); }

        internal TypeAspect Type { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeAspect>(this, false).FirstOrDefault(); }

        internal IEnumerable<VariableDeclaratorAspect> Declarators { get => CSAspectCompilerHelper.GetDescendingNodesOfType<VariableDeclaratorAspect>(this); }

        internal FieldDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.FieldDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }
}