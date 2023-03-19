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
    internal class TypeParameterConstraintsClauseAspect : CSAspectNode
    {
        internal IdentifierNameAspect Identifier { get => (IdentifierNameAspect)ChildAspectNodes[0]; }

        internal IEnumerable<TypeParameterConstraintAspect> TypeParameterConstraints { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeParameterConstraintAspect>(this); }

        internal TypeParameterConstraintsClauseAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.TypeParameterConstraintsClause(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal abstract class TypeParameterConstraintAspect : CSAspectNode
    {
        internal TypeParameterConstraintAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class TypeParameterConstraintConstructutorAspect : TypeParameterConstraintAspect
    {
        internal TypeParameterConstraintConstructutorAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.TypeParameterConstraintContsructor(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class TypeParameterConstraintTypeAspect : TypeParameterConstraintAspect
    {
        internal TypeAspect Type => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeAspect>(this, false).FirstOrDefault();
        internal TypeParameterConstraintTypeAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.TypeParameterConstraintType(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class TypeParameterConstraintClassOrStructAspect : TypeParameterConstraintAspect
    {
        internal KeywordAspect KeyWord { get; }

        internal TypeParameterConstraintClassOrStructAspect(ISynToken token, KeywordAspect keyword) : base(token)
        {
            KeyWord = keyword;
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.TypeParameterConstraintClassOrStruct(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }
}