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
    internal class NullableTypeAspect : TypeAspect
    {
        internal TypeAspect Type => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeAspect>(this, false).FirstOrDefault();
        internal NullableTypeAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.NullableType(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }

        internal override string GetName()
        {
            return Type.GetName();
        }
    }

    internal class AnonymousTypeAspect : TypeAspect
    {
        internal AnonymousTypeAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AnonymousTypeAspect(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }

        internal override string GetName()
        {
            return "var";
        }
    }
}