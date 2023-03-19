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
    internal class MethodDeclarationAspect : TypeMemberDeclarationAspect
    {
        internal IEnumerable<AttributeSectionAspect> AttributeSections { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeSectionAspect>(this); }

        internal IEnumerable<ModifierAspect> Modifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ModifierAspect>(this, false).ToList(); }

        internal ReturnTypeAspect ReturnType { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ReturnTypeAspect>(this).FirstOrDefault(); }

        internal MemberNameAspect MemberName { get => CSAspectCompilerHelper.GetDescendingNodesOfType<MemberNameAspect>(this).FirstOrDefault(); }

        internal TypeParameterListAspect TypeParameterList { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeParameterListAspect>(this, false).FirstOrDefault(); }

        internal FormalParameterListAspect ParameterList { get => CSAspectCompilerHelper.GetDescendingNodesOfType<FormalParameterListAspect>(this).FirstOrDefault(); }

        internal IEnumerable<TypeParameterConstraintsClauseAspect> TypeParameterConstraintsClauses { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeParameterConstraintsClauseAspect>(this); }

        internal BlockAspect Block { get => CSAspectCompilerHelper.GetDescendingNodesOfType<BlockAspect>(this).FirstOrDefault(); }

        internal MethodDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.MethodDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class MemberNameAspect : CSAspectNode
    {
        internal TypeAspect Type { get => ChildAspectNodes.Count > 0 && ChildAspectNodes.First() is NamespaceOrTypenameAspect ? (TypeAspect)ChildAspectNodes.First().ChildAspectNodes.First() : null; }

        internal NameAspect Identifier { get => ChildAspectNodes.Count > 0 && ChildAspectNodes.First() is NamespaceOrTypenameAspect ? (NameAspect)ChildAspectNodes.First().ChildAspectNodes.Last() : (NameAspect)ChildAspectNodes.First(); }

        internal MemberNameAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            throw AspectDNErrorFactory.GetException("NotImplementedException");
        }
    }
}