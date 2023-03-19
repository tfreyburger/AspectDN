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
    internal class ThsDeclarationAdviceModifier
    {
        internal void ApplyChanges(Dictionary<string, List<ITypeSymbol>> _PrototypeTypeAvicesList, CSWorkspace csWorkspace)
        {
            var changes = new List<(SyntaxNode oldNode, SyntaxNode newNode)>();
            foreach (var prototypeTypeAvices in _PrototypeTypeAvicesList)
            {
                var protototypeTypeSymbol = csWorkspace.GetTypeByMetadataName(prototypeTypeAvices.Key);
                if (protototypeTypeSymbol == null)
                    continue;
                changes.AddRange(_ApplChanges(csWorkspace, protototypeTypeSymbol, prototypeTypeAvices.Value));
            }

            csWorkspace.ReplaceNodes(changes);
        }

        List<(SyntaxNode oldNode, SyntaxNode newNode)> _ApplChanges(CSWorkspace csWorkspace, ITypeSymbol prototypeTypeSymbol, IEnumerable<ITypeSymbol> adviceSymbols)
        {
            var prototypeTypeMemberDeclarationSyntaxes = _GetPrototypeTypeMemberDeclaraionSyntaxes(prototypeTypeSymbol);
            var prototypeTypeMappingDeclarationSyntaxes = _GetPrototypeItemTypeMappings(prototypeTypeSymbol);
            var referencedPrototypeTypeAttributeValues = _GetReferencedPrototypeTypesAttributeValues(csWorkspace, prototypeTypeSymbol);
            var baseListSyntax = _GetBaseTypeDeclarationSyntax(prototypeTypeSymbol);
            return _GetChanges(csWorkspace, prototypeTypeSymbol, adviceSymbols, prototypeTypeMemberDeclarationSyntaxes, prototypeTypeMappingDeclarationSyntaxes, baseListSyntax, referencedPrototypeTypeAttributeValues);
        }

        IEnumerable<string> _GetReferencedPrototypeTypesAttributeValues(CSWorkspace cSWorkspace, ITypeSymbol typeSymbol)
        {
            var attributeData = RoslynHelper.GetAttributeDatasFromSymbol(cSWorkspace, typeSymbol, typeof(ReferencedPrototypeTypesAttribute)).FirstOrDefault();
            if (attributeData != null)
            {
                return ((object[])RoslynHelper.GetAttributeConstructorArgValues(attributeData.ConstructorArguments.First())).Select(t => ((ITypeSymbol)t).ToDisplayString());
            }

            return null;
        }

        IEnumerable<(ISymbol member, MemberDeclarationSyntax memberDeclaration)> _GetPrototypeTypeMemberDeclaraionSyntaxes(ITypeSymbol prototypeTypeSymbol)
        {
            var members = RoslynHelper.GetSymbolMembers(prototypeTypeSymbol, false);
            var memberDeclarationsSyntaxes = new SymbolMemberToSyntaxConverter(typeof(PrototypeItemDeclarationAttribute)).GetMemberDeclarationSyntaxes(members, true);
            return memberDeclarationsSyntaxes;
        }

        IEnumerable<ITypeSymbol> _GetBaseTypes(ITypeSymbol prototypeTypeSymbol)
        {
            var baseTypes = new List<ITypeSymbol>();
            if (RoslynHelper.HasBaseType(prototypeTypeSymbol))
                baseTypes.Add(prototypeTypeSymbol.BaseType);
            baseTypes.AddRange(prototypeTypeSymbol.Interfaces);
            return baseTypes;
        }

        BaseListSyntax _GetBaseTypeDeclarationSyntax(ITypeSymbol prototypeTypeSymbol)
        {
            var baseTypes = _GetBaseTypes(prototypeTypeSymbol);
            switch (baseTypes.Count())
            {
                case 0:
                    return null;
                case 1:
                    return SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(SyntaxFactory.SimpleBaseType(RoslynHelper.ParseTypeName(baseTypes.First()))));
                default:
                    var syntaxNodeOrTokens = new SyntaxNodeOrToken[baseTypes.Count()];
                    int i = 0;
                    foreach (var baseType in baseTypes)
                    {
                        if (i != 0)
                            syntaxNodeOrTokens[i++] = SyntaxFactory.Token(SyntaxKind.CommaToken);
                        syntaxNodeOrTokens[i++] = SyntaxFactory.SimpleBaseType(RoslynHelper.ParseTypeName(baseType));
                    }

                    return SyntaxFactory.BaseList(SyntaxFactory.SeparatedList<BaseTypeSyntax>(syntaxNodeOrTokens));
            }
        }

        List<(SyntaxNode oldNode, SyntaxNode newNode)> _GetChanges(CSWorkspace csWorkSpace, ITypeSymbol prototypeTypeSymbol, IEnumerable<ITypeSymbol> adviceSymbols, IEnumerable<(ISymbol member, MemberDeclarationSyntax memberDeclaration)> memberDeclarationSyntaxes, IEnumerable<(AttributeData, AttributeSyntax)> prototypeTypeMappingDeclarationSyntaxes, BaseListSyntax baseListSyntax, IEnumerable<string> referencedPrototypeTypenames)
        {
            var changes = new List<(SyntaxNode oldNode, SyntaxNode newNode)>();
            foreach (var adviceSymbol in adviceSymbols)
            {
                var addedMemberDeclarationSyntaxes = _GetAddedMemberDeclarationSyntaxes(prototypeTypeSymbol, adviceSymbol, memberDeclarationSyntaxes);
                var change = _GetAdviceChange(csWorkSpace, prototypeTypeSymbol, adviceSymbol, addedMemberDeclarationSyntaxes, baseListSyntax, referencedPrototypeTypenames);
                if (change.oldNode != null)
                    changes.Add(change);
                var aspectSymbol = (ITypeSymbol)adviceSymbol.GetAttributes().First(t => t.AttributeClass.ToDisplayString() == typeof(AspectParentAttribute).FullName).ConstructorArguments[0].Value;
                if (aspectSymbol == null)
                    continue;
                change = _GetAspectChange(prototypeTypeSymbol, aspectSymbol, addedMemberDeclarationSyntaxes, prototypeTypeMappingDeclarationSyntaxes);
                if (change.oldNode != null)
                    changes.Add(change);
            }

            return changes;
        }

        (SyntaxNode oldNode, SyntaxNode newNode) _GetAdviceChange(CSWorkspace cSWorkspace, ITypeSymbol prototypeTypeSymbol, ITypeSymbol adviceSymbol, IEnumerable<(ISymbol member, MemberDeclarationSyntax memberDeclaration)> memberDeclarationSyntaxes, BaseListSyntax baseListSyntax, IEnumerable<string> referencedPrototypeTypeAttributeValues)
        {
            var oldAdviceSyntax = (TypeDeclarationSyntax)adviceSymbol.DeclaringSyntaxReferences.First().GetSyntax();
            var newAdviceSyntax = oldAdviceSyntax;
            if (baseListSyntax != null)
            {
                var newBaseListSyntax = baseListSyntax.AddTypes(newAdviceSyntax.BaseList.Types.ToArray());
                newAdviceSyntax = newAdviceSyntax.WithBaseList(newBaseListSyntax);
            }

            if (memberDeclarationSyntaxes.Any())
                newAdviceSyntax = newAdviceSyntax.AddMembers(memberDeclarationSyntaxes.Select(t => t.memberDeclaration).ToArray());
            if (!prototypeTypeSymbol.IsStatic && prototypeTypeSymbol.TypeKind != TypeKind.Interface && prototypeTypeSymbol.TypeKind != TypeKind.Enum)
            {
                var thisSyntax = SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(RoslynHelper.ParseTypeName(prototypeTypeSymbol), SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier("&this"))))).WithModifiers(SyntaxFactory.TokenList(new[]{SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.StaticKeyword)}));
                thisSyntax = thisSyntax.AddAttributeLists(CSAspectCompilerHelper.ExcludedMemberAttribute());
                newAdviceSyntax = newAdviceSyntax.AddMembers(thisSyntax);
            }

            if (referencedPrototypeTypeAttributeValues != null)
            {
                var adviceReferencedPrototypeTypeAttributeValues = _GetReferencedPrototypeTypesAttributeValues(cSWorkspace, adviceSymbol).ToList();
                foreach (var referencedPrototypeTypeAttributeValue in referencedPrototypeTypeAttributeValues)
                {
                    if (!adviceReferencedPrototypeTypeAttributeValues.Any(t => t == referencedPrototypeTypeAttributeValue))
                        adviceReferencedPrototypeTypeAttributeValues.Add((referencedPrototypeTypeAttributeValue));
                }

                var newAttributeList = CSAspectCompilerHelper.BuildReferencedPrototypeTypeAttribute(adviceReferencedPrototypeTypeAttributeValues.ToArray());
                var oldAttributeList = newAdviceSyntax.AttributeLists.FirstOrDefault(t => t.Attributes.Any(a => a.Name.ToFullString() == typeof(ReferencedPrototypeTypesAttribute).FullName));
                if (oldAttributeList != null)
                {
                    var oldAttribute = oldAttributeList.Attributes.First();
                    newAdviceSyntax = newAdviceSyntax.ReplaceNode(oldAttributeList, newAttributeList);
                }
                else
                    newAdviceSyntax = newAdviceSyntax.AddAttributeLists(new AttributeListSyntax[]{newAttributeList});
            }

            if (newAdviceSyntax.DescendantNodes().OfType<ThisExpressionSyntax>().Any())
            {
                var thisChanges = new List<(SyntaxNode old, SyntaxNode @new)>();
                foreach (var old in newAdviceSyntax.DescendantNodes().OfType<ThisExpressionSyntax>())
                    thisChanges.Add((old, SyntaxFactory.IdentifierName("&this")));
                newAdviceSyntax = (TypeDeclarationSyntax)RoslynHelper.ReplaceNodes(newAdviceSyntax, thisChanges.ToArray());
            }

            if (oldAdviceSyntax != newAdviceSyntax)
                return (oldAdviceSyntax, newAdviceSyntax);
            else
                return (null, null);
        }

        (SyntaxNode oldNode, SyntaxNode newNode) _GetAspectChange(ITypeSymbol prototypeTypeSymbol, ITypeSymbol aspectSymbol, IEnumerable<(ISymbol member, MemberDeclarationSyntax memberDeclaration)> memberDeclarationSyntaxes, IEnumerable<(AttributeData, AttributeSyntax)> prototypeTypeMappingDeclarationSyntaxes)
        {
            var oldAspectSyntax = (ClassDeclarationSyntax)aspectSymbol.DeclaringSyntaxReferences.First().GetSyntax();
            var newAspectSyntax = oldAspectSyntax;
            var newAttributes = _GetPrototypeItemTypeMappings(memberDeclarationSyntaxes);
            if (newAttributes.Any())
                newAspectSyntax = newAspectSyntax.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(newAttributes)));
            var newPrototypeTypeMappingDeclarationSyntaxes = _GetPrototypeItemTypeMappings(aspectSymbol, prototypeTypeMappingDeclarationSyntaxes);
            if (newPrototypeTypeMappingDeclarationSyntaxes.Any())
                newAspectSyntax = newAspectSyntax.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SeparatedList(newPrototypeTypeMappingDeclarationSyntaxes)));
            if (oldAspectSyntax != newAspectSyntax)
                return (oldAspectSyntax, newAspectSyntax);
            else
                return (null, null);
        }

        IEnumerable<AttributeSyntax> _GetPrototypeItemTypeMappings(ITypeSymbol aspectSymbol, IEnumerable<(AttributeData, AttributeSyntax)> prototypeTypeMappingDeclarationSyntaxes)
        {
            var aspectMappingAttributes = aspectSymbol.GetAttributes().Where(t => t.AttributeClass.ToDisplayString() == typeof(PrototypeItemMappingAttribute).FullName && (((PrototypeItemMappingSourceKinds)t.ConstructorArguments[0].Value) == PrototypeItemMappingSourceKinds.AdviceType || ((PrototypeItemMappingSourceKinds)t.ConstructorArguments[0].Value) == PrototypeItemMappingSourceKinds.PrototypeType));
            var addedAspectMappingAttributes = new List<(AttributeData, AttributeSyntax)>();
            foreach (var addedAspectMappingAttribute in prototypeTypeMappingDeclarationSyntaxes.Where(t => !aspectMappingAttributes.Any(a => RoslynHelper.IsPrototypeItemMappingEquals(a, t.Item1))))
                addedAspectMappingAttributes.Add((addedAspectMappingAttribute.Item1, _BuildMappingItempAttributeSyntax(addedAspectMappingAttribute.Item1)));
            return addedAspectMappingAttributes.Select(t => t.Item2);
        }

        IEnumerable<(ISymbol member, MemberDeclarationSyntax memberDeclaration)> _GetAddedMemberDeclarationSyntaxes(ITypeSymbol prototypeTypeSymbol, ITypeSymbol adviceSymbol, IEnumerable<(ISymbol member, MemberDeclarationSyntax memberDeclaration)> memberDeclarationSyntaxes)
        {
            var adviceMembers = RoslynHelper.GetSymbolMembers(adviceSymbol, false).Where(t => !t.GetAttributes().Any(a => a.AttributeClass.ContainingNamespace.ToDisplayString() == typeof(ExludedMemberAttribute).Namespace));
            var newMemberSyntaxes = memberDeclarationSyntaxes.Where(t => !adviceMembers.Any(a => RoslynHelper.AreSymbolSame(t.member, a))).ToArray();
            for (int i = 0; i < newMemberSyntaxes.Length; i++)
            {
                if (!(newMemberSyntaxes[i].memberDeclaration is ConstructorDeclarationSyntax))
                    continue;
                var memberDeclaration = ((ConstructorDeclarationSyntax)newMemberSyntaxes[i].memberDeclaration).WithIdentifier(SyntaxFactory.Identifier(adviceSymbol.Name));
                newMemberSyntaxes[i] = (newMemberSyntaxes[i].member, memberDeclaration);
            }

            return newMemberSyntaxes;
        }

        IEnumerable<AttributeSyntax> _GetPrototypeItemTypeMappings(IEnumerable<(ISymbol member, MemberDeclarationSyntax memberDeclaration)> memberDeclarationSyntaxes)
        {
            var mappingAttributes = new List<AttributeSyntax>();
            foreach (var memberDeclarationSyntax in memberDeclarationSyntaxes)
                mappingAttributes.Add(_BuildMappingItempAttributeSyntax(memberDeclarationSyntax.member.Name));
            return mappingAttributes;
        }

        AttributeSyntax _BuildMappingItempAttributeSyntax(string memberName)
        {
            var sourceKind = Enum.GetName(typeof(PrototypeItemMappingSourceKinds), PrototypeItemMappingSourceKinds.Member);
            var source = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(memberName));
            var targetKind = Enum.GetName(typeof(PrototypeItemMappingTargetKinds), (PrototypeItemMappingTargetKinds.Member));
            var targetName = memberName;
            return CSAspectCompilerHelper.PrototypeItemMappingAttribute(sourceKind, source, targetKind, targetName);
        }

        IEnumerable<(AttributeData, AttributeSyntax)> _GetPrototypeItemTypeMappings(ITypeSymbol prototypeTypeSymbol)
        {
            var mappingAttributes = prototypeTypeSymbol.GetAttributes().Where(t => t.AttributeClass.ToDisplayString() == typeof(PrototypeItemMappingAttribute).FullName && (((PrototypeItemMappingSourceKinds)t.ConstructorArguments[0].Value) == PrototypeItemMappingSourceKinds.AdviceType || ((PrototypeItemMappingSourceKinds)t.ConstructorArguments[0].Value) == PrototypeItemMappingSourceKinds.PrototypeType));
            var mappingSyntaxes = new List<(AttributeData, AttributeSyntax)>();
            foreach (var mappingAttribute in mappingAttributes.Where(t => !mappingSyntaxes.Any(a => RoslynHelper.IsPrototypeItemMappingEquals(a.Item1, t))))
                mappingSyntaxes.Add((mappingAttribute, _BuildMappingItempAttributeSyntax(mappingAttribute)));
            return mappingSyntaxes;
        }

        AttributeSyntax _BuildMappingItempAttributeSyntax(AttributeData attributeData)
        {
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
            return CSAspectCompilerHelper.PrototypeItemMappingAttribute(sourceKind, source, targetKind, targetName);
        }
    }
}