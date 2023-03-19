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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

namespace AspectDN.Aspect.Compilation.CS.CS5
{
    internal class SymbolMemberToSyntaxConverter
    {
        Type _AttributeTypeToAdd;
        internal SymbolMemberToSyntaxConverter(Type attributeTypeToAdd)
        {
            _AttributeTypeToAdd = attributeTypeToAdd;
        }

        internal IEnumerable<(ISymbol member, MemberDeclarationSyntax memberDeclaration)> GetMemberDeclarationSyntaxes(IEnumerable<ISymbol> members, bool withConstructors)
        {
            var memberDeclarations = new List<(ISymbol member, MemberDeclarationSyntax memberDeclaration)>();
            foreach (var member in members.Where(t => t.Name != ".ctor" || withConstructors))
            {
                var memberDeclaration = _GetMemberDeclarations(member);
                if (memberDeclaration == null)
                    continue;
                memberDeclarations.Add((member, memberDeclaration));
            }

            return memberDeclarations;
        }

        MemberDeclarationSyntax _GetMemberDeclarations(ISymbol symbol)
        {
            switch (symbol)
            {
                case IFieldSymbol fieldSymbol:
                    return _FieldDeclaration(fieldSymbol);
                case IPropertySymbol propertySymbol:
                    return propertySymbol.Parameters.Any() ? _IndexerDeclaration(propertySymbol) : _PropertyDeclaration(propertySymbol);
                case IMethodSymbol methodSymbol:
                    if (methodSymbol.Name != ".ctor")
                        return _MethodDeclaration(methodSymbol);
                    else
                        return _ConstructorDeclaration(methodSymbol);
                case IEventSymbol eventSymbol:
                    return _EventDeclaration(eventSymbol);
                case ITypeSymbol typeSymbol:
                    return _TypeDeclarationSyntax(typeSymbol);
                default:
                    throw new NotSupportedException();
            }
        }

        MemberDeclarationSyntax _FieldDeclaration(IFieldSymbol fieldSymbol)
        {
            if (fieldSymbol.AssociatedSymbol != null)
                return null;
            var field = SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(RoslynHelper.ParseTypeName(fieldSymbol.Type)).WithVariables(SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(SyntaxFactory.VariableDeclarator(fieldSymbol.Name))));
            var modifiers = _CustomModifiers(fieldSymbol);
            if (modifiers.Any())
                field = field.AddModifiers(modifiers.ToArray());
            if (_AttributeTypeToAdd != null)
                field = field.WithAttributeLists(CSAspectCompilerHelper.GetAttributeListOfAttributeType(_AttributeTypeToAdd));
            return field;
        }

        PropertyDeclarationSyntax _PropertyDeclaration(IPropertySymbol propertySymbol)
        {
            var name = propertySymbol.Name;
            NameSyntax interfaceName = null;
            var isExplicitInterfaceImplementation = propertySymbol.Name.Contains(".");
            if (isExplicitInterfaceImplementation)
            {
                var qualifiedName = (QualifiedNameSyntax)CSAspectCompilerHelper.ParseTypename(name);
                name = qualifiedName.Right.ToFullString();
                interfaceName = qualifiedName.Left;
            }

            var property = SyntaxFactory.PropertyDeclaration(RoslynHelper.ParseTypeName(propertySymbol.Type), name).WithAccessorList(_GetAccessors(propertySymbol));
            if (interfaceName != null)
                property = property.WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier(interfaceName));
            var modifiers = _CustomModifiers(propertySymbol);
            if (modifiers.Any())
            {
                if (propertySymbol.ContainingType.TypeKind == TypeKind.Interface || isExplicitInterfaceImplementation)
                {
                    var newToken = SyntaxFactory.Token(SyntaxKind.NewKeyword);
                    if (modifiers.Any(t => t.ValueText == newToken.ValueText))
                        property.AddModifiers(new SyntaxToken[]{newToken});
                }
                else
                    property = property.AddModifiers(modifiers.ToArray());
            }

            if (_AttributeTypeToAdd != null)
                property = property.WithAttributeLists(CSAspectCompilerHelper.GetAttributeListOfAttributeType(_AttributeTypeToAdd));
            return property;
        }

        MemberDeclarationSyntax _IndexerDeclaration(IPropertySymbol propertySymbol)
        {
            var parameters = new SeparatedSyntaxList<ParameterSyntax>().AddRange(_ParameterList(propertySymbol.Parameters));
            var indexer = SyntaxFactory.IndexerDeclaration(RoslynHelper.ParseTypeName(propertySymbol.Type)).WithAccessorList(_GetAccessors(propertySymbol));
            if (propertySymbol.Parameters.Any())
                indexer = indexer.WithParameterList(SyntaxFactory.BracketedParameterList(parameters));
            var modifiers = _CustomModifiers(propertySymbol);
            if (modifiers.Any())
                indexer = indexer.AddModifiers(modifiers.ToArray());
            if (propertySymbol.RefCustomModifiers.Any())
                indexer = indexer.AddModifiers(_CustomModifiers(propertySymbol.RefCustomModifiers).ToArray());
            if (_AttributeTypeToAdd != null)
                indexer = indexer.WithAttributeLists(CSAspectCompilerHelper.GetAttributeListOfAttributeType(_AttributeTypeToAdd));
            return indexer;
        }

        AccessorListSyntax _GetAccessors(IPropertySymbol propertySymbol)
        {
            var accessors = new List<AccessorDeclarationSyntax>();
            if (propertySymbol.GetMethod != null)
                accessors.Add(_GetAccessor(propertySymbol.IsAbstract, SyntaxKind.GetAccessorDeclaration));
            if (propertySymbol.SetMethod != null)
                accessors.Add(_GetAccessor(propertySymbol.IsAbstract, SyntaxKind.SetAccessorDeclaration));
            return SyntaxFactory.AccessorList(new SyntaxList<AccessorDeclarationSyntax>(accessors));
        }

        AccessorDeclarationSyntax _GetAccessor(bool isAbstract, SyntaxKind syntaxKind)
        {
            var accessor = SyntaxFactory.AccessorDeclaration(syntaxKind);
            if (isAbstract)
                accessor = accessor.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            else
                accessor = accessor.WithBody(_BlockNotImplementedException());
            if (_AttributeTypeToAdd != null)
                accessor = accessor.WithAttributeLists(CSAspectCompilerHelper.GetAttributeListOfAttributeType(_AttributeTypeToAdd));
            return accessor;
        }

        AccessorDeclarationSyntax _SetAccessor(bool isAbstract)
        {
            var accessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            if (_AttributeTypeToAdd != null)
                accessor = accessor.WithAttributeLists(CSAspectCompilerHelper.GetAttributeListOfAttributeType(_AttributeTypeToAdd));
            if (!isAbstract)
                accessor = accessor.WithBody(_BlockNotImplementedException());
            return accessor;
        }

        MemberDeclarationSyntax _ConstructorDeclaration(IMethodSymbol methodSymbol)
        {
            var constructor = SyntaxFactory.ConstructorDeclaration(methodSymbol.Name).WithBody(_BlockNotImplementedException());
            if (_AttributeTypeToAdd != null)
                constructor = constructor.WithAttributeLists(CSAspectCompilerHelper.GetAttributeListOfAttributeType(_AttributeTypeToAdd));
            if (methodSymbol.Parameters != null)
            {
                var parameters = SyntaxFactory.SeparatedList<ParameterSyntax>(_ParameterList(methodSymbol.Parameters));
                constructor = constructor.WithParameterList(SyntaxFactory.ParameterList(parameters));
            }

            var modifiers = _CustomModifiers(methodSymbol);
            if (modifiers.Any())
                constructor = constructor.AddModifiers(modifiers.ToArray());
            if (methodSymbol.RefCustomModifiers.Any())
                constructor = constructor.AddModifiers(_CustomModifiers(methodSymbol.RefCustomModifiers).ToArray());
            if (_AttributeTypeToAdd != null)
                constructor = constructor.WithAttributeLists(CSAspectCompilerHelper.GetAttributeListOfAttributeType(_AttributeTypeToAdd));
            return constructor;
        }

        MemberDeclarationSyntax _MethodDeclaration(IMethodSymbol methodSymbol)
        {
            if (methodSymbol.AssociatedSymbol != null)
                return null;
            var name = methodSymbol.Name;
            NameSyntax interfaceName = null;
            var isExplicitInterfaceImplementation = name.Contains(".");
            if (isExplicitInterfaceImplementation)
            {
                var qualifiedName = (QualifiedNameSyntax)CSAspectCompilerHelper.ParseTypename(name);
                name = qualifiedName.Right.ToFullString();
                interfaceName = qualifiedName.Left;
            }

            var type = (TypeSyntax)RoslynHelper.ParseTypeName(methodSymbol.ReturnType);
            if (methodSymbol.ReturnType.Name == typeof(void).Name)
                type = SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
            var method = SyntaxFactory.MethodDeclaration(type, name);
            if (interfaceName != null)
                method = method.WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier(interfaceName));
            if (!methodSymbol.IsAbstract)
                method = method.WithBody(_BlockNotImplementedException());
            if (_AttributeTypeToAdd != null)
                method = method.WithAttributeLists(CSAspectCompilerHelper.GetAttributeListOfAttributeType(_AttributeTypeToAdd));
            if (methodSymbol.TypeParameters.Any())
                method = method.WithTypeParameterList(_TypeParameterList(methodSymbol.TypeParameters));
            if (methodSymbol.Parameters != null)
            {
                var parameters = SyntaxFactory.SeparatedList<ParameterSyntax>(_ParameterList(methodSymbol.Parameters));
                method = method.WithParameterList(SyntaxFactory.ParameterList(parameters));
            }

            var modifiers = _CustomModifiers(methodSymbol);
            if (modifiers.Any())
                method = method.AddModifiers(modifiers.ToArray());
            return method;
        }

        IEnumerable<ParameterSyntax> _ParameterList(IEnumerable<IParameterSymbol> parameterSymbols)
        {
            var parameters = new List<ParameterSyntax>();
            foreach (var parameterSymbol in parameterSymbols)
                parameters.Add(_Parameter(parameterSymbol));
            return SyntaxFactory.SeparatedList<ParameterSyntax>(parameters);
        }

        ParameterSyntax _Parameter(IParameterSymbol parameterSymbol)
        {
            var parameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameterSymbol.Name)).WithType(RoslynHelper.ParseTypeName(parameterSymbol.Type));
            if (parameterSymbol.CustomModifiers.Any())
                parameter = parameter.WithModifiers(new SyntaxTokenList(_ParameterModifiers(parameterSymbol.CustomModifiers)));
            if (parameterSymbol.IsParams)
                parameter = parameter.AddModifiers(SyntaxFactory.Token(SyntaxKind.ParamsKeyword));
            return parameter;
        }

        IEnumerable<SyntaxToken> _ParameterModifiers(IEnumerable<CustomModifier> modifiers)
        {
            var syntaxTokens = new List<SyntaxToken>(modifiers.Count());
            foreach (var modifier in modifiers)
            {
                syntaxTokens.Add(_CustomModifier(modifier));
            }

            return syntaxTokens;
        }

        SyntaxToken _ParameterModifier(CustomModifier modifier)
        {
            switch (modifier.Modifier.Name)
            {
                case "ref":
                    return SyntaxFactory.Token(SyntaxKind.RefKeyword);
                case "out":
                    return SyntaxFactory.Token(SyntaxKind.OutKeyword);
                case "this":
                    return SyntaxFactory.Token(SyntaxKind.ThisKeyword);
                default:
                    throw new NotImplementedException();
            }
        }

        MemberDeclarationSyntax _EventDeclaration(IEventSymbol eventSymbol)
        {
            var @event = SyntaxFactory.EventFieldDeclaration(SyntaxFactory.VariableDeclaration(RoslynHelper.ParseTypeName(eventSymbol.Type)).WithVariables(SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(SyntaxFactory.VariableDeclarator(eventSymbol.Name))));
            if (_AttributeTypeToAdd != null)
                @event = @event.WithAttributeLists(CSAspectCompilerHelper.GetAttributeListOfAttributeType(_AttributeTypeToAdd));
            return @event;
        }

        AccessorListSyntax _GetAccessors(IEventSymbol eventSymbol)
        {
            var accessors = new List<AccessorDeclarationSyntax>();
            if (eventSymbol.AddMethod != null)
                accessors.Add(_AddAccessor(eventSymbol.IsAbstract));
            if (eventSymbol.RemoveMethod != null)
                accessors.Add(_RemoveAccessor(eventSymbol.IsAbstract));
            return SyntaxFactory.AccessorList(new SyntaxList<AccessorDeclarationSyntax>(accessors));
        }

        AccessorDeclarationSyntax _AddAccessor(bool isAbstract)
        {
            var accessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.AddAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            if (_AttributeTypeToAdd != null)
                accessor = accessor.WithAttributeLists(CSAspectCompilerHelper.GetAttributeListOfAttributeType(_AttributeTypeToAdd));
            if (!isAbstract)
                accessor = accessor.WithBody(_BlockNotImplementedException());
            return accessor;
        }

        AccessorDeclarationSyntax _RemoveAccessor(bool isAbstract)
        {
            var accessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.RemoveAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)).WithAttributeLists(CSAspectCompilerHelper.GetAttributeListOfAttributeType(_AttributeTypeToAdd));
            if (!isAbstract)
                accessor = accessor.WithBody(_BlockNotImplementedException());
            return accessor;
        }

        MemberDeclarationSyntax _TypeDeclarationSyntax(ITypeSymbol typeSymbol)
        {
            switch (typeSymbol.TypeKind)
            {
                case TypeKind.Class:
                    return _ClassDeclaration((INamedTypeSymbol)typeSymbol);
                case TypeKind.Delegate:
                case TypeKind.Dynamic:
                case TypeKind.Enum:
                case TypeKind.Struct:
                default:
                    throw new NotImplementedException();
            }
        }

        MemberDeclarationSyntax _ClassDeclaration(INamedTypeSymbol typeSymbol)
        {
            var memberSyntaxes = _MemberDeclarations(typeSymbol.GetMembers()).ToList();
            foreach (var memberSyntax in memberSyntaxes.OfType<ConstructorDeclarationSyntax>().ToArray())
            {
                var newMemberSyntax = memberSyntax.WithIdentifier(SyntaxFactory.Identifier(typeSymbol.Name));
                memberSyntaxes[memberSyntaxes.IndexOf(memberSyntax)] = newMemberSyntax;
            }

            var @class = SyntaxFactory.ClassDeclaration(SyntaxFactory.Identifier(typeSymbol.Name)).WithMembers(new SyntaxList<MemberDeclarationSyntax>(memberSyntaxes)).AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            if (typeSymbol.TypeParameters.Any())
                @class = @class.WithTypeParameterList(_TypeParameterList(typeSymbol.TypeParameters));
            if (!(typeSymbol.IsValueType || typeSymbol.SpecialType == SpecialType.System_Object))
                @class = @class.AddBaseListTypes(SyntaxFactory.SimpleBaseType(RoslynHelper.ParseTypeName(typeSymbol.BaseType)));
            if (_AttributeTypeToAdd != null)
                @class = @class.WithAttributeLists(CSAspectCompilerHelper.GetAttributeListOfAttributeType(_AttributeTypeToAdd));
            return @class;
        }

        IEnumerable<MemberDeclarationSyntax> _MemberDeclarations(IEnumerable<ISymbol> members)
        {
            var membersSyntaxes = new List<MemberDeclarationSyntax>();
            foreach (var member in members)
            {
                var memberDeclaration = _GetMemberDeclarations(member);
                if (memberDeclaration == null)
                    continue;
                membersSyntaxes.Add(memberDeclaration);
            }

            return membersSyntaxes;
        }

        IEnumerable<SyntaxToken> _CustomModifiers(ImmutableArray<CustomModifier> modifiers)
        {
            var syntaxTokens = new List<SyntaxToken>(modifiers.Length);
            foreach (var modifier in modifiers)
            {
                syntaxTokens.Add(_CustomModifier(modifier));
            }

            return syntaxTokens;
        }

        SyntaxToken _CustomModifier(CustomModifier customModifier)
        {
            SyntaxKind? kind = null;
            switch (customModifier.Modifier.Name)
            {
                case "partial":
                    kind = SyntaxKind.PartialKeyword;
                    break;
                case "constant":
                    kind = SyntaxKind.ConstKeyword;
                    break;
                case "new":
                    kind = SyntaxKind.NewKeyword;
                    break;
                case "internal":
                    kind = SyntaxKind.InternalKeyword;
                    break;
                case "protected":
                    kind = SyntaxKind.ProtectedKeyword;
                    break;
                case "public":
                    kind = SyntaxKind.PublicKeyword;
                    break;
                case "private":
                    kind = SyntaxKind.PrivateKeyword;
                    break;
                case "abstract":
                    kind = SyntaxKind.AbstractKeyword;
                    break;
                case "sealed":
                    kind = SyntaxKind.SealedKeyword;
                    break;
                case "static":
                    kind = SyntaxKind.StaticKeyword;
                    break;
                case "readonly":
                    kind = SyntaxKind.ReadOnlyKeyword;
                    break;
                case "volatile":
                    kind = SyntaxKind.VolatileKeyword;
                    break;
                case "extern":
                    kind = SyntaxKind.ExternKeyword;
                    break;
                case "prototype":
                    kind = SyntaxKind.VirtualKeyword;
                    break;
                case "override":
                    kind = SyntaxKind.OverrideKeyword;
                    break;
                case "const":
                    kind = SyntaxKind.ConstKeyword;
                    break;
                case "virtual":
                    kind = SyntaxKind.VirtualKeyword;
                    break;
                default:
                    throw new NotImplementedException();
            }

            return SyntaxFactory.Token(kind.Value);
        }

        IEnumerable<SyntaxToken> _CustomModifiers(ISymbol symbol)
        {
            var tokens = new List<SyntaxToken>();
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.NotApplicable:
                    break;
                case Accessibility.Private:
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
                    break;
                case Accessibility.ProtectedAndInternal:
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.InternalKeyword));
                    break;
                case Accessibility.Protected:
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
                    break;
                case Accessibility.Internal:
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.InternalKeyword));
                    break;
                case Accessibility.ProtectedOrInternal:
                    throw new NotImplementedException();
                case Accessibility.Public:
                    tokens.Add(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
                    break;
                default:
                    break;
            }

            if (symbol.IsAbstract)
                tokens.Add(SyntaxFactory.Token(SyntaxKind.AbstractKeyword));
            if (symbol.IsExtern)
                tokens.Add(SyntaxFactory.Token(SyntaxKind.ExternKeyword));
            if (symbol.IsOverride)
                tokens.Add(SyntaxFactory.Token(SyntaxKind.OverrideKeyword));
            if (symbol.IsSealed)
                tokens.Add(SyntaxFactory.Token(SyntaxKind.SealedKeyword));
            if (symbol.IsStatic)
                tokens.Add(SyntaxFactory.Token(SyntaxKind.StaticKeyword));
            if (symbol.IsVirtual)
                tokens.Add(SyntaxFactory.Token(SyntaxKind.VirtualKeyword));
            return tokens;
        }

        BlockSyntax _BlockNotImplementedException()
        {
            return SyntaxFactory.Block().AddStatements(new StatementSyntax[]{SyntaxFactory.ThrowStatement(SyntaxFactory.ObjectCreationExpression(SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("NotImplementedException"))).WithArgumentList(SyntaxFactory.ArgumentList()))});
        }

        TypeParameterListSyntax _TypeParameterList(IEnumerable<ITypeParameterSymbol> typeParameterSymbols)
        {
            var typeParameters = new List<TypeParameterSyntax>();
            foreach (var typeParameterSymbol in typeParameterSymbols)
            {
                typeParameters.Add(SyntaxFactory.TypeParameter(typeParameterSymbol.Name));
            }

            return SyntaxFactory.TypeParameterList(SyntaxFactory.SeparatedList<TypeParameterSyntax>(typeParameters));
        }

        TypeSyntax _xParseTypeName(ITypeSymbol type)
        {
            if (type.SpecialType != SpecialType.None)
                return SyntaxFactory.ParseTypeName(type.ToDisplayString());
            else
                return CSAspectCompilerHelper.ParseTypename(type.ToDisplayString());
        }
    }
}