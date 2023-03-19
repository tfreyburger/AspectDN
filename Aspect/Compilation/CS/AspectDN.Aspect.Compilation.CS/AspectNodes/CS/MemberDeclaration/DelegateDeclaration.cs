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
    internal class DelegateDeclarationAspect : TypeMemberDeclarationAspect
    {
        internal IEnumerable<AttributeSectionAspect> AttributeSections { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeSectionAspect>(this, false); }

        internal IEnumerable<ModifierAspect> Modifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ModifierAspect>(this, false).ToList(); }

        internal ReturnTypeAspect ReturnType { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ReturnTypeAspect>(this, false).FirstOrDefault(); }

        internal IdentifierNameAspect Identifier { get => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this, false).FirstOrDefault(); }

        internal VariantTypeParameterListAspect VariantParameterList { get => CSAspectCompilerHelper.GetDescendingNodesOfType<VariantTypeParameterListAspect>(this, false).FirstOrDefault(); }

        internal FormalParameterListAspect FormalParameterList { get => CSAspectCompilerHelper.GetDescendingNodesOfType<FormalParameterListAspect>(this, false).FirstOrDefault(); }

        internal IEnumerable<TypeParameterConstraintsClauseAspect> ConstrainstsClauses { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeParameterConstraintsClauseAspect>(this, false); }

        internal DelegateDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.DelegateDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }
}