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
    internal class IndexerDeclarationAspect : TypeMemberDeclarationAspect
    {
        internal IEnumerable<AttributeSectionAspect> AttributeSections { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeSectionAspect>(this, false); }

        internal IEnumerable<ModifierAspect> Modifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ModifierAspect>(this, false).ToList(); }

        internal TypeAspect Type { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeAspect>(this).FirstOrDefault(); }

        internal IndexerExplicitTypeAspect IndexerExplicitType { get => CSAspectCompilerHelper.GetDescendingNodesOfType<IndexerExplicitTypeAspect>(this).FirstOrDefault(); }

        internal FormalParameterListAspect FormalParameterList { get => CSAspectCompilerHelper.GetDescendingNodesOfType<FormalParameterListAspect>(this, false).FirstOrDefault(); }

        internal IEnumerable<PropertyAccessorAspect> Accessors { get => CSAspectCompilerHelper.GetDescendingNodesOfType<PropertyAccessorAspect>(this, false); }

        internal IndexerDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.IndexerDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class IndexerExplicitTypeAspect : CSAspectNode
    {
        internal IndexerExplicitTypeAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            throw AspectDNErrorFactory.GetException("NotImplementedException");
        }
    }
}