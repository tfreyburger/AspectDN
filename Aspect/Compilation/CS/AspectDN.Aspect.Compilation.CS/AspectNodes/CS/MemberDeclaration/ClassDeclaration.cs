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
    internal class ClassDeclarationAspect : TypeMemberDeclarationAspect
    {
        internal IEnumerable<AttributeSectionAspect> AttributeSections { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeSectionAspect>(this); }

        internal IdentifierNameAspect IdentifierName { get => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this, false).FirstOrDefault(); }

        internal IEnumerable<ModifierAspect> ClassModifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ModifierAspect>(this, false).ToList(); }

        internal TypeParameterListAspect TypeParameterList { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeParameterListAspect>(this, false).FirstOrDefault(); }

        internal BaseListAspect BaseList { get => CSAspectCompilerHelper.GetDescendingNodesOfType<BaseListAspect>(this, false).FirstOrDefault(); }

        internal IEnumerable<TypeParameterConstraintsClauseAspect> TypeParameterConstraintsClauses { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeParameterConstraintsClauseAspect>(this); }

        internal IEnumerable<TypeMemberDeclarationAspect> Members { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeMemberDeclarationAspect>(this); }

        internal ClassDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ClassDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class BaseListAspect : CSAspectNode
    {
        internal IEnumerable<BaseTypeAspect> BaseTypes { get => CSAspectCompilerHelper.GetDescendingNodesOfType<BaseTypeAspect>(this, false); }

        internal BaseListAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.BaseList(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class BaseTypeAspect : CSAspectNode
    {
        internal NameAspect Type => (NameAspect)ChildCSAspectNodes.FirstOrDefault();
        internal BaseTypeAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.BaseType(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }
}