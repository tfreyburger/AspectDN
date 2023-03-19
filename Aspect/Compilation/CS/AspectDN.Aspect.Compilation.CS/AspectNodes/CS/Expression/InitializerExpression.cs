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
    internal abstract class InitiatializerExpressionAspect : CSAspectNode
    {
        internal bool IsComplex
        {
            get
            {
                return CSAspectCompilerHelper.GetAscendingNodesOfType<InitiatializerExpressionAspect>(this).Count() > 0;
            }
        }

        internal InitiatializerExpressionAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class ObjectInitializerAspect : InitiatializerExpressionAspect
    {
        internal IEnumerable<MemberInitializerAspect> MemberInitializers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<MemberInitializerAspect>(this); }

        internal ObjectInitializerAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ObjectIntializerExpression(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class MemberInitializerAspect : CSAspectNode
    {
        internal IdentifierNameAspect Left { get => (IdentifierNameAspect)this[0]; }

        internal CSAspectNode Right { get => (CSAspectNode)this[1]; }

        internal MemberInitializerAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.MemberInitializer(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class CollectionInitializerAspect : InitiatializerExpressionAspect
    {
        internal IEnumerable<CSAspectNode> Elements { get => this.ChildCSAspectNodes; }

        internal CollectionInitializerAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.CollectionInitializerExpression(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class ElementInitializerAspect : CSAspectNode
    {
        internal CSAspectNode Content { get => this[0]; }

        internal ElementInitializerAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ElementInitializer(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }
}