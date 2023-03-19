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
    internal abstract class EventDeclarationAspect : TypeMemberDeclarationAspect
    {
        internal EventDeclarationAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class EventFieldDeclarationAspect : TypeMemberDeclarationAspect
    {
        internal IEnumerable<AttributeSectionAspect> AttributeSections { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeSectionAspect>(this); }

        internal IEnumerable<ModifierAspect> Modifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ModifierAspect>(this, false).ToList(); }

        internal TypeAspect Type { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeAspect>(this).FirstOrDefault(); }

        internal IEnumerable<VariableDeclaratorAspect> VariableDeclarators { get => CSAspectCompilerHelper.GetDescendingNodesOfType<VariableDeclaratorAspect>(this, false); }

        internal EventFieldDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.EventFieldDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class EventPropertyDeclarationAspect : TypeMemberDeclarationAspect
    {
        internal IEnumerable<AttributeSectionAspect> AttributeSections { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeSectionAspect>(this); }

        internal IEnumerable<ModifierAspect> Modifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ModifierAspect>(this, false).ToList(); }

        internal CSAspectNode Type { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeAspect, NameAspect>(this, false).FirstOrDefault(); }

        internal MemberNameAspect MemberName { get => CSAspectCompilerHelper.GetDescendingNodesOfType<MemberNameAspect>(this, false).FirstOrDefault(); }

        internal IEnumerable<EventAccessorAspect> EventAccessors { get => CSAspectCompilerHelper.GetDescendingNodesOfType<EventAccessorAspect>(this, false); }

        internal EventPropertyDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.EventPropertyDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class EventAccessorAspect : CSAspectNode
    {
        internal IEnumerable<AttributeSectionAspect> AttributeSections { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeSectionAspect>(this); }

        internal EventAccessorTypesAspect AccessorType { get; }

        internal BlockAspect Block { get => CSAspectCompilerHelper.GetDescendingNodesOfType<BlockAspect>(this).FirstOrDefault(); }

        internal EventAccessorAspect(ISynToken token, EventAccessorTypesAspect accessorType) : base(token)
        {
            AccessorType = accessorType;
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.EventAccessor(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal enum EventAccessorTypesAspect
    {
        Add,
        Remove
    }
}