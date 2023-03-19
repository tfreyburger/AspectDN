// Author:
//
// T. Freyburger (t.freyburger@gmail.com)
//
// Copyright (c)  Thierry Freyburger
//
// Licensed under the GPLV3 license.
////
using AspectDN.Aspect.Weaving.IConcerns;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AspectDN.Aspect.Compilation.CS.CS5
{
    internal class PrototypeTypesModifier
    {
        internal void ApplyReferencedPrototypeOrAdviceTypes(CSWorkspace csWorkspace)
        {
            var localPrototypeTypes = csWorkspace.GetAllTypeSymbols().Where(t => RoslynHelper.GetAttributeDatasFromSymbol(csWorkspace.CSharpCompilation, t, typeof(PrototypeTypeDeclarationAttribute)).Any() && t.DeclaringSyntaxReferences.Any() && (t.TypeKind != TypeKind.Delegate && t.TypeKind != TypeKind.Enum));
            _ApplyReferencedPrototypeOrAdviceTypes(csWorkspace, localPrototypeTypes);
        }

        void _ApplyReferencedPrototypeOrAdviceTypes(CSWorkspace csWorkspace, IEnumerable<ITypeSymbol> localPrototypeTypes)
        {
            var changes = new List<(SyntaxNode oldNode, SyntaxNode newNode)>();
            foreach (var localPrototypeType in localPrototypeTypes)
            {
                var change = _ApplyReferencedPrototypeOrAdviceTypes(csWorkspace, localPrototypeType);
                if (change.oldNode != null)
                    changes.Add(change);
            }

            csWorkspace.ReplaceNodes(changes);
        }

        (SyntaxNode oldNode, SyntaxNode newNode) _ApplyReferencedPrototypeOrAdviceTypes(CSWorkspace csWorkspace, ITypeSymbol localPrototypeType)
        {
            var referencedTypes = csWorkspace.GetAllReferencedConcernTypesFromType((INamedTypeSymbol)localPrototypeType);
            var prototypeOrAdviceTypes = RoslynHelper.GetReferencddAdviceOrPrototypeTypes(csWorkspace, referencedTypes, localPrototypeType.ToDisplayString());
            var oldLocalPrototypeTypeSyntax = (TypeDeclarationSyntax)localPrototypeType.DeclaringSyntaxReferences.First().GetSyntax();
            var newLocalPrototypeTypeSyntax = oldLocalPrototypeTypeSyntax;
            if (prototypeOrAdviceTypes.referencedPrototypeTypenames.Any())
            {
                var attributeList = CSAspectCompilerHelper.BuildReferencedPrototypeTypeAttribute(prototypeOrAdviceTypes.referencedPrototypeTypenames.ToArray());
                newLocalPrototypeTypeSyntax = newLocalPrototypeTypeSyntax.AddAttributeLists(attributeList);
            }

            if (prototypeOrAdviceTypes.referencedAdviceTypenames.Any())
            {
                throw new NotSupportedException();
            }

            if (oldLocalPrototypeTypeSyntax != newLocalPrototypeTypeSyntax)
                return (oldLocalPrototypeTypeSyntax, newLocalPrototypeTypeSyntax);
            return (null, null);
        }
    }
}