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
using AspectDN.Aspect.Weaving.IConcerns;
using AspectDN.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AspectDN.Aspect.Compilation.CS.CS5
{
    internal class PrototypeMemberModifierModifier
    {
        internal void ApplyChanges(IEnumerable<ITypeSymbol> _Advices, CSWorkspace csWorkspace)
        {
            var changes = new List<(SyntaxNode oldNode, SyntaxNode newNode)>();
            foreach (var advice in _Advices)
            {
                var prototypeMembers = RoslynHelper.GetSymbolMembers(advice, false).Where(t => t.GetAttributes().Any(a => a.AttributeClass.ToDisplayString() == typeof(PrototypeItemDeclarationAttribute).FullName));
                if (prototypeMembers.Any())
                {
                    foreach (var prototypeMember in prototypeMembers)
                    {
                        if (prototypeMember is IMethodSymbol && ((IMethodSymbol)prototypeMember).AssociatedSymbol != null)
                            continue;
                        changes.Add(_ApplChange(prototypeMember));
                    }
                }
            }

            csWorkspace.ReplaceNodes(changes);
        }

        (SyntaxNode oldNode, SyntaxNode newNode) _ApplChange(ISymbol prototypeMember)
        {
            var oldNode = prototypeMember.DeclaringSyntaxReferences.First().GetSyntax().AncestorsAndSelf().OfType<MemberDeclarationSyntax>().First();
            SyntaxTokenList tokens = new SyntaxTokenList();
            switch (prototypeMember)
            {
                case IFieldSymbol fieldSymbol:
                    tokens = _GetTypeModifiiers(fieldSymbol.Type);
                    break;
                case IPropertySymbol propertySymbol:
                    tokens = _GetTypeModifiiers(propertySymbol.Type);
                    break;
                case IMethodSymbol methodSymbol:
                    tokens = _GetTypeModifiiers(methodSymbol.ReturnType);
                    break;
                case IEventSymbol eventSymbol:
                    tokens = _GetTypeModifiiers(eventSymbol.Type);
                    break;
            }

            if (prototypeMember.IsStatic)
                tokens = tokens.Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
            if (prototypeMember.IsAbstract)
                tokens = tokens.Add(SyntaxFactory.Token(SyntaxKind.AbstractKeyword));
            var newNode = oldNode.WithModifiers(tokens);
            return (oldNode, newNode);
        }

        SyntaxTokenList _GetTypeModifiiers(ISymbol symbol)
        {
            var tokens = new SyntaxTokenList();
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.NotApplicable:
                    break;
                case Accessibility.Private:
                    tokens = tokens.Add(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
                    break;
                case Accessibility.ProtectedAndInternal:
                    tokens = tokens.Add(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
                    tokens = tokens.Add(SyntaxFactory.Token(SyntaxKind.InternalKeyword));
                    break;
                case Accessibility.Protected:
                    tokens = tokens.Add(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
                    break;
                case Accessibility.Internal:
                    tokens = tokens.Add(SyntaxFactory.Token(SyntaxKind.InternalKeyword));
                    break;
                case Accessibility.ProtectedOrInternal:
                    throw new NotImplementedException();
                case Accessibility.Public:
                    tokens = tokens.Add(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
                    break;
                default:
                    break;
            }

            return tokens;
        }
    }
}