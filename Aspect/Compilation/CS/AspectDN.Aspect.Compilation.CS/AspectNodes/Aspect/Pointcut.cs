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
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AspectDN.Aspect.Compilation.CS
{
    internal class PointcutDeclarationAspect : PackageMemberAspect
    {
        internal string Name
        {
            get
            {
                string name = null;
                var identifier = this.ChildAspectNodes.Where(t => t.SynToken.Name == "identifier").FirstOrDefault();
                if (identifier != null)
                    name = identifier.SynToken.Value;
                return name;
            }
        }

        internal string Fullname
        {
            get
            {
                var sb = new StringBuilder(Name);
                var parentPackage = CSAspectCompilerHelper.GetAscendingNodesOfType<PackageDeclarationAspect>(this).FirstOrDefault();
                if (parentPackage != null)
                    sb.Insert(0, '.').Insert(0, parentPackage.PackageName);
                return sb.ToString();
            }
        }

        internal IdentifierNameAspect Identifier { get => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this).FirstOrDefault(); }

        internal PointcutTypeAspect PointcutType { get => CSAspectCompilerHelper.GetDescendingNodesOfType<PointcutTypeAspect>(this).FirstOrDefault(); }

        internal PointcutExpressionAspect Expression { get => CSAspectCompilerHelper.GetDescendingNodesOfType<PointcutExpressionAspect>(this).FirstOrDefault(); }

        internal PointcutDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PointcutDeclarationAspect(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class PointcutExpressionAspect : CSAspectNode
    {
        internal ExpressionAspect Expression => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false).FirstOrDefault();
        internal PointcutExpressionAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return Expression.GetSyntaxNode();
        }
    }

    internal class PointcutTypeAspect : CSAspectNode
    {
        internal IdentifierNameAspect IdentifierName => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this, false).FirstOrDefault();
        internal PointcutTypeAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PointcutType(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }
}