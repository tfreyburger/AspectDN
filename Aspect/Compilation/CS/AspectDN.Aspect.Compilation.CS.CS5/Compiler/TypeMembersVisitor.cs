// Author:
//
// T. Freyburger (t.freyburger@gmail.com)
//
// Copyright (c)  Thierry Freyburger
//
// Licensed under the GPLV3 license.
////
using AspectDN.Common;
using AspectDN.Aspect.Weaving.IConcerns;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Collections.Immutable;

namespace AspectDN.Aspect.Compilation.CS.CS5
{
    internal class TypeMembersVisitor
    {
        ITypeSymbol _PrototypeTypeSymbol;
        IEnumerable<ITypeSymbol> _AdviceTypeSymbols;
        CSWorkspace CSWorkspace;
        List<(ITypeSymbol advice, IEnumerable<AttributeListSyntax> prototypeItemMappingSyntaxes)> _PrototypeItemMappingSyntaxes;
        List<(ISymbol symbol, MemberDeclarationSyntax syntax)> _MembersSyntaxes;
        public TypeMembersVisitor(CSWorkspace cSWorkspace, ITypeSymbol prototypeTypeSymbol, IEnumerable<ITypeSymbol> adviceTypeSymbols)
        {
            _PrototypeTypeSymbol = prototypeTypeSymbol;
            _AdviceTypeSymbols = adviceTypeSymbols;
            CSWorkspace = cSWorkspace;
            _PrototypeItemMappingSyntaxes = new List<(ITypeSymbol advice, IEnumerable<AttributeListSyntax> prototypeItemMappingSyntaxes)>();
            _MembersSyntaxes = new List<(ISymbol symbol, MemberDeclarationSyntax syntax)>();
        }

        internal TypeMembersVisitor Visit()
        {
            _GetPrototypeTypeMembers();
            _GetAdviceAndPrototypeItemMappings();
            return this;
        }

        internal (IEnumerable<MemberDeclarationSyntax> memberSyntaxes, IEnumerable<AttributeListSyntax> prototypeItemMappingSyntaxes) GetSyntaxNodes(INamedTypeSymbol adviceType)
        {
            var memberSyntaxes = new List<MemberDeclarationSyntax>();
            var prototypeItemMappingsSyntaxes = new List<AttributeListSyntax>();
            foreach (var member in _MembersSyntaxes)
            {
                if (member.symbol.ContainingType.ToDisplayString() == adviceType.ToDisplayString())
                    continue;
                var syntax = member.syntax;
                if (member.symbol.Name == ".ctor")
                    syntax = ((ConstructorDeclarationSyntax)member.syntax).WithIdentifier(SyntaxFactory.Identifier(adviceType.Name));
            }

            if (!_PrototypeTypeSymbol.IsStatic && _PrototypeTypeSymbol.TypeKind != TypeKind.Interface && _PrototypeTypeSymbol.TypeKind != TypeKind.Enum)
            {
                var thisSyntax = SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(RoslynHelper.ParseTypeName(_PrototypeTypeSymbol), SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(_PrototypeTypeSymbol.Name))))).WithModifiers(SyntaxFactory.TokenList(new[]{SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword)}));
                thisSyntax = thisSyntax.WithAttributeLists(new SyntaxList<AttributeListSyntax>(_GetAdviceMemberAttributeList(typeof(AdviceMemberOrign), _PrototypeTypeSymbol)));
                thisSyntax = thisSyntax.AddAttributeLists(CSAspectCompilerHelper.GetAttributeListOfAttributeType(typeof(PrototypeItemDeclarationAttribute)).ToArray());
                memberSyntaxes.Add(thisSyntax);
            }

            foreach (var attribute in _PrototypeItemMappingSyntaxes)
            {
                if (attribute.advice.ToDisplayString() == adviceType.ToDisplayString())
                    continue;
                prototypeItemMappingsSyntaxes.AddRange(attribute.prototypeItemMappingSyntaxes);
            }

            return (memberSyntaxes, prototypeItemMappingsSyntaxes);
        }

        internal IEnumerable<MemberDeclarationSyntax> GetAdviceMemberSyntaxNodes()
        {
            var memberSyntaxes = new List<MemberDeclarationSyntax>();
            foreach (var member in _MembersSyntaxes)
            {
                if (member.symbol.ContainingType.ToDisplayString() == _PrototypeTypeSymbol.ToDisplayString())
                    continue;
                if (member.symbol.GetAttributes().Any(t => t.AttributeClass.ToDisplayString() == typeof(PrototypeItemDeclarationAttribute).FullName))
                    continue;
                var syntax = member.syntax;
                if (member.symbol.Name == ".ctor")
                    syntax = ((ConstructorDeclarationSyntax)member.syntax).WithIdentifier(SyntaxFactory.Identifier(_PrototypeTypeSymbol.Name));
                if (member.symbol is IMethodSymbol)
                {
                    if (_PrototypeTypeSymbol.TypeKind == TypeKind.Interface)
                    {
                        syntax = ((MethodDeclarationSyntax)syntax).WithBody(null).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)).WithModifiers(new SyntaxTokenList());
                    }
                    else
                    {
                        if (member.symbol.Name.Contains("."))
                        {
                            syntax = syntax.WithModifiers(new SyntaxTokenList());
                        }
                    }
                }

                syntax = syntax.WithAttributeLists(new SyntaxList<AttributeListSyntax>(_GetAdviceMemberAttributeList(typeof(AdviceMemberOrign), member.symbol.ContainingType)));
                memberSyntaxes.Add(syntax);
            }

            return memberSyntaxes;
        }

        void _GetPrototypeTypeMembers()
        {
            var typeSymbol = _PrototypeTypeSymbol;
            var withConstructors = true;
            var members = typeSymbol.GetMembers().Where(t => !t.IsImplicitlyDeclared);
            _MembersSyntaxes.AddRange(new SymbolMemberToSyntaxConverter(typeof(PrototypeItemDeclarationAttribute)).GetMemberDeclarationSyntaxes(members, withConstructors).ToList());
        }

        void _GetAdviceAndPrototypeItemMappings()
        {
            foreach (var adviceTypeSymbol in _AdviceTypeSymbols.Where(t => t.AllInterfaces.Any(i => i.ToDisplayString() == typeof(ITypeMembersAdviceDeclaration).FullName || i.ToDisplayString() == typeof(IInterfaceMembersAdviceDeclaration).FullName)))
            {
                var adviceMembers = adviceTypeSymbol.GetMembers().Where(t => !t.IsImplicitlyDeclared);
                _MembersSyntaxes.AddRange(new SymbolMemberToSyntaxConverter(typeof(PrototypeItemDeclarationAttribute)).GetMemberDeclarationSyntaxes(adviceMembers, true).ToList());
                var aspect = (ITypeSymbol)adviceTypeSymbol.GetAttributes().First(t => t.AttributeClass.ToDisplayString() == typeof(AspectParentAttribute).FullName).ConstructorArguments.First().Value;
                var attributeDatas = aspect.GetAttributes().Where(t => t.AttributeClass.ToDisplayString() == typeof(PrototypeItemMappingAttribute).FullName);
                _GetPrototypeItemMappingSyntaxes(adviceTypeSymbol, attributeDatas);
            }
        }

        void _GetPrototypeItemMappingSyntaxes(ITypeSymbol adviceType, IEnumerable<AttributeData> attributeDatas)
        {
            var syntaxes = new List<AttributeListSyntax>();
            foreach (var attributeData in attributeDatas)
            {
                if (PrototypeItemMappingSourceKinds.AdviceType != (PrototypeItemMappingSourceKinds)attributeData.ConstructorArguments[0].Value || PrototypeItemMappingSourceKinds.AdviceType != (PrototypeItemMappingSourceKinds)attributeData.ConstructorArguments[0].Value)
                    continue;
                var sourceKind = Enum.GetName(typeof(PrototypeItemMappingSourceKinds), (PrototypeItemMappingSourceKinds)attributeData.ConstructorArguments[0].Value);
                var sourceValue = attributeData.ConstructorArguments[1].Value;
                ExpressionSyntax source = null;
                switch (attributeData.ConstructorArguments[1].Value)
                {
                    case string s:
                        source = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(s));
                        break;
                    case ITypeSymbol type:
                        source = SyntaxFactory.TypeOfExpression(RoslynHelper.ParseTypeName(type));
                        break;
                    default:
                        throw new NotImplementedException();
                }

                var targetKind = Enum.GetName(typeof(PrototypeItemMappingTargetKinds), (PrototypeItemMappingTargetKinds)attributeData.ConstructorArguments[2].Value);
                var targetName = (string)attributeData.ConstructorArguments[3].Value;
                var syntax = CSAspectCompilerHelper.PrototypeItemMappingAttributeList(sourceKind, source, targetKind, targetName);
                syntaxes.Add(syntax);
            }

            _PrototypeItemMappingSyntaxes.Add((adviceType, syntaxes));
        }

        SyntaxList<AttributeListSyntax> _GetAdviceMemberAttributeList(Type attributeType, ITypeSymbol memberSynmbol)
        {
            var typeOf = RoslynHelper.ParseTypeName(memberSynmbol);
            return SyntaxFactory.SingletonList<AttributeListSyntax>(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(SyntaxFactory.Attribute(CSAspectCompilerHelper.ParseName(attributeType.FullName)).WithArgumentList(SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList<AttributeArgumentSyntax>(new SyntaxNodeOrToken[]{SyntaxFactory.AttributeArgument(SyntaxFactory.TypeOfExpression(typeOf)), SyntaxFactory.Token(SyntaxKind.CommaToken), SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(memberSynmbol.ToDisplayString())))}))))));
        }
    }
}