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
    internal abstract class TypeAspect : NameAspect
    {
        internal TypeAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class TypeNameAspect : TypeAspect
    {
        internal TypeNameAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.TypeName(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }

        internal override string GetName()
        {
            return ((NameAspect)ChildAspectNodes[0]).GetName();
        }
    }

    internal class VoidTypeAspect : TypeAspect
    {
        public VoidTypeAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.VoidType(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }

        internal override string GetName()
        {
            return "void";
        }
    }
}