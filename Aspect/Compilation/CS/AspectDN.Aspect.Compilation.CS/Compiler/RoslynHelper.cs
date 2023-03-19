// Author:
//
// T. Freyburger (t.freyburger@gmail.com)
//
// Copyright (c)  Thierry Freyburger
//
// Licensed under the GPLV3 license.
////
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using AspectDN.Common;
using AspectDN.Aspect.Weaving.IConcerns;

namespace AspectDN.Aspect.Compilation.CS
{
    internal static class RoslynHelper
    {
        static internal IEnumerable<SyntaxNode> GetSyntaxNodeUsingType(SyntaxTree[] syntaxTrees, INamedTypeSymbol type, SemanticModel semanticModel)
        {
            foreach (var syntaxTree in syntaxTrees)
            {
                var nodes = syntaxTree.GetRoot().DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Where(t => type.Equals(semanticModel.GetTypeInfo((ExpressionSyntax)t).ConvertedType));
                foreach (var node in nodes)
                {
                    SyntaxNode topNode = node;
                    while (topNode.Parent is QualifiedNameSyntax)
                        topNode = topNode.Parent;
                    yield return topNode;
                }
            }
        }

        internal static IEnumerable<ITypeSymbol> GetAllReferencedAdviceTypes_old(CSharpCompilation cSharpCompilation, INamedTypeSymbol fromType)
        {
            var referencedTypes = new List<ITypeSymbol>();
            if (!fromType.DeclaringSyntaxReferences.Any())
                return referencedTypes;
            var fromTypeSyntax = fromType.DeclaringSyntaxReferences.First().GetSyntax();
            var semanticModel = cSharpCompilation.GetSemanticModel(fromTypeSyntax.SyntaxTree);
            var syntaxNodes = fromTypeSyntax.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>();
            foreach (var syntaxNode in syntaxNodes)
            {
                SyntaxNode referencedNameSyntax = syntaxNode;
                while (referencedNameSyntax.Parent is QualifiedNameSyntax)
                    referencedNameSyntax = referencedNameSyntax.Parent;
                var parentMemberSyntax = referencedNameSyntax.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().FirstOrDefault();
                if (parentMemberSyntax is null)
                    continue;
                switch (parentMemberSyntax)
                {
                    case ConstructorDeclarationSyntax constructor:
                    case MethodDeclarationSyntax method:
                    case FieldDeclarationSyntax field:
                    case PropertyDeclarationSyntax property:
                    case IndexerDeclarationSyntax indexer:
                    case EventDeclarationSyntax @event:
                        var memberSymbolx = semanticModel.GetSymbolInfo(parentMemberSyntax);
                        var memberSymbol = semanticModel.GetSymbolInfo(parentMemberSyntax).Symbol;
                        if (!memberSymbol.ContainingAssembly.GetAttributes().Any(a => a.AttributeClass.ToDisplayString() == typeof(AspectDNAssemblyAttribute).FullName))
                            continue;
                        break;
                    case ClassDeclarationSyntax @class:
                        var classSymbol = (INamedTypeSymbol)semanticModel.GetDeclaredSymbol(@class);
                        if (!classSymbol.ContainingAssembly.GetAttributes().Any(a => a.AttributeClass.ToDisplayString() == typeof(AspectDNAssemblyAttribute).FullName))
                            continue;
                        break;
                    case StructDeclarationSyntax @struct:
                        var structSyntax = (INamedTypeSymbol)semanticModel.GetDeclaredSymbol(@struct);
                        if (structSyntax.AllInterfaces.Any(i => i.Name == typeof(IConcernDeclaration).Name))
                            continue;
                        break;
                    default:
                        continue;
                }

                var referencedType = (ITypeSymbol)semanticModel.GetTypeInfo((ExpressionSyntax)referencedNameSyntax).ConvertedType;
                if (referencedType != null && referencedType.Kind != SymbolKind.ErrorType)
                {
                    if (!referencedTypes.Any(t => t.Equals(referencedType)))
                        referencedTypes.Add(referencedType);
                }
            }

            var attributeDatas = RoslynHelper.GetAttributeDatasFromSymbol(cSharpCompilation, fromType, typeof(AdviceBaseTypeAttribute));
            return referencedTypes;
        }

        internal static IEnumerable<ITypeSymbol> GetAllReferencedConcernTypesFromType(CSharpCompilation cSharpCompilation, INamedTypeSymbol fromType)
        {
            if (!fromType.DeclaringSyntaxReferences.Any())
                return null;
            var fromTypeSyntax = fromType.DeclaringSyntaxReferences.First().GetSyntax();
            var semanticModel = cSharpCompilation.GetSemanticModel(fromTypeSyntax.SyntaxTree);
            IEnumerable<MemberDeclarationSyntax> memberDeclarationSyntaxes = null;
            switch (fromTypeSyntax)
            {
                case TypeDeclarationSyntax typeDeclarationSyntax:
                    memberDeclarationSyntaxes = typeDeclarationSyntax.Members;
                    break;
                case EnumDeclarationSyntax enumDeclarationSyntax:
                    memberDeclarationSyntaxes = enumDeclarationSyntax.Members;
                    break;
                default:
                    throw new NotSupportedException();
            }

            var referencedTypes = _GetReferencedConcernTypesFromSyntaxNode(semanticModel, memberDeclarationSyntaxes);
            var attributeDatas = RoslynHelper.GetAttributeDatasFromSymbol(cSharpCompilation, fromType, typeof(AdviceBaseTypeAttribute));
            foreach (var attributeData in attributeDatas)
            {
                var attributeReferencedTypes = _GetReferencedConcernTypesFromSyntaxNode(semanticModel, new SyntaxNode[]{attributeData.ApplicationSyntaxReference.GetSyntax()});
                if (attributeReferencedTypes != null && attributeReferencedTypes.Any())
                    referencedTypes = referencedTypes.Union(attributeReferencedTypes).ToList();
            }

            if (fromType.AllInterfaces.Any(i => i.ToDisplayString() == typeof(IAttributesAdviceDeclaration).FullName))
            {
                attributeDatas = fromType.GetAttributes();
                foreach (var attributeData in attributeDatas)
                {
                    var attributeReferencedTypes = _GetReferencedConcernTypesFromSyntaxNode(semanticModel, new SyntaxNode[]{attributeData.ApplicationSyntaxReference.GetSyntax()});
                    if (attributeReferencedTypes != null)
                        referencedTypes = referencedTypes.Union(attributeReferencedTypes).ToList();
                }
            }

            return referencedTypes;
        }

        static List<ITypeSymbol> _GetReferencedConcernTypesFromSyntaxNode(SemanticModel semanticModel, IEnumerable<SyntaxNode> syntaxNode)
        {
            var referencedTypes = new List<ITypeSymbol>();
            var identifierNames = syntaxNode.SelectMany(t => t.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Cast<NameSyntax>()).Union(syntaxNode.SelectMany(t => t.DescendantNodesAndSelf().OfType<GenericNameSyntax>()));
            var referencedNames = new List<NameSyntax>();
            foreach (var identifierName in identifierNames)
            {
                if (identifierName.Parent is MemberAccessExpressionSyntax)
                {
                    var memberAccessExpression = (MemberAccessExpressionSyntax)identifierName.Parent;
                    while (memberAccessExpression.Parent is MemberAccessExpressionSyntax)
                        memberAccessExpression = (MemberAccessExpressionSyntax)memberAccessExpression.Parent;
                    var type = semanticModel.GetTypeInfo(memberAccessExpression.Expression);
                    referencedTypes.Add(type.Type);
                }

                if (identifierName.Parent is VariableDeclarationSyntax && (!identifierName.AncestorsAndSelf().OfType<FieldDeclarationSyntax>().Any() && !identifierName.AncestorsAndSelf().OfType<EventFieldDeclarationSyntax>().Any()))
                    continue;
                NameSyntax referencedNameSyntax = identifierName;
                if (referencedNameSyntax.Parent is QualifiedNameSyntax)
                {
                    while (referencedNameSyntax.Parent is QualifiedNameSyntax)
                        referencedNameSyntax = (QualifiedNameSyntax)referencedNameSyntax.Parent;
                    referencedNames.Add(referencedNameSyntax);
                    continue;
                }

                referencedNames.Add(referencedNameSyntax);
            }

            foreach (var referencedName in referencedNames.Distinct())
            {
                var referencedTypeInfo = semanticModel.GetTypeInfo(referencedName);
                if (referencedTypeInfo.Type is null && referencedName.Parent is ObjectCreationExpressionSyntax)
                    referencedTypeInfo = semanticModel.GetTypeInfo(referencedName.Parent);
                if (referencedTypeInfo.Type is null)
                    continue;
                if (referencedTypeInfo.Type.ContainingAssembly == null || !referencedTypeInfo.Type.ContainingAssembly.GetAttributes().Any(a => a.AttributeClass.ToDisplayString() == typeof(AspectDNAssemblyAttribute).FullName))
                    continue;
                referencedTypes.Add(referencedTypeInfo.Type);
            }

            return referencedTypes.Distinct().ToList();
        }

        internal static (IEnumerable<string> referencedPrototypeTypenames, IEnumerable<string> referencedAdviceTypenames) GetReferencddAdviceOrPrototypeTypes(CSWorkspace cSWorkspace, IEnumerable<ITypeSymbol> fromTypes, string ignoreTypeName)
        {
            var referencedPrototypeTypenames = new List<string>();
            var referencedAdviceTypenames = new List<string>();
            foreach (var fromTypeDefinition in fromTypes.OfType<INamedTypeSymbol>().Select(t => t.ConstructedFrom).Distinct())
            {
                var referencedType = fromTypeDefinition;
                if (referencedType.AllInterfaces.Any(i => i.ToDisplayString() == typeof(IAdviceDeclaration).FullName))
                    continue;
                if (referencedType.AllInterfaces.Any(i => i.ToDisplayString() == typeof(ITypesAdviceDeclaration).FullName))
                {
                    referencedAdviceTypenames.Add(referencedType.ToDisplayString());
                    continue;
                }

                if (referencedType.ToDisplayString().IndexOf($"{ignoreTypeName}.") == 0)
                    continue;
                while (referencedType != null)
                {
                    if (cSWorkspace.GetAttributeDatasFromSymbol(referencedType, typeof(PrototypeTypeDeclarationAttribute)).Any())
                    {
                        var typeofName = referencedType.ToDisplayString();
                        if (referencedType.IsGenericType)
                            typeofName = RoslynHelper.GetUnboundTypeName(referencedType.Name, referencedType.TypeArguments.Count(), referencedType.ContainingSymbol).ToFullString();
                        referencedPrototypeTypenames.Add(typeofName);
                        referencedType = null;
                        continue;
                    }

                    if (referencedType.AllInterfaces.Any(i => i.ToDisplayString() == typeof(ITypesAdviceDeclaration).FullName) && referencedType.AllInterfaces.Any(t => t.ToDisplayString() == typeof(IAspectTypeDeclaration).FullName))
                    {
                        referencedAdviceTypenames.Add(fromTypeDefinition.ToDisplayString());
                        referencedType = null;
                        continue;
                    }

                    referencedType = referencedType.ContainingType;
                }
            }

            return (referencedPrototypeTypenames.Distinct(), referencedAdviceTypenames.Distinct());
        }

        internal static IEnumerable<INamedTypeSymbol> GetTypeSymbols(CSharpCompilation cSharpCompilation, SyntaxTree syntaxTree, string searchTypename)
        {
            var symbols = GetConcernTypeSymbols(cSharpCompilation, syntaxTree);
            symbols = symbols.Where(t => IsTypenameFound(searchTypename, t));
            return symbols;
        }

        internal static IEnumerable<INamedTypeSymbol> GetConcernTypeSymbols(CSharpCompilation cSharpCompilation, SyntaxTree syntaxTree)
        {
            var globalNamespaceTypes = cSharpCompilation.Assembly.GlobalNamespace.GetMembers().OfType<INamedTypeSymbol>().Where(t => IsSymbolInAspectDNAssembly(t));
            var semanticModel = cSharpCompilation.GetSemanticModel(syntaxTree, true);
            var highLevelSymbols = semanticModel.LookupNamespacesAndTypes(0);
            var symbols = GetConcernTypeSymbolsFromNamespaces(highLevelSymbols.OfType<INamespaceSymbol>()).Union(highLevelSymbols.OfType<INamedTypeSymbol>()).Union(globalNamespaceTypes);
            return symbols.OfType<INamedTypeSymbol>().Distinct();
        }

        internal static IEnumerable<INamedTypeSymbol> GetAssemblyConcernTypeSymbols(CSharpCompilation cSharpCompilation)
        {
            var typeSymbols = new List<INamedTypeSymbol>();
            foreach (var syntaxTree in cSharpCompilation.SyntaxTrees)
            {
                var concernTypeSymbols = GetConcernTypeSymbols(cSharpCompilation, syntaxTree);
                foreach (var concernTypeSymbol in concernTypeSymbols)
                {
                    if (concernTypeSymbol.ContainingAssembly.ToDisplayString() != cSharpCompilation.Assembly.ToDisplayString())
                        continue;
                    typeSymbols.Add(concernTypeSymbol);
                }
            }

            return typeSymbols.Distinct();
        }

        internal static IEnumerable<INamedTypeSymbol> GetConcernTypeSymbolsFromNamespaces(IEnumerable<INamespaceSymbol> namespaceSymbols, string searchTypename)
        {
            var typeSymbols = GetConcernTypeSymbolsFromNamespaces(namespaceSymbols).Where(t => IsTypenameFound(searchTypename, t));
            return typeSymbols;
        }

        internal static IEnumerable<INamedTypeSymbol> GetConcernTypeSymbolsFromNamespaces(IEnumerable<INamespaceSymbol> namespaceSymbols)
        {
            var typeSymbols = new List<INamedTypeSymbol>();
            foreach (var @namespace in namespaceSymbols.Where(t => IsSymbolInAspectDNAssembly(t)))
            {
                var values = @namespace.GetMembers();
                typeSymbols.AddRange(values.OfType<INamedTypeSymbol>().Where(t => IsSymbolInAspectDNAssembly(t)));
                var childNamespaces = values.OfType<INamespaceSymbol>();
                if (childNamespaces.Any())
                    typeSymbols.AddRange(GetConcernTypeSymbolsFromNamespaces(childNamespaces));
            }

            return typeSymbols;
        }

        internal static bool IsSymbolInAspectDNAssembly(ISymbol symbol) => symbol.ContainingAssembly == null || IsAssemblyAspectDNAssembly(symbol.ContainingAssembly);
        internal static bool IsAssemblyAspectDNAssembly(IAssemblySymbol assemblySymbol) => assemblySymbol.GetAttributes().Any(t => t.AttributeClass.ToDisplayString() == typeof(AspectDNAssemblyAttribute).FullName);
        internal static IEnumerable<INamedTypeSymbol> GetTypeSymbols(CSharpCompilation cSharpCompilation, Type interfaceType)
        {
            var typeSymbols = GetAssemblyConcernTypeSymbols(cSharpCompilation);
            return typeSymbols.Where(t => t.AllInterfaces.Any(i => i.ToDisplayString() == interfaceType.FullName));
        }

        internal static string GetFullMetadataName(ISymbol s)
        {
            if (s == null || IsRootNamespace(s))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(s.MetadataName);
            var last = s;
            s = s.ContainingSymbol;
            while (!IsRootNamespace(s))
            {
                if (s is ITypeSymbol && last is ITypeSymbol)
                {
                    sb.Insert(0, '+');
                }
                else
                {
                    sb.Insert(0, '.');
                }

                sb.Insert(0, s.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                s = s.ContainingSymbol;
            }

            return sb.ToString();
        }

        internal static bool IsRootNamespace(ISymbol symbol)
        {
            INamespaceSymbol s = null;
            return ((s = symbol as INamespaceSymbol) != null) && s.IsGlobalNamespace;
        }

        internal static IEnumerable<INamedTypeSymbol> GetNamedTypeSymbols(CSharpCompilation cSharpCompilation)
        {
            var symbols = new List<INamedTypeSymbol>();
            symbols.AddRange(GetNamedTypeSymbols(cSharpCompilation.Assembly));
            foreach (var referencedAssemblySymbol in GetReferencedAssemblySymbols(cSharpCompilation.Assembly))
            {
                symbols.AddRange(GetNamedTypeSymbols(referencedAssemblySymbol));
            }

            return symbols.Distinct();
        }

        internal static IEnumerable<INamedTypeSymbol> GetNamedTypeSymbols(IAssemblySymbol assemblySymbol)
        {
            var symbols = new List<INamedTypeSymbol>();
            symbols.AddRange(assemblySymbol.GlobalNamespace.GetTypeMembers().OfType<INamedTypeSymbol>());
            foreach (var namespaceSymbol in assemblySymbol.GlobalNamespace.GetNamespaceMembers())
                symbols.AddRange(GetNamedTypeSymbols(namespaceSymbol));
            return symbols;
        }

        internal static IEnumerable<INamedTypeSymbol> GetNamedTypeSymbols(INamespaceSymbol namespaceSymbol)
        {
            var symbols = new List<INamedTypeSymbol>();
            symbols.AddRange(namespaceSymbol.GetTypeMembers().OfType<INamedTypeSymbol>());
            foreach (var childNamespaceSymbol in namespaceSymbol.GetNamespaceMembers())
                symbols.AddRange(GetNamedTypeSymbols(childNamespaceSymbol));
            return symbols;
        }

        internal static IEnumerable<INamedTypeSymbol> LookupAdvices(CSharpCompilation cSharpCompilation, SyntaxTree syntaxTree, INamespaceSymbol containingNamespace, string typename)
        {
            return LookupConcerSymbols(cSharpCompilation, syntaxTree, containingNamespace, typename, typeof(IAdviceDeclaration));
        }

        internal static IEnumerable<INamedTypeSymbol> LookupConcerSymbols(CSharpCompilation cSharpCompilation, SyntaxTree syntaxTree, INamespaceSymbol containingNamespace, string typename, Type typeConcern)
        {
            var symbols = new List<INamedTypeSymbol>();
            var semanticModel = cSharpCompilation.GetSemanticModel(syntaxTree, true);
            var globalSymbols = cSharpCompilation.Assembly.GlobalNamespace.GetTypeMembers().Where(t => ((ITypeSymbol)t).AllInterfaces.Any(i => i.ToDisplayString() == typeConcern.FullName) && GetNonGenericMetadataName(t.MetadataName) == typename);
            symbols.AddRange(globalSymbols);
            if (containingNamespace != null)
            {
                var newSymbols = containingNamespace.GetTypeMembers().OfType<INamedTypeSymbol>();
                symbols.AddRange(newSymbols.Where(t => ((ITypeSymbol)t).AllInterfaces.Any(i => i.ToDisplayString() == typeConcern.FullName) && GetNonGenericMetadataName(t.MetadataName) == typename));
            }

            var namespacesInUsed = LookupNamespaceSymbolsInUSed(semanticModel, syntaxTree.GetRoot());
            foreach (var @namespace in namespacesInUsed)
            {
                var newSymbols = @namespace.GetTypeMembers().OfType<INamedTypeSymbol>();
                symbols.AddRange(newSymbols.Where(t => ((ITypeSymbol)t).AllInterfaces.Any(i => i.ToDisplayString() == typeConcern.FullName) && GetNonGenericMetadataName(t.MetadataName) == typename));
            }

            var fullNamedSymbols = GetNamedTypeSymbols(cSharpCompilation).Where(t => t.AllInterfaces.Any(i => i.ToDisplayString() == typeConcern.FullName));
            symbols.AddRange(fullNamedSymbols.Where(t => GetNoneGenericFullname(t.ToDisplayString()) == typename));
            return symbols.Distinct();
        }

        internal static IEnumerable<INamedTypeSymbol> LookupPrototypeTypes(CSharpCompilation cSharpCompilation, SyntaxTree syntaxTree, string typename)
        {
            var symbols = new List<INamedTypeSymbol>();
            var semanticModel = cSharpCompilation.GetSemanticModel(syntaxTree, true);
            var globalSymbols = cSharpCompilation.Assembly.GlobalNamespace.GetTypeMembers().OfType<INamedTypeSymbol>().Where(t => RoslynHelper.GetAttributeDatasFromSymbol(cSharpCompilation, t, typeof(PrototypeTypeDeclarationAttribute)).Any() && GetNonGenericMetadataName(t.MetadataName) == typename);
            symbols.AddRange(globalSymbols);
            var namespacesInUsed = LookupNamespaceSymbolsInUSed(semanticModel, syntaxTree.GetRoot(), true);
            foreach (var @namespace in namespacesInUsed)
            {
                var newSymbols = @namespace.GetTypeMembers().OfType<INamedTypeSymbol>();
                symbols.AddRange(newSymbols.Where(t => RoslynHelper.GetAttributeDatasFromSymbol(cSharpCompilation, t, typeof(PrototypeTypeDeclarationAttribute)).Any() && GetNonGenericMetadataName(t.MetadataName) == typename));
            }

            var fullNamedSymbols = GetNamedTypeSymbols(cSharpCompilation);
            symbols.AddRange(fullNamedSymbols.Where(t => RoslynHelper.GetAttributeDatasFromSymbol(cSharpCompilation, t, typeof(PrototypeTypeDeclarationAttribute)).Any() && GetNonGenericMetadataName(t.MetadataName) == typename));
            return symbols.Distinct();
        }

        internal static string GetNonGenericMetadataName(string metadataName)
        {
            var indexOf = metadataName.IndexOf("`");
            if (indexOf > 1)
                metadataName = metadataName.Substring(0, indexOf);
            return metadataName;
        }

        internal static string GetNoneGenericFullname(string fullName)
        {
            var indexOf = fullName.IndexOf("<");
            if (indexOf > 1)
                fullName = fullName.Substring(0, indexOf);
            return fullName;
        }

        static internal NameSyntax ConvertTypeToUnboundTypeName(string typename)
        {
            var nameSyntax = SyntaxFactory.ParseName(typename);
            if (!(nameSyntax is QualifiedNameSyntax))
                return nameSyntax;
            var qualifiedName = nameSyntax as QualifiedNameSyntax;
            if (qualifiedName.Right is GenericNameSyntax)
            {
                var genericName = SyntaxFactory.GenericName(((GenericNameSyntax)qualifiedName.Right).Identifier);
                var oldArguments = ((GenericNameSyntax)qualifiedName.Right).TypeArgumentList.ChildNodesAndTokens().ToArray();
                var arguments = new SyntaxNodeOrToken[oldArguments.Length - 2];
                Array.Copy(oldArguments, 1, arguments, 0, oldArguments.Length - 2);
                for (int i = 0; i < arguments.Length; i = i + 2)
                    arguments[i] = SyntaxFactory.OmittedTypeArgument();
                genericName = genericName.WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList<TypeSyntax>(arguments)));
                qualifiedName = SyntaxFactory.QualifiedName(qualifiedName.Left, genericName);
            }

            return qualifiedName;
        }

        static internal NameSyntax GetUnboundTypeName(string name, int paramtersCount, ISymbol containingSymbol)
        {
            string containingName = null;
            if (!(containingSymbol is INamespaceSymbol) || !((INamespaceSymbol)containingSymbol).IsGlobalNamespace)
                containingName = containingSymbol.ToDisplayString();
            return GetUnboundTypeName(name, paramtersCount, containingName);
        }

        static internal NameSyntax GetUnboundTypeName(string name, int paramtersCount, string containingName = null)
        {
            var arguments = new List<SyntaxNodeOrToken>();
            for (int i = 0; i < paramtersCount; i++)
            {
                if (i != 0)
                    arguments.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
                arguments.Add(SyntaxFactory.OmittedTypeArgument());
            }

            NameSyntax nameSyntax = SyntaxFactory.GenericName(name).WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList<TypeSyntax>(arguments)));
            if (!string.IsNullOrEmpty(containingName))
                nameSyntax = SyntaxFactory.QualifiedName(SyntaxFactory.ParseName(containingName), (SimpleNameSyntax)nameSyntax);
            return nameSyntax;
        }

        internal static IEnumerable<INamespaceSymbol> LookupNamespaceSymbolsInUSed(SemanticModel semanticModel, SyntaxNode syntaxNode, bool withDeclaredNamespaces = false)
        {
            var namespaceSybols = new List<INamespaceSymbol>();
            var usingClauses = syntaxNode.DescendantNodes().OfType<UsingDirectiveSyntax>();
            var namespaceSymbols = usingClauses.Select(n => LookupNamespace(semanticModel, n.Name.ToFullString())).Where(s => s != null);
            if (withDeclaredNamespaces)
            {
                var declaredClauses = syntaxNode.DescendantNodes().OfType<NamespaceDeclarationSyntax>();
                var declaredNamespaceSymbols = declaredClauses.Select(n => LookupNamespace(semanticModel, n.Name.ToFullString())).Where(s => s != null);
                namespaceSymbols = namespaceSymbols.Union(declaredNamespaceSymbols);
            }

            return namespaceSymbols.Distinct();
        }

        internal static INamespaceSymbol LookupNamespace(SemanticModel semanticModel, string @namespace)
        {
            var names = @namespace.Split('.');
            var namespaceName = "";
            INamespaceSymbol namespaceSymbol = null;
            for (int i = 0; i < names.Length; i++)
            {
                namespaceName = string.IsNullOrEmpty(namespaceName) ? names[i] : $"{namespaceName}.{names[i]}";
                INamespaceSymbol newNamespaceSymbol = null;
                if (namespaceSymbol == null)
                    newNamespaceSymbol = (INamespaceSymbol)semanticModel.LookupNamespacesAndTypes(0, null, namespaceName).FirstOrDefault();
                else
                    newNamespaceSymbol = namespaceSymbol.GetNamespaceMembers().FirstOrDefault(n => n.Name == names[i]);
                if (newNamespaceSymbol != null)
                {
                    namespaceSymbol = newNamespaceSymbol;
                    namespaceName = "";
                }
            }

            return namespaceSymbol;
        }

        internal static IEnumerable<UsingDirectiveSyntax> LookupUsingNamespaces(SyntaxNode syntaxNode)
        {
            var list = new List<UsingDirectiveSyntax>();
            var usings = syntaxNode.DescendantNodes().OfType<UsingDirectiveSyntax>();
            foreach (var @using in usings)
            {
                list.Add(@using);
                list.AddRange(LookupUsingNamespaces(@using));
            }

            return list;
        }

        internal static IEnumerable<INamedTypeSymbol> GetTypes(IAssemblySymbol assemblySymbol)
        {
            return GetNameTypeSymbols(assemblySymbol.GlobalNamespace);
        }

        internal static IEnumerable<INamedTypeSymbol> GetNameTypeSymbols(INamespaceOrTypeSymbol namespaceOrTypeSymbol)
        {
            switch (namespaceOrTypeSymbol)
            {
                case INamedTypeSymbol namedTypeSymbol:
                    yield return namedTypeSymbol;
                    foreach (var typeMember in namedTypeSymbol.GetTypeMembers())
                    {
                        foreach (var nestNamedTypeSymbol in GetNameTypeSymbols(typeMember))
                            yield return nestNamedTypeSymbol;
                    }

                    break;
                case INamespaceSymbol namespaceSymbol:
                    foreach (var childMember in namespaceSymbol.GetMembers())
                    {
                        foreach (var namedTypeSymbol in GetNameTypeSymbols(childMember))
                            yield return namedTypeSymbol;
                    }

                    break;
            }
        }

        internal static string GetNameWithoutGenericMark(ITypeSymbol namedTypeSymbol)
        {
            return GetNameWithoutGenericMark(namedTypeSymbol.ToDisplayString());
        }

        internal static string GetNameWithoutGenericMark(string name)
        {
            var index = name.IndexOf("~");
            if (index > 0)
                name = name.Substring(0, index);
            return name;
        }

        internal static INamedTypeSymbol GetTypeByMetadataName(CSharpCompilation cSharpCompilation, string fullName)
        {
            var names = fullName.Split('.');
            int i = names.Length - 1;
            var symbol = cSharpCompilation.GetTypeByMetadataName(fullName);
            while (symbol == null && i - 1 >= 0)
            {
                i--;
                var sb = new StringBuilder();
                for (int j = 0; j <= i; j++)
                    sb.Append(j != 0 ? "." : "").Append(names[j]);
                symbol = cSharpCompilation.GetTypeByMetadataName(sb.ToString());
            }

            if (symbol != null && i < names.Length - 1)
            {
                for (int j = i + 1; j < names.Length; j++)
                {
                    symbol = symbol.GetTypeMembers(names[j]).FirstOrDefault();
                    if (symbol == null)
                        break;
                }
            }

            return symbol;
        }

        internal static IEnumerable<ITypeSymbol> GetTypeSymbols(CSharpCompilation cSharpCompilation, string name = null)
        {
            var filter = name == null ? new Func<string, bool>(s => true) : new Func<string, bool>(s => s.Length >= name.Length && s.EndsWith(name));
            return cSharpCompilation.GetSymbolsWithName(filter).OfType<ITypeSymbol>();
        }

        internal static IEnumerable<AttributeData> GetAttributeDatasFromSymbol(CSharpCompilation cSharpCompilation, ISymbol fromSymbol, Type attributeType)
        {
            return GetAttributeDatasFromSymbol(fromSymbol, GetTypeByMetadataName(cSharpCompilation, attributeType.FullName));
        }

        internal static IEnumerable<AttributeData> GetAttributeDatasFromSymbol(CSWorkspace cSWorkspace, ISymbol fromSymbol, Type attributeType)
        {
            return GetAttributeDatasFromSymbol(cSWorkspace.CSharpCompilation, fromSymbol, attributeType);
        }

        internal static IEnumerable<AttributeData> GetAttributeDatasFromSymbol(ISymbol fromSymbol, ITypeSymbol attributeTypeSymbol)
        {
            return fromSymbol.GetAttributes().Where(a => a.AttributeClass.ToDisplayString() == attributeTypeSymbol.ToDisplayString());
        }

        internal static IEnumerable<AttributeData> GetAttributeDatasFromAssembly(IAssemblySymbol assemblySymbol, ITypeSymbol attributeTypeSymbol)
        {
            return RoslynHelper.GetAttributeDatasFromSymbol(assemblySymbol, attributeTypeSymbol);
        }

        internal static IEnumerable<INamedTypeSymbol> GetSymbolsWithInterfaceType(CSharpCompilation cSharpCompilation, Type interfaceType)
        {
            return GetSymbolsWithInterfaceType(cSharpCompilation, GetTypeByMetadataName(cSharpCompilation, interfaceType.FullName));
        }

        internal static IEnumerable<INamedTypeSymbol> GetSymbolsWithInterfaceType(CSharpCompilation cSharpCompilation, INamedTypeSymbol interfaceSymbol)
        {
            return GetAssemblyConcernTypeSymbols(cSharpCompilation).Where(t => t.AllInterfaces.Any(i => i.Equals(interfaceSymbol))).Cast<INamedTypeSymbol>();
        }

        internal static bool IsImplementingType(INamedTypeSymbol type, INamedTypeSymbol implementedType)
        {
            if (type.Equals(implementedType))
                return true;
            if (implementedType.TypeKind == TypeKind.Interface)
                return type.AllInterfaces.Any(i => i.Equals(implementedType));
            while (type.BaseType != null)
            {
                if (type.BaseType.Equals(implementedType))
                    return true;
                type = type.BaseType;
            }

            return false;
        }

        internal static bool IsTypenameFound(string searchTypename, ITypeSymbol foundType)
        {
            var names = searchTypename.Split('.');
            if (foundType.Name != names.Last())
                return false;
            if (names.Count() > 1)
            {
                var symbolName = foundType.ToString();
                if (symbolName.Length >= searchTypename.Length)
                {
                    if (searchTypename == symbolName.Substring(symbolName.Length - searchTypename.Length, searchTypename.Length))
                        return true;
                }

                return false;
            }
            else
                return true;
        }

        internal static IEnumerable<IAssemblySymbol> GetReferencedAssemblySymbols(IAssemblySymbol assemblySymbol, bool allLevel = false)
        {
            return _GetReferencedAssemblySymbols(assemblySymbol, allLevel).Distinct();
        }

        static IEnumerable<IAssemblySymbol> _GetReferencedAssemblySymbols(IAssemblySymbol assemblySymbol, bool allLevel)
        {
            foreach (var referencedAssemblySymbol in assemblySymbol.Modules.SelectMany(m => m.ReferencedAssemblySymbols, (m, ass) => ass))
            {
                if (IsAssemblyAspectDNAssembly((IAssemblySymbol)referencedAssemblySymbol))
                {
                    yield return referencedAssemblySymbol;
                    if (allLevel)
                    {
                        foreach (var child in GetReferencedAssemblySymbols(referencedAssemblySymbol, allLevel))
                            yield return child;
                    }
                }
            }
        }

        internal static object GetAttributeConstructorArgValues(TypedConstant constructorArgument)
        {
            object values = null;
            if (!constructorArgument.IsNull)
            {
                if (constructorArgument.Kind == TypedConstantKind.Array)
                {
                    var objects = new object[constructorArgument.Values.Length];
                    for (int i = 0; i < constructorArgument.Values.Length; i++)
                    {
                        var childValues = GetAttributeConstructorArgValues(constructorArgument.Values[i]);
                        objects[i] = childValues;
                    }

                    values = objects;
                }
                else
                {
                    values = constructorArgument.Value;
                }
            }

            return values;
        }

        internal static string GetFullName(TypeDeclarationSyntax type)
        {
            StringBuilder sb = new StringBuilder(type.Identifier.ValueText);
            var declaringType = type.Ancestors().OfType<TypeDeclarationSyntax>().First();
            while (declaringType != null)
                sb.Insert(0, ",").Insert(0, declaringType.Identifier.ValueText);
            var @namespace = type.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
            if (@namespace != null)
                sb.Insert(0, ",").Insert(0, GetFullName(@namespace.Name));
            return sb.ToString();
        }

        internal static string GetFullName(NamespaceDeclarationSyntax @namespace)
        {
            StringBuilder sb = new StringBuilder();
            while (@namespace != null)
            {
                sb.Insert(0, ".").Insert(0, GetFullName(@namespace.Name));
                @namespace = @namespace.Ancestors().OfType<NamespaceDeclarationSyntax>().First();
            }

            return sb.ToString();
        }

        internal static string GetFullName(NameSyntax name)
        {
            switch (name)
            {
                case QualifiedNameSyntax qualifiedName:
                    return GetFullName(qualifiedName);
                case SimpleNameSyntax identifier:
                    return identifier.GetText().ToString();
                default:
                    throw AspectDNErrorFactory.GetException("NotImplementedException");
            }
        }

        internal static string GetFullName(QualifiedNameSyntax qualifiedName)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(GetFullName(qualifiedName.Left)).Append(".").Append(GetFullName(qualifiedName.Right));
            return sb.ToString();
        }

        internal static NameSyntax QualifiedNameSyntax(TypeDeclarationSyntax typeDeclaration)
        {
            NameSyntax typename = SyntaxFactory.IdentifierName(typeDeclaration.Identifier);
            foreach (var parentType in typeDeclaration.Ancestors().OfType<TypeDeclarationSyntax>().Reverse())
            {
                var parentTypeName = SyntaxFactory.IdentifierName(parentType.Identifier);
                if (typename is IdentifierNameSyntax)
                    typename = SyntaxFactory.QualifiedName(parentTypeName, (SimpleNameSyntax)typename);
                else
                {
                    var qualifiedName = SyntaxFactory.QualifiedName(((QualifiedNameSyntax)typename).Left, parentTypeName);
                    typename = SyntaxFactory.QualifiedName(qualifiedName, ((QualifiedNameSyntax)typename).Right);
                }
            }

            return typename;
        }

        internal static bool IsAdviceDeclaration(ClassDeclarationSyntax classDeclaration, CSWorkspace cSWorkspace)
        {
            bool isAdviceDeclaration = false;
            if (classDeclaration.BaseList != null)
            {
                foreach (var type in classDeclaration.BaseList.Types)
                {
                    switch (type.ToString())
                    {
                        case "AspectDN.Weaving.IConcerns.IAdviceDeclaration":
                        case "AspectDN.Weaving.IConcerns.IAdviceCodeDeclaration":
                        case "AspectDN.Weaving.IConcerns.IAdviceStackDeclaration":
                        case "AspectDN.Weaving.IConcerns.IAdviceInterfaceMembersDeclaration":
                        case "AspectDN.Weaving.IConcerns.IAdviceTypeMembersDeclaration":
                        case "AspectDN.Weaving.IConcerns.IAdviceEnumMembersDeclaration":
                        case "AspectDN.Weaving.IConcerns.IAdviceBaseTypeListDeclaration":
                        case "AspectDN.Weaving.IConcerns.IAdviceTypeDeclaration":
                        case "AspectDN.Weaving.IConcerns.IAdviceAttributeDeclaration":
                            isAdviceDeclaration = true;
                            break;
                    }
                }
            }

            return isAdviceDeclaration;
        }

        internal static IEnumerable<SyntaxNode> GetSyntaxNodeUsingType(CSharpCompilation cSharpCompilation, INamedTypeSymbol type)
        {
            foreach (var syntaxTree in cSharpCompilation.SyntaxTrees)
            {
                var semanticModel = cSharpCompilation.GetSemanticModel(syntaxTree);
                var nodes = syntaxTree.GetRoot().DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Where(t => type.Equals(semanticModel.GetTypeInfo((ExpressionSyntax)t).ConvertedType));
                foreach (var node in nodes)
                {
                    SyntaxNode topNode = node;
                    while (topNode.Parent is QualifiedNameSyntax)
                        topNode = topNode.Parent;
                    yield return topNode;
                }
            }
        }

        internal static bool ArePropertySame(IPropertySymbol propertySymbolA, IPropertySymbol propertySymbolB)
        {
            if (propertySymbolA.Name != propertySymbolB.Name)
                return false;
            return AreParametersSame(propertySymbolA.Parameters, propertySymbolB.Parameters);
        }

        internal static bool AreMethodSame(IMethodSymbol methodSymbolA, IMethodSymbol methodSymbolB)
        {
            if (methodSymbolA.Name != methodSymbolB.Name)
                return false;
            return AreParametersSame(methodSymbolA.Parameters, methodSymbolB.Parameters);
        }

        internal static bool AreParametersSame(ImmutableArray<IParameterSymbol> aParameters, ImmutableArray<IParameterSymbol> bParameters)
        {
            if (aParameters.Length != bParameters.Length)
                return false;
            for (int i = 0; i < aParameters.Length; i++)
                if (aParameters[i].Type.ToDisplayString() != bParameters[i].Type.ToDisplayString())
                    return false;
            return true;
        }

        internal static TypeSyntax ParseTypeName(ITypeSymbol type)
        {
            if (type.SpecialType != SpecialType.None)
                return SyntaxFactory.ParseTypeName(type.ToDisplayString());
            else
                return CSAspectCompilerHelper.ParseTypename(type.ToDisplayString());
        }

#region GetSymbolMembers
        internal static IEnumerable<ISymbol> GetSymbolMembers(ITypeSymbol typeSymbol, bool includingInheritedMembers)
        {
            return _GetSymbolMembers(typeSymbol, includingInheritedMembers, new List<ISymbol>());
        }

        static List<ISymbol> _GetSymbolMembers(ITypeSymbol typeSymbol, bool includingInheritedMembers, List<ISymbol> symbolMembers)
        {
            var newSymbolMembers = typeSymbol.GetMembers().Where(t => !symbolMembers.Any(s => s.ToDisplayString() == t.ToDisplayString())).ToList();
            newSymbolMembers.AddRange(symbolMembers);
            if (includingInheritedMembers && HasBaseType(typeSymbol))
                newSymbolMembers = _GetSymbolMembers(typeSymbol.BaseType, includingInheritedMembers, newSymbolMembers);
            return newSymbolMembers;
        }

#endregion
        internal static bool HasBaseType(ITypeSymbol typeSymbol)
        {
            return (typeSymbol.BaseType != null && typeSymbol.BaseType.SpecialType == SpecialType.None && !typeSymbol.BaseType.IsValueType);
        }

        internal static bool IsPrototypeItemMappingEquals(AttributeData propertyItemMapiingAttributeData1, AttributeData propertyItemMapiingAttributeData2)
        {
            if (Enum.GetName(typeof(PrototypeItemMappingSourceKinds), (PrototypeItemMappingSourceKinds)propertyItemMapiingAttributeData1.ConstructorArguments[0].Value) != Enum.GetName(typeof(PrototypeItemMappingSourceKinds), (PrototypeItemMappingSourceKinds)propertyItemMapiingAttributeData2.ConstructorArguments[0].Value))
                return false;
            if (propertyItemMapiingAttributeData1.ConstructorArguments[1].Value.GetType().FullName != propertyItemMapiingAttributeData2.ConstructorArguments[1].Value.GetType().FullName)
                return false;
            switch (propertyItemMapiingAttributeData1.ConstructorArguments[1].Value)
            {
                case string s:
                    if (propertyItemMapiingAttributeData2.ConstructorArguments[1].Value as string != s)
                        return false;
                    break;
                case ITypeSymbol type:
                    if ((propertyItemMapiingAttributeData2.ConstructorArguments[1].Value as ITypeSymbol).ToDisplayString() != type.ToDisplayString())
                        return false;
                    break;
                default:
                    throw new NotImplementedException();
            }

            if (Enum.GetName(typeof(PrototypeItemMappingTargetKinds), (PrototypeItemMappingTargetKinds)propertyItemMapiingAttributeData1.ConstructorArguments[2].Value) != Enum.GetName(typeof(PrototypeItemMappingTargetKinds), (PrototypeItemMappingTargetKinds)propertyItemMapiingAttributeData2.ConstructorArguments[2].Value))
                return false;
            ;
            return (string)propertyItemMapiingAttributeData1.ConstructorArguments[3].Value == (string)propertyItemMapiingAttributeData2.ConstructorArguments[3].Value;
        }

        internal static SyntaxNode ReplaceIdentifierNameNodes(SyntaxNode syntaxNode, string oldIdentifierName, string newIdentifierName)
        {
            var changes = new List<(SyntaxNode old, SyntaxNode @new)>();
            foreach (var identifierNameSyntax in syntaxNode.DescendantNodes().Where(t => t is IdentifierNameSyntax && ((IdentifierNameSyntax)t).Identifier.ValueText == oldIdentifierName))
            {
                var newIdentifierNameSyntax = SyntaxFactory.IdentifierName(newIdentifierName);
                changes.Add((identifierNameSyntax, newIdentifierNameSyntax));
            }

            return ReplaceNodes(syntaxNode, changes.ToArray());
        }

        internal static SyntaxNode ReplaceNodes(SyntaxNode syntaxNode, (SyntaxNode old, SyntaxNode @new)[] changes)
        {
            var newSyntaxNode = syntaxNode;
            for (int i = 0; i < changes.Length; i++)
            {
                var old = newSyntaxNode.DescendantNodes().FirstOrDefault(t => t.Span.Start == changes[i].old.Span.Start && t.Span.Length == changes[i].old.Span.Length && t.GetType().ToString() == changes[i].old.GetType().ToString());
                var annotedOld = old.WithAdditionalAnnotations(new SyntaxAnnotation("change", $"{i}"));
                newSyntaxNode = newSyntaxNode.ReplaceNode(old, annotedOld);
                changes[i].old = annotedOld;
            }

            for (int i = 0; i < changes.Length; i++)
            {
                var old = newSyntaxNode.DescendantNodes().FirstOrDefault(t => t.GetAnnotations("change").Any(a => a.Data == $"{i}"));
                newSyntaxNode = newSyntaxNode.ReplaceNode(old, changes[i].@new);
            }

            return newSyntaxNode;
        }

        internal static bool AreSymbolSame(ISymbol symbol1, ISymbol symbol2)
        {
            if (symbol1.GetType().FullName != symbol2.GetType().FullName)
                return false;
            if (symbol1.Name != symbol2.Name)
                return false;
            switch (symbol1)
            {
                case IFieldSymbol fieldSymbol1:
                    return SymbolEqualityComparer.Default.Equals(fieldSymbol1.Type, ((IFieldSymbol)symbol2).Type);
                case IPropertySymbol propertySymbol1:
                    return SymbolEqualityComparer.Default.Equals(propertySymbol1.Type, ((IPropertySymbol)symbol2).Type) && AreSymbolSame(propertySymbol1.Parameters.ToArray(), ((IPropertySymbol)symbol2).Parameters.ToArray());
                case IMethodSymbol methodSymbo11:
                    return SymbolEqualityComparer.Default.Equals(methodSymbo11.ReturnType, ((IMethodSymbol)symbol2).ReturnType) && AreSymbolSame(methodSymbo11.Parameters.ToArray(), ((IMethodSymbol)symbol2).Parameters.ToArray());
                case IEventSymbol eventSymbol1:
                    return SymbolEqualityComparer.Default.Equals(eventSymbol1.Type, ((IEventSymbol)symbol2).Type);
                case ITypeSymbol typeSymbol1:
                    return SymbolEqualityComparer.Default.Equals(typeSymbol1, (ITypeSymbol)symbol2);
                default:
                    throw new NotSupportedException();
            }
        }

        internal static bool AreSymbolSame(IParameterSymbol[] parameterSymbol1s, IParameterSymbol[] parameterSymbol2s)
        {
            if (parameterSymbol1s.Length != parameterSymbol2s.Length)
                return false;
            for (int i = 0; i < parameterSymbol1s.Length; i++)
            {
                if (SymbolEqualityComparer.Default.Equals(parameterSymbol1s[i].Type, parameterSymbol2s[i].Type) && parameterSymbol1s[i].RefKind == parameterSymbol2s[i].RefKind)
                    continue;
                return false;
            }

            return true;
        }
    }
}