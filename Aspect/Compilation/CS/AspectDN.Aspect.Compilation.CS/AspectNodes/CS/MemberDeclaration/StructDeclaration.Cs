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
    internal class StructDeclarationAspect : TypeMemberDeclarationAspect
    {
        internal IEnumerable<AttributeSectionAspect> AttributeSections { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeSectionAspect>(this); }

        internal IEnumerable<ModifierAspect> Modifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ModifierAspect>(this, false).ToList(); }

        internal IdentifierNameAspect IdentifierName { get => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this, false).FirstOrDefault(); }

        internal TypeParameterListAspect TypeParameterList { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeParameterListAspect>(this, false).FirstOrDefault(); }

        internal StructInterfacesAspect StructInterfaces { get => CSAspectCompilerHelper.GetDescendingNodesOfType<StructInterfacesAspect>(this, false).FirstOrDefault(); }

        internal IEnumerable<TypeParameterConstraintsClauseAspect> TypeParameterConstraintsClauses { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeParameterConstraintsClauseAspect>(this); }

        internal IEnumerable<TypeMemberDeclarationAspect> Members { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeMemberDeclarationAspect>(this); }

        internal StructDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.StructDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class StructInterfacesAspect : CSAspectNode
    {
        internal IEnumerable<BaseTypeAspect> BaseTypes { get => CSAspectCompilerHelper.GetDescendingNodesOfType<BaseTypeAspect>(this, false); }

        internal StructInterfacesAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.StructInterfaces(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }
}