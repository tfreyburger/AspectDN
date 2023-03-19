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
using AspectDN.Aspect.Compilation.Foundation;
using TokenizerDN.Common.SourceAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AspectDN.Aspect.Compilation.CS
{
    internal class PackageDeclarationAspect : PackageMemberAspect
    {
        internal PackageDeclarationAspect ParentPackage
        {
            get
            {
                return CSAspectCompilerHelper.GetAscendingNodesOfType<PackageDeclarationAspect>(this, false).FirstOrDefault();
            }
        }

        internal string PackageFullName
        {
            get
            {
                var sb = new StringBuilder(PackageName);
                var parentPackage = ParentPackage;
                while (parentPackage != null)
                {
                    sb.Insert(0, '.').Insert(0, parentPackage.PackageName);
                    parentPackage = CSAspectCompilerHelper.GetAscendingNodesOfType<PackageDeclarationAspect>(parentPackage, false).FirstOrDefault();
                }

                return sb.ToString();
            }
        }

        internal string PackageName
        {
            get
            {
                return this.NameAspect.GetName();
            }
        }

        internal NameAspect NameAspect { get => ChildAspectNodes.OfType<NameAspect>().FirstOrDefault(); }

        internal IEnumerable<UsingNamespaceDirectiveAspect> UsingDirectives { get => CSAspectCompilerHelper.GetDescendingNodesOfType<UsingNamespaceDirectiveAspect>(this, false); }

        internal IEnumerable<PackageMemberAspect> PackageMembers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<PackageMemberAspect>(this, false); }

        internal PackageDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PackageDeclarationSyntax(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal abstract class PackageMemberAspect : CSAspectNode
    {
        internal PackageMemberAspect(ISynToken token) : base(token)
        {
        }
    }
}