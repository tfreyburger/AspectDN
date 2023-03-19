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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace AspectDN.Aspect.Compilation.CS
{
    internal class CompilationUnitAspect : CSAspectNode
    {
        AspectTree _AspectTree;
        internal IEnumerable<UsingDirectiveAspect> Usings { get => CSAspectCompilerHelper.GetDescendingNodesOfType<UsingDirectiveAspect>(this, false); }

        internal IEnumerable<PackageMemberAspect> PackageMembers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<PackageMemberAspect>(this, false); }

        internal IEnumerable<PrototypeTypeMappingAspect> PrototypeTypeMappings { get => CSAspectCompilerHelper.GetDescendingNodesOfType<PrototypeTypeMappingAspect>(this, true); }

        internal override AspectTree AspectTree
        {
            get
            {
                return _AspectTree;
            }
        }

        internal CompilationUnitAspect(AspectTree tree, ISynToken token) : base(token)
        {
            _AspectTree = tree;
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.CompilationUnit(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }
}