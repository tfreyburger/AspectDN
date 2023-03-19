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
using Microsoft.CodeAnalysis.Emit;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using AspectDN.Aspect.Compilation.Foundation;

namespace AspectDN.Aspect.Compilation.CS
{
    internal class CSWorkspace : Workspace
    {
        CSharpCompilation _CSharpCompilation;
        protected byte[] _AspectRepositoryConstant = new byte[]{77, 90, 144, 0, 3, 0, 0, 0, 4, 0, 0, 0, 255, 255, 0, 0, 184, 0, 0, 0, 0, 0, 0, 0, 64, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 128, 0, 0, 0, 14, 31, 186, 14, 0, 180, 9, 205, 33, 184, 1, 76, 205, 33, 84, 104, 105, 115, 32, 112, 114, 111, 103, 114, 97, 109, 32, 99, 97, 110, 110, 111, 116, 32, 98, 101, 32, 114, 117, 110, 32, 105, 110, 32, 68, 79, 83, 32, 109, 111, 100, 101, 46, 13, 13, 10, 36, 0, 0, 0, 0, 0, 0, 0};
        internal IEnumerable<SyntaxTree> SyntaxTrees { get => _CSharpCompilation.SyntaxTrees; }

        internal string AssemblyName
        {
            get
            {
                return _CSharpCompilation.AssemblyName;
            }

            set
            {
                if (value == _CSharpCompilation.AssemblyName)
                    return;
                _SetAssemblyName(value);
            }
        }

        public CSharpCompilation CSharpCompilation { get => _CSharpCompilation; }

        internal CSWorkspace() : base()
        {
            _ReferencedAssemblies.AddRange(new[]{MetadataReference.CreateFromFile(typeof(object).Assembly.Location), MetadataReference.CreateFromFile(typeof(Func<>).Assembly.Location), MetadataReference.CreateFromFile(typeof(HashSet<>).Assembly.Location), MetadataReference.CreateFromFile(typeof(IPointcutDefinition).Assembly.Location), MetadataReference.CreateFromFile(typeof(AspectDN.Aspect.Weaving.IConcerns.PointcutAttribute).Assembly.Location), MetadataReference.CreateFromFile(typeof(IQueryable).Assembly.Location), MetadataReference.CreateFromFile(typeof(AssemblyDefinition).Assembly.Location), MetadataReference.CreateFromFile(typeof(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException).Assembly.Location)});
            var csharpOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            _CSharpCompilation = CSharpCompilation.Create("", options: csharpOptions, references: ReferencedAssemblies);
        }

        internal void GenerateSouceFiles(string path)
        {
            int i = 0;
            foreach (var syntaxTree in SyntaxTrees)
            {
                var filename = Path.ChangeExtension(Path.GetFileName(syntaxTree.FilePath), "CS");
                if (string.IsNullOrEmpty(filename))
                    filename = Path.ChangeExtension(i++.ToString(), "CS");
                using (var sw = new StreamWriter($"{path}.{filename}"))
                    sw.Write(syntaxTree.GetRoot().NormalizeWhitespace().ToFullString());
            }
        }

        internal override void AddReferencedAssembly(params string[] referencedAssemblyFilenames)
        {
            MetadataReference[] referencedAssemblies = new MetadataReference[referencedAssemblyFilenames.Count(t => !string.IsNullOrEmpty(t))];
            for (int i = 0; i < referencedAssemblies.Length; i++)
            {
                if (string.IsNullOrEmpty(referencedAssemblyFilenames[i]))
                    continue;
                MetadataReference referencedAssembly = null;
                string fullfilename = Helper.GetFullPath(referencedAssemblyFilenames[i]);
                if (Path.GetExtension(fullfilename).ToUpper() == ".ASPDN")
                {
                    var fileBytes = File.ReadAllBytes(fullfilename);
                    var aspectRepositoryBytes = new byte[fileBytes.Length + _AspectRepositoryConstant.Length];
                    Array.Copy(_AspectRepositoryConstant, aspectRepositoryBytes, _AspectRepositoryConstant.Length);
                    Array.Copy(fileBytes, 0, aspectRepositoryBytes, 128, fileBytes.Length);
                    referencedAssembly = MetadataReference.CreateFromImage((byte[])aspectRepositoryBytes, MetadataReferenceProperties.Assembly, null, fullfilename);
                }
                else
                    referencedAssembly = MetadataReference.CreateFromFile(fullfilename);
                if (referencedAssembly == null)
                    throw AspectDNErrorFactory.GetException("UnknowmReferencedAssembly", referencedAssembly.Display);
                referencedAssemblies[i] = referencedAssembly;
            }

            AddReferencedAssembly(referencedAssemblies);
        }

        internal void AddReferencedAssembly(params MetadataReference[] referencedAssemblies)
        {
            foreach (var assembly in referencedAssemblies)
            {
                if (ReferencedAssemblies.Exists(t => t.Display == assembly.Display))
                    continue;
                ReferencedAssemblies.Add(assembly);
            }

            _CSharpCompilation = _CSharpCompilation.WithReferences(ReferencedAssemblies);
        }

        internal IEnumerable<SyntaxNode> GetSyntaxNodeUsingType(INamedTypeSymbol fromType)
        {
            return RoslynHelper.GetSyntaxNodeUsingType(_CSharpCompilation, fromType);
        }

        internal IEnumerable<ITypeSymbol> GetAllReferencedConcernTypesFromType(INamedTypeSymbol fromAdviceSymbol)
        {
            return RoslynHelper.GetAllReferencedConcernTypesFromType(_CSharpCompilation, fromAdviceSymbol);
        }

        internal bool IsImplementedType(INamedTypeSymbol type, string fullImplementedTypeName)
        {
            var implementedType = _CSharpCompilation.GetTypeByMetadataName(fullImplementedTypeName);
            return RoslynHelper.IsImplementingType(type, implementedType);
        }

        internal IEnumerable<AttributeData> GetAttributeDatasFromSymbol(ISymbol fromSymbol, Type attributeType)
        {
            return RoslynHelper.GetAttributeDatasFromSymbol(_CSharpCompilation, fromSymbol, attributeType);
        }

        internal IEnumerable<AttributeData> GetAssemblyAttributeDatas(Type attributeType)
        {
            return RoslynHelper.GetAttributeDatasFromAssembly(_CSharpCompilation.Assembly, RoslynHelper.GetTypeByMetadataName(_CSharpCompilation, attributeType.FullName));
        }

        internal IEnumerable<IAssemblySymbol> GetAssemblySymbols(bool allLevel = false)
        {
            IEnumerable<IAssemblySymbol> assemblies = new IAssemblySymbol[]{_CSharpCompilation.Assembly};
            if (allLevel)
                assemblies = assemblies.Union(RoslynHelper.GetReferencedAssemblySymbols(_CSharpCompilation.Assembly, allLevel));
            return assemblies.Distinct();
        }

        internal IEnumerable<IAssemblySymbol> GetReferencedAssemblySymbols(bool allLevel = false)
        {
            return RoslynHelper.GetReferencedAssemblySymbols(_CSharpCompilation.Assembly, allLevel).Distinct();
        }

        internal IEnumerable<INamedTypeSymbol> GetTypeSymbolsImplementingType(Type implementedType, bool onlyInThisAssembly = false)
        {
            var interfaceSymbol = RoslynHelper.GetTypeByMetadataName(_CSharpCompilation, implementedType.FullName);
            var namedTypeSymbols = RoslynHelper.GetAssemblyConcernTypeSymbols(_CSharpCompilation).Cast<INamedTypeSymbol>();
            var results = namedTypeSymbols.Where(t => t.ConstructedFrom.AllInterfaces.Any(i => i.ToDisplayString() == interfaceSymbol.ToDisplayString()) && (!onlyInThisAssembly || t.ContainingAssembly.Name == _CSharpCompilation.Assembly.MetadataName));
            return results;
        }

        internal EmitResult Emit(MemoryStream stream, MemoryStream pdbStream = null)
        {
            var emitResut = _CSharpCompilation.Emit(stream, pdbStream);
            return emitResut;
        }

        internal ISymbol GetSymbolFromSyntaxNodeDeclaration(SyntaxNode syntaxNode)
        {
            return _CSharpCompilation.GetSemanticModel(syntaxNode.SyntaxTree).GetDeclaredSymbol(syntaxNode);
        }

        internal CSharpCompilation SetTrees(params SyntaxTree[] trees)
        {
            var syntaxOptions = new CSharpParseOptions(LanguageVersion.Default);
            if (trees.Length > 0)
                syntaxOptions = new CSharpParseOptions(((CSharpParseOptions)trees.First().Options).LanguageVersion);
            var attributeTree = CSharpSyntaxTree.ParseText($"[assembly:{typeof(AspectDNAssemblyAttribute).FullName}]", syntaxOptions);
            return _CSharpCompilation = _CSharpCompilation.RemoveAllSyntaxTrees().AddSyntaxTrees(trees).AddSyntaxTrees(attributeTree);
        }

        internal ImmutableArray<Diagnostic> GetDiagnostics()
        {
            Thread.Sleep(3600);
            CancellationToken cancellationToken = new CancellationToken();
            var diagnostics = _CSharpCompilation.WithAssemblyName(AssemblyName).GetDiagnostics(cancellationToken);
            while (cancellationToken.IsCancellationRequested)
                ;
            return diagnostics;
        }

        internal SyntaxTree ReplaceNode(SyntaxNode oldNode, SyntaxNode newNode)
        {
            var oldCompilationUnit = oldNode.SyntaxTree.GetRoot();
            var oldSyntaxTree = oldNode.SyntaxTree;
            var newTree = oldCompilationUnit.ReplaceNode(oldNode, newNode).SyntaxTree;
            _CSharpCompilation = _CSharpCompilation.ReplaceSyntaxTree(oldSyntaxTree, newTree);
            return newTree;
        }

        public void ReplaceNodes(IEnumerable<(SyntaxNode old, SyntaxNode @new)> changes)
        {
            var trees = changes.Select(t => t.old.SyntaxTree).Distinct().ToList();
            foreach (var syntaxTree in trees)
                ReplaceNodes(syntaxTree, changes.Where(t => t.old.SyntaxTree == syntaxTree).ToArray());
        }

        public SyntaxTree ReplaceNodes(SyntaxTree oldSyntaxTree, (SyntaxNode old, SyntaxNode @new)[] changes)
        {
            var newSyntaxTree = oldSyntaxTree;
            for (int i = 0; i < changes.Length; i++)
            {
                var old = newSyntaxTree.GetRoot().DescendantNodes().FirstOrDefault(t => t.Span.Start == changes[i].old.Span.Start && t.Span.Length == changes[i].old.Span.Length && t.GetType().ToString() == changes[i].old.GetType().ToString());
                var annotedOld = old.WithAdditionalAnnotations(new SyntaxAnnotation("change", $"{i}"));
                newSyntaxTree = newSyntaxTree.GetRoot().ReplaceNode(old, annotedOld).SyntaxTree;
                changes[i].old = annotedOld;
            }

            for (int i = 0; i < changes.Length; i++)
            {
                var old = newSyntaxTree.GetRoot().DescendantNodes().FirstOrDefault(t => t.GetAnnotations("change").Any(a => a.Data == $"{i}"));
                newSyntaxTree = newSyntaxTree.GetRoot().ReplaceNode(old, changes[i].@new).SyntaxTree;
            }

            _CSharpCompilation = _CSharpCompilation.ReplaceSyntaxTree(oldSyntaxTree, newSyntaxTree);
            return newSyntaxTree;
        }

        internal INamedTypeSymbol GetSpecialType(SpecialType specialType)
        {
            return _CSharpCompilation.GetSpecialType(specialType);
        }

        internal INamedTypeSymbol BuildGenericTypeSymbol(string fullName, params object[] argumentTypeSymbols)
        {
            var symbol = _CSharpCompilation.GetTypeByMetadataName($"{fullName}`{argumentTypeSymbols.Count()}");
            INamedTypeSymbol[] args = new INamedTypeSymbol[argumentTypeSymbols.Count()];
            for (int i = 0; i < args.Length; i++)
            {
                switch (argumentTypeSymbols[i])
                {
                    case SpecialType specialType:
                        args[i] = _CSharpCompilation.GetSpecialType(specialType);
                        break;
                    case INamedTypeSymbol typeSymbol:
                        args[i] = typeSymbol;
                        break;
                    default:
                        throw new NotFiniteNumberException();
                }
            }

            return symbol.Construct(args);
        }

        internal INamedTypeSymbol GetTypeByMetadataName(string fullName)
        {
            return RoslynHelper.GetTypeByMetadataName(_CSharpCompilation, fullName);
        }

        internal INamedTypeSymbol GetNamedTypeSymbol(SyntaxNode nodeDeclaration)
        {
            var semanticModel = _CSharpCompilation.GetSemanticModel(nodeDeclaration.SyntaxTree);
            var symbol = semanticModel.GetDeclaredSymbol(nodeDeclaration);
            if (symbol is INamedTypeSymbol)
                return (INamedTypeSymbol)symbol;
            return null;
        }

        internal INamedTypeSymbol LookupLookupDeclaredNamespaceSymbol(string fullyQualifiedMetadataName)
        {
            return _CSharpCompilation.GetTypeByMetadataName(fullyQualifiedMetadataName);
        }

        internal IEnumerable<ITypeSymbol> GetTypeSymbols(Func<string, bool> selection)
        {
            return _CSharpCompilation.GetSymbolsWithName(selection, SymbolFilter.Type).Cast<ITypeSymbol>();
        }

        internal SemanticModel GetSemanicModel(SyntaxTree tree)
        {
            return _CSharpCompilation.GetSemanticModel(tree);
        }

        internal ITypeSymbol GetTypeSymbolFromSyntaxNodeExpresson(SyntaxNode syntaxNode)
        {
            return _CSharpCompilation.GetSemanticModel(syntaxNode.SyntaxTree).GetTypeInfo(syntaxNode).ConvertedType;
        }

        internal IEnumerable<ITypeSymbol> GetAllTypeSymbols(Type interfaceType)
        {
            return RoslynHelper.GetTypeSymbols(_CSharpCompilation, interfaceType);
        }

        internal IEnumerable<INamedTypeSymbol> GetReferencedAssemblyConcernTypeSymbols()
        {
            var referencedAssemblies = RoslynHelper.GetReferencedAssemblySymbols(_CSharpCompilation.Assembly, true);
            return referencedAssemblies.SelectMany(a => RoslynHelper.GetNamedTypeSymbols(a), (a, symbols) => symbols);
        }

        internal IEnumerable<INamedTypeSymbol> GetAssemblyConcernTypeSymbols()
        {
            var typeSymbols = new List<INamedTypeSymbol>();
            foreach (var syntaxTree in _CSharpCompilation.SyntaxTrees)
                typeSymbols.AddRange(RoslynHelper.GetConcernTypeSymbols(_CSharpCompilation, syntaxTree));
            return typeSymbols.Distinct();
        }

        internal IEnumerable<INamedTypeSymbol> GetAllTypeSymbols()
        {
            return GetAllTypeSymbols(_CSharpCompilation.GlobalNamespace);
        }

        internal IEnumerable<INamedTypeSymbol> GetAllTypeSymbols(INamespaceSymbol @namespace)
        {
            foreach (var namespaeceOrType in @namespace.GetMembers())
            {
                switch (namespaeceOrType)
                {
                    case INamespaceSymbol namespaceSymbol:
                        foreach (var type in GetAllTypeSymbols(namespaceSymbol))
                            yield return type;
                        break;
                    case INamedTypeSymbol typeSymbol:
                        yield return typeSymbol;
                        break;
                }
            }
        }

        internal SymbolInfo GetSymbolFromExpression(SyntaxNode syntaxNode)
        {
            return GetSemanicModel(syntaxNode.SyntaxTree).GetSymbolInfo(syntaxNode);
        }

        internal TypeInfo GetTypeInfo(ExpressionSyntax expression)
        {
            return GetSemanicModel(expression.SyntaxTree).GetTypeInfo(expression);
        }

        internal IEnumerable<ITypeSymbol> GetTypeSymbolsFromFullName(string fullName)
        {
            return GetTypeSymbols(s => s == fullName);
        }

        internal IEnumerable<ITypeSymbol> GetTypeSymbolsFromShortName(string shortName)
        {
            return GetTypeSymbols(s => s.Length >= shortName.Length && s.EndsWith($".{shortName}"));
        }

        internal IEnumerable<ITypeSymbol> GetTypeSymbolsFromName(string name)
        {
            return GetTypeSymbolsFromFullName(name).Union(GetTypeSymbolsFromShortName(name));
        }

        internal IEnumerable<ITypeSymbol> GetTypeSymbols(string searchTypename, SyntaxTree inSyntaxTree)
        {
            return RoslynHelper.GetTypeSymbols(_CSharpCompilation, inSyntaxTree, searchTypename);
        }

        internal IEnumerable<ITypeSymbol> GetTypeSymbols(string searchTypename)
        {
            var names = searchTypename.Split('.');
            var symbols = GetTypeSymbols(s => s == names[names.Length - 1]);
            foreach (var symbol in symbols)
            {
                if (names.Count() > 1)
                {
                    var symbolName = symbol.ToString();
                    if (symbolName.Length >= searchTypename.Length)
                    {
                        if (searchTypename == symbolName.Substring(symbolName.Length - searchTypename.Length, searchTypename.Length))
                            yield return symbol;
                    }
                }
                else
                    yield return symbol;
            }
        }

        internal IEnumerable<INamedTypeSymbol> LokupAdvices(SyntaxTree syntaxTree, INamespaceSymbol containingNamespace, string typename)
        {
            return RoslynHelper.LookupAdvices(_CSharpCompilation, syntaxTree, containingNamespace, typename);
        }

        internal IEnumerable<INamedTypeSymbol> LookupPrototypeTypes(SyntaxTree syntaxTree, string typename)
        {
            return RoslynHelper.LookupPrototypeTypes(_CSharpCompilation, syntaxTree, typename);
        }

        internal IEnumerable<INamedTypeSymbol> GetNamedTypeSymbols()
        {
            return RoslynHelper.GetNamedTypeSymbols(_CSharpCompilation);
        }

        internal IEnumerable<ITypeSymbol> GetTypeSymbols()
        {
            return RoslynHelper.GetAssemblyConcernTypeSymbols(_CSharpCompilation);
        }

        internal ControlFlowAnalysis GetControlFlowAnalysis(MethodDeclarationSyntax methodSyntax)
        {
            var first = methodSyntax.Body.DescendantNodes().OfType<StatementSyntax>().FirstOrDefault();
            var last = methodSyntax.Body.DescendantNodes().OfType<StatementSyntax>().LastOrDefault();
            return GetSemanicModel(methodSyntax.SyntaxTree).AnalyzeControlFlow(methodSyntax.DescendantNodes().OfType<BlockSyntax>().FirstOrDefault());
        }

        internal IEnumerable<SyntaxNodeType> GetAllNodes<SyntaxNodeType>()
        {
            List<SyntaxNodeType> nodes = new List<SyntaxNodeType>();
            foreach (var tree in _CSharpCompilation.SyntaxTrees)
            {
                foreach (var node in tree.GetRoot().DescendantNodes().OfType<SyntaxNodeType>())
                    nodes.Add(node);
            }

            return nodes;
        }

        internal List<MethodDeclarationSyntax> GetMethodDeclarations<AdviceType>(SyntaxTree tree)
        {
            return tree.GetCompilationUnitRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(t => t.Parent is ClassDeclarationSyntax && ((ClassDeclarationSyntax)t.Parent).BaseList.DescendantNodes().OfType<ICodeAdviceDeclaration>().Count() != 0).ToList();
        }

        internal List<ClassDeclarationSyntax> GetClassDeclarations<AdviceType>(SyntaxTree tree)
        {
            return tree.GetCompilationUnitRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Where(t => t.BaseList.Types.Where(b => b.ToString() == typeof(AdviceType).ToString()).Count() != 0).ToList();
        }

        internal List<ClassDeclarationSyntax> GetClassDeclarations<AdviceType>()
        {
            List<ClassDeclarationSyntax> classes = new List<ClassDeclarationSyntax>();
            foreach (var tree in SyntaxTrees)
                classes.AddRange(GetClassDeclarations<AdviceType>(tree));
            return classes;
        }

        internal SyntaxNode GetFistNodeWithAnnotation(SyntaxTree tree, string annotationKind)
        {
            return tree.GetRoot().GetAnnotatedNodes(annotationKind).FirstOrDefault();
        }

        internal IEnumerable<SyntaxNode> GetNodeWithAnnotations(SyntaxTree tree, string annotationKind)
        {
            return tree.GetRoot().GetAnnotatedNodes(annotationKind);
        }

        internal IEnumerable<SyntaxNode> GetNodeWithAnnotations(string annotationKind)
        {
            foreach (var tree in SyntaxTrees)
            {
                foreach (var node in GetNodeWithAnnotations(annotationKind))
                    yield return node;
            }
        }

        void _SetAssemblyName(string assemblyName)
        {
            _CSharpCompilation = _CSharpCompilation.WithAssemblyName(assemblyName);
        }
    }
}