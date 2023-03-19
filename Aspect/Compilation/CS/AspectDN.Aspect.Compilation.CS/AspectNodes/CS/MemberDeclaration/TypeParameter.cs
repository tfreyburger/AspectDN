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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AspectDN.Aspect.Compilation.CS
{
    internal class TypeParameterListAspect : CSAspectNode
    {
        internal IEnumerable<TypeParameterItemAspect> TypeParameters { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeParameterItemAspect>(this); }

        internal TypeParameterListAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.TypeParameterList(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class TypeParameterItemAspect : CSAspectNode
    {
        internal IEnumerable<AttributeSectionAspect> AttributeSections { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeSectionAspect>(this); }

        internal IdentifierNameAspect IdentifierName => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this, false).FirstOrDefault();
        internal TypeParameterItemAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.TypeParameterItem(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }
}