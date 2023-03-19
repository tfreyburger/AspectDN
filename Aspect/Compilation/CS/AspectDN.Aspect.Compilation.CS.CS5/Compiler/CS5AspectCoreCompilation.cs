// Author:
//
// T. Freyburger (t.freyburger@gmail.com)
//
// Copyright (c)  Thierry Freyburger
//
// Licensed under the GPLV3 license.
////
using AspectDN.Aspect.Weaving.IConcerns;
using AspectDN.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TokenizerDN.Common;
using TokenizerDN.Common.SourceAnalysis;

namespace AspectDN.Aspect.Compilation.CS.CS5
{
    internal class CS5AspectCoreCompilation : CSAspectCoreCompilation
    {
        Dictionary<string, CSAspectNode> _AspectItems;
        Dictionary<string, AttributeData> _PrototypeTypesMappings;
        internal CS5AspectCoreCompilation(CSWorkspace workspace, string loggerName, string logFilename) : base(workspace, loggerName, logFilename)
        {
        }

        protected override void _Compile()
        {
            if (_Errors.Count() != 0)
                return;
            _Setup();
            _GenerateTrees();
            _Complete();
            _Checkitems();
        }

        void _Setup()
        {
            _Errors = new List<ICompilerError>();
            _AspectItems = new Dictionary<string, CSAspectNode>();
            _PrototypeTypesMappings = new Dictionary<string, AttributeData>();
        }

        void _GenerateTrees()
        {
            var cSharpParseOptions = new CSharpParseOptions(LanguageVersion.CSharp5);
            List<SyntaxTree> syntaxTrees = new List<SyntaxTree>();
            foreach (var aspectTree in CSAspectTrees)
            {
                var syntaxTree = SyntaxFactory.SyntaxTree((SyntaxNode)aspectTree.AspectRoot.GetSyntaxNode(), cSharpParseOptions, aspectTree.Document.Filename);
                aspectTree.SyntaxTree = syntaxTree;
                syntaxTrees.Add(syntaxTree);
            }

            CSWorkspace.SetTrees(syntaxTrees.ToArray());
        }

        void _Complete()
        {
            _CompletePrototypeType();
            _CompletePrototypeTypeMappings();
            _CompleteAdvices();
            _CompleteAspects();
        }

        void _CompletePrototypeType()
        {
            new PrototypeTypesModifier().ApplyReferencedPrototypeOrAdviceTypes(CSWorkspace);
        }

        void _CompletePrototypeTypeMappings()
        {
            var changes = new List<(SyntaxNode oldNode, SyntaxNode newNode)>();
            foreach (AttributeData prototypeTypeMappingAttributeData in CSWorkspace.GetAssemblyAttributeDatas(typeof(PrototypeTypeMappingAttribute)))
            {
                var prototypeType = (INamedTypeSymbol)prototypeTypeMappingAttributeData.ConstructorArguments[0].Value;
                if (prototypeType.Kind != SymbolKind.ErrorType)
                    continue;
                var prototypeTypename = RoslynHelper.GetNoneGenericFullname(prototypeType.ToDisplayString());
                var prototypeSymbols = CSWorkspace.LookupPrototypeTypes(prototypeTypeMappingAttributeData.ApplicationSyntaxReference.GetSyntax().SyntaxTree, prototypeTypename);
                if (!prototypeSymbols.Any())
                    prototypeSymbols = CSWorkspace.GetReferencedAssemblyConcernTypeSymbols();
                if (prototypeSymbols.Any())
                {
                    if (prototypeSymbols.Count() > 1)
                    {
                        _Errors.Add(AspectDNErrorFactory.GetCompilerError("AmbigusAdvice", _GetSourceLocation(prototypeTypeMappingAttributeData.ApplicationSyntaxReference.GetSyntax()), prototypeType.ToDisplayString()));
                    }
                    else
                    {
                        var prototypeTypeDefinition = prototypeSymbols.First();
                        var adviceAttributeSyntax = (AttributeSyntax)prototypeTypeMappingAttributeData.ApplicationSyntaxReference.GetSyntax();
                        var namespaceName = SyntaxFactory.ParseName(prototypeTypeDefinition.ContainingNamespace.ToDisplayString());
                        var name = (SimpleNameSyntax)SyntaxFactory.IdentifierName(prototypeTypeDefinition.Name);
                        if (prototypeType.IsGenericType)
                            name = adviceAttributeSyntax.DescendantNodes().OfType<GenericNameSyntax>().First();
                        var typeofName = SyntaxFactory.QualifiedName(namespaceName, name);
                        var argument = SyntaxFactory.AttributeArgument(SyntaxFactory.TypeOfExpression(typeofName));
                        var newAdviceAttributeSyntax = adviceAttributeSyntax.WithArgumentList(SyntaxFactory.AttributeArgumentList(new SeparatedSyntaxList<AttributeArgumentSyntax>().AddRange(new AttributeArgumentSyntax[]{argument, adviceAttributeSyntax.ArgumentList.Arguments.Last()})));
                        changes.Add((adviceAttributeSyntax, newAdviceAttributeSyntax));
                    }
                }
            }

            CSWorkspace.ReplaceNodes(changes);
        }

        void _CompleteAdvices()
        {
            var assemblyAdviceTypes = CSWorkspace.GetTypeSymbolsImplementingType(typeof(IAdviceDeclaration), true);
            _CompleteReferencedPrototypeTypeAndAdvice(assemblyAdviceTypes);
            assemblyAdviceTypes = CSWorkspace.GetTypeSymbolsImplementingType(typeof(IAdviceDeclaration), true);
            _ChangeReferencedPrototypeItem(assemblyAdviceTypes);
            assemblyAdviceTypes = CSWorkspace.GetTypeSymbolsImplementingType(typeof(IAdviceDeclaration), true).Where(t => t.GetAttributes().Any(a => a.AttributeClass.MetadataName == nameof(ThisDeclarationAttribute)));
            var prototypeTypeAndAdviceList = _GetPrototypeTypesAndAdvices(assemblyAdviceTypes);
            _ChangePrototypeTypeThisDeclaration(prototypeTypeAndAdviceList);
            assemblyAdviceTypes = CSWorkspace.GetTypeSymbolsImplementingType(typeof(IAdviceDeclaration), true).Where(t => t.GetAttributes().Any(a => a.AttributeClass.MetadataName == nameof(ThisDeclarationAttribute)));
            prototypeTypeAndAdviceList = _GetPrototypeTypesAndAdvices(assemblyAdviceTypes);
            new ThsDeclarationAdviceModifier().ApplyChanges(prototypeTypeAndAdviceList, CSWorkspace);
            _ChengeAbstractTypeMembersModifier();
        }

        void _ChengeAbstractTypeMembersModifier()
        {
            var changes = new List<(SyntaxNode oldNode, SyntaxNode newNode)>();
            var types = CSWorkspace.GetTypeSymbols();
            if (types == null)
                return;
            var overrideAdviceTypeMembers = types.Where(t => t.AllInterfaces.Any(i => i.ToDisplayString() == typeof(ITypesAdviceDeclaration).FullName)).SelectMany(a => a.GetMembers().OfType<ITypeSymbol>()).SelectMany(a => a.GetMembers().Where(m => m.IsOverride)).Where(m => (m is IPropertySymbol && ((IPropertySymbol)m).OverriddenProperty != null && ((IPropertySymbol)m).OverriddenProperty.ContainingType.GetAttributes().Any(at => at.AttributeClass.ConstructedFrom.ToDisplayString() == typeof(PrototypeTypeDeclarationAttribute).FullName)) || (m is IMethodSymbol && ((IMethodSymbol)m).OverriddenMethod != null && ((IMethodSymbol)m).OverriddenMethod.ContainingType.GetAttributes().Any(at => at.AttributeClass.ConstructedFrom.ToDisplayString() == typeof(PrototypeTypeDeclarationAttribute).FullName)) || (m is IEventSymbol && ((IEventSymbol)m).OverriddenEvent != null && ((IEventSymbol)m).OverriddenEvent.ContainingType.GetAttributes().Any(at => at.AttributeClass.ConstructedFrom.ToDisplayString() == typeof(PrototypeTypeDeclarationAttribute).FullName)));
            foreach (var overrideAdviceTypeMember in overrideAdviceTypeMembers.Where(t => t.DeclaringSyntaxReferences.Any()))
            {
                if (overrideAdviceTypeMember is IMethodSymbol)
                {
                    if (((IMethodSymbol)overrideAdviceTypeMember).MethodKind == MethodKind.PropertySet || ((IMethodSymbol)overrideAdviceTypeMember).MethodKind == MethodKind.PropertyGet || ((IMethodSymbol)overrideAdviceTypeMember).MethodKind == MethodKind.EventAdd || ((IMethodSymbol)overrideAdviceTypeMember).MethodKind == MethodKind.EventRemove)
                        continue;
                }

                var oldMemberSyntax = (MemberDeclarationSyntax)overrideAdviceTypeMember.DeclaringSyntaxReferences.First().GetSyntax();
                var newMemberSyntax = oldMemberSyntax.WithModifiers(SyntaxFactory.TokenList(new[]{SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.OverrideKeyword)}));
                if (overrideAdviceTypeMember.IsStatic)
                    newMemberSyntax = newMemberSyntax.AddModifiers(new[]{SyntaxFactory.Token(SyntaxKind.StaticKeyword)});
                changes.Add((oldMemberSyntax, newMemberSyntax));
            }

            CSWorkspace.ReplaceNodes(changes);
        }

        void _CompleteReferencedPrototypeTypeAndAdvice(IEnumerable<INamedTypeSymbol> assemblyAdviceTypes)
        {
            var changes = new List<(SyntaxNode oldNode, SyntaxNode newNode)>();
            foreach (var adviceCodeSymbol in assemblyAdviceTypes)
            {
                changes.AddRange(_CompleteReferencedPrototypeTypeAndAdvice(adviceCodeSymbol));
            }

            CSWorkspace.ReplaceNodes(changes);
        }

        IEnumerable<(SyntaxNode oldNode, SyntaxNode newNode)> _CompleteReferencedPrototypeTypeAndAdvice(INamedTypeSymbol adviceDeclarationType)
        {
            var changes = new List<(SyntaxNode oldNode, SyntaxNode newNode)>();
            var adviceDeclarationSyntax = (TypeDeclarationSyntax)adviceDeclarationType.DeclaringSyntaxReferences.First().GetSyntax();
            var referencedConcernTypes = CSWorkspace.GetAllReferencedConcernTypesFromType(adviceDeclarationType).ToList();
            var thisDeclaration = CSWorkspace.GetAttributeDatasFromSymbol(adviceDeclarationType, typeof(ThisDeclarationAttribute)).FirstOrDefault();
            if (thisDeclaration != null)
            {
                var prototypeType = CSWorkspace.GetTypeByMetadataName((string)thisDeclaration.ConstructorArguments[0].Value);
                if (prototypeType == null || prototypeType.TypeKind == TypeKind.Error)
                {
                    string aspectName = null;
                    var sourceLocation = _GetSourceLocation(adviceDeclarationType);
                    var aspectAttribute = CSWorkspace.GetAttributeDatasFromSymbol(adviceDeclarationType, typeof(AspectParentAttribute)).FirstOrDefault();
                    if (aspectAttribute != null)
                    {
                        var aspect = (ITypeSymbol)aspectAttribute.ConstructorArguments[0].Value;
                        aspectName = (aspect).ToDisplayString();
                        sourceLocation = _GetSourceLocation(aspect);
                    }

                    _Errors.Add(AspectDNErrorFactory.GetCompilerError("PrototypeTypeNotDefined", sourceLocation, (string)thisDeclaration.ConstructorArguments[0].Value, aspectName));
                }
                else
                    referencedConcernTypes.Add(prototypeType);
            }

            var prototypeOrAdviceTypes = RoslynHelper.GetReferencddAdviceOrPrototypeTypes(CSWorkspace, referencedConcernTypes, adviceDeclarationType.ToDisplayString());
            var prototypeTypes = prototypeOrAdviceTypes.referencedPrototypeTypenames;
            var adviceTypes = prototypeOrAdviceTypes.referencedAdviceTypenames;
            if (prototypeTypes.Any())
            {
                var attributeList = CSAspectCompilerHelper.BuildReferencedPrototypeTypeAttribute(prototypeTypes.ToArray());
                adviceDeclarationSyntax = adviceDeclarationSyntax.AddAttributeLists(attributeList);
            }

            if (adviceTypes.Any())
            {
                var attributeList = CSAspectCompilerHelper.BuildReferencedAdviceTypeAttribute(adviceTypes.ToArray());
                adviceDeclarationSyntax = adviceDeclarationSyntax.AddAttributeLists(attributeList);
            }

            if (prototypeTypes.Any() || adviceTypes.Any())
                changes.Add((adviceDeclarationType.DeclaringSyntaxReferences.First().GetSyntax(), adviceDeclarationSyntax));
            return changes;
        }

        INamedTypeSymbol _GetAnonymousAdviceType(INamedTypeSymbol oldReferencedType)
        {
            INamedTypeSymbol newReferencedType = null;
            INamedTypeSymbol aspectSymbol = null;
            var referencedTypeNames = oldReferencedType.ToDisplayString().Split('.');
            if (referencedTypeNames.Length > 1)
            {
                var sb = new StringBuilder(referencedTypeNames[0]);
                for (int i = 1; i < referencedTypeNames.Length - 1; i++)
                {
                    sb.Append(".").Append(referencedTypeNames[i]);
                    aspectSymbol = CSWorkspace.GetTypeByMetadataName(sb.ToString());
                    if (aspectSymbol != null)
                    {
                        if (!aspectSymbol.AllInterfaces.Any(t => t.ToDisplayString() != typeof(IAspectTypeDeclaration).FullName))
                            aspectSymbol = null;
                        else
                            break;
                    }
                }
            }

            if (aspectSymbol != null)
            {
                var anonymousAdviceType = _GetAdviceTypeSymbol(aspectSymbol);
                if (anonymousAdviceType != null)
                {
                    newReferencedType = anonymousAdviceType.GetMembers().OfType<INamedTypeSymbol>().First();
                }
            }

            return newReferencedType;
        }

        IEnumerable<(SyntaxNode oldNode, SyntaxNode newNode)> _ChangeToAnonymousAdvice(TypeDeclarationSyntax adviceDeclarationSyntax, NameSyntax oldReferencedTypeName, INamedTypeSymbol newReferecedType)
        {
            var changes = new List<(SyntaxNode oldNode, SyntaxNode newNode)>();
            var oldNames = adviceDeclarationSyntax.DescendantNodes().OfType<NameSyntax>().Where(t => oldReferencedTypeName.ToFullString().EndsWith(t.ToFullString()));
            foreach (var oldName in oldNames)
            {
                NameSyntax oldFullName = oldName;
                while (oldFullName.Parent != null && oldFullName.Parent is QualifiedNameSyntax)
                    oldFullName = (QualifiedNameSyntax)oldFullName.Parent;
                var newFullName = CSAspectCompilerHelper.ParseName(newReferecedType.ToDisplayString());
                changes.Add((oldFullName, newFullName));
            }

            return changes;
        }

        void _ChangeReferencedPrototypeItem(IEnumerable<INamedTypeSymbol> assemblyAdviceTypes)
        {
            var changes = new List<(SyntaxNode oldNode, SyntaxNode newNode)>();
            foreach (var adviceCodeSymbol in assemblyAdviceTypes)
            {
                changes.AddRange(_ChangeReferencedPrototypeItem(adviceCodeSymbol));
            }

            CSWorkspace.ReplaceNodes(changes);
        }

        IEnumerable<(SyntaxNode oldNode, SyntaxNode newNode)> _ChangeReferencedPrototypeItem(INamedTypeSymbol adviceDeclarationType)
        {
            var changes = new List<(SyntaxNode oldNode, SyntaxNode newNode)>();
            var adviceDeclarationSyntax = (TypeDeclarationSyntax)adviceDeclarationType.DeclaringSyntaxReferences.First().GetSyntax();
            var elementAccessExpressions = adviceDeclarationSyntax.DescendantNodes().OfType<ElementAccessExpressionSyntax>().Where(t => t.Expression is IdentifierNameSyntax && (string)((IdentifierNameSyntax)t.Expression).Identifier.Value == "#this");
            foreach (var elementAccessExpression in elementAccessExpressions)
            {
                var newElementAccessExpression = elementAccessExpression.WithExpression(SyntaxFactory.ThisExpression());
                changes.Add((elementAccessExpression, newElementAccessExpression));
            }

            var constructors = adviceDeclarationSyntax.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Where(t => t.Type is IdentifierNameSyntax && (string)((IdentifierNameSyntax)t.Type).Identifier.Value == "#");
            foreach (var constructor in constructors)
            {
                var newConstructor = constructor.WithType(SyntaxFactory.IdentifierName(adviceDeclarationType.Name));
                changes.Add((constructor, newConstructor));
            }

            return changes;
        }

        void _GenerateCodeAdviceReturnValue()
        {
            var changes = new List<(SyntaxNode oldNode, SyntaxNode newNode)>();
            foreach (var adviceCodeSymbol in CSWorkspace.GetTypeSymbolsImplementingType(typeof(ICodeAdviceDeclaration), true))
            {
                var change = _GenerateCodeAdviceReturnValue(adviceCodeSymbol);
                if (change.oldNode != null && change.newNode != null)
                    changes.Add(change);
            }

            if (changes.Any())
                CSWorkspace.ReplaceNodes(changes);
        }

        (SyntaxNode oldNode, SyntaxNode newNode) _GenerateCodeAdviceReturnValue(INamedTypeSymbol adviceCodeSymbol)
        {
            var methodCodeAdviceName = CSAspectCompilerHelper.GetAdviceMemberAnonymousName(RoslynHelper.GetNameWithoutGenericMark(adviceCodeSymbol.Name));
            var codeMethodSyntax = (MethodDeclarationSyntax)(adviceCodeSymbol.GetMembers(methodCodeAdviceName).First()).DeclaringSyntaxReferences.First().GetSyntax();
            MethodDeclarationSyntax newCodeMethodSyntax = null;
            var returnStatements = CSWorkspace.GetControlFlowAnalysis(codeMethodSyntax).ReturnStatements;
            if (returnStatements.Count() != 0)
            {
                var returnExpression = returnStatements.Cast<ReturnStatementSyntax>().Where(t => t.Expression != null).FirstOrDefault();
                if (returnExpression != null)
                {
                    var returnType = CSWorkspace.GetTypeInfo(returnExpression.Expression).ConvertedType;
                    foreach (var expression in returnStatements.Cast<ReturnStatementSyntax>().Select(r => r.Expression))
                    {
                        var adviceReturnType = CSWorkspace.GetTypeInfo(returnExpression.Expression).ConvertedType;
                        if (!returnType.Equals(adviceReturnType))
                            _Errors.Add(AspectDNErrorFactory.GetCompilerError("AdviceCodeTypeMismatch", _GetSourceLocation(expression), adviceCodeSymbol.ToString(), adviceReturnType.Name));
                    }

                    newCodeMethodSyntax = codeMethodSyntax.WithReturnType(CSAspectCompilerHelper.ParseName(returnType.Name));
                }
            }

            var controlFlow = CSWorkspace.GetControlFlowAnalysis(codeMethodSyntax);
            if (controlFlow.EndPointIsReachable)
            {
                newCodeMethodSyntax = (newCodeMethodSyntax ?? codeMethodSyntax);
                var fullCodeExceptionName = SyntaxFactory.ParseName(typeof(EndCodeException).FullName);
                var endCodeStatmentSyntax = SyntaxFactory.ThrowStatement(SyntaxFactory.ObjectCreationExpression(fullCodeExceptionName).WithArgumentList(SyntaxFactory.ArgumentList()));
                var newBlock = newCodeMethodSyntax.Body.AddStatements(endCodeStatmentSyntax);
                newCodeMethodSyntax = newCodeMethodSyntax.ReplaceNode(newCodeMethodSyntax.Body, newBlock);
            }

            if (codeMethodSyntax != null)
                return (codeMethodSyntax, newCodeMethodSyntax);
            return (null, null);
        }

        SyntaxTree _ChangeAdviceCode(ClassDeclarationSyntax codeAdvice)
        {
            var oldCodeAdviceMethod = codeAdvice.Members.OfType<MethodDeclarationSyntax>().Where(t => t.Identifier.ValueText == ConcernConstantValues.AdviceNameUnderscore).FirstOrDefault();
            MethodDeclarationSyntax newCodeAdviceMethod = null;
            ClassDeclarationSyntax newCodeAdvice = null;
            var controlFlow = CSWorkspace.GetControlFlowAnalysis(oldCodeAdviceMethod);
            if (controlFlow.ReturnStatements.Count() != 0)
            {
                var hasNoValue = controlFlow.ReturnStatements.Cast<ReturnStatementSyntax>().Where(t => t.Expression == null).Count() != 0;
                TypeInfo? typeInfo = null;
                foreach (var returnStmt in controlFlow.ReturnStatements.Cast<ReturnStatementSyntax>().Where(t => t.Expression != null))
                {
                    if (hasNoValue)
                        throw new NotImplementedException("Not all returns are returning a value");
                    TypeInfo returnType = CSWorkspace.GetTypeInfo(returnStmt.Expression);
                    if (typeInfo == null)
                        typeInfo = returnType;
                    if (typeInfo.Value.ConvertedType.ToString() != returnType.ConvertedType.ToString())
                    {
                        throw new NotImplementedException();
                    }
                }

                if (!hasNoValue)
                {
                    var typeSyntax = SyntaxFactory.ParseTypeName(((TypeInfo)typeInfo).ConvertedType.Name);
                    if (typeSyntax.ToString() != typeof(void).ToString())
                        newCodeAdviceMethod = (newCodeAdviceMethod ?? oldCodeAdviceMethod).WithReturnType(typeSyntax);
                    if (controlFlow.EndPointIsReachable)
                    {
                        var oldDummyMethod = codeAdvice.DescendantNodes().OfType<MethodDeclarationSyntax>().Where(t => t.Identifier.ValueText == ConcernConstantValues.DummyReturn).First();
                        var newDummyMethod = oldDummyMethod.WithReturnType(typeSyntax);
                        newCodeAdvice = codeAdvice.ReplaceNode(oldDummyMethod, newDummyMethod);
                        var blockSyntax = (newCodeAdviceMethod ?? oldCodeAdviceMethod).Body.AddStatements((SyntaxFactory.ReturnStatement(SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(ConcernConstantValues.DummyReturn)))));
                        newCodeAdviceMethod = (newCodeAdviceMethod ?? oldCodeAdviceMethod).ReplaceNode(newCodeAdviceMethod.Body, blockSyntax);
                    }

                    oldCodeAdviceMethod = (newCodeAdvice ?? codeAdvice).Members.OfType<MethodDeclarationSyntax>().Where(t => t.Identifier.ValueText == ConcernConstantValues.AdviceNameUnderscore).FirstOrDefault();
                    newCodeAdvice = (newCodeAdvice ?? codeAdvice).ReplaceNode(oldCodeAdviceMethod, newCodeAdviceMethod);
                }
            }

            newCodeAdvice = (newCodeAdvice ?? codeAdvice).WithoutAnnotations(ConcernConstantValues.CodeAdviceAnnotation);
            return CSWorkspace.ReplaceNode(codeAdvice, newCodeAdvice);
        }

        void _ChangePrototypeTypeThisDeclaration(Dictionary<string, List<ITypeSymbol>> prototypeTypes)
        {
            var changes = new List<(SyntaxNode oldNode, SyntaxNode newNode)>();
            foreach (var prototypeType in prototypeTypes)
            {
                var protototypeTypeSymbol = CSWorkspace.GetTypeByMetadataName(prototypeType.Key);
                if (protototypeTypeSymbol == null)
                    continue;
                var typeMembersVisitor = new TypeMembersVisitor(CSWorkspace, protototypeTypeSymbol, prototypeType.Value).Visit();
                foreach (var advice in prototypeType.Value)
                {
                    var syntaxes = typeMembersVisitor.GetSyntaxNodes((INamedTypeSymbol)advice);
                    var aspect = (ITypeSymbol)advice.GetAttributes().First(t => t.AttributeClass.ToDisplayString() == typeof(AspectParentAttribute).FullName).ConstructorArguments[0].Value;
                    if (aspect.DeclaringSyntaxReferences.Any())
                    {
                        var aspectSyntax = (ClassDeclarationSyntax)aspect.DeclaringSyntaxReferences.First().GetSyntax();
                        var newAspectSyntax = aspectSyntax.AddAttributeLists(syntaxes.prototypeItemMappingSyntaxes.ToArray());
                        changes.Add((aspectSyntax, newAspectSyntax));
                    }

                    var pointcutSymbol = (ITypeSymbol)aspect.GetAttributes().Where(t => t.AttributeClass.ToDisplayString() == typeof(AspectPointcutAttribute).FullName).Select(a => a.ConstructorArguments.First().Value).FirstOrDefault();
                    if (pointcutSymbol.DeclaringSyntaxReferences.Any())
                    {
                        var protototypeTypeTargetName = CSWorkspace.GetAssemblyAttributeDatas(typeof(PrototypeTypeMappingAttribute)).Where(t => ((ITypeSymbol)t.ConstructorArguments[0].Value).ToDisplayString() == protototypeTypeSymbol.ToDisplayString()).Select(t => (string)t.ConstructorArguments[1].Value).FirstOrDefault();
                        if (protototypeTypeTargetName == null)
                        {
                            _Errors.Add(AspectDNErrorFactory.GetCompilerError("VitualTypeNoMap", _GetSourceLocation(pointcutSymbol.DeclaringSyntaxReferences.First().GetSyntax()), protototypeTypeSymbol.ToDisplayString()));
                            return;
                        }

                        var pointcutSyntax = pointcutSymbol.DeclaringSyntaxReferences.First().GetSyntax();
                        var pointcutTypename = "classes";
                        switch (protototypeTypeSymbol.TypeKind)
                        {
                            case TypeKind.Class:
                                pointcutTypename = "classes";
                                break;
                            case TypeKind.Delegate:
                                pointcutTypename = "delegates";
                                break;
                            case TypeKind.Enum:
                                pointcutTypename = "enums";
                                break;
                            case TypeKind.Struct:
                                pointcutTypename = "structs";
                                break;
                            case TypeKind.Interface:
                                pointcutTypename = "interfaces";
                                break;
                            case TypeKind.Error:
                            case TypeKind.Unknown:
                            case TypeKind.Array:
                            case TypeKind.Dynamic:
                            case TypeKind.Module:
                            case TypeKind.Pointer:
                            case TypeKind.TypeParameter:
                            case TypeKind.Submission:
                            case TypeKind.FunctionPointer:
                            default:
                                throw new NotSupportedException();
                        }

                        var newPointcutSyntax = pointcutSyntax;
                        var oldIds = pointcutSyntax.DescendantNodes().Where(t => ((t is ParameterSyntax parameterSyntax) && parameterSyntax.Identifier.ValueText == "classes") || ((t is IdentifierNameSyntax id) && id.Identifier.ValueText == "classes"));
                        foreach (var oldId in oldIds)
                        {
                            if (oldId is ParameterSyntax)
                                newPointcutSyntax = newPointcutSyntax.ReplaceNode(oldId, SyntaxFactory.Parameter((SyntaxFactory.Identifier(pointcutTypename))));
                            else
                                newPointcutSyntax = newPointcutSyntax.ReplaceNode(oldId, SyntaxFactory.IdentifierName(pointcutTypename));
                        }

                        var targetTypeSyntax = newPointcutSyntax.DescendantNodes().OfType<LiteralExpressionSyntax>().FirstOrDefault(t => t.Token.ValueText == protototypeTypeSymbol.ToDisplayString());
                        newPointcutSyntax = newPointcutSyntax.ReplaceNode(targetTypeSyntax, SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(protototypeTypeTargetName)));
                        changes.Add((pointcutSyntax, newPointcutSyntax));
                    }
                }

                if (protototypeTypeSymbol.DeclaringSyntaxReferences.Any())
                {
                    var prototypeTypeSyntax = (TypeDeclarationSyntax)protototypeTypeSymbol.DeclaringSyntaxReferences.First().GetSyntax();
                    var membersAndMappings = typeMembersVisitor.GetAdviceMemberSyntaxNodes();
                    var newPrototypeTypeSyntax = prototypeTypeSyntax.AddMembers(membersAndMappings.ToArray());
                    changes.Add((prototypeTypeSyntax, newPrototypeTypeSyntax));
                }
            }

            CSWorkspace.ReplaceNodes(changes);
        }

        Dictionary<string, List<ITypeSymbol>> _GetPrototypeTypesAndAdvices(IEnumerable<INamedTypeSymbol> assemblyAdviceTypes)
        {
            var prototypeTypes = new Dictionary<string, List<ITypeSymbol>>();
            foreach (var adviceThisDeclaration in assemblyAdviceTypes)
            {
                var attribute = adviceThisDeclaration.GetAttributes().FirstOrDefault(a => a.AttributeClass.MetadataName == nameof(ThisDeclarationAttribute));
                var prototypeTypeName = (string)attribute.ConstructorArguments.First().Value;
                if (prototypeTypeName.IndexOf(".") == -1)
                    prototypeTypeName = $"{adviceThisDeclaration.ContainingNamespace.ToDisplayString()}.{prototypeTypeName}";
                var prototypeType = CSWorkspace.GetTypeByMetadataName(prototypeTypeName);
                if (prototypeType != null && prototypeType.TypeKind != TypeKind.Error)
                {
                    if (prototypeType.ContainingModule.ToDisplayString() != adviceThisDeclaration.ContainingModule.ToDisplayString())
                    {
                        var aspect = (ITypeSymbol)adviceThisDeclaration.GetAttributes().First(t => t.AttributeClass.ToDisplayString() == typeof(AspectParentAttribute).FullName).ConstructorArguments[0].Value;
                        _Errors.Add(AspectDNErrorFactory.GetCompilerError("PrototypeTypeThisNotAllowed", _GetSourceLocation(aspect.DeclaringSyntaxReferences.First().GetSyntax()), aspect.ToDisplayString(), prototypeType.ToDisplayString()));
                        continue;
                    }
                }

                if (!prototypeTypes.ContainsKey(prototypeTypeName))
                    prototypeTypes.Add(prototypeTypeName, new List<ITypeSymbol>());
                prototypeTypes[prototypeTypeName].Add(adviceThisDeclaration);
            }

            return prototypeTypes;
        }

        void _AddInterfaceTypeMembers(IEnumerable<INamedTypeSymbol> typeMembersAdviceSymbols)
        {
            var changes = new List<(SyntaxNode oldNode, SyntaxNode newNode)>();
            var memberKinds = new SymbolKind[]{SymbolKind.Property, SymbolKind.Method, SymbolKind.Event};
            foreach (var typeMembersAdviceSymbol in typeMembersAdviceSymbols.Where(t => t.GetMembers().Any(m => m.Name.Contains("."))))
            {
                var oldAdviceSyntaxNode = (ClassDeclarationSyntax)typeMembersAdviceSymbol.DeclaringSyntaxReferences.First().GetSyntax();
                var newAdviceSyntaxNode = oldAdviceSyntaxNode;
                var members = new List<ISymbol>();
                var interfaceTypes = new List<ITypeSymbol>();
                var insterfaceTypeNames = typeMembersAdviceSymbol.GetMembers().Where(m => m.Name.Contains(".") && !m.IsImplicitlyDeclared && memberKinds.Contains(m.Kind) && (m is IMethodSymbol ? ((IMethodSymbol)m).MethodKind == MethodKind.DeclareMethod : true)).Select(member => member.Name.Substring(0, member.Name.LastIndexOf("."))).Distinct();
                foreach (var insterfaceTypeName in insterfaceTypeNames)
                {
                    var interfaceType = CSWorkspace.GetTypeSymbols(insterfaceTypeName).FirstOrDefault();
                    if (interfaceType != null)
                    {
                        if (!interfaceType.GetAttributes().Any(t => t.AttributeClass.ToDisplayString() == typeof(PrototypeTypeDeclarationAttribute).FullName))
                            continue;
                    }

                    interfaceTypes.Add(interfaceType);
                    var interfaceMembers = interfaceType.GetMembers().Where(m => memberKinds.Contains(m.Kind) && (m is IMethodSymbol ? ((IMethodSymbol)m).MethodKind == MethodKind.DeclareMethod : true));
                    members.AddRange(interfaceMembers.Where(m => !members.Any(t => t.ToDisplayString() == m.ToDisplayString())));
                }

                var newMembers = new List<ISymbol>();
                var adviceMembers = typeMembersAdviceSymbol.GetMembers().Where(m => memberKinds.Contains(m.Kind) && (m is IMethodSymbol ? ((IMethodSymbol)m).MethodKind == MethodKind.DeclareMethod : true));
                foreach (var member in members.Where(m => !adviceMembers.Any(am => am.ToDisplayString().Replace(am.ContainingType.ToDisplayString(), "") == m.ToDisplayString().Replace(m.ContainingType.ToDisplayString(), ""))))
                {
                    if (newMembers.Any(m => m.ToDisplayString().Replace(m.ContainingType.ToDisplayString(), "") == member.ToDisplayString().Replace(member.ContainingType.ToDisplayString(), "")))
                        newMembers.Add(member);
                }

                if (newMembers.Any())
                {
                    var newSyntaxNodesMembers = new SymbolMemberToSyntaxConverter(typeof(ExludedMemberAttribute)).GetMemberDeclarationSyntaxes(newMembers, false).Select(t => t.memberDeclaration);
                    newAdviceSyntaxNode = newAdviceSyntaxNode.AddMembers(newSyntaxNodesMembers.ToArray());
                }

                var interfaceSyntaxNodes = interfaceTypes.Select(i => SyntaxFactory.SimpleBaseType(RoslynHelper.ParseTypeName(i))).ToArray();
                newAdviceSyntaxNode = newAdviceSyntaxNode.AddBaseListTypes(interfaceSyntaxNodes);
                changes.Add((oldAdviceSyntaxNode, newAdviceSyntaxNode));
            }

            CSWorkspace.ReplaceNodes(changes);
        }

        void _CompleteAspects()
        {
            _CompleteReferencedAdviceAspect();
            _ChangePrototypeMemberModifier();
        }

        void _CompleteReferencedAdviceAspect()
        {
            var changes = new List<(SyntaxNode oldNode, SyntaxNode newNode)>();
            var assemblyAspectDeclarations = CSWorkspace.GetTypeSymbolsImplementingType(typeof(IAspectDeclaration), true);
            foreach (var aspectDeclaration in assemblyAspectDeclarations)
            {
                var change = _CompleteReferencedAdviceAspect(aspectDeclaration);
                if (change.oldNode != null)
                    changes.Add(change);
            }

            CSWorkspace.ReplaceNodes(changes);
        }

        void _ChangePrototypeMemberModifier()
        {
            var advices = CSWorkspace.GetAllTypeSymbols(typeof(IAdviceDeclaration)).Where(t => t.DeclaringSyntaxReferences.Any() && t.AllInterfaces.Any(i => i.ToDisplayString() == typeof(ITypeMembersAdviceDeclaration).FullName || i.ToDisplayString() == typeof(ICodeAdviceDeclaration).FullName || i.ToDisplayString() == typeof(IChangeValueAdviceDeclaration).FullName)).Distinct();
            new PrototypeMemberModifierModifier().ApplyChanges(advices, CSWorkspace);
        }

        (SyntaxNode oldNode, SyntaxNode newNode) _CompleteReferencedAdviceAspect(INamedTypeSymbol aspectDeclaration)
        {
            var adviceAttribute = CSWorkspace.GetAttributeDatasFromSymbol(aspectDeclaration, typeof(AspectAdviceAttribute)).First();
            var referencedAdvice = (INamedTypeSymbol)adviceAttribute.ConstructorArguments.First().Value;
            if (referencedAdvice.Kind != SymbolKind.ErrorType)
                return (null, null);
            var adviceSymbols = CSWorkspace.LokupAdvices(adviceAttribute.ApplicationSyntaxReference.GetSyntax().SyntaxTree, aspectDeclaration.ContainingNamespace, referencedAdvice.ToDisplayString());
            if (!adviceSymbols.Any())
                adviceSymbols = CSWorkspace.GetReferencedAssemblyConcernTypeSymbols();
            if (!adviceSymbols.Any())
                return (null, null);
            if (adviceSymbols.Count() > 1)
            {
                _Errors.Add(AspectDNErrorFactory.GetCompilerError("AmbigusAdvice", _GetSourceLocation(aspectDeclaration.DeclaringSyntaxReferences.First().GetSyntax()), referencedAdvice.ToDisplayString()));
                return (null, null);
            }

            referencedAdvice = adviceSymbols.First();
            var adviceAttributeSyntax = (AttributeSyntax)adviceAttribute.ApplicationSyntaxReference.GetSyntax();
            var typeofName = RoslynHelper.GetUnboundTypeName(referencedAdvice.Name, referencedAdvice.TypeArguments.Count(), referencedAdvice.ContainingSymbol);
            var argument = SyntaxFactory.AttributeArgument(SyntaxFactory.TypeOfExpression(typeofName));
            var newAdviceAttributeSyntax = adviceAttributeSyntax.WithArgumentList(SyntaxFactory.AttributeArgumentList(new SeparatedSyntaxList<AttributeArgumentSyntax>().Add(argument)));
            return (adviceAttributeSyntax, newAdviceAttributeSyntax);
        }

        void _Checkitems()
        {
            _CheckAdvices();
            _CheckPointcuts();
            _CheckPrototypeTypeMappings();
            _CheckAspects();
            _GenerateCodeAdviceReturnValue();
        }

        void _CheckAdvices()
        {
            _CheckNameAdviceUnicity();
            _CheckAllAdvicesIntegrities();
        }

        void _CheckNameAdviceUnicity()
        {
            var advices = CSWorkspace.GetAllTypeSymbols(typeof(IAdviceDeclaration));
            foreach (var advice in advices.Where(a => a.ContainingAssembly.MetadataName == CSWorkspace.AssemblyName))
            {
                if (advices.Any(b => RoslynHelper.GetNameWithoutGenericMark(b) == RoslynHelper.GetNameWithoutGenericMark(advice) && advice != b))
                    _Errors.Add(AspectDNErrorFactory.GetCompilerError("AdviceAlreadyDefined", _GetSourceLocation(advice.DeclaringSyntaxReferences.First().GetSyntax()), advice.ToDisplayString()));
            }
        }

        void _CheckAllAdvicesIntegrities()
        {
            foreach (var adviceSynmbol in CSWorkspace.GetTypeSymbolsImplementingType(typeof(IAdviceDeclaration)))
            {
                _CheckAdvice(adviceSynmbol);
            }
        }

        void _CheckAdvice(INamedTypeSymbol adviceSymbol)
        {
            _CheckReferencedTypesInAdvice(adviceSymbol);
            var adviceType = adviceSymbol.AllInterfaces.FirstOrDefault(t => t.Interfaces.Any(i => i.ToDisplayString() == typeof(IAdviceDeclaration).FullName));
            switch (adviceType.Name)
            {
                case nameof(ICodeAdviceDeclaration):
                    _CheckCodeAdvice(adviceSymbol);
                    break;
                case nameof(IChangeValueAdviceDeclaration):
                    _CheckChangeValueAdvice(adviceSymbol);
                    break;
                case nameof(IInterfaceMembersAdviceDeclaration):
                    _CheckAdviceInterfaceMembers(adviceSymbol);
                    break;
                case nameof(ITypeMembersAdviceDeclaration):
                    _CheckTypeMembersAdvice(adviceSymbol);
                    break;
                case nameof(IEnumMembersAdviceDeclaration):
                    _CheckAdviceEnumMembers(adviceSymbol);
                    break;
                case nameof(IInheritedTypesAdviceDeclaration):
                    _CheckAdviceBaseTypeList(adviceSymbol);
                    break;
                case nameof(IAttributesAdviceDeclaration):
                    _CheckAdviceAttributes(adviceSymbol);
                    break;
                case nameof(ITypesAdviceDeclaration):
                    _CheckAdviceType(adviceSymbol);
                    break;
                default:
                    throw AspectDNErrorFactory.GetException("NotImplementedMethod");
            }
        }

        void _CheckReferencedTypesInAdvice(INamedTypeSymbol adviceSymbol)
        {
            var referencedTypes = CSWorkspace.GetAllReferencedConcernTypesFromType(adviceSymbol);
            referencedTypes = referencedTypes.Where(t => t.ContainingAssembly != null && t.ContainingAssembly.GetAttributes().Any(a => a.AttributeClass.ToDisplayString() == typeof(AspectDNAssemblyAttribute).FullName));
            var typesOnError = referencedTypes.Where(t => t.Interfaces.Any(i => i.Equals(CSWorkspace.GetTypeByMetadataName((typeof(IAspectDeclaration).FullName)))));
            foreach (var usedType in typesOnError)
                _Errors.Add(AspectDNErrorFactory.GetCompilerError("AspectUseLikeType", _GetSourceLocation(adviceSymbol.DeclaringSyntaxReferences.First().GetSyntax()), adviceSymbol.MetadataName, usedType.MetadataName));
            typesOnError = referencedTypes.Where(t => t.Interfaces.Any(i => i.Equals(CSWorkspace.GetTypeByMetadataName((typeof(IAdviceDeclaration).FullName)))));
            foreach (var usedType in typesOnError)
                _Errors.Add(AspectDNErrorFactory.GetCompilerError("AdviceUseLikeType", _GetSourceLocation(adviceSymbol.DeclaringSyntaxReferences.First().GetSyntax()), adviceSymbol.MetadataName, usedType.MetadataName));
            typesOnError = referencedTypes.Where(t => t.Interfaces.Any(i => i.Equals(CSWorkspace.GetTypeByMetadataName((typeof(IPointcutDeclaration).FullName)))));
            foreach (var usedType in typesOnError)
                _Errors.Add(AspectDNErrorFactory.GetCompilerError("PointcutUseLikeType", _GetSourceLocation(adviceSymbol.DeclaringSyntaxReferences.First().GetSyntax()), adviceSymbol.MetadataName, usedType.MetadataName));
        }

        void _CheckCodeAdvice(INamedTypeSymbol adviceSymbol)
        {
        }

        void _CheckChangeValueAdvice(INamedTypeSymbol adviceSymbol)
        {
        }

        void _CheckAdviceInterfaceMembers(INamedTypeSymbol adviceSymbol)
        {
        }

        void _CheckTypeMembersAdvice(INamedTypeSymbol adviceSymbol)
        {
            var constructors = adviceSymbol.Constructors.Where(c => c.GetAttributes().Any(a => CSWorkspace.GetAttributeDatasFromSymbol(c, typeof(PrototypeItemDeclarationAttribute)).Any() || CSWorkspace.GetAttributeDatasFromSymbol(c, typeof(AdviceConstructorAttribute)).Any()));
            foreach (var constructor in constructors.Where(c => CSWorkspace.GetAttributeDatasFromSymbol(c, typeof(PrototypeItemDeclarationAttribute)).Any()))
            {
                if (constructors.Any(c => CSWorkspace.GetAttributeDatasFromSymbol(c, typeof(AdviceConstructorAttribute)).Any() && RoslynHelper.AreMethodSame(c, constructor)))
                    _Errors.Add(AspectDNErrorFactory.GetCompilerError("PrototyItemConstructorAlreadyExists", _GetSourceLocation(constructor.DeclaringSyntaxReferences.First().GetSyntax()), constructor.MetadataName, adviceSymbol.MetadataName));
            }

            var indexers = adviceSymbol.GetMembers().OfType<IPropertySymbol>().Where(p => p.Parameters.Length != 0);
            foreach (var indexer in indexers.Where(c => CSWorkspace.GetAttributeDatasFromSymbol(c, typeof(PrototypeItemDeclarationAttribute)).Any()))
            {
                if (indexers.Any(i => !CSWorkspace.GetAttributeDatasFromSymbol(i, typeof(PrototypeItemDeclarationAttribute)).Any() && RoslynHelper.ArePropertySame(i, indexer)))
                    _Errors.Add(AspectDNErrorFactory.GetCompilerError("PrototyItemConstructorAlreadyExists", _GetSourceLocation(indexer.DeclaringSyntaxReferences.First().GetSyntax()), indexer.MetadataName, adviceSymbol.MetadataName));
            }
        }

        void _CheckAdviceEnumMembers(INamedTypeSymbol adviceSymbol)
        {
        }

        void _CheckAdviceBaseTypeList(INamedTypeSymbol adviceSymbol)
        {
            var types = CSWorkspace.GetAttributeDatasFromSymbol(adviceSymbol, typeof(AdviceBaseTypeAttribute)).Select(t => (INamedTypeSymbol)t.ConstructorArguments[0].Value);
            var errorTypeCounters =
                from type in types
                group type by type.MetadataName into metadataName
                    select new
                    {
                    TypeSymbol = metadataName.Key, Count = metadataName.Count(), }

            ;
            foreach (var errorType in errorTypeCounters.Where(t => t.Count > 1))
            {
                var sourceLocation = _GetSourceLocation(types.First(t => t.MetadataName == errorType.TypeSymbol).DeclaringSyntaxReferences.First().GetSyntax());
                _Errors.Add(AspectDNErrorFactory.GetCompilerError("SameBaseTypeDefined", sourceLocation, errorType.TypeSymbol));
            }

            var baseTypes = types.Where(t => t.TypeKind != TypeKind.Interface && t.TypeKind != TypeKind.Error);
            if (baseTypes.Count() > 1)
            {
                foreach (var baseType in baseTypes)
                {
                    var sourceLocation = _GetSourceLocation(adviceSymbol.DeclaringSyntaxReferences.First().GetSyntax());
                    _Errors.Add(AspectDNErrorFactory.GetCompilerError("TooManyBaseType", sourceLocation));
                }
            }
        }

        void _CheckAdviceAttributes(INamedTypeSymbol adviceSymbol)
        {
        }

        void _CheckAdviceType(INamedTypeSymbol adviceSymbol)
        {
        }

        void _CheckPointcuts()
        {
            foreach (var poitcutSymbol in CSWorkspace.GetTypeSymbolsImplementingType(typeof(IPointcutDeclaration)))
            {
                _CheckPoincut(poitcutSymbol);
            }
        }

        void _CheckPoincut(INamedTypeSymbol poincutSymbol)
        {
            _CheckPointcutUsage(poincutSymbol);
        }

        void _CheckPointcutUsage(INamedTypeSymbol poincutSymbol)
        {
            foreach (var syntaxNode in CSWorkspace.GetSyntaxNodeUsingType(poincutSymbol).Where(s => s.DescendantNodes().OfType<MemberDeclarationSyntax>().Any()))
            {
                var sourceLocation = _GetSourceLocation(syntaxNode);
                _Errors.Add(AspectDNErrorFactory.GetCompilerError("InvalidTypeUsage", sourceLocation));
            }
        }

        void _CheckPrototypeTypeMappings()
        {
            var assemblies = CSWorkspace.GetAssemblySymbols(true);
            foreach (var referencedAssemblySymbol in assemblies)
            {
                foreach (var prototypeTypeMappingAttributeData in CSWorkspace.GetAttributeDatasFromSymbol(referencedAssemblySymbol, typeof(PrototypeTypeMappingAttribute)))
                {
                    var prototypeTypeMapping = (INamedTypeSymbol)prototypeTypeMappingAttributeData.ConstructorArguments[0].Value;
                    if (_PrototypeTypesMappings.ContainsKey(prototypeTypeMapping.ToDisplayString()))
                    {
                        var sourceLocation = _GetSourceLocation(referencedAssemblySymbol);
                        _Errors.Add(AspectDNErrorFactory.GetCompilerError("PrototypeMappingTypeTargetAlreadyDefined", sourceLocation, prototypeTypeMapping.MetadataName));
                    }
                    else
                        _PrototypeTypesMappings.Add(prototypeTypeMapping.ToDisplayString(), prototypeTypeMappingAttributeData);
                }
            }

            foreach (AttributeData prototypeTypeMappingAttributeData in CSWorkspace.GetAssemblyAttributeDatas(typeof(PrototypeTypeMappingAttribute)))
            {
                if (!(prototypeTypeMappingAttributeData.ConstructorArguments[0].Value is INamedTypeSymbol) || ((INamedTypeSymbol)prototypeTypeMappingAttributeData.ConstructorArguments[0].Value).Kind == SymbolKind.ErrorType)
                {
                    var sourceLocation = _GetSourceLocation((ISymbol)prototypeTypeMappingAttributeData.ConstructorArguments[0].Value);
                    _Errors.Add(AspectDNErrorFactory.GetCompilerError("BadPrototypeTypeName", sourceLocation));
                }

                var prototypeTypeToMap = (INamedTypeSymbol)prototypeTypeMappingAttributeData.ConstructorArguments[0].Value;
                if (!CSWorkspace.GetAttributeDatasFromSymbol(prototypeTypeToMap, typeof(PrototypeTypeDeclarationAttribute)).Any())
                {
                    _Errors.Add(AspectDNErrorFactory.GetCompilerError("PrototypeTypeNotFound", _GetSourceLocation(prototypeTypeToMap), prototypeTypeToMap.MetadataName));
                }

                if (string.IsNullOrEmpty(((string)prototypeTypeMappingAttributeData.ConstructorArguments[1].Value).Trim()))
                    _Errors.Add(AspectDNErrorFactory.GetCompilerError("PrototypeTypeNotFound", _GetSourceLocation(prototypeTypeToMap), prototypeTypeToMap.MetadataName));
            }
        }

        void _CheckAspects()
        {
            var assemblyAspectDeclarations = CSWorkspace.GetTypeSymbolsImplementingType(typeof(IAspectDeclaration), true);
            foreach (var aspectDeclaration in assemblyAspectDeclarations)
                _CheckAspect(aspectDeclaration);
        }

        void _CheckAspect(INamedTypeSymbol aspectSymbol)
        {
            var pointcutAttribute = CSWorkspace.GetAttributeDatasFromSymbol(aspectSymbol, typeof(AspectPointcutAttribute)).First();
            var pointcutSymbol = ((INamedTypeSymbol)pointcutAttribute.ConstructorArguments.First().Value);
            if (pointcutSymbol.Kind == SymbolKind.ErrorType)
                _Errors.Add(AspectDNErrorFactory.GetCompilerError("UndefinedPointcut", _GetSourceLocation(pointcutAttribute.ApplicationSyntaxReference.GetSyntax()), pointcutSymbol.Name));
            var pointcutAttributeDatas = CSWorkspace.GetAttributeDatasFromSymbol(pointcutSymbol, typeof(PointcutTypeAttribute));
            if (!pointcutAttributeDatas.Any())
                _Errors.Add(AspectDNErrorFactory.GetCompilerError("PointcutMismatch", _GetSourceLocation(aspectSymbol.DeclaringSyntaxReferences.First().GetSyntax()), pointcutSymbol.Name));
            var adviceAttribute = CSWorkspace.GetAttributeDatasFromSymbol(aspectSymbol, typeof(AspectAdviceAttribute)).First();
            var adviceNamedType = (INamedTypeSymbol)adviceAttribute.ConstructorArguments.First().Value;
            if (adviceNamedType.Kind == SymbolKind.ErrorType)
            {
                var adviceSymbols = CSWorkspace.LokupAdvices(adviceAttribute.ApplicationSyntaxReference.GetSyntax().SyntaxTree, aspectSymbol.ContainingNamespace, adviceNamedType.ToDisplayString());
                if (adviceSymbols.Count() > 1)
                {
                    _Errors.Add(AspectDNErrorFactory.GetCompilerError("AmbigusAdvice", _GetSourceLocation(adviceAttribute.ApplicationSyntaxReference.GetSyntax()), adviceNamedType.ToDisplayString()));
                    return;
                }

                if (!adviceSymbols.Any())
                {
                    _Errors.Add(AspectDNErrorFactory.GetCompilerError("UndefinedAdvice", _GetSourceLocation(adviceAttribute.ApplicationSyntaxReference.GetSyntax()), adviceNamedType.ToDisplayString()));
                    return;
                }

                adviceNamedType = adviceSymbols.First();
            }

            var adviceType = adviceNamedType;
            if (adviceType.DeclaringSyntaxReferences.Any())
                adviceType = CSWorkspace.GetNamedTypeSymbol(adviceNamedType.DeclaringSyntaxReferences.First().GetSyntax());
            if (!adviceType.ConstructedFrom.AllInterfaces.Any(t => t.ToDisplayString() == typeof(IAdviceDeclaration).FullName))
                _Errors.Add(AspectDNErrorFactory.GetCompilerError("AdviceMismatch", _GetSourceLocation(aspectSymbol.DeclaringSyntaxReferences.First().GetSyntax()), adviceNamedType.ToDisplayString()));
            var controlFlow = _CheckAspectControlFlow(aspectSymbol);
            _CheckAspectExcutionTime(aspectSymbol, controlFlow, pointcutSymbol);
            var sourceLocation = _GetSourceLocation(aspectSymbol.DeclaringSyntaxReferences.First().GetSyntax());
            if (adviceNamedType.Kind != SymbolKind.ErrorType && pointcutSymbol.Kind != SymbolKind.ErrorType)
                _CheckAspectAdvicePointcutCompability(controlFlow, adviceNamedType, pointcutSymbol, sourceLocation);
            if (adviceType.Kind != SymbolKind.ErrorType)
            {
                if (adviceType.AllInterfaces.Any(i => i.ToDisplayString() == typeof(ITypesAdviceDeclaration).FullName) && adviceType.TypeParameters.Any())
                    _CheckTypeParameters(aspectSymbol, adviceType);
                _CheckPrototypeMappingAttributes(aspectSymbol, adviceType);
            }
        }

        INamedTypeSymbol _GetAdviceTypeSymbol(INamedTypeSymbol aspectSymbol)
        {
            INamedTypeSymbol adviceType = null;
            var adviceAttribute = CSWorkspace.GetAttributeDatasFromSymbol(aspectSymbol, typeof(AspectAdviceAttribute)).First();
            var adviceNamedType = (INamedTypeSymbol)adviceAttribute.ConstructorArguments.First().Value;
            if (adviceNamedType.Kind != SymbolKind.ErrorType)
            {
                var adviceSymbols = CSWorkspace.LokupAdvices(adviceAttribute.ApplicationSyntaxReference.GetSyntax().SyntaxTree, aspectSymbol.ContainingNamespace, adviceNamedType.ToDisplayString());
                if (adviceSymbols.Count() == 1)
                    adviceType = adviceSymbols.First();
            }

            return adviceType;
        }

        ControlFlows _CheckAspectControlFlow(INamedTypeSymbol aspectSymbol)
        {
            var controlFlowAttribute = CSWorkspace.GetAttributeDatasFromSymbol(aspectSymbol, typeof(ControlFlowsAttribute)).FirstOrDefault();
            var controlFlow = ControlFlows.none;
            if (controlFlowAttribute != null)
                controlFlow = (ControlFlows)controlFlowAttribute.ConstructorArguments[0].Value;
            switch (controlFlow)
            {
                case ControlFlows.none:
                case ControlFlows.set:
                case ControlFlows.set | ControlFlows.body:
                case ControlFlows.get:
                case ControlFlows.get | ControlFlows.body:
                case ControlFlows.call:
                case ControlFlows.body:
                case ControlFlows.@throw:
                case ControlFlows.add:
                case ControlFlows.add | ControlFlows.body:
                case ControlFlows.remove:
                case ControlFlows.remove | ControlFlows.body:
                    break;
                default:
                    Errors.Add(AspectDNErrorFactory.GetCompilerError("BadControlFlow", _GetSourceLocation(aspectSymbol)));
                    break;
            }

            return controlFlow;
        }

        void _CheckAspectAdvicePointcutCompability(ControlFlows controlFlow, INamedTypeSymbol adviceSymbol, INamedTypeSymbol pointcutSymbol, SourceLocation sourceLocation)
        {
            var pointcutTypeAttribute = CSWorkspace.GetAttributeDatasFromSymbol(pointcutSymbol, typeof(PointcutTypeAttribute)).First();
            var pointcutType = (PointcutTypes)pointcutTypeAttribute.ConstructorArguments[0].Value;
            switch (pointcutType)
            {
                case PointcutTypes.assemblies:
                    _CheckAspectPointcutAssemblyCompability(adviceSymbol, sourceLocation);
                    break;
                case PointcutTypes.classes:
                case PointcutTypes.structs:
                    _CheckAspectPointcutClassOrStructCompability(adviceSymbol, sourceLocation);
                    break;
                case PointcutTypes.interfaces:
                    _CheckAspectPointcutInterfaceCompability(adviceSymbol, sourceLocation);
                    break;
                case PointcutTypes.constructors:
                case PointcutTypes.methods:
                    _CheckAspectPointcutMethodOrConstructorCompability(controlFlow, adviceSymbol, sourceLocation);
                    break;
                case PointcutTypes.fields:
                    _CheckAspectPointcutFieldCompability(controlFlow, adviceSymbol, sourceLocation);
                    break;
                case PointcutTypes.properties:
                    _CheckAspectPointcutPropertyCompability(controlFlow, adviceSymbol, sourceLocation);
                    break;
                case PointcutTypes.events:
                    _CheckAspectPointcutEventCompability(controlFlow, adviceSymbol, sourceLocation);
                    break;
                case PointcutTypes.delegates:
                    _CheckAspectPointcutDelegateCompability(controlFlow, adviceSymbol, sourceLocation);
                    break;
                case PointcutTypes.exceptions:
                    _CheckAspectPointcutExeptionCompability(controlFlow, adviceSymbol, sourceLocation);
                    break;
                case PointcutTypes.enums:
                    _CheckAspectPointcutEnumCompability(controlFlow, adviceSymbol, sourceLocation);
                    break;
                default:
                    throw AspectDNErrorFactory.GetException("NotSupportedExpcetion");
            }
        }

        void _CheckAspectPointcutAssemblyCompability(ITypeSymbol adviceType, SourceLocation sourceLocation)
        {
            switch (adviceType.Name)
            {
                case nameof(ICodeAdviceDeclaration):
                case nameof(IChangeValueAdviceDeclaration):
                case nameof(IInterfaceMembersAdviceDeclaration):
                case nameof(ITypeMembersAdviceDeclaration):
                case nameof(IEnumMembersAdviceDeclaration):
                case nameof(IInheritedTypesAdviceDeclaration):
                case nameof(IAttributesAdviceDeclaration):
                    _Errors.Add(AspectDNErrorFactory.GetCompilerError("BadPointcutTypeForAdvice", sourceLocation));
                    break;
                case nameof(ITypesAdviceDeclaration):
                    break;
            }
        }

        void _CheckAspectPointcutClassOrStructCompability(ITypeSymbol adviceType, SourceLocation sourceLocation)
        {
            switch (adviceType.Name)
            {
                case nameof(ICodeAdviceDeclaration):
                case nameof(IChangeValueAdviceDeclaration):
                case nameof(IInterfaceMembersAdviceDeclaration):
                    _Errors.Add(AspectDNErrorFactory.GetCompilerError("BadPointcutTypeForAdvice", sourceLocation));
                    break;
                case nameof(IInheritedTypesAdviceDeclaration):
                case nameof(ITypeMembersAdviceDeclaration):
                case nameof(ITypesAdviceDeclaration):
                case nameof(IAttributesAdviceDeclaration):
                case nameof(IEnumMembersAdviceDeclaration):
                    break;
            }
        }

        void _CheckAspectPointcutInterfaceCompability(ITypeSymbol adviceType, SourceLocation sourceLocation)
        {
            switch (adviceType.Name)
            {
                case nameof(ICodeAdviceDeclaration):
                case nameof(IChangeValueAdviceDeclaration):
                case nameof(ITypeMembersAdviceDeclaration):
                case nameof(ITypesAdviceDeclaration):
                case nameof(IEnumMembersAdviceDeclaration):
                    _Errors.Add(AspectDNErrorFactory.GetCompilerError("BadPointcutTypeForAdvice", sourceLocation));
                    break;
                case nameof(IInheritedTypesAdviceDeclaration):
                    foreach (var field in adviceType.GetMembers().OfType<IFieldSymbol>().Where(t => t.GetAttributes().Count() == 0))
                    {
                        if (field.Type.TypeKind != TypeKind.Interface)
                            _Errors.Add(AspectDNErrorFactory.GetCompilerError("InterfaceTypeMismatch", sourceLocation));
                    }

                    break;
                case nameof(IInterfaceMembersAdviceDeclaration):
                case nameof(IAttributesAdviceDeclaration):
                    break;
            }
        }

        void _CheckAspectPointcutMethodOrConstructorCompability(ControlFlows controlFlow, ITypeSymbol adviceType, SourceLocation sourceLocation)
        {
            switch (adviceType.Name)
            {
                case nameof(ITypeMembersAdviceDeclaration):
                case nameof(ITypesAdviceDeclaration):
                case nameof(IEnumMembersAdviceDeclaration):
                case nameof(IInterfaceMembersAdviceDeclaration):
                case nameof(IInheritedTypesAdviceDeclaration):
                case nameof(IAttributesAdviceDeclaration):
                    _Errors.Add(AspectDNErrorFactory.GetCompilerError("BadPointcutTypeForAdvice", sourceLocation));
                    break;
                case nameof(IChangeValueAdviceDeclaration):
                    switch (controlFlow)
                    {
                        case ControlFlows.none:
                        case ControlFlows.set:
                        case ControlFlows.set | ControlFlows.body:
                        case ControlFlows.get | ControlFlows.body:
                        case ControlFlows.body:
                        case ControlFlows.@throw:
                        case ControlFlows.add:
                        case ControlFlows.add | ControlFlows.body:
                        case ControlFlows.remove:
                        case ControlFlows.remove | ControlFlows.body:
                        case ControlFlows.get:
                            _Errors.Add(AspectDNErrorFactory.GetCompilerError("BadControlFlow", sourceLocation));
                            break;
                        case ControlFlows.call:
                            break;
                    }

                    break;
                case nameof(ICodeAdviceDeclaration):
                    switch (controlFlow)
                    {
                        case ControlFlows.none:
                        case ControlFlows.@throw:
                        case ControlFlows.set:
                        case ControlFlows.get:
                        case ControlFlows.set | ControlFlows.body:
                        case ControlFlows.get | ControlFlows.body:
                        case ControlFlows.add:
                        case ControlFlows.remove:
                        case ControlFlows.add | ControlFlows.body:
                        case ControlFlows.remove | ControlFlows.body:
                            _Errors.Add(AspectDNErrorFactory.GetCompilerError("BadPointcutTypeForAdvice", sourceLocation));
                            break;
                        case ControlFlows.call:
                        case ControlFlows.body:
                            break;
                    }

                    break;
            }
        }

        void _CheckAspectPointcutFieldCompability(ControlFlows controlFlow, ITypeSymbol adviceType, SourceLocation sourceLocation)
        {
            switch (adviceType.Name)
            {
                case nameof(ITypeMembersAdviceDeclaration):
                case nameof(ITypesAdviceDeclaration):
                case nameof(IEnumMembersAdviceDeclaration):
                case nameof(IInterfaceMembersAdviceDeclaration):
                case nameof(IInheritedTypesAdviceDeclaration):
                case nameof(IAttributesAdviceDeclaration):
                    _Errors.Add(AspectDNErrorFactory.GetCompilerError("BadPointcutTypeForAdvice", sourceLocation));
                    break;
                case nameof(IChangeValueAdviceDeclaration):
                    switch (controlFlow)
                    {
                        case ControlFlows.none:
                        case ControlFlows.@throw:
                        case ControlFlows.call:
                        case ControlFlows.body:
                        case ControlFlows.add:
                        case ControlFlows.remove:
                        case ControlFlows.set:
                        case ControlFlows.set | ControlFlows.body:
                        case ControlFlows.get | ControlFlows.body:
                        case ControlFlows.add | ControlFlows.body:
                        case ControlFlows.remove | ControlFlows.body:
                            _Errors.Add(AspectDNErrorFactory.GetCompilerError("BadControlFlow", sourceLocation));
                            break;
                        case ControlFlows.get:
                            break;
                    }

                    break;
                case nameof(ICodeAdviceDeclaration):
                    switch (controlFlow)
                    {
                        case ControlFlows.none:
                        case ControlFlows.@throw:
                        case ControlFlows.call:
                        case ControlFlows.body:
                        case ControlFlows.add:
                        case ControlFlows.remove:
                        case ControlFlows.set | ControlFlows.body:
                        case ControlFlows.get | ControlFlows.body:
                        case ControlFlows.add | ControlFlows.body:
                        case ControlFlows.remove | ControlFlows.body:
                            _Errors.Add(AspectDNErrorFactory.GetCompilerError("BadControlFlow", sourceLocation));
                            break;
                        case ControlFlows.set:
                        case ControlFlows.get:
                            break;
                    }

                    break;
            }
        }

        void _CheckAspectPointcutPropertyCompability(ControlFlows controlFlow, ITypeSymbol adviceType, SourceLocation sourceLocation)
        {
            switch (adviceType.Name)
            {
                case nameof(ITypeMembersAdviceDeclaration):
                case nameof(ITypesAdviceDeclaration):
                case nameof(IEnumMembersAdviceDeclaration):
                case nameof(IInterfaceMembersAdviceDeclaration):
                case nameof(IInheritedTypesAdviceDeclaration):
                case nameof(IAttributesAdviceDeclaration):
                    _Errors.Add(AspectDNErrorFactory.GetCompilerError("BadPointcutTypeForAdvice", sourceLocation));
                    break;
                case nameof(IChangeValueAdviceDeclaration):
                    switch (controlFlow)
                    {
                        case ControlFlows.none:
                        case ControlFlows.@throw:
                        case ControlFlows.call:
                        case ControlFlows.body:
                        case ControlFlows.add:
                        case ControlFlows.remove:
                        case ControlFlows.set:
                        case ControlFlows.set | ControlFlows.body:
                        case ControlFlows.get | ControlFlows.body:
                        case ControlFlows.add | ControlFlows.body:
                        case ControlFlows.remove | ControlFlows.body:
                            _Errors.Add(AspectDNErrorFactory.GetCompilerError("BadControlFlow", sourceLocation));
                            break;
                        case ControlFlows.get:
                            break;
                    }

                    break;
                case nameof(ICodeAdviceDeclaration):
                    switch (controlFlow)
                    {
                        case ControlFlows.none:
                        case ControlFlows.@throw:
                        case ControlFlows.call:
                        case ControlFlows.body:
                        case ControlFlows.add:
                        case ControlFlows.remove:
                        case ControlFlows.add | ControlFlows.body:
                        case ControlFlows.remove | ControlFlows.body:
                            _Errors.Add(AspectDNErrorFactory.GetCompilerError("BadControlFlow", sourceLocation));
                            break;
                        case ControlFlows.set:
                        case ControlFlows.get:
                        case ControlFlows.set | ControlFlows.body:
                        case ControlFlows.get | ControlFlows.body:
                            break;
                    }

                    break;
            }
        }

        void _CheckAspectPointcutEventCompability(ControlFlows controlFlow, ITypeSymbol adviceType, SourceLocation sourceLocation)
        {
            switch (adviceType.Name)
            {
                case nameof(ITypeMembersAdviceDeclaration):
                case nameof(ITypesAdviceDeclaration):
                case nameof(IEnumMembersAdviceDeclaration):
                case nameof(IInterfaceMembersAdviceDeclaration):
                case nameof(IInheritedTypesAdviceDeclaration):
                case nameof(IAttributesAdviceDeclaration):
                case nameof(IChangeValueAdviceDeclaration):
                    _Errors.Add(AspectDNErrorFactory.GetCompilerError("BadPointcutTypeForAdvice", sourceLocation));
                    break;
                case nameof(ICodeAdviceDeclaration):
                    switch (controlFlow)
                    {
                        case ControlFlows.none:
                        case ControlFlows.@throw:
                        case ControlFlows.body:
                        case ControlFlows.set:
                        case ControlFlows.get:
                        case ControlFlows.set | ControlFlows.body:
                        case ControlFlows.get | ControlFlows.body:
                            _Errors.Add(AspectDNErrorFactory.GetCompilerError("BadControlFlow", sourceLocation));
                            break;
                        case ControlFlows.call:
                        case ControlFlows.add:
                        case ControlFlows.remove:
                        case ControlFlows.add | ControlFlows.body:
                        case ControlFlows.remove | ControlFlows.body:
                            break;
                    }

                    break;
            }
        }

        void _CheckAspectPointcutDelegateCompability(ControlFlows controlFlow, ITypeSymbol adviceType, SourceLocation sourceLocation)
        {
            switch (adviceType.Name)
            {
                case nameof(ITypeMembersAdviceDeclaration):
                case nameof(ITypesAdviceDeclaration):
                case nameof(IEnumMembersAdviceDeclaration):
                case nameof(IInterfaceMembersAdviceDeclaration):
                case nameof(IInheritedTypesAdviceDeclaration):
                case nameof(IAttributesAdviceDeclaration):
                case nameof(IChangeValueAdviceDeclaration):
                    _Errors.Add(AspectDNErrorFactory.GetCompilerError("BadPointcutTypeForAdvice", sourceLocation));
                    break;
                case nameof(ICodeAdviceDeclaration):
                    switch (controlFlow)
                    {
                        case ControlFlows.none:
                        case ControlFlows.@throw:
                        case ControlFlows.body:
                        case ControlFlows.set:
                        case ControlFlows.get:
                        case ControlFlows.set | ControlFlows.body:
                        case ControlFlows.get | ControlFlows.body:
                        case ControlFlows.add | ControlFlows.body:
                        case ControlFlows.remove | ControlFlows.body:
                            _Errors.Add(AspectDNErrorFactory.GetCompilerError("BadControlFlow", sourceLocation));
                            break;
                        case ControlFlows.call:
                        case ControlFlows.add:
                        case ControlFlows.remove:
                            break;
                    }

                    break;
            }
        }

        void _CheckAspectPointcutExeptionCompability(ControlFlows controlFlow, ITypeSymbol adviceType, SourceLocation sourceLocation)
        {
            switch (adviceType.Name)
            {
                case nameof(ITypeMembersAdviceDeclaration):
                case nameof(ITypesAdviceDeclaration):
                case nameof(IEnumMembersAdviceDeclaration):
                case nameof(IInterfaceMembersAdviceDeclaration):
                case nameof(IInheritedTypesAdviceDeclaration):
                case nameof(IAttributesAdviceDeclaration):
                case nameof(IChangeValueAdviceDeclaration):
                    _Errors.Add(AspectDNErrorFactory.GetCompilerError("BadPointcutTypeForAdvice", sourceLocation));
                    break;
                case nameof(ICodeAdviceDeclaration):
                    switch (controlFlow)
                    {
                        case ControlFlows.none:
                        case ControlFlows.body:
                        case ControlFlows.set:
                        case ControlFlows.get:
                        case ControlFlows.set | ControlFlows.body:
                        case ControlFlows.get | ControlFlows.body:
                        case ControlFlows.add | ControlFlows.body:
                        case ControlFlows.remove | ControlFlows.body:
                        case ControlFlows.call:
                        case ControlFlows.add:
                        case ControlFlows.remove:
                            _Errors.Add(AspectDNErrorFactory.GetCompilerError("BadControlFlow", sourceLocation));
                            break;
                        case ControlFlows.@throw:
                            break;
                    }

                    break;
            }
        }

        void _CheckAspectPointcutEnumCompability(ControlFlows controlFlow, ITypeSymbol adviceType, SourceLocation sourceLocation)
        {
            switch (adviceType.Name)
            {
                case nameof(ITypeMembersAdviceDeclaration):
                case nameof(ITypesAdviceDeclaration):
                case nameof(IInterfaceMembersAdviceDeclaration):
                case nameof(IInheritedTypesAdviceDeclaration):
                case nameof(IAttributesAdviceDeclaration):
                case nameof(IChangeValueAdviceDeclaration):
                    _Errors.Add(AspectDNErrorFactory.GetCompilerError("BadPointcutTypeForAdvice", sourceLocation));
                    break;
                case nameof(IEnumMembersAdviceDeclaration):
                    break;
                case nameof(ICodeAdviceDeclaration):
                    switch (controlFlow)
                    {
                        case ControlFlows.none:
                        case ControlFlows.body:
                        case ControlFlows.set | ControlFlows.body:
                        case ControlFlows.get | ControlFlows.body:
                        case ControlFlows.add | ControlFlows.body:
                        case ControlFlows.remove | ControlFlows.body:
                        case ControlFlows.call:
                        case ControlFlows.add:
                        case ControlFlows.remove:
                        case ControlFlows.@throw:
                            _Errors.Add(AspectDNErrorFactory.GetCompilerError("BadControlFlow", sourceLocation));
                            break;
                        case ControlFlows.set:
                        case ControlFlows.get:
                            break;
                    }

                    break;
            }
        }

        void _CheckAspectExcutionTime(INamedTypeSymbol aspectSymbol, ControlFlows controlFlow, INamedTypeSymbol pointcutSymbol)
        {
            var aspectKind = aspectSymbol.AllInterfaces.FirstOrDefault(t => t.Interfaces.Any(i => i.ToDisplayString() == typeof(IAspectDeclaration).FullName));
            var executionAttribute = CSWorkspace.GetAttributeDatasFromSymbol(aspectSymbol, typeof(ExecutionTimeAttribute)).FirstOrDefault();
            var executionTime = ExecutionTimes.none;
            if (executionAttribute != null)
                executionTime = (ExecutionTimes)executionAttribute.ConstructorArguments[0].Value;
            var pointcutKindAttribute = CSWorkspace.GetAttributeDatasFromSymbol(pointcutSymbol, typeof(PointcutTypeAttribute)).First();
            var pointcutKind = (PointcutTypes)pointcutKindAttribute.ConstructorArguments[0].Value;
            switch (aspectKind.Name)
            {
                case nameof(ICodeAspectDeclaration):
                    if (pointcutKind == PointcutTypes.constructors && ControlFlows.body == (controlFlow & ControlFlows.body) && executionTime != ExecutionTimes.after)
                        _Errors.Add(AspectDNErrorFactory.GetCompilerError("BadExecutionTime", _GetSourceLocation(aspectSymbol.DeclaringSyntaxReferences.First().GetSyntax())));
                    break;
            }
        }

        void _CheckPrototypeMappingAttributes(INamedTypeSymbol aspectSymbol, INamedTypeSymbol adviceSymbol)
        {
            var prototypeMappingChanges = new List<(AttributeSyntax oldAttribute, AttributeSyntax newAttribute)>();
            var prototypeItemMappingDataAttributes = CSWorkspace.GetAttributeDatasFromSymbol(aspectSymbol, typeof(PrototypeItemMappingAttribute));
            var prototypeMemberDeclarations = adviceSymbol.GetMembers().ToList();
            prototypeMemberDeclarations = prototypeMemberDeclarations.Where(m => m.GetAttributes().Any(a => a.AttributeClass.ToDisplayString() == typeof(PrototypeItemDeclarationAttribute).FullName)).ToList();
            var adviceTypeDeclarationSymbol = CSWorkspace.GetTypeByMetadataName(typeof(ITypesAdviceDeclaration).FullName);
            var prototypeTypes = new List<INamedTypeSymbol>();
            var adviceTypes = new List<INamedTypeSymbol>();
            var prototypeAttributeDatas = CSWorkspace.GetAttributeDatasFromSymbol(adviceSymbol, typeof(ReferencedPrototypeTypesAttribute));
            foreach (var prototypeTypeAttributeData in prototypeAttributeDatas)
            {
                if (prototypeTypeAttributeData.ConstructorArguments[0].Kind == TypedConstantKind.Array)
                {
                    foreach (var usedType in prototypeTypeAttributeData.ConstructorArguments[0].Values.Select(v => v.Value))
                        prototypeTypes.Add((INamedTypeSymbol)usedType);
                }
                else
                {
                    var usedType = prototypeTypeAttributeData.ConstructorArguments[0].Value;
                    prototypeTypes.Add((INamedTypeSymbol)usedType);
                }
            }

            var prototypeAttributeData = CSWorkspace.GetAttributeDatasFromSymbol(adviceSymbol, typeof(ReferencedAdviceTypesAttribute)).FirstOrDefault();
            if (prototypeAttributeData != null)
            {
                var constructorArgValues = RoslynHelper.GetAttributeConstructorArgValues(prototypeAttributeData.ConstructorArguments[0]);
                switch (constructorArgValues)
                {
                    case object[] values:
                        foreach (var usedType in values)
                            adviceTypes.Add((INamedTypeSymbol)usedType);
                        break;
                    case object value:
                        adviceTypes.Add((INamedTypeSymbol)value);
                        break;
                }
            }

            foreach (var prototypeMappingItemDataAttribute in prototypeItemMappingDataAttributes)
            {
                PrototypeItemMappingSourceKinds prototypeItemMappingSourceKind = (PrototypeItemMappingSourceKinds)prototypeMappingItemDataAttribute.ConstructorArguments[0].Value;
                var prototypeItemMappingSource = prototypeMappingItemDataAttribute.ConstructorArguments[1].Value;
                var syntaxLocationReference = prototypeMappingItemDataAttribute.ApplicationSyntaxReference.GetSyntax();
                switch (prototypeItemMappingSourceKind)
                {
                    case PrototypeItemMappingSourceKinds.GenericParameter:
                        if (adviceSymbol.AllInterfaces.Any(i => i.ToDisplayString() == typeof(ITypesAdviceDeclaration).FullName))
                        {
                            if (!adviceSymbol.GetMembers().Any(m => m is INamedTypeSymbol && ((INamedTypeSymbol)m).TypeParameters.Any(t => t.Name == (string)prototypeItemMappingSource)))
                                _Errors.Add(AspectDNErrorFactory.GetCompilerError("MappingMemberMismatch", _GetSourceLocation(syntaxLocationReference), (string)prototypeItemMappingSource));
                            break;
                        }

                        if (!adviceSymbol.TypeParameters.Any(t => t.Name == (string)prototypeItemMappingSource))
                            _Errors.Add(AspectDNErrorFactory.GetCompilerError("MappingMemberMismatch", _GetSourceLocation(syntaxLocationReference), (string)prototypeItemMappingSource));
                        break;
                    case PrototypeItemMappingSourceKinds.Member:
                        if (!prototypeMemberDeclarations.Any(t => t.Name == (string)prototypeItemMappingSource))
                            _Errors.Add(AspectDNErrorFactory.GetCompilerError("MappingMemberMismatch", _GetSourceLocation(syntaxLocationReference), (string)prototypeItemMappingSource));
                        break;
                    case PrototypeItemMappingSourceKinds.AdviceType:
                        if (((INamedTypeSymbol)prototypeItemMappingSource).TypeKind == TypeKind.Error)
                        {
                            _Errors.Add(AspectDNErrorFactory.GetCompilerError("UndefinedAdviceType", _GetSourceLocation(syntaxLocationReference), ((INamedTypeSymbol)prototypeItemMappingSource).ToDisplayString()));
                            break;
                        }

                        if (((INamedTypeSymbol)prototypeItemMappingSource).ToDisplayString().IndexOf(".") <= 0)
                        {
                            _Errors.Add(AspectDNErrorFactory.GetCompilerError("NotWellFormReferencedAdviceTypeName", _GetSourceLocation(syntaxLocationReference), ((INamedTypeSymbol)prototypeItemMappingSource).ToDisplayString()));
                            break;
                        }

                        var adviceType = (INamedTypeSymbol)prototypeItemMappingSource;
                        if (adviceType.TypeKind != TypeKind.Error)
                            break;
                        if (((INamedTypeSymbol)prototypeItemMappingSource).ToDisplayString().IndexOf(".") <= 0)
                        {
                            _Errors.Add(AspectDNErrorFactory.GetCompilerError("NotWellFormReferencedAdviceTypeName", _GetSourceLocation(syntaxLocationReference), ((INamedTypeSymbol)prototypeItemMappingSource).ToDisplayString()));
                            break;
                        }

                        var adviceName = ((INamedTypeSymbol)prototypeItemMappingSource).ToDisplayString().Substring(0, ((INamedTypeSymbol)prototypeItemMappingSource).ToDisplayString().LastIndexOf("."));
                        var adviceTypeName = ((INamedTypeSymbol)prototypeItemMappingSource).ToDisplayString().Substring(((INamedTypeSymbol)prototypeItemMappingSource).ToDisplayString().LastIndexOf(".") + 1);
                        var adviceSymbols = CSWorkspace.LokupAdvices(syntaxLocationReference.SyntaxTree, aspectSymbol.ContainingNamespace, adviceName);
                        if (adviceSymbols.Count() > 1)
                        {
                            _Errors.Add(AspectDNErrorFactory.GetCompilerError("AmbigusAdvice", _GetSourceLocation(syntaxLocationReference), adviceName));
                            break;
                        }

                        if (!adviceSymbols.Any())
                        {
                            _Errors.Add(AspectDNErrorFactory.GetCompilerError("UndefinedAdvice", _GetSourceLocation(syntaxLocationReference), adviceName));
                            break;
                        }

                        var nameSyntax = SyntaxFactory.ParseName(adviceTypeName);
                        if (nameSyntax is GenericNameSyntax)
                            adviceTypeName = $"{((GenericNameSyntax)nameSyntax).Identifier.ToString()}`{((GenericNameSyntax)nameSyntax).TypeArgumentList.Arguments.Count()}";
                        var referencedAdviceType = adviceSymbols.First().GetMembers().OfType<INamedTypeSymbol>().FirstOrDefault(t => t.MetadataName == adviceTypeName);
                        if (referencedAdviceType == null)
                            _Errors.Add(AspectDNErrorFactory.GetCompilerError("MappingMemberMismatch", _GetSourceLocation(syntaxLocationReference), (string)prototypeItemMappingSource));
                        else
                        {
                            var oldAttributSyntax = (AttributeSyntax)prototypeMappingItemDataAttribute.ApplicationSyntaxReference.GetSyntax();
                            var typeofArgument = SyntaxFactory.AttributeArgument(SyntaxFactory.TypeOfExpression(RoslynHelper.ConvertTypeToUnboundTypeName(referencedAdviceType.ToDisplayString())));
                            var oldArgument = oldAttributSyntax.ArgumentList.Arguments[1];
                            var newAttributeArgumentList = oldAttributSyntax.ArgumentList.Arguments.Replace(oldArgument, typeofArgument);
                            var newAttributeSyntax = oldAttributSyntax.WithArgumentList(SyntaxFactory.AttributeArgumentList(newAttributeArgumentList));
                            prototypeMappingChanges.Add((oldAttributSyntax, newAttributeSyntax));
                        }

                        if (!prototypeMemberDeclarations.Any(t => t.Name == (string)prototypeItemMappingSource))
                            _Errors.Add(AspectDNErrorFactory.GetCompilerError("MappingMemberMismatch", _GetSourceLocation(syntaxLocationReference), (string)prototypeItemMappingSource));
                        break;
                    default:
                        break;
                }
            }

            foreach (var prototypeMemberDeclaration in prototypeMemberDeclarations)
            {
                if (prototypeMemberDeclaration is IPropertySymbol || (prototypeMemberDeclaration is IMethodSymbol && CSWorkspace.GetAttributeDatasFromSymbol(prototypeMemberDeclaration, typeof(PrototypeItemDeclarationAttribute)).Any()))
                    continue;
                if (!prototypeItemMappingDataAttributes.Any(t => PrototypeItemMappingSourceKinds.Member == (PrototypeItemMappingSourceKinds)t.ConstructorArguments[0].Value && prototypeMemberDeclaration.Name == (string)t.ConstructorArguments[1].Value))
                    _Errors.Add(AspectDNErrorFactory.GetCompilerError("PrototypeMemberNoMap", _GetSourceLocation(prototypeMemberDeclaration), prototypeMemberDeclaration.MetadataName));
            }

            foreach (var adviceType in adviceTypes)
            {
                if (!prototypeItemMappingDataAttributes.Any(t => PrototypeItemMappingSourceKinds.AdviceType == (PrototypeItemMappingSourceKinds)t.ConstructorArguments[0].Value && adviceType.Name == ((INamedTypeSymbol)t.ConstructorArguments[1].Value).Name))
                    _Errors.Add(AspectDNErrorFactory.GetCompilerError("AdviceTypeNoMap", _GetSourceLocation(adviceType), adviceType.MetadataName, aspectSymbol.MetadataName));
            }

            foreach (var typeMarameter in adviceTypeDeclarationSymbol.TypeArguments)
            {
                if (!prototypeItemMappingDataAttributes.Any(t => PrototypeItemMappingSourceKinds.GenericParameter == (PrototypeItemMappingSourceKinds)t.ConstructorArguments[0].Value && typeMarameter.Name == (string)t.ConstructorArguments[1].Value))
                    _Errors.Add(AspectDNErrorFactory.GetCompilerError("PrototypeMemberNoMap", null, aspectSymbol.MetadataName));
            }

            foreach (var prototypeType in prototypeTypes)
            {
                if (!_PrototypeTypesMappings.ContainsKey(prototypeType.ToDisplayString()))
                    _Errors.Add(AspectDNErrorFactory.GetCompilerError("PrototypeTypeNoMap", _GetSourceLocation(prototypeType), prototypeType.MetadataName));
            }

            if (prototypeMappingChanges.Any())
            {
                var syntaxTree = prototypeMappingChanges.First().oldAttribute.SyntaxTree;
                foreach (var changeAttribute in prototypeMappingChanges)
                {
                    var x = syntaxTree.GetRoot().DescendantNodesAndSelf();
                    var oldNode = syntaxTree.GetRoot().DescendantNodesAndSelf().OfType<AttributeSyntax>().FirstOrDefault(t => SyntaxFactory.Equals(t, changeAttribute.oldAttribute));
                    syntaxTree = CSWorkspace.ReplaceNode(oldNode, changeAttribute.newAttribute);
                }
            }
        }

        void _CheckTypeParameters(INamedTypeSymbol aspectSymbol, INamedTypeSymbol adviceSymbol)
        {
            var prototypeMappingItemDataAttributes = CSWorkspace.GetAttributeDatasFromSymbol(aspectSymbol, typeof(PrototypeItemMappingAttribute)).Where(t => (PrototypeItemMappingSourceKinds)t.ConstructorArguments.First().Value == PrototypeItemMappingSourceKinds.GenericParameter);
            var typeDeclarations = adviceSymbol.GetMembers().OfType<INamedTypeSymbol>().ToList();
            var typeArgumentNames = new List<string>();
            foreach (var typeDeclaration in typeDeclarations)
            {
                foreach (var typeArguementName in typeDeclaration.TypeArguments.Select(a => a.Name))
                {
                    if (!typeArgumentNames.Contains(typeArguementName))
                        typeArgumentNames.Add(typeArguementName);
                }
            }

            foreach (var typeArgumentName in typeArgumentNames)
            {
                var virtualMappingItemDataAttribute = prototypeMappingItemDataAttributes.FirstOrDefault(t => (string)t.ConstructorArguments[1].Value == typeArgumentName);
                if (virtualMappingItemDataAttribute == null)
                {
                    foreach (var typeDeclaration in typeDeclarations.Where(t => t.TypeArguments.Any(tp => tp.Name == typeArgumentName)))
                    {
                        _Errors.Add(AspectDNErrorFactory.GetCompilerError("NoMappingForTypeParameter", _GetSourceLocation(typeDeclaration.DeclaringSyntaxReferences.First().GetSyntax()), (string)typeArgumentName, aspectSymbol.ToDisplayString()));
                    }
                }
            }

            foreach (var virtualMappingItemDataAttribute in prototypeMappingItemDataAttributes)
            {
                if (!typeArgumentNames.Contains((string)virtualMappingItemDataAttribute.ConstructorArguments[1].Value))
                    _Errors.Add(AspectDNErrorFactory.GetCompilerError("MappingForInexitingTypeParameter", _GetSourceLocation(virtualMappingItemDataAttribute.ApplicationSyntaxReference.GetSyntax()), (string)virtualMappingItemDataAttribute.ConstructorArguments[1].Value, aspectSymbol.ToDisplayString()));
            }

            if (!aspectSymbol.AllInterfaces.Any(a => a.ToDisplayString() == typeof(ICodeAspectDeclaration).FullName || a.ToDisplayString() == typeof(ICodeAspectDeclaration).FullName) && prototypeMappingItemDataAttributes.Any(t => (PrototypeItemMappingTargetKinds)t.ConstructorArguments[2].Value == PrototypeItemMappingTargetKinds.MethodGenericParameter))
            {
                _Errors.Add(AspectDNErrorFactory.GetCompilerError("MethodGenericParameterRestriction", _GetSourceLocation(aspectSymbol.DeclaringSyntaxReferences.First().GetSyntax()), aspectSymbol.ToDisplayString()));
            }
        }

        IEnumerable<INamedTypeSymbol> _GetReferencedAdviceType(INamespaceSymbol containingNamespace, string adviceName, string adviceTypeName, int adviceTypeParameterCount, SyntaxTree syntaxTree)
        {
            var symbols = new List<INamedTypeSymbol>();
            var adviceSymbols = CSWorkspace.LokupAdvices(syntaxTree, containingNamespace, adviceName);
            foreach (var advice in adviceSymbols.Where(t => t.GetMembers(adviceTypeName).OfType<INamedTypeSymbol>().Any(type => adviceTypeParameterCount == type.TypeArguments.Length)))
                symbols.Add(advice.GetMembers(adviceTypeName).OfType<INamedTypeSymbol>().First(type => adviceTypeParameterCount == type.TypeArguments.Length));
            return symbols;
        }

        SourceLocation _GetSourceLocation(ISymbol symbol)
        {
            var syntax = symbol.DeclaringSyntaxReferences.FirstOrDefault();
            if (syntax == null)
                return SourceLocation.Empy;
            return _GetSourceLocation(syntax.GetSyntax());
        }

        SourceLocation _GetSourceLocation(SyntaxNode syntaxNode)
        {
            while (syntaxNode.GetAnnotations("Id").FirstOrDefault() == null)
                syntaxNode = syntaxNode.Parent;
            var aspectNode = _GetAspectFromSyntaxNode(syntaxNode);
            return aspectNode.SynToken.GetSourceLocation();
        }

        CSAspectNode _GetAspectFromSyntaxNode(SyntaxNode syntaxNode)
        {
            var syntaxTreeRoot = syntaxNode.SyntaxTree.GetRoot().GetAnnotations("Id").First().Data;
            var treeAspect = CSAspectTrees.Where(t => ((CSAspectNode)t.Root).Id == syntaxTreeRoot).First();
            var syntaxNodeAnnotated = syntaxNode.AncestorsAndSelf().Where(n => n.GetAnnotations("Id").FirstOrDefault() != null).FirstOrDefault();
            var id = syntaxNodeAnnotated.GetAnnotations("Id").FirstOrDefault();
            var aspectNode = CSAspectCompilerHelper.GetAspectNode((CSAspectNode)treeAspect.Root, id.Data);
            return aspectNode;
        }
    }
}