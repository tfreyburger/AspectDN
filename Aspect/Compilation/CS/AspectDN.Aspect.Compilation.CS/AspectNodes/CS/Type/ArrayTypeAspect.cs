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
    internal class ArrayTypeAspect : TypeAspect
    {
        internal TypeAspect TypeAspect => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeAspect>(this, false).FirstOrDefault();
        internal IEnumerable<RankSpecifierAspect> RankSpecifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<RankSpecifierAspect>(this); }

        internal ArrayTypeAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ArrayType(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }

        internal override string GetName()
        {
            StringBuilder sb = new StringBuilder().Append(TypeAspect.GetName());
            foreach (var rankSpecifier in RankSpecifiers)
                sb.Append(rankSpecifier.ToString());
            return sb.ToString();
        }
    }
}