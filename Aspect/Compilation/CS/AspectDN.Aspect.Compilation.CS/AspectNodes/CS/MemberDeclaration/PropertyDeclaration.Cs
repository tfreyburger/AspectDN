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
    internal class PropertyDeclarationAspect : TypeMemberDeclarationAspect
    {
        internal IEnumerable<AttributeSectionAspect> AttributeSections { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeSectionAspect>(this); }

        internal IEnumerable<ModifierAspect> Modifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ModifierAspect>(this, false).ToList(); }

        internal TypeAspect Type { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeAspect>(this).FirstOrDefault(); }

        internal MemberNameAspect MemberName { get => CSAspectCompilerHelper.GetDescendingNodesOfType<MemberNameAspect>(this, false).FirstOrDefault(); }

        internal IEnumerable<PropertyAccessorAspect> Accessors { get => CSAspectCompilerHelper.GetDescendingNodesOfType<PropertyAccessorAspect>(this, false); }

        internal PropertyDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PropertyDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class PropertyAccessorAspect : CSAspectNode
    {
        internal IEnumerable<AttributeSectionAspect> AttributeSections { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeSectionAspect>(this, false); }

        internal IEnumerable<ModifierAspect> Modifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ModifierAspect>(this, false); }

        internal BlockAspect Block { get => CSAspectCompilerHelper.GetDescendingNodesOfType<BlockAspect>(this, false).FirstOrDefault(); }

        internal PropertyAccessorTypesAspect AccessorType { get; }

        internal PropertyAccessorAspect(ISynToken token, PropertyAccessorTypesAspect accessorType) : base(token)
        {
            AccessorType = accessorType;
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PropertyAccessor(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal enum PropertyAccessorTypesAspect
    {
        Get,
        Set
    }
}