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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Data.Common;
using AspectDN.Aspect.Compilation.Foundation;

namespace AspectDN.Aspect.Compilation.CS
{
    internal static class CSAspectCompilerHelper
    {
        static CultureInfo _CultureInfo = CultureInfo.InvariantCulture;
        internal static CompilationUnitSyntax CompilationUnit(CompilationUnitAspect compilationUnitAspect)
        {
            var compilationUnit = SyntaxFactory.CompilationUnit();
            if (compilationUnitAspect.Usings.Any())
            {
                var usings = new SyntaxList<UsingDirectiveSyntax>();
                foreach (var @using in compilationUnitAspect.Usings)
                    usings = usings.Add((UsingDirectiveSyntax)@using.GetSyntaxNode());
                compilationUnit = compilationUnit.WithUsings(usings);
            }

            if (compilationUnitAspect.PackageMembers.Any())
            {
                var members = new SyntaxList<MemberDeclarationSyntax>();
                foreach (var member in compilationUnitAspect.PackageMembers)
                    members = members.Add((MemberDeclarationSyntax)member.GetSyntaxNode());
                members = members.AddRange(_GetAnonymousMembers(compilationUnitAspect));
                compilationUnit = compilationUnit.AddMembers(members.ToArray());
            }

            if (compilationUnitAspect.PrototypeTypeMappings.Any())
            {
                var list = new SyntaxList<AttributeListSyntax>();
                compilationUnitAspect.PrototypeTypeMappings.ToList().ForEach(t => list = list.AddRange(PrototypeTypeMapping(t)));
                compilationUnit = compilationUnit.WithAttributeLists(list);
            }

            return compilationUnit;
        }

        static ClassDeclarationSyntax _ClassDeclarationAspect(string className, string concernClassName)
        {
            var tokenClassName = !string.IsNullOrEmpty(className) ? SyntaxFactory.Identifier(className) : SyntaxFactory.MissingToken(SyntaxKind.IdentifierToken);
            var @class = SyntaxFactory.ClassDeclaration(tokenClassName).AddBaseListTypes(SyntaxFactory.SimpleBaseType(_IConcernQualifiedName(concernClassName))).WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)));
            return @class;
        }

        static InterfaceDeclarationSyntax _InterfaceDeclarationAspect(string interfaceName, string concernInterfaceName)
        {
            var tokenClassName = !string.IsNullOrEmpty(interfaceName) ? SyntaxFactory.Identifier(interfaceName) : SyntaxFactory.MissingToken(SyntaxKind.IdentifierToken);
            var @class = SyntaxFactory.InterfaceDeclaration(tokenClassName).AddBaseListTypes(SyntaxFactory.SimpleBaseType(_IConcernQualifiedName(concernInterfaceName))).WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)));
            return @class;
        }

        static EnumDeclarationSyntax _EnumDeclarationAspect(string enumName, string concernEnulName)
        {
            var tokenEnumName = string.IsNullOrEmpty(enumName) ? SyntaxFactory.Identifier(enumName) : SyntaxFactory.MissingToken(SyntaxKind.IdentifierToken);
            var @enum = SyntaxFactory.EnumDeclaration(tokenEnumName).AddBaseListTypes(SyntaxFactory.SimpleBaseType(_IConcernQualifiedName(concernEnulName))).WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)));
            return @enum;
        }

        internal static NamespaceDeclarationSyntax PackageDeclarationSyntax(PackageDeclarationAspect package)
        {
            var syntax = SyntaxFactory.NamespaceDeclaration((NameSyntax)package.NameAspect.GetSyntaxNode()).WithUsings(SyntaxList<UsingDirectiveSyntax, UsingDirectiveAspect>(package.UsingDirectives)).WithMembers(SyntaxList<MemberDeclarationSyntax, PackageMemberAspect>(package.PackageMembers));
            var members = _GetAnonymousMembers(package);
            if (members.Any())
                syntax = syntax.AddMembers(members.ToArray());
            return syntax;
        }

        static IEnumerable<MemberDeclarationSyntax> _GetAnonymousMembers(CSAspectNode parentMember)
        {
            List<MemberDeclarationSyntax> anonymousTypes = new List<MemberDeclarationSyntax>();
            foreach (var anonymousPointcut in GetDescendingNodesOfType<AspectMemberDeclarationAspect>(parentMember, false).Where(t => GetDescendingNodesOfType<AspectPointcutAnonymousAspect>(t, false).Any()).Select(t => GetDescendingNodesOfType<AspectPointcutAnonymousAspect>(t, false).First()))
            {
                anonymousTypes.Add(_AspectPointcutAnonynousMember(anonymousPointcut));
            }

            foreach (var anonymousAdvice in GetDescendingNodesOfType<AspectMemberDeclarationAspect>(parentMember, false).Where(t => t.Advice != null && t.Advice.GetType().GetInterface(nameof(IAspectAdviceAnnonymousAspect)) != null).Select(t => t.Advice))
            {
                if (anonymousAdvice is AspectAdviceTypeAnonymousAspect)
                    continue;
                anonymousTypes.Add(_AspectAdviceAnonymousMember(anonymousAdvice));
            }

            return anonymousTypes;
        }

#region pointcut
        internal static ClassDeclarationSyntax PointcutDeclarationAspect(PointcutDeclarationAspect pointcutDeclaration)
        {
            return CommonPointcutDeclarationAspect(pointcutDeclaration.Identifier.TokenValue, pointcutDeclaration.PointcutType, pointcutDeclaration.Expression, pointcutDeclaration.OnError);
        }

        internal static ClassDeclarationSyntax CommonPointcutDeclarationAspect(string name, PointcutTypeAspect pointcutType, PointcutExpressionAspect pointcutExpression, bool onError)
        {
            var pointcut = _ClassDeclarationAspect(name, PointcutInterfaceName(pointcutType));
            if (!onError)
            {
                var pointcutTypeSyntax = _AspectPointcutTypeAttribute(pointcutType.TokenValue);
                var pointcutExpressionSyntax = (ExpressionSyntax)pointcutExpression.GetSyntaxNode();
                pointcut = _PointcutDeclarationAspect(pointcut, pointcutType.TokenValue, pointcutTypeSyntax, pointcutExpressionSyntax);
            }

            return pointcut;
        }

        internal static ClassDeclarationSyntax ThisTypeMembersPointcutDeclarationAspect(string pointcutName, NameAspect prototypeFullTypeName, bool onError)
        {
            var pointcutTypename = "classes";
            var pointcut = _ClassDeclarationAspect(pointcutName, PointcutInterfaceName(pointcutTypename));
            if (!onError)
            {
                var pointcutTypeSyntax = _AspectPointcutTypeAttribute(pointcutTypename);
                var pointcutExpressionSyntax = SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName(pointcutTypename), SyntaxFactory.IdentifierName("FullName")), SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(prototypeFullTypeName.GetName())));
                pointcut = _PointcutDeclarationAspect(pointcut, pointcutTypename, pointcutTypeSyntax, pointcutExpressionSyntax);
            }

            return pointcut;
        }

        internal static ClassDeclarationSyntax ThisCodePointcutDeclarationAspect(string pointcutName, NameAspect fullPrototypeName, PointcutTypeAspect pointcutType, PointcutExpressionAspect pointcutExpression, bool onError)
        {
            var pointcut = _ClassDeclarationAspect(pointcutName, PointcutInterfaceName(pointcutType));
            if (!onError)
            {
                var pointcutTypeSyntax = _AspectPointcutTypeAttribute(pointcutType.TokenValue);
                var pointcutExpressionSyntax = (ExpressionSyntax)pointcutExpression.GetSyntaxNode();
                var poincutThisExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName(pointcutType.TokenValue), SyntaxFactory.IdentifierName("DeclaringType")), SyntaxFactory.IdentifierName("FullName"));
                var thisPointcutTypeSyntax = SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, poincutThisExpression, SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(fullPrototypeName.GetName())));
                pointcutExpressionSyntax = SyntaxFactory.BinaryExpression(SyntaxKind.LogicalAndExpression, thisPointcutTypeSyntax, pointcutExpressionSyntax);
                pointcut = _PointcutDeclarationAspect(pointcut, pointcutType.TokenValue, pointcutTypeSyntax, pointcutExpressionSyntax);
            }

            return pointcut;
        }

        static ClassDeclarationSyntax _PointcutDeclarationAspect(ClassDeclarationSyntax pointcut, string pointcutTypename, AttributeListSyntax pointcutTypeSyntax, ExpressionSyntax pointcutExpressionSyntax)
        {
            pointcut = pointcut.AddAttributeLists(pointcutTypeSyntax);
            var method = SyntaxFactory.MethodDeclaration(SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.GenericName(SyntaxFactory.Identifier("Func")).WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList<TypeSyntax>(new SyntaxNodeOrToken[]{PointcutType(pointcutTypename), SyntaxFactory.Token(SyntaxKind.CommaToken), GetNameSyntax(typeof(Mono.Cecil.MethodDefinition)), SyntaxFactory.Token(SyntaxKind.CommaToken), SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword))})))), SyntaxFactory.Identifier("GetDefinition")).WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword))).WithBody(SyntaxFactory.Block(SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.ReturnStatement(SyntaxFactory.ParenthesizedLambdaExpression(pointcutExpressionSyntax).WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList<ParameterSyntax>(new SyntaxNodeOrToken[]{SyntaxFactory.Parameter(SyntaxFactory.Identifier(pointcutTypename)), SyntaxFactory.Token(SyntaxKind.CommaToken), SyntaxFactory.Parameter(SyntaxFactory.Identifier("caller"))})))))));
            pointcut = pointcut.AddMembers(method);
            return pointcut;
        }

        internal static NameSyntax PointcutType(PointcutTypeAspect pointcutType)
        {
            return PointcutType(pointcutType.TokenValue);
        }

        internal static NameSyntax PointcutType(string pointcutTypeTokenValue)
        {
            switch (pointcutTypeTokenValue)
            {
                case "assemblies":
                    return GetNameSyntax(typeof(Mono.Cecil.ModuleDefinition));
                case "classes":
                case "interfaces":
                case "delegates":
                case "exceptions":
                case "structs":
                case "enums":
                    return GetNameSyntax(typeof(Mono.Cecil.TypeDefinition));
                case "methods":
                case "constructors":
                    return GetNameSyntax(typeof(Mono.Cecil.MethodDefinition));
                case "fields":
                    return GetNameSyntax(typeof(Mono.Cecil.FieldDefinition));
                case "properties":
                    return GetNameSyntax(typeof(Mono.Cecil.PropertyDefinition));
                case "events":
                    return GetNameSyntax(typeof(Mono.Cecil.EventDefinition));
                default:
                    throw new KeyNotFoundException();
            }
        }

        internal static string PointcutInterfaceName(PointcutTypeAspect pointcutType)
        {
            return PointcutInterfaceName(pointcutType.TokenValue);
        }

        internal static string PointcutInterfaceName(string pointcutTypeName)
        {
            switch (pointcutTypeName)
            {
                case "assemblies":
                    return nameof(IPointcutAsssemblyDeclaration);
                case "classes":
                    return nameof(IPointcutTypeDeclaration);
                case "interfaces":
                    return nameof(IPointcutTypeDeclaration);
                case "constructors":
                    return nameof(IPointcutMethodDeclaration);
                case "fields":
                    return nameof(IPointcutFieldDeclaration);
                case "properties":
                    return nameof(IPointcutPropertyDeclaration);
                case "events":
                    return nameof(IPointcutEventDeclaration);
                case "delegates":
                    return nameof(IPointcutTypeDeclaration);
                case "exceptions":
                    return nameof(IPointcutTypeDeclaration);
                case "structs":
                    return nameof(IPointcutTypeDeclaration);
                case "methods":
                    return nameof(IPointcutMethodDeclaration);
                case "enums":
                    return nameof(IPointcutTypeDeclaration);
                default:
                    throw new KeyNotFoundException();
            }
        }

        internal static AttributeListSyntax AspectPointcutNamed(AspectPointcutNamedAspect aspectPoincut)
        {
            return AspectPointcutCommonAttribute((NameSyntax)aspectPoincut.PointcutName.GetSyntaxNode());
        }

        internal static AttributeListSyntax AspectPointcutAttributeAnonymous(AspectPointcutAnonymousAspect aspectPoincut)
        {
            return AspectPointcutCommonAttribute(SyntaxFactory.IdentifierName(_PointcutAnonymousName(aspectPoincut.AspectMember)));
        }

        internal static AttributeListSyntax AspectPointcutCommonAttribute(NameSyntax nameSyntax)
        {
            var attribute = _GetAttribute(typeof(AspectPointcutAttribute)).WithArgumentList(SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList<AttributeArgumentSyntax>(new SyntaxNodeOrToken[]{SyntaxFactory.AttributeArgument(SyntaxFactory.TypeOfExpression(nameSyntax))})));
            return SyntaxFactory.AttributeList().AddAttributes(attribute);
        }

        static MemberDeclarationSyntax _AspectPointcutAnonynousMember(AspectPointcutAnonymousAspect aspectPointcut)
        {
            ClassDeclarationSyntax pointcut = null;
            switch (aspectPointcut)
            {
                case AspectPointcutCommonAnonymousAspect commonPointcut:
                    pointcut = CommonPointcutDeclarationAspect(_PointcutAnonymousName(commonPointcut.AspectMember), commonPointcut.PointcutType, commonPointcut.Expression, aspectPointcut.OnError);
                    break;
                case AspectPointcutThisTypeMembersAnonymousAspect thisTypeMembersPointcut:
                    pointcut = ThisTypeMembersPointcutDeclarationAspect(_PointcutAnonymousName(thisTypeMembersPointcut.AspectMember), thisTypeMembersPointcut.PrototypeFullName, aspectPointcut.OnError);
                    break;
                case AspectPointcutThisCodeAnonymousAspect thiscodePointcut:
                    pointcut = ThisCodePointcutDeclarationAspect(_PointcutAnonymousName(thiscodePointcut.AspectMember), thiscodePointcut.PrototypeFullName, thiscodePointcut.PointcutType, thiscodePointcut.Expression, thiscodePointcut.OnError);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return pointcut;
        }

        static AttributeListSyntax _AspectPointcutTypeAttribute(string pointcutTypeName)
        {
            var attribute = _GetAttribute(typeof(PointcutTypeAttribute)).WithArgumentList(SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList<AttributeArgumentSyntax>(new SyntaxNodeOrToken[]{SyntaxFactory.AttributeArgument(_IConcernEnumMemberAccess(nameof(PointcutTypes), pointcutTypeName))})));
            return SyntaxFactory.AttributeList().AddAttributes(attribute);
        }

#endregion
#region aspect
#region InheritanceAspect
        internal static MemberDeclarationSyntax AdviceInheritDeclaration(InheritDeclarationAspect aspect, string name, IEnumerable<BaseTypeAspect> baseTypes, IEnumerable<PrototypeMemberDeclarationAspect> prototypeMembers, bool onError)
        {
            var adviceBaseType = _ClassDeclarationAspect(name, nameof(IInheritedTypesAdviceDeclaration));
            var members = new SyntaxList<MemberDeclarationSyntax>();
            int fieldNb = 1;
            foreach (var baseTypeAdvice in baseTypes)
            {
                var field = SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration((TypeSyntax)baseTypeAdvice.Type.GetSyntaxNode()).WithVariables(SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier((fieldNb++).ToString()))))).WithAttributeLists(_GetIConcernAttributeList(typeof(ExludedMemberAttribute)));
                members = members.Add(field);
            }

            if (prototypeMembers.Any(t => !(t is PrototypeTypeParameterAspect)))
                members = members.AddRange(SyntaxList<MemberDeclarationSyntax, PrototypeMemberDeclarationAspect>(prototypeMembers.Where(t => !(t is PrototypeTypeParameterAspect))));
            var declaredTypeParameters = aspect.PrototypeMappingItems.OfType<PrototypeMappingTypeParameterAspect>().Select(p => p.Source.GetName());
            if (declaredTypeParameters.Any())
            {
                var typeParameterListSyntax = TypeParameterList(declaredTypeParameters);
                adviceBaseType = adviceBaseType.WithTypeParameterList(typeParameterListSyntax);
            }

            if (aspect.OverrideConstructorsDeclarations != null)
            {
                var overrideMethods = OverrideConstructors(aspect.OverrideConstructorsDeclarations);
                members = members.AddRange(overrideMethods);
            }

            adviceBaseType = adviceBaseType.WithMembers(members);
            return adviceBaseType;
        }

        static AttributeListSyntax _AspectInheritBaseTypeAttributex(BaseTypeAspect baseType)
        {
            var attributeList = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(SyntaxFactory.Attribute(GetNameSyntax(typeof(AdviceBaseTypeAttribute)), SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList<AttributeArgumentSyntax>(new SyntaxNodeOrToken[]{SyntaxFactory.AttributeArgument(SyntaxFactory.TypeOfExpression((NameSyntax)baseType.Type.GetSyntaxNode()))})))));
            return attributeList;
        }

        static AttributeListSyntax _AspectInheritBaseTypeAttribute(BaseTypeAspect baseTypeAdvice)
        {
            var attributeArguments = new List<AttributeArgumentSyntax>();
            if (baseTypeAdvice.Type.ChildAspectNodes.FirstOrDefault() is GenericNameAspect)
            {
                var genericType = (GenericNameAspect)baseTypeAdvice.Type.ChildAspectNodes.FirstOrDefault();
                var i = 0;
                var syntaxNodeOrTokens = new SyntaxNodeOrToken[1 + (genericType.TypeArgumentList.ChildAspectNodes.Count() - 1) * 2];
                foreach (var type in genericType.TypeArgumentList.ChildCSAspectNodes)
                {
                    if (i > 0)
                        syntaxNodeOrTokens[i++] = SyntaxFactory.Token(SyntaxKind.CommaToken);
                    syntaxNodeOrTokens[i++] = SyntaxFactory.OmittedTypeArgument();
                    var aspect = GetAscendingNodesOfType<InheritDeclarationAspect>(baseTypeAdvice, true).FirstOrDefault();
                    AttributeArgumentSyntax argument = null;
                    if (!aspect.PrototypeMappingItems.Any(t => t is PrototypeMappingTypeParameterAspect))
                        argument = SyntaxFactory.AttributeArgument(SyntaxFactory.TypeOfExpression((NameSyntax)type.GetSyntaxNode()));
                    else
                        argument = SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(((NameAspect)type).GetName())));
                    attributeArguments.Add(argument);
                }

                var typeArguments = SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList<TypeSyntax>(syntaxNodeOrTokens));
                attributeArguments.Insert(0, SyntaxFactory.AttributeArgument(SyntaxFactory.TypeOfExpression(SyntaxFactory.GenericName(SyntaxFactory.Identifier(baseTypeAdvice.Type.TokenValue)).WithTypeArgumentList(typeArguments))));
            }
            else
                attributeArguments.Add(SyntaxFactory.AttributeArgument(SyntaxFactory.TypeOfExpression((NameSyntax)baseTypeAdvice.Type.GetSyntaxNode())));
            var attributeArgumentList = SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList<AttributeArgumentSyntax>(attributeArguments.ToArray()));
            return SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(SyntaxFactory.Attribute(GetNameSyntax(typeof(AdviceBaseTypeAttribute)), attributeArgumentList)));
        }

        internal static NameSyntax GenericName(GenericNameAspect genericNameAspect)
        {
            return GenericName(genericNameAspect.IdentfierName.TokenValue, genericNameAspect.TypeArgumentList);
        }

        internal static NameSyntax GenericName(IdentifierNameAspect genericAspect, CSAspectNode typeArguments)
        {
            var generic = GenericName(genericAspect.TokenValue, typeArguments);
            return generic;
        }

        internal static NameSyntax GenericName(string identifierName, CSAspectNode typeArguments)
        {
            if (typeArguments == null)
                return SyntaxFactory.IdentifierName(identifierName);
            return SyntaxFactory.GenericName(identifierName).WithTypeArgumentList((TypeArgumentListSyntax)typeArguments.GetSyntaxNode());
        }

        internal static ClassDeclarationSyntax AspectInheritDeclaration(InheritDeclarationAspect aspectInheritDeclaration)
        {
            var aspectMember = _ClassDeclarationAspect(aspectInheritDeclaration.Identifier != null ? aspectInheritDeclaration.Identifier.TokenValue : null, nameof(IAspectInheritanceDeclaration));
            if (aspectInheritDeclaration.OnError)
                return aspectMember;
            if (aspectInheritDeclaration.PrototypeMappingItems.Any())
                aspectMember = aspectMember.AddAttributeLists(SyntaxList<AttributeListSyntax, PrototypeMappingItemAspect>(aspectInheritDeclaration.PrototypeMappingItems).ToArray());
            aspectMember = aspectMember.AddAttributeLists((AttributeListSyntax)aspectInheritDeclaration.Pointcut.GetSyntaxNode(), (AttributeListSyntax)aspectInheritDeclaration.Advice.GetSyntaxNode());
            return aspectMember;
        }

        internal static AttributeListSyntax AspectAdviceInheritAnonymous(AspectAdviceInheritAnonymousAspect aspectAdviceIhneritAnonymous)
        {
            return _AspectAdviceAttribute(SyntaxFactory.IdentifierName(GetAdviceAnonymousName(aspectAdviceIhneritAnonymous.ParentDeclarator)));
        }

        static ClassDeclarationSyntax _AspectAdviceInheritAnonymous(AspectAdviceInheritAnonymousAspect aspectAdviceAnonymous)
        {
            var advice = (ClassDeclarationSyntax)AdviceInheritDeclaration(aspectAdviceAnonymous.ParentDeclarator, GetAdviceAnonymousName(aspectAdviceAnonymous.ParentDeclarator), aspectAdviceAnonymous.BaseTypes, aspectAdviceAnonymous.PrototypeMembers, aspectAdviceAnonymous.OnError);
            ;
            return advice;
        }

#endregion
#region CodeAspect
        internal static ClassDeclarationSyntax AdviceCodeDeclaration(AdviceCodeDeclarationAspect adviceCodeDeclaration)
        {
            return AdviceCodeDeclaration(adviceCodeDeclaration.Identifier.TokenValue, null, adviceCodeDeclaration.PrototypeMembers, adviceCodeDeclaration.Statements, adviceCodeDeclaration.OnError);
        }

        internal static ClassDeclarationSyntax AdviceCodeDeclaration(string adviceName, AspectPointcutThisCodeAnonymousAspect thisPointcutDeclaration, IEnumerable<PrototypeMemberDeclarationAspect> prototypeMembers, IEnumerable<StatementAspect> statements, bool onError)
        {
            var advice = _ClassDeclarationAspect(adviceName, nameof(ICodeAdviceDeclaration)).WithAdditionalAnnotations(new SyntaxAnnotation(ConcernConstantValues.CodeAdviceAnnotation));
            if (onError)
                return advice;
            if (thisPointcutDeclaration != null)
            {
                advice = advice.AddAttributeLists((AttributeListSyntax)ThisDeclarationAttribute(thisPointcutDeclaration.PrototypeFullName.GetName()));
            }

            SyntaxList<MemberDeclarationSyntax> memberDeclarations = new SyntaxList<MemberDeclarationSyntax>();
            var method = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), SyntaxFactory.Identifier(GetAdviceMemberAnonymousName(adviceName))).WithAttributeLists(_GetIConcernAttributeList(typeof(ShadowMethod))).WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword))).WithBody((BlockSyntax)SyntaxFactory.Block(SyntaxList<StatementSyntax, StatementAspect>(statements)));
            memberDeclarations = memberDeclarations.Add(method);
            var aroundMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), SyntaxFactory.Identifier(ConcernConstantValues.AroundStatement)).WithBody(SyntaxFactory.Block()).WithAttributeLists(new SyntaxList<AttributeListSyntax>(ExcludedMemberAttribute()));
            ;
            memberDeclarations = memberDeclarations.Add(aroundMethod);
            var dummyReturnMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)), SyntaxFactory.Identifier(ConcernConstantValues.DummyReturn)).WithBody(_BlockSyntaxNotImplementedException()).WithAttributeLists(new SyntaxList<AttributeListSyntax>(ExcludedMemberAttribute()));
            memberDeclarations = memberDeclarations.Add(dummyReturnMethod);
            if (prototypeMembers.Any(t => !(t is PrototypeTypeParameterAspect)))
            {
                memberDeclarations = memberDeclarations.AddRange(SyntaxList<MemberDeclarationSyntax, PrototypeMemberDeclarationAspect>(prototypeMembers.Where(t => !(t is PrototypeTypeParameterAspect))));
                advice = advice.WithMembers(new SyntaxList<MemberDeclarationSyntax>().AddRange(memberDeclarations));
            }

            if (prototypeMembers.Any(t => t is PrototypeTypeParameterAspect))
            {
                advice = advice.WithTypeParameterList(SyntaxFactory.TypeParameterList(SeparatedSyntaxList<TypeParameterSyntax, PrototypeTypeParameterAspect>(prototypeMembers.OfType<PrototypeTypeParameterAspect>())));
            }

            return advice.WithMembers(new SyntaxList<MemberDeclarationSyntax>().AddRange(memberDeclarations));
        }

        internal static ClassDeclarationSyntax AspectCodeDeclaration(AspectCodeDeclarationAspect aspectCode)
        {
            var aspectMember = SyntaxFactory.ClassDeclaration(aspectCode.Identifier.TokenValue).WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(SyntaxFactory.SimpleBaseType(_IConcernQualifiedName(nameof(ICodeAspectDeclaration)))))).WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)));
            ;
            if (aspectCode.PrototypeMappingItems.Any())
            {
                aspectMember = aspectMember.WithAttributeLists(SyntaxList<AttributeListSyntax, PrototypeMappingItemAspect>(aspectCode.PrototypeMappingItems));
            }

            aspectMember = aspectMember.AddAttributeLists((AttributeListSyntax)aspectCode.ExecutionTime.GetSyntaxNode(), (AttributeListSyntax)aspectCode.ControlFlows.GetSyntaxNode(), (AttributeListSyntax)aspectCode.Pointcut.GetSyntaxNode(), (AttributeListSyntax)aspectCode.Advice.GetSyntaxNode());
            return aspectMember;
        }

        internal static AttributeListSyntax AspectAdviceCodeNamed(AspectAdviceCodeNamedAspect aspectAdviceCodeNamed)
        {
            return _AspectAdviceAttribute((NameSyntax)aspectAdviceCodeNamed.Name.GetSyntaxNode());
        }

        internal static AttributeListSyntax AspectAdviceCodeAnonymous(AspectAdviceCodeAnonymousAspect aspectAdviceCodeAnonymous)
        {
            return _AspectAdviceAttribute(SyntaxFactory.IdentifierName(GetAdviceAnonymousName(aspectAdviceCodeAnonymous.ParentDeclarator)));
        }

        static ClassDeclarationSyntax _AspectAdviceCodeAnonymousMember(AspectAdviceCodeAnonymousAspect aspectAdviceAnonymous)
        {
            var advice = AdviceCodeDeclaration(GetAdviceAnonymousName(aspectAdviceAnonymous.ParentDeclarator), aspectAdviceAnonymous.ThisPointcut, ((AspectAdviceCodeAnonymousAspect)aspectAdviceAnonymous).PrototypeMembers, ((AspectAdviceCodeAnonymousAspect)aspectAdviceAnonymous).Statements, aspectAdviceAnonymous.OnError);
            var aspectName = aspectAdviceAnonymous.ParentAspectMember.Fullname;
            var arg = SyntaxFactory.AttributeArgument(SyntaxFactory.TypeOfExpression(ParseName(aspectName)));
            var aspectLink = SyntaxFactory.Attribute(_IConcernQualifiedName(nameof(AspectParentAttribute))).WithArgumentList(SyntaxFactory.AttributeArgumentList(new SeparatedSyntaxList<AttributeArgumentSyntax>().Add(arg)));
            advice = advice.AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(aspectLink));
            return advice;
        }

#endregion
#region AttributeAspect
        internal static ClassDeclarationSyntax AdviceAttributesDeclaration(AdviceAttributesDeclarationAspect adviceAttributes)
        {
            return AdviceAttributesDeclaration(adviceAttributes.Identifier.TokenValue, adviceAttributes.Attributes, adviceAttributes.PrototypeMembers, adviceAttributes.OnError);
        }

        internal static ClassDeclarationSyntax AdviceAttributesDeclaration(string adviceName, IEnumerable<AttributeSectionAspect> attributesAspects, IEnumerable<PrototypeMemberDeclarationAspect> prototypeMembers, bool onError)
        {
            var advice = _ClassDeclarationAspect(adviceName, nameof(IAttributesAdviceDeclaration));
            if (onError)
                return advice;
            if (attributesAspects.Any())
                advice = advice.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(attributesAspects));
            if (prototypeMembers.Count(t => !(t is PrototypeTypeParameterAspect)) != 0)
            {
                var memberDeclarations = SyntaxList<MemberDeclarationSyntax, PrototypeMemberDeclarationAspect>(prototypeMembers.Where(t => !(t is PrototypeTypeParameterAspect)));
                advice = advice.WithMembers(new SyntaxList<MemberDeclarationSyntax>().AddRange(memberDeclarations));
            }

            if (prototypeMembers.Count(t => t is PrototypeTypeParameterAspect) != 0)
            {
                advice = advice.WithTypeParameterList(SyntaxFactory.TypeParameterList(SeparatedSyntaxList<TypeParameterSyntax, PrototypeTypeParameterAspect>(prototypeMembers.OfType<PrototypeTypeParameterAspect>())));
            }

            return advice;
        }

        internal static ClassDeclarationSyntax AspectAttributesDeclaration(AspectAttributesDeclarationAspect aspectAttribute)
        {
            var aspectMember = SyntaxFactory.ClassDeclaration(aspectAttribute.Identifier.TokenValue).WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(SyntaxFactory.SimpleBaseType(_IConcernQualifiedName(nameof(IAspectAttributesDeclaration)))))).WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)));
            ;
            if (aspectAttribute.PrototypeMappingItems.Any())
            {
                aspectMember = aspectMember.WithAttributeLists(SyntaxList<AttributeListSyntax, PrototypeMappingItemAspect>(aspectAttribute.PrototypeMappingItems));
            }

            aspectMember = aspectMember.AddAttributeLists((AttributeListSyntax)aspectAttribute.Pointcut.GetSyntaxNode(), (AttributeListSyntax)aspectAttribute.Advice.GetSyntaxNode());
            return aspectMember;
        }

        internal static AttributeListSyntax AspectAdviceAttributesNamed(AspectAdviceAttributesNamedAspect aspectAdviceAttributesNamed)
        {
            return _AspectAdviceAttribute((NameSyntax)aspectAdviceAttributesNamed.Name.GetSyntaxNode());
        }

        internal static AttributeListSyntax AspectAdviceAttributesAnonymous(AspectAdviceAsttributesAnonymousAspect aspectAdviceAsttributesAnonymous)
        {
            return _AspectAdviceAttribute(SyntaxFactory.IdentifierName(GetAdviceAnonymousName(aspectAdviceAsttributesAnonymous.ParentAspectMember)));
        }

        static ClassDeclarationSyntax _AspectAdviceAttributesAnonymousMember(AspectAdviceAsttributesAnonymousAspect aspectAdviceAnonymous)
        {
            var advice = AdviceAttributesDeclaration(GetAdviceAnonymousName(aspectAdviceAnonymous.ParentAspectMember), aspectAdviceAnonymous.Attributes, aspectAdviceAnonymous.PrototypeMembers, aspectAdviceAnonymous.OnError);
            return advice;
        }

#endregion
#region ChangeValueAspect
        internal static ClassDeclarationSyntax AdviceChangeValueDeclaration(AdviceChangeValueDeclarationAspect adviceChangeValueDeclaration)
        {
            return AdviceChangeValueDeclaration(adviceChangeValueDeclaration.Identifier.TokenValue, null, adviceChangeValueDeclaration.Type, adviceChangeValueDeclaration.PrototypeMembers, adviceChangeValueDeclaration.Statements, adviceChangeValueDeclaration.OnError);
        }

        internal static ClassDeclarationSyntax AdviceChangeValueDeclaration(string adviceName, AspectPointcutThisCodeAnonymousAspect thisPointcutDeclaration, TypeAspect type, IEnumerable<PrototypeMemberDeclarationAspect> prototypeMembers, IEnumerable<StatementAspect> statements, bool onError)
        {
            var advice = _ClassDeclarationAspect(adviceName, nameof(IChangeValueAdviceDeclaration)).WithAdditionalAnnotations(new SyntaxAnnotation(ConcernConstantValues.AdviceChangeValueAnnotation));
            if (onError)
                return advice;
            if (thisPointcutDeclaration != null)
            {
                advice = advice.AddAttributeLists((AttributeListSyntax)ThisDeclarationAttribute(thisPointcutDeclaration.PrototypeFullName.TokenValue));
            }

            SyntaxList<StatementSyntax> statementSyntaxes = SyntaxList<StatementSyntax, StatementAspect>(statements);
            statementSyntaxes = statementSyntaxes.Insert(0, SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration((TypeSyntax)type.GetSyntaxNode()).WithVariables(SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(ConcernConstantValues.VarStackName)).WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.IdentifierName(ConcernConstantValues.ArgStackName)))))));
            statementSyntaxes = statementSyntaxes.Insert(statementSyntaxes.Count(), SyntaxFactory.ReturnStatement(SyntaxFactory.IdentifierName("value")));
            var method = SyntaxFactory.MethodDeclaration((TypeSyntax)type.GetSyntaxNode(), GetAdviceMemberAnonymousName(adviceName)).WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword))).WithBody((BlockSyntax)SyntaxFactory.Block(statementSyntaxes)).WithParameterList(SyntaxFactory.ParameterList(new SeparatedSyntaxList<ParameterSyntax>().Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier(ConcernConstantValues.ArgStackName)).WithType((TypeSyntax)type.GetSyntaxNode())))).WithAttributeLists(_GetIConcernAttributeList(typeof(ShadowMethod)));
            advice = advice.AddMembers(method);
            if (prototypeMembers.Any(t => !(t is PrototypeTypeParameterAspect)))
            {
                var memberDeclarations = SyntaxList<MemberDeclarationSyntax, PrototypeMemberDeclarationAspect>(prototypeMembers.Where(t => !(t is PrototypeTypeParameterAspect)));
                advice = advice.AddMembers(new SyntaxList<MemberDeclarationSyntax>().AddRange(memberDeclarations).ToArray());
            }

            if (prototypeMembers.Any(t => t is PrototypeTypeParameterAspect))
            {
                advice = advice.WithTypeParameterList(SyntaxFactory.TypeParameterList(SeparatedSyntaxList<TypeParameterSyntax, PrototypeTypeParameterAspect>(prototypeMembers.OfType<PrototypeTypeParameterAspect>())));
            }

            return advice;
        }

        internal static ClassDeclarationSyntax AspectChangeValueDeclaration(AspectChangeValueDeclarationAspect changeValueAspect)
        {
            var aspectMember = SyntaxFactory.ClassDeclaration(changeValueAspect.Identifier.TokenValue).WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(SyntaxFactory.SimpleBaseType(GetNameSyntax(typeof(IChangeValueAspectDeclaration)))))).WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)));
            if (changeValueAspect.PrototypeMappingItems.Any())
            {
                aspectMember = aspectMember.WithAttributeLists(SyntaxList<AttributeListSyntax, PrototypeMappingItemAspect>(changeValueAspect.PrototypeMappingItems));
            }

            aspectMember = aspectMember.AddAttributeLists((AttributeListSyntax)changeValueAspect.ControlFlows.GetSyntaxNode(), (AttributeListSyntax)changeValueAspect.Pointcut.GetSyntaxNode(), (AttributeListSyntax)changeValueAspect.Advice.GetSyntaxNode());
            return aspectMember;
        }

        static ClassDeclarationSyntax _AspectAdviceChangeValueAnonymousMember(AspectAdviceChangeValueAnonymousAspect aspectAdviceChangeValueAnonymous)
        {
            var advice = AdviceChangeValueDeclaration(GetAdviceAnonymousName(aspectAdviceChangeValueAnonymous.ParentAspectMember), aspectAdviceChangeValueAnonymous.ThisPointcut, ((AspectAdviceChangeValueAnonymousAspect)aspectAdviceChangeValueAnonymous).Type, ((AspectAdviceChangeValueAnonymousAspect)aspectAdviceChangeValueAnonymous).PrototypeItems, ((AspectAdviceChangeValueAnonymousAspect)aspectAdviceChangeValueAnonymous).Statements, aspectAdviceChangeValueAnonymous.OnError);
            var aspectName = aspectAdviceChangeValueAnonymous.ParentAspectMember.Fullname;
            var arg = SyntaxFactory.AttributeArgument(SyntaxFactory.TypeOfExpression(SyntaxFactory.ParseTypeName(aspectName)));
            var aspectLink = SyntaxFactory.Attribute(_IConcernQualifiedName(nameof(AspectParentAttribute))).WithArgumentList(SyntaxFactory.AttributeArgumentList(new SeparatedSyntaxList<AttributeArgumentSyntax>().Add(arg)));
            advice = advice.AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(aspectLink));
            return advice;
        }

        internal static AttributeListSyntax AspectAdviceChangeValueNamed(AspectAdviceChangeValueNamedAspect aspectAdviceChangeValueNamed)
        {
            return _AspectAdviceAttribute((NameSyntax)aspectAdviceChangeValueNamed.Name.GetSyntaxNode());
        }

        internal static AttributeListSyntax AspectAdviceChangeValueAnonymous(AspectAdviceChangeValueAnonymousAspect aspectAdviceChangeValueAnonymous)
        {
            return _AspectAdviceAttribute(SyntaxFactory.IdentifierName(GetAdviceAnonymousName(aspectAdviceChangeValueAnonymous.ParentDeclarator)));
        }

#endregion
#region TypeMembersAspect
        internal static ClassDeclarationSyntax AdviceTypeMembersDeclaration(AdviceTypeMembersDeclarationAspect adviceMembersDeclaration)
        {
            return AdviceTypeMembersDeclaration(adviceMembersDeclaration.Identifier.TokenValue, null, adviceMembersDeclaration.PrototypeMembers, adviceMembersDeclaration.Members, adviceMembersDeclaration.OnError);
        }

        internal static ClassDeclarationSyntax AdviceTypeMembersDeclaration(string adviceName, AspectPointcutThisTypeMembersAnonymousAspect thisPointcutDeclaration, IEnumerable<PrototypeMemberDeclarationAspect> prototypeMembers, IEnumerable<TypeMemberDeclarationAspect> members, bool onError)
        {
            var advice = _ClassDeclarationAspect(adviceName, nameof(ITypeMembersAdviceDeclaration));
            if (onError)
                return advice;
            if (thisPointcutDeclaration != null)
                advice = advice.AddAttributeLists((AttributeListSyntax)ThisDeclarationAttribute(thisPointcutDeclaration.PrototypeFullName.GetName()));
            if (prototypeMembers.Count(t => !(t is PrototypeTypeParameterAspect)) != 0)
            {
                var memberDeclarations = SyntaxList<MemberDeclarationSyntax, PrototypeMemberDeclarationAspect>(prototypeMembers.Where(t => !(t is PrototypeTypeParameterAspect)));
                advice = advice.AddMembers(new SyntaxList<MemberDeclarationSyntax>().AddRange(memberDeclarations).ToArray());
            }

            if (members.Any())
                advice = advice.AddMembers(SyntaxNodeArray<MemberDeclarationSyntax, TypeMemberDeclarationAspect>(members));
            if (prototypeMembers.Count(t => t is PrototypeTypeParameterAspect) != 0)
            {
                advice = advice.WithTypeParameterList(SyntaxFactory.TypeParameterList(SeparatedSyntaxList<TypeParameterSyntax, PrototypeTypeParameterAspect>(prototypeMembers.OfType<PrototypeTypeParameterAspect>())));
            }

            return advice;
        }

        internal static ClassDeclarationSyntax AspectTypeMembersDeclaration(AspectTypeMembersDeclarationAspect aspectTypeMembersDeclaration)
        {
            var aspectMember = _ClassDeclarationAspect(aspectTypeMembersDeclaration.Identifier.TokenValue, nameof(IAspectTypeMembersDeclaration));
            if (aspectTypeMembersDeclaration.OnError)
                return aspectMember;
            if (aspectTypeMembersDeclaration.PrototypeMappingItems.Any())
            {
                aspectMember = aspectMember.WithAttributeLists(SyntaxList<AttributeListSyntax, PrototypeMappingItemAspect>(aspectTypeMembersDeclaration.PrototypeMappingItems));
            }

            aspectMember = aspectMember.AddAttributeLists((AttributeListSyntax)aspectTypeMembersDeclaration.Pointcut.GetSyntaxNode(), (AttributeListSyntax)aspectTypeMembersDeclaration.Advice.GetSyntaxNode());
            if (aspectTypeMembersDeclaration.AspectMemberModifiers.Any())
            {
                aspectMember = aspectMember.AddAttributeLists(_AspectMemberModifiersAttribute(aspectTypeMembersDeclaration.AspectMemberModifiers));
            }

            return aspectMember;
        }

        internal static AttributeListSyntax AspectAdviceTypeMembersNamed(AspectAdviceTypeMembersNamedAspect aspectAdviceTyoeMembersNamed)
        {
            return _AspectAdviceAttribute((NameSyntax)aspectAdviceTyoeMembersNamed.Name.GetSyntaxNode());
        }

        internal static AttributeListSyntax AspectAdviceTypeMembersAnonymous(AspectAdviceTypeMembersDeclarationAspect aspectAdviceTypeMembersAnonymous)
        {
            return _AspectAdviceAttribute(SyntaxFactory.IdentifierName(GetAdviceAnonymousName(aspectAdviceTypeMembersAnonymous.ParentAspectMember)));
        }

        static ClassDeclarationSyntax _AspectAdviceTypeMembersAnonymousMember(AspectAdviceTypeMembersAnonymousAspect aspectAdviceAnonymous)
        {
            var advice = AdviceTypeMembersDeclaration(GetAdviceAnonymousName(aspectAdviceAnonymous.ParentAspectMember), aspectAdviceAnonymous.ThisPointcut, aspectAdviceAnonymous.PrototypeMembers, aspectAdviceAnonymous.TypeMembers, aspectAdviceAnonymous.OnError);
            var aspectName = aspectAdviceAnonymous.ParentAspectMember.Fullname;
            var arg = SyntaxFactory.AttributeArgument(SyntaxFactory.TypeOfExpression(ParseName(aspectName)));
            var aspectLink = SyntaxFactory.Attribute(_IConcernQualifiedName(nameof(AspectParentAttribute))).WithArgumentList(SyntaxFactory.AttributeArgumentList(new SeparatedSyntaxList<AttributeArgumentSyntax>().Add(arg)));
            advice = advice.AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(aspectLink));
            return advice;
        }

        static AttributeListSyntax _AspectMemberModifiersAttribute(IEnumerable<AspectMemberModifier> modifiers)
        {
            var attribute = SyntaxFactory.Attribute(_IConcernQualifiedName(nameof(AspectTypeMemberModifersAttribute))).WithArgumentList(SyntaxFactory.AttributeArgumentList(SeparatedSyntaxList<AttributeArgumentSyntax, AspectMemberModifier>(modifiers)));
            return SyntaxFactory.AttributeList().AddAttributes(attribute);
        }

        internal static AttributeArgumentSyntax AspectMemberModifier(AspectMemberModifier modifier)
        {
            return SyntaxFactory.AttributeArgument(_IConcernEnumMemberAccess(nameof(AspectTypeMemberModifers), modifier.TokenValue));
        }

        internal static ConstructorDeclarationSyntax AdviceConstructorDeclaration(AdviceConstructorDeclarationAspect constructorDeclaration)
        {
            var constructor = SyntaxFactory.ConstructorDeclaration(GetAdviceTypeMembersAdviceTypeName(constructorDeclaration).ToString());
            if (constructorDeclaration.AttributeSections.Any())
                constructor = constructor.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(constructorDeclaration.AttributeSections));
            if (constructorDeclaration.Modifiers.Any())
                constructor = constructor.WithModifiers(SyntaxTokenList<ModifierAspect>(constructorDeclaration.Modifiers));
            if (constructorDeclaration.ParameterList != null)
                constructor = constructor.WithParameterList((ParameterListSyntax)(constructorDeclaration.ParameterList.GetSyntaxNode()));
            if (constructorDeclaration.ConstructorInitializer != null)
                constructor = constructor.WithInitializer(((ConstructorInitializerSyntax)constructorDeclaration.ConstructorInitializer.GetSyntaxNode()));
            if (constructorDeclaration.Block != null)
                constructor = constructor.WithBody(((BlockSyntax)constructorDeclaration.Block.GetSyntaxNode()));
            constructor = constructor.AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(SyntaxFactory.Attribute(_IConcernQualifiedName(nameof(AdviceConstructorAttribute)))));
            return constructor;
        }

        internal static DestructorDeclarationSyntax AdviceDestructorDeclaration(AdviceDestructorDeclarationAspect destructorDeclaration)
        {
            var destructor = SyntaxFactory.DestructorDeclaration($"{GetAdviceTypeMembersAdviceTypeName(destructorDeclaration).ToString()}");
            if (destructorDeclaration.AttributeSections.Any())
                destructor = destructor.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(destructorDeclaration.AttributeSections));
            if (destructorDeclaration.Modifiers.Any())
                destructor = destructor.WithModifiers(SyntaxTokenList<ModifierAspect>(destructorDeclaration.Modifiers));
            if (destructorDeclaration.Block != null)
                destructor = destructor.WithBody(((BlockSyntax)destructorDeclaration.Block.GetSyntaxNode()));
            return destructor;
        }

        internal static ConstructorDeclarationSyntax AdviceStaticConstructorDeclaration(AdviceStaticConstructorDeclarationAspect constructorDeclaration)
        {
            var constructor = SyntaxFactory.ConstructorDeclaration(Identifier(((AdviceTypeMembersDeclarationAspect)constructorDeclaration.ParentAspectNode).Identifier)).WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)));
            if (constructorDeclaration.AttributeSections.Any())
                constructor = constructor.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(constructorDeclaration.AttributeSections));
            if (constructorDeclaration.Modifiers.Any())
                constructor = constructor.WithModifiers(SyntaxTokenList<ModifierAspect>(constructorDeclaration.Modifiers));
            if (constructorDeclaration.Block != null)
                constructor = constructor.WithBody(((BlockSyntax)constructorDeclaration.Block.GetSyntaxNode()));
            constructor = constructor.AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(SyntaxFactory.Attribute(_IConcernQualifiedName(nameof(AdviceConstructorAttribute)))));
            return constructor;
        }

        internal static NameSyntax GetAdviceTypeMembersAdviceTypeName(TypeMemberDeclarationAspect declaration)
        {
            switch (declaration.ParentAspectNode)
            {
                case AspectAdviceTypeMembersAnonymousAspect anoynmous:
                    return SyntaxFactory.IdentifierName(GetAdviceAnonymousName(anoynmous.ParentAspectMember));
                case AdviceTypeMembersDeclarationAspect advice:
                    return SyntaxFactory.IdentifierName(advice.Identifier.TokenValue);
                default:
                    throw AspectDNErrorFactory.GetException("NotImplementedException");
            }
        }

        internal static OperatorDeclarationSyntax AdviceUnaryOperatorDeclarator(AdviceUnaryOperatorDeclaratorAspect operatorDeclaration)
        {
            var returnTypeSyntax = operatorDeclaration.ReturnType != null ? (TypeSyntax)operatorDeclaration.ReturnType.GetSyntaxNode() : SyntaxFactory.ParseTypeName(((AdviceTypeMembersDeclarationAspect)operatorDeclaration.ParentAspectNode).Name);
            var @operator = SyntaxFactory.OperatorDeclaration(returnTypeSyntax, UnaryOverloadedOperator(operatorDeclaration.Operator)).WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList<ParameterSyntax>((ParameterSyntax)operatorDeclaration.Parameter.GetSyntaxNode())));
            if (operatorDeclaration.AttributeSections.Any())
                @operator = @operator.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(operatorDeclaration.AttributeSections));
            if (operatorDeclaration.Modifiers.Any())
                @operator = @operator.WithModifiers(SyntaxTokenList<ModifierAspect>(operatorDeclaration.Modifiers));
            if (operatorDeclaration.Block != null)
                @operator = @operator.WithBody(((BlockSyntax)operatorDeclaration.Block.GetSyntaxNode()));
            return @operator;
        }

        internal static OperatorDeclarationSyntax AdviceBinaryOperatorDeclaratorAspect(AdviceBinaryOperatorDeclaratorAspect operatorDeclaration)
        {
            var returnTypeSyntax = operatorDeclaration.ReturnType != null ? (TypeSyntax)operatorDeclaration.ReturnType.GetSyntaxNode() : SyntaxFactory.ParseTypeName(((AdviceTypeMembersDeclarationAspect)operatorDeclaration.ParentAspectNode).Name);
            var parameters = new ParameterSyntax[]{(ParameterSyntax)operatorDeclaration.Parameter1.GetSyntaxNode(), (ParameterSyntax)operatorDeclaration.Parameter2.GetSyntaxNode(), };
            var @operator = SyntaxFactory.OperatorDeclaration(returnTypeSyntax, BinaryOverloadedOperator(operatorDeclaration.Operator)).WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList<ParameterSyntax>(parameters)));
            if (operatorDeclaration.AttributeSections.Any())
                @operator = @operator.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(operatorDeclaration.AttributeSections));
            if (operatorDeclaration.Modifiers.Any())
                @operator = @operator.WithModifiers(SyntaxTokenList<ModifierAspect>(operatorDeclaration.Modifiers));
            if (operatorDeclaration.Block != null)
                @operator = @operator.WithBody(((BlockSyntax)operatorDeclaration.Block.GetSyntaxNode()));
            return @operator;
        }

        internal static ConversionOperatorDeclarationSyntax AdviceConversionOperatorDeclaratorAspect(AdviceConversionOperatorDeclaratorAspect operatorDeclaration)
        {
            var returnTypeSyntax = operatorDeclaration.ReturnType != null ? (TypeSyntax)operatorDeclaration.ReturnType.GetSyntaxNode() : SyntaxFactory.ParseTypeName(((AdviceTypeMembersDeclarationAspect)operatorDeclaration.ParentAspectNode).Name);
            var @operator = SyntaxFactory.ConversionOperatorDeclaration(ConversionOperatorType(operatorDeclaration.Operator), returnTypeSyntax).WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList<ParameterSyntax>((ParameterSyntax)operatorDeclaration.Parameter.GetSyntaxNode())));
            if (operatorDeclaration.AttributeSections.Any())
                @operator = @operator.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(operatorDeclaration.AttributeSections));
            if (operatorDeclaration.Modifiers.Any())
                @operator = @operator.WithModifiers(SyntaxTokenList<ModifierAspect>(operatorDeclaration.Modifiers));
            if (operatorDeclaration.Block != null)
                @operator = @operator.WithBody(((BlockSyntax)operatorDeclaration.Block.GetSyntaxNode()));
            return @operator;
        }

        internal static ParameterSyntax AdviceOperatorDeclaratorParameterAspect(AdviceOperatorDeclaratorParameterAspect adviceOperatorDeclaratorParameter)
        {
            var returnTypeSyntax = adviceOperatorDeclaratorParameter.Type != null ? (TypeSyntax)adviceOperatorDeclaratorParameter.Type.GetSyntaxNode() : SyntaxFactory.ParseTypeName(((AdviceTypeMembersDeclarationAspect)adviceOperatorDeclaratorParameter.ParentAspectNode.ParentAspectNode).Name);
            var identifier = adviceOperatorDeclaratorParameter.Identifier.GetSyntaxNode(typeof(SyntaxToken));
            var paremeter = SyntaxFactory.Parameter((SyntaxToken)identifier).WithType(returnTypeSyntax);
            return paremeter;
        }

#endregion
#region InterfaceMembersAspect
        internal static InterfaceDeclarationSyntax AdviceInterfaceMembersDeclaration(AdviceInterfaceMembersDeclarationAspect adviceInterfaceMembersDeclaration)
        {
            return AdviceInterfaceMembersDeclaration(adviceInterfaceMembersDeclaration.Name, null, adviceInterfaceMembersDeclaration.Members, adviceInterfaceMembersDeclaration.PrototypeMembers, adviceInterfaceMembersDeclaration.OnError);
        }

        internal static InterfaceDeclarationSyntax AdviceInterfaceMembersDeclaration(string adviceName, AspectPointcutThisTypeMembersAnonymousAspect thisPointcutDeclaration, IEnumerable<InterfaceMemberAspect> members, IEnumerable<PrototypeMemberDeclarationAspect> prototypeMembers, bool onError)
        {
            var advice = _InterfaceDeclarationAspect(adviceName, nameof(IInterfaceMembersAdviceDeclaration));
            if (onError)
                return advice;
            if (thisPointcutDeclaration != null)
                advice = advice.AddAttributeLists((AttributeListSyntax)ThisDeclarationAttribute(thisPointcutDeclaration.PrototypeFullName.GetName()));
            if (members.Any())
                advice = advice.WithMembers(SyntaxList<MemberDeclarationSyntax, InterfaceMemberAspect>(members));
            if (prototypeMembers.Count(t => !(t is PrototypeTypeParameterAspect)) != 0)
            {
                var memberDeclarations = SyntaxList<MemberDeclarationSyntax, PrototypeMemberDeclarationAspect>(prototypeMembers.Where(t => !(t is PrototypeTypeParameterAspect)));
                advice = advice.WithMembers(new SyntaxList<MemberDeclarationSyntax>().AddRange(memberDeclarations));
            }

            if (prototypeMembers.Count(t => t is PrototypeTypeParameterAspect) != 0)
            {
                advice = advice.WithTypeParameterList(SyntaxFactory.TypeParameterList(SeparatedSyntaxList<TypeParameterSyntax, PrototypeTypeParameterAspect>(prototypeMembers.OfType<PrototypeTypeParameterAspect>())));
            }

            return advice;
        }

        internal static ClassDeclarationSyntax AspectInterfaceMembersDeclaration(AspectInterfaceMembersDeclarationAspect aspectInterfaceMembersDeclaration)
        {
            var aspectMember = _ClassDeclarationAspect(aspectInterfaceMembersDeclaration.Identifier.TokenValue, nameof(IAspectInterfaceMembersDeclaration));
            if (aspectInterfaceMembersDeclaration.OnError)
                return aspectMember;
            if (aspectInterfaceMembersDeclaration.PrototypeMappingItems.Any())
            {
                aspectMember = aspectMember.WithAttributeLists(SyntaxList<AttributeListSyntax, PrototypeMappingItemAspect>(aspectInterfaceMembersDeclaration.PrototypeMappingItems));
            }

            aspectMember = aspectMember.AddAttributeLists((AttributeListSyntax)aspectInterfaceMembersDeclaration.Pointcut.GetSyntaxNode(), (AttributeListSyntax)aspectInterfaceMembersDeclaration.Advice.GetSyntaxNode());
            return aspectMember;
        }

        static InterfaceDeclarationSyntax _AspectAdviceInterfaceMembersAnonymousMember(AspectAdviceInterfaceMembersAnonymousAspect adviceMember)
        {
            var advice = AdviceInterfaceMembersDeclaration(GetAdviceAnonymousName(adviceMember.ParentDeclarator), ((AspectAdviceInterfaceMembersAnonymousAspect)adviceMember).ThisPointcut, ((AspectAdviceInterfaceMembersAnonymousAspect)adviceMember).Members, ((AspectAdviceInterfaceMembersAnonymousAspect)adviceMember).PrototypeMembers, adviceMember.OnError);
            var aspectName = adviceMember.ParentAspectMember.Fullname;
            var arg = SyntaxFactory.AttributeArgument(SyntaxFactory.TypeOfExpression(ParseName(aspectName)));
            var aspectLink = SyntaxFactory.Attribute(_IConcernQualifiedName(nameof(AspectParentAttribute))).WithArgumentList(SyntaxFactory.AttributeArgumentList(new SeparatedSyntaxList<AttributeArgumentSyntax>().Add(arg)));
            advice = advice.AddAttributeLists(SyntaxFactory.AttributeList().AddAttributes(aspectLink));
            return advice;
        }

        internal static AttributeListSyntax AspectAdviceInterfaceMembersNamed(AspectAdviceInterfaceMembersNamedAspect aspectAdviceInterfaceMembersNamed)
        {
            return _AspectAdviceAttribute((NameSyntax)aspectAdviceInterfaceMembersNamed.Name.GetSyntaxNode());
        }

        internal static AttributeListSyntax AspectAdviceInterfaceMembersAnonymous(AspectAdviceInterfaceMembersAnonymousAspect aspectAdviceInterfaceMembersAnonymous)
        {
            return _AspectAdviceAttribute(SyntaxFactory.IdentifierName(GetAdviceAnonymousName(aspectAdviceInterfaceMembersAnonymous.ParentAspectMember)));
        }

#endregion
#region EnumAspect
        internal static ClassDeclarationSyntax AdviceEnumMembersDeclaration(AdviceEnumMembersDeclarationAspect adviceEnumMembersDeclaration)
        {
            return AdviceEnumMembersDeclaration(adviceEnumMembersDeclaration.Identifier.TokenValue, adviceEnumMembersDeclaration.Members, adviceEnumMembersDeclaration.OnError);
        }

        internal static ClassDeclarationSyntax AdviceEnumMembersDeclaration(string adviceName, IEnumerable<EnumMemberDeclarationApsect> members, bool onError)
        {
            var advice = _ClassDeclarationAspect(adviceName, nameof(IEnumMembersAdviceDeclaration));
            if (onError)
                return advice;
            var nestedEnum = SyntaxFactory.EnumDeclaration(GetAdviceAnonymousName(adviceName)).WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)));
            nestedEnum = nestedEnum.WithMembers(SeparatedSyntaxList<EnumMemberDeclarationSyntax, EnumMemberDeclarationApsect>(members));
            return advice.WithMembers(new SyntaxList<MemberDeclarationSyntax>(new MemberDeclarationSyntax[]{(MemberDeclarationSyntax)nestedEnum}));
        }

        internal static ClassDeclarationSyntax AspectEnumMembersDeclaration(AspectEnumMembersDeclarationAspect aspectEnumMembersDeclaration)
        {
            var aspectMember = _ClassDeclarationAspect(aspectEnumMembersDeclaration.Identifier.TokenValue, nameof(IAspectEnumMembersDeclaration));
            if (aspectEnumMembersDeclaration.OnError)
                return aspectMember;
            aspectMember = aspectMember.AddAttributeLists((AttributeListSyntax)aspectEnumMembersDeclaration.Pointcut.GetSyntaxNode());
            aspectMember = aspectMember.AddAttributeLists((AttributeListSyntax)aspectEnumMembersDeclaration.Advice.GetSyntaxNode());
            return aspectMember;
        }

        internal static AttributeListSyntax AspectAdviceEnumMembersNamed(AspectAdviceEnumMembersNamedAspect aspectAdviceEnumMembersNamed)
        {
            return _AspectAdviceAttribute((NameSyntax)aspectAdviceEnumMembersNamed.Name.GetSyntaxNode());
        }

        internal static AttributeListSyntax AspectAdviceEnumMembersAnonymous(AspectAdviceEnumMembersAnonymousAspect aspectAdviceEnumMembersAnonymous)
        {
            return _AspectAdviceAttribute(SyntaxFactory.IdentifierName(GetAdviceAnonymousName(aspectAdviceEnumMembersAnonymous.ParentAspectMember)));
        }

        internal static ClassDeclarationSyntax _AspectAdviceEnumMembersAnonymousMember(AspectAdviceEnumMembersAnonymousAspect aspectAdviceAnonymous)
        {
            var advice = AdviceEnumMembersDeclaration(GetAdviceAnonymousName(aspectAdviceAnonymous.ParentAspectMember), ((AspectAdviceEnumMembersAnonymousAspect)aspectAdviceAnonymous).Members, aspectAdviceAnonymous.OnError);
            return advice;
        }

#endregion
#region TypeDeclaration
        internal static ClassDeclarationSyntax AdviceTypeDeclaration(AdviceTypesDeclarationAspect adviceTypeDeclaration)
        {
            return AdviceTypeDeclaration(adviceTypeDeclaration.Identifier.TokenValue, adviceTypeDeclaration.TypeMembers, adviceTypeDeclaration.PrototypeMembers, adviceTypeDeclaration.OnError);
        }

        internal static ClassDeclarationSyntax AdviceTypeDeclaration(string adviceName, IEnumerable<TypeMemberDeclarationAspect> typeMembers, IEnumerable<PrototypeMemberDeclarationAspect> prototypeMembers, bool onError)
        {
            var advice = _ClassDeclarationAspect(adviceName, nameof(ITypesAdviceDeclaration));
            if (onError)
                return advice;
            advice = advice.WithMembers(SyntaxList<MemberDeclarationSyntax, TypeMemberDeclarationAspect>(typeMembers));
            return advice;
        }

        internal static ClassDeclarationSyntax AspectTypesDeclaration(AspectTypesDeclarationAspect aspectTypeDeclaration)
        {
            var aspectMember = _ClassDeclarationAspect(aspectTypeDeclaration.Identifier != null ? aspectTypeDeclaration.Identifier.TokenValue : null, nameof(IAspectTypeDeclaration));
            if (aspectTypeDeclaration.OnError)
                return aspectMember;
            if (aspectTypeDeclaration.PrototypeMappingItems.Any())
                aspectMember = aspectMember.WithAttributeLists(SyntaxList<AttributeListSyntax, PrototypeMappingItemAspect>(aspectTypeDeclaration.PrototypeMappingItems));
            if (aspectTypeDeclaration.Namespace != null)
            {
                var namespaceOrTypenameAttribute = SyntaxFactory.AttributeList().AddAttributes(SyntaxFactory.Attribute(_IConcernQualifiedName(nameof(NamespaceOrTypeNameAttribute))).AddArgumentListArguments(SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(aspectTypeDeclaration.Namespace.GetName())))));
                aspectMember = aspectMember.AddAttributeLists(namespaceOrTypenameAttribute);
            }

            if (aspectTypeDeclaration.Advice is AspectAdviceTypeAnonymousAspect)
            {
                aspectMember = aspectMember.AddBaseListTypes(SyntaxFactory.SimpleBaseType(_IConcernQualifiedName(nameof(ITypesAdviceDeclaration))));
                aspectMember = aspectMember.WithMembers(SyntaxList<MemberDeclarationSyntax, TypeMemberDeclarationAspect>(((AspectAdviceTypeAnonymousAspect)aspectTypeDeclaration.Advice).TypeMembers));
                aspectMember = aspectMember.AddAttributeLists((AttributeListSyntax)aspectTypeDeclaration.Pointcut.GetSyntaxNode(), _AspectAdviceAttribute(SyntaxFactory.IdentifierName(aspectTypeDeclaration.Identifier.GetName())));
            }
            else
            {
                aspectMember = aspectMember.AddAttributeLists((AttributeListSyntax)aspectTypeDeclaration.Pointcut.GetSyntaxNode(), (AttributeListSyntax)aspectTypeDeclaration.Advice.GetSyntaxNode());
            }

            return aspectMember;
        }

        internal static AttributeListSyntax AspectAdviceTypeNamed(AspectAdviceTypeNamedAspect aspectAdviceTypeNamed)
        {
            return _AspectAdviceAttribute((NameSyntax)aspectAdviceTypeNamed.Name.GetSyntaxNode());
        }

        internal static AttributeListSyntax AspectAdviceTypeAnonymous(AspectAdviceTypeAnonymousAspect aspectAdviceTypeAnonymous)
        {
            return _AspectAdviceAttribute(SyntaxFactory.IdentifierName(GetAdviceAnonymousName(aspectAdviceTypeAnonymous.ParentAspectMember)));
        }

        internal static TypeDeclarationSyntax _AspectAdviceTypeAnonymousMember(AspectAdviceTypeAnonymousAspect aspectAdviceAnonymous)
        {
            var advice = AdviceTypeDeclaration(GetAdviceAnonymousName(aspectAdviceAnonymous.ParentAspectMember), aspectAdviceAnonymous.TypeMembers, aspectAdviceAnonymous.PrototypeMembers, aspectAdviceAnonymous.OnError);
            return advice;
        }

#endregion
        internal static AttributeListSyntax ExcludedMemberAttribute()
        {
            return SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(SyntaxFactory.Attribute(_IConcernQualifiedName(nameof(ExludedMemberAttribute))).WithArgumentList(SyntaxFactory.AttributeArgumentList())));
        }

        static TypeDeclarationSyntax _AspectAdviceAnonymousMember(AspectAdviceAspect aspectAdviceAnonymous)
        {
            switch (aspectAdviceAnonymous)
            {
                case AspectAdviceCodeAnonymousAspect aspectAdviceCodeAnonymous:
                    return _AspectAdviceCodeAnonymousMember(aspectAdviceCodeAnonymous);
                case AspectAdviceAsttributesAnonymousAspect aspectAdviceAsttributesAnonymous:
                    return _AspectAdviceAttributesAnonymousMember(aspectAdviceAsttributesAnonymous);
                case AspectAdviceChangeValueAnonymousAspect aspectAdviceChangeValueAnonymous:
                    return _AspectAdviceChangeValueAnonymousMember(aspectAdviceChangeValueAnonymous);
                case AspectAdviceTypeMembersAnonymousAspect aspectAdviceTypeMembersAnonymous:
                    return _AspectAdviceTypeMembersAnonymousMember(aspectAdviceTypeMembersAnonymous);
                case AspectAdviceInterfaceMembersAnonymousAspect aspectAdviceInterfaceMembers:
                    return _AspectAdviceInterfaceMembersAnonymousMember(aspectAdviceInterfaceMembers);
                case AspectAdviceEnumMembersAnonymousAspect aspectAdviceEnumMembersAnonymous:
                    return _AspectAdviceEnumMembersAnonymousMember(aspectAdviceEnumMembersAnonymous);
                case AspectAdviceInheritAnonymousAspect aspectAdviceInheritAnonymousAspect:
                    return _AspectAdviceInheritAnonymous(aspectAdviceInheritAnonymousAspect);
            }

            var advice = AdviceTypeDeclaration(GetAdviceAnonymousName(aspectAdviceAnonymous.ParentAspectMember), ((AspectAdviceTypeAnonymousAspect)aspectAdviceAnonymous).TypeMembers, ((AspectAdviceTypeAnonymousAspect)aspectAdviceAnonymous).PrototypeMembers, aspectAdviceAnonymous.OnError);
            return advice;
        }

        static AttributeListSyntax _AspectAdviceAttribute(NameSyntax attributeTypeName)
        {
            var attribute = _GetAttribute(typeof(AspectAdviceAttribute)).WithArgumentList(SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList<AttributeArgumentSyntax>(new SyntaxNodeOrToken[]{SyntaxFactory.AttributeArgument(SyntaxFactory.TypeOfExpression(attributeTypeName))})));
            ;
            return SyntaxFactory.AttributeList().AddAttributes(attribute);
        }

        internal static AttributeListSyntax ExecutionTime(ExecutionTimeAspect executionTime)
        {
            var enumValue = _IConcernEnumMemberAccess(nameof(ExecutionTimes), executionTime.TokenValue);
            var attribute = _GetAttribute(typeof(ExecutionTimeAttribute)).WithArgumentList(SyntaxFactory.AttributeArgumentList(SyntaxFactory.SingletonSeparatedList<AttributeArgumentSyntax>(SyntaxFactory.AttributeArgument((ExpressionSyntax)enumValue))));
            return SyntaxFactory.AttributeList().AddAttributes(attribute);
        }

        internal static AttributeListSyntax ControlsFlow(ControlFlowsAspect controlFlows)
        {
            var expression = (ExpressionSyntax)_IConcernEnumMemberAccess(nameof(ControlFlows), controlFlows.TokenValue);
            if (controlFlows.ControlFlowItems.Count() != 1)
            {
                var enumerator = controlFlows.ControlFlowItems.GetEnumerator();
                enumerator.MoveNext();
                while (enumerator.MoveNext())
                {
                    var value = _IConcernEnumMemberAccess(nameof(ControlFlows), enumerator.Current.TokenValue);
                    expression = SyntaxFactory.BinaryExpression(SyntaxKind.BitwiseOrExpression, expression, value);
                }
            }

            var attribute = SyntaxFactory.Attribute(GetNameSyntax(typeof(ControlFlowsAttribute))).WithArgumentList(SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList<AttributeArgumentSyntax>(new SyntaxNodeOrToken[]{SyntaxFactory.AttributeArgument(expression)})));
            return SyntaxFactory.AttributeList().AddAttributes(attribute);
        }

        internal static MemberAccessExpressionSyntax ControlFlow(ControlFlowAspect controlFlow)
        {
            return _IConcernEnumMemberAccess(nameof(ControlFlows), controlFlow.TokenValue);
        }

        static string _PointcutAnonymousName(AspectMemberDeclarationAspect aspect)
        {
            return _PointcutAnonymousName(aspect.Identifier.TokenValue);
        }

        static string _PointcutAnonymousName(string name)
        {
            return $"P*{name}";
        }

        internal static string GetAdviceAnonymousName(AspectMemberDeclarationAspect aspect)
        {
            return GetAdviceAnonymousName(aspect.Identifier.TokenValue);
        }

        internal static string GetAdviceAnonymousName(string name)
        {
            return $"A*{name}";
        }

        internal static string GetAdviceMemberAnonymousName(AdviceDeclarationAspect adviceDeclaration)
        {
            return GetAdviceMemberAnonymousName(adviceDeclaration.Identifier.TokenValue);
        }

        internal static string GetAdviceMemberAnonymousName(string name)
        {
            return $"M*{name}";
        }

        internal static AttributeListSyntax BuildReferencedPrototypeTypeAttribute(string[] typenames)
        {
            return SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(SyntaxFactory.Attribute(_IConcernQualifiedName(nameof(ReferencedPrototypeTypesAttribute))).WithArgumentList(SyntaxFactory.AttributeArgumentList(_GetTypeofAttributeArguemntList(typenames)))));
        }

        internal static AttributeListSyntax BuildReferencedAdviceTypeAttribute(string[] typenames)
        {
            return SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(SyntaxFactory.Attribute(_IConcernQualifiedName(nameof(ReferencedAdviceTypesAttribute))).WithArgumentList(SyntaxFactory.AttributeArgumentList(_GetTypeofAttributeArguemntList(typenames)))));
        }

#endregion
#region PrototypeDeclaration
        internal static ClassDeclarationSyntax PrototypeClassDeclaration(PrototypeClassDeclarationAspect prototypeClassDeclaration)
        {
            var @class = SyntaxFactory.ClassDeclaration(Identifier(prototypeClassDeclaration.IdentifierName)).WithMembers(SyntaxList<MemberDeclarationSyntax, CSAspectNode>(prototypeClassDeclaration.Members)).WithModifiers(SyntaxTokenList<KeywordAspect>(prototypeClassDeclaration.Modifiers)).AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            if (!prototypeClassDeclaration.IsNested)
                @class = @class.WithAttributeLists(_GetPrototypeTypeDeclarationAttribute(prototypeClassDeclaration.Id));
            if (prototypeClassDeclaration.TypeParameterList != null)
                @class = @class.WithTypeParameterList((TypeParameterListSyntax)prototypeClassDeclaration.TypeParameterList.GetSyntaxNode());
            if (prototypeClassDeclaration.BaseList != null)
                @class = @class.WithBaseList((BaseListSyntax)prototypeClassDeclaration.BaseList.GetSyntaxNode());
            if (prototypeClassDeclaration.TypeParameterConstraintsClauses != null)
            {
                var list = SyntaxList<TypeParameterConstraintClauseSyntax, TypeParameterConstraintsClauseAspect>(prototypeClassDeclaration.TypeParameterConstraintsClauses);
                @class = @class.WithConstraintClauses(list);
            }

            return @class;
        }

        internal static BaseListSyntax PrototypeBaseList(PrototypeBaseListAspect baseList)
        {
            return SyntaxFactory.BaseList(SeparatedSyntaxList<BaseTypeSyntax, PrototypeBaseTypeAspect>(baseList.BaseTyoes));
        }

        internal static BaseTypeSyntax PrototypeBaseType(PrototypeBaseTypeAspect prototypeBaseType)
        {
            return SyntaxFactory.SimpleBaseType((TypeSyntax)prototypeBaseType.BaseTyoe.GetSyntaxNode());
        }

        internal static FieldDeclarationSyntax PrototypeFieldDeclaration(PrototypeFieldDeclarationAspect prototypeTypeFieldDeclaration)
        {
            var field = SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration((TypeSyntax)prototypeTypeFieldDeclaration.Type.GetSyntaxNode()).WithVariables(SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(SyntaxFactory.VariableDeclarator(prototypeTypeFieldDeclaration.Identifier.GetName())))).AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            if (prototypeTypeFieldDeclaration.Modifiers.Any())
                field = field.AddModifiers(SyntaxTokens<KeywordAspect>(prototypeTypeFieldDeclaration.Modifiers));
            if (!prototypeTypeFieldDeclaration.FromPrototypeType)
                field = field.WithAttributeLists(GetPrototypeItemDeclarationAttribute(prototypeTypeFieldDeclaration.Id));
            return field;
        }

        internal static SyntaxList<AttributeListSyntax> GetPrototypeItemDeclarationAttribute(string id)
        {
            return SyntaxFactory.SingletonList<AttributeListSyntax>(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(_GetAttribute(typeof(PrototypeItemDeclarationAttribute)).WithArgumentList(SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList<AttributeArgumentSyntax>(new SyntaxNodeOrToken[]{SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(id)))}))))));
        }

        static SyntaxList<AttributeListSyntax> _GetPrototypeTypeDeclarationAttribute(string id)
        {
            return SyntaxFactory.SingletonList<AttributeListSyntax>(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(_GetAttribute(typeof(PrototypeTypeDeclarationAttribute)).WithArgumentList(SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList<AttributeArgumentSyntax>(new SyntaxNodeOrToken[]{SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(id)))}))))));
        }

        internal static PropertyDeclarationSyntax PrototypePropertyDeclaration(PrototypePropertyDeclarationAspect prototypeTypePropertyDeclaration)
        {
            var property = SyntaxFactory.PropertyDeclaration((TypeSyntax)prototypeTypePropertyDeclaration.Type.GetSyntaxNode(), prototypeTypePropertyDeclaration.MemberName.Identifier.TokenValue).WithAccessorList(SyntaxFactory.AccessorList(SyntaxList<AccessorDeclarationSyntax, PrototypeAccessorAspect>(prototypeTypePropertyDeclaration.Accessors)));
            if (!prototypeTypePropertyDeclaration.FromPrototypeType)
                property = property.WithAttributeLists(GetPrototypeItemDeclarationAttribute(prototypeTypePropertyDeclaration.Id));
            if (prototypeTypePropertyDeclaration.MemberName.Type != null)
                property = property.WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier((NameSyntax)prototypeTypePropertyDeclaration.MemberName.Type.GetSyntaxNode()));
            property = property.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            if (prototypeTypePropertyDeclaration.Modifiers.Any())
                property = property.AddModifiers(SyntaxTokens<KeywordAspect>(prototypeTypePropertyDeclaration.Modifiers));
            return property;
        }

        internal static AccessorDeclarationSyntax PrototypeGetAccessorDeclaration(PrototypeGetAccessorAspect prototypeGetAccessor)
        {
            var accessorSyntax = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            if (!((PrototypePropertyBaseDeclarationAspect)prototypeGetAccessor.ParentAspectNode).Modifiers.Any(t => t.Keyword == Keywords.ABSTRACT))
                accessorSyntax = accessorSyntax.WithBody(_BlockSyntaxNotImplementedException());
            return accessorSyntax;
        }

        internal static AccessorDeclarationSyntax PrototypeSetAccessorDeclaration(PrototypeSetAccessorAspect prototypeSetAccessor)
        {
            var accessorSyntax = SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            if (!((PrototypePropertyBaseDeclarationAspect)prototypeSetAccessor.ParentAspectNode).Modifiers.Any(t => t.Keyword == Keywords.ABSTRACT))
                accessorSyntax = accessorSyntax.WithBody(_BlockSyntaxNotImplementedException());
            return accessorSyntax;
        }

        internal static AccessorDeclarationSyntax PrototypeAddAccessorDeclaration(PrototypeAddAccessorAspect prototypeAddtAccessor)
        {
            return SyntaxFactory.AccessorDeclaration(SyntaxKind.AddAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)).WithBody(_BlockSyntaxNotImplementedException());
        }

        internal static AccessorDeclarationSyntax PrototypeRemoveAccessorDeclaration(PrototypeRemoveAccessorAspect prototypeRemoveAccessor)
        {
            return SyntaxFactory.AccessorDeclaration(SyntaxKind.RemoveAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)).WithBody(_BlockSyntaxNotImplementedException());
        }

        internal static MethodDeclarationSyntax PrototypeMethodDeclaration(PrototypeMethodDeclarationAspect prototypeTypeMethodDeclaration)
        {
            var method = SyntaxFactory.MethodDeclaration((TypeSyntax)prototypeTypeMethodDeclaration.ReturnType.GetSyntaxNode(), prototypeTypeMethodDeclaration.Identifier.TokenValue).WithBody(_BlockSyntaxNotImplementedException());
            if (!prototypeTypeMethodDeclaration.FromPrototypeType)
                method = method.WithAttributeLists(GetPrototypeItemDeclarationAttribute(prototypeTypeMethodDeclaration.Id));
            if (prototypeTypeMethodDeclaration.TypeParameterList != null)
                method = method.WithTypeParameterList((TypeParameterListSyntax)(prototypeTypeMethodDeclaration.TypeParameterList.GetSyntaxNode()));
            if (prototypeTypeMethodDeclaration.ParameterList != null)
                method = method.WithParameterList((ParameterListSyntax)(prototypeTypeMethodDeclaration.ParameterList.GetSyntaxNode()));
            if (!prototypeTypeMethodDeclaration.FromPrototypeType)
            {
                method = method.WithBody(SyntaxFactory.Block(SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.ThrowStatement(SyntaxFactory.ObjectCreationExpression(SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("NotImplementedException"))).WithArgumentList(SyntaxFactory.ArgumentList())))));
            }
            else
            {
                method = method.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            }

            method = method.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            if (prototypeTypeMethodDeclaration.Modifiers.Any())
                method = method.AddModifiers(SyntaxTokens<KeywordAspect>(prototypeTypeMethodDeclaration.Modifiers));
            return method;
        }

        internal static IndexerDeclarationSyntax PrototypeIndexerDeclaration(PrototypeIndexerDeclarationtAspect prototypeTypeIndexerDeclaration)
        {
            var indexer = SyntaxFactory.IndexerDeclaration((TypeSyntax)prototypeTypeIndexerDeclaration.Type.GetSyntaxNode()).WithAccessorList(SyntaxFactory.AccessorList(SyntaxList<AccessorDeclarationSyntax, PrototypeAccessorAspect>(prototypeTypeIndexerDeclaration.Accessors)));
            if (!prototypeTypeIndexerDeclaration.FromPrototypeType)
                indexer = indexer.WithAttributeLists(GetPrototypeItemDeclarationAttribute(prototypeTypeIndexerDeclaration.Id));
            if (prototypeTypeIndexerDeclaration.FormalParameterList != null)
                indexer = indexer.WithParameterList(BracketedParameterList(prototypeTypeIndexerDeclaration.FormalParameterList));
            indexer = indexer.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            if (prototypeTypeIndexerDeclaration.Modifiers.Any())
                indexer = indexer.AddModifiers(SyntaxTokens<KeywordAspect>(prototypeTypeIndexerDeclaration.Modifiers));
            return indexer;
        }

        internal static EventFieldDeclarationSyntax PrototypeEventFieldDeclaration(PrototypeEventFieldDeclarationAspect prototypeTypeEventFieldDeclaration)
        {
            var @event = SyntaxFactory.EventFieldDeclaration(SyntaxFactory.VariableDeclaration((TypeSyntax)prototypeTypeEventFieldDeclaration.Type.GetSyntaxNode()).WithVariables(SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(prototypeTypeEventFieldDeclaration.Identifier.TokenValue)))));
            if (!prototypeTypeEventFieldDeclaration.FromPrototypeType)
                @event = @event.WithAttributeLists(GetPrototypeItemDeclarationAttribute(prototypeTypeEventFieldDeclaration.Id));
            @event = @event.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            if (prototypeTypeEventFieldDeclaration.Modifiers.Any())
                @event = @event.AddModifiers(SyntaxTokens<KeywordAspect>(prototypeTypeEventFieldDeclaration.Modifiers));
            return @event;
        }

        internal static EventDeclarationSyntax PrototypeEventPropertyDeclaration(PrototypeEventPropertyDeclarationAspect prototypeTypeEventPropertyDeclaration)
        {
            var @event = SyntaxFactory.EventDeclaration((TypeSyntax)prototypeTypeEventPropertyDeclaration.Type.GetSyntaxNode(), prototypeTypeEventPropertyDeclaration.Identifier.TokenValue);
            if (!prototypeTypeEventPropertyDeclaration.FromPrototypeType)
                @event = @event.WithAttributeLists(GetPrototypeItemDeclarationAttribute(prototypeTypeEventPropertyDeclaration.Id));
            @event = @event.WithAccessorList(SyntaxFactory.AccessorList(SyntaxList<AccessorDeclarationSyntax, PrototypeEventccessorAspect>(prototypeTypeEventPropertyDeclaration.Accessors)));
            @event = @event.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            if (prototypeTypeEventPropertyDeclaration.Modifiers.Any())
                @event = @event.AddModifiers(SyntaxTokens<KeywordAspect>(prototypeTypeEventPropertyDeclaration.Modifiers));
            return @event;
        }

        internal static EnumDeclarationSyntax PrototypeEnumDeclaration(PrototypeEnumDeclarationAspect prototypeEnumDeclaration)
        {
            var @enum = SyntaxFactory.EnumDeclaration(Identifier(prototypeEnumDeclaration.Identifier)).WithMembers(SeparatedSyntaxList<EnumMemberDeclarationSyntax, EnumMemberDeclarationApsect>(prototypeEnumDeclaration.EnumMembers)).AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            if (!prototypeEnumDeclaration.IsNested)
                @enum = @enum.WithAttributeLists(_GetPrototypeTypeDeclarationAttribute(prototypeEnumDeclaration.Id));
            if (prototypeEnumDeclaration.EnumBase != null)
                @enum = @enum.WithBaseList((BaseListSyntax)prototypeEnumDeclaration.EnumBase.GetSyntaxNode());
            return @enum;
        }

        internal static ConstructorDeclarationSyntax PrototypeConstructorDeclaration(PrototypeConstructorDeclarationtAspect prototypeTypeConstructorDeclaration)
        {
            var identifier = prototypeTypeConstructorDeclaration.Identifier?.GetName();
            if (!prototypeTypeConstructorDeclaration.FromPrototypeType)
            {
                switch (prototypeTypeConstructorDeclaration.ParentAspectNode)
                {
                    case AdviceTypeMembersDeclarationAspect advice:
                        identifier = advice.Identifier.GetName();
                        break;
                    case IAspectAdviceAnnonymousAspect anonymous:
                        identifier = GetAdviceAnonymousName(((IAspectAdviceAnnonymousAspect)anonymous).ParentAspectMember);
                        break;
                }
            }

            var constructor = SyntaxFactory.ConstructorDeclaration(identifier).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)).WithBody(_BlockSyntaxNotImplementedException());
            if (!prototypeTypeConstructorDeclaration.FromPrototypeType)
                constructor = constructor.WithAttributeLists(GetPrototypeItemDeclarationAttribute(prototypeTypeConstructorDeclaration.Id));
            if (prototypeTypeConstructorDeclaration.ParameterList != null)
                constructor = constructor.WithParameterList((ParameterListSyntax)(prototypeTypeConstructorDeclaration.ParameterList.GetSyntaxNode()));
            if (prototypeTypeConstructorDeclaration.ConstructorInitializer != null)
                constructor = constructor.WithInitializer(((ConstructorInitializerSyntax)prototypeTypeConstructorDeclaration.ConstructorInitializer.GetSyntaxNode()));
            return constructor.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
        }

        internal static StructDeclarationSyntax PrototypeStructDeclaration(PrototypeStructDeclarationAspect prototypeStructDeclaration)
        {
            var @struct = SyntaxFactory.StructDeclaration(Identifier(prototypeStructDeclaration.IdentifierName)).WithMembers(SyntaxList<MemberDeclarationSyntax, CSAspectNode>(prototypeStructDeclaration.Members)).AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            if (!prototypeStructDeclaration.IsNested)
                @struct = @struct.WithAttributeLists(_GetPrototypeTypeDeclarationAttribute(prototypeStructDeclaration.Id));
            if (prototypeStructDeclaration.TypeParameterList != null)
                @struct = @struct.WithTypeParameterList((TypeParameterListSyntax)prototypeStructDeclaration.TypeParameterList.GetSyntaxNode());
            if (prototypeStructDeclaration.BaseList != null)
                @struct = @struct.WithBaseList((BaseListSyntax)prototypeStructDeclaration.BaseList.GetSyntaxNode());
            return @struct;
        }

        internal static InterfaceDeclarationSyntax PrototypeInterfaceDeclaration(PrototypeInterfaceDeclarationAspect prototypeInterfaceDeclaration)
        {
            var @interface = SyntaxFactory.InterfaceDeclaration(Identifier(prototypeInterfaceDeclaration.Identifier)).WithMembers(SyntaxList<MemberDeclarationSyntax, InterfaceMemberAspect>(prototypeInterfaceDeclaration.Members)).WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)));
            if (!prototypeInterfaceDeclaration.IsNested)
                @interface = @interface.WithAttributeLists(_GetPrototypeTypeDeclarationAttribute(prototypeInterfaceDeclaration.Id));
            if (prototypeInterfaceDeclaration.TypeParameterList != null)
                @interface = @interface.WithTypeParameterList((TypeParameterListSyntax)prototypeInterfaceDeclaration.TypeParameterList.GetSyntaxNode());
            if (prototypeInterfaceDeclaration.BaseList != null)
                @interface = @interface.WithBaseList((BaseListSyntax)prototypeInterfaceDeclaration.BaseList.GetSyntaxNode());
            return @interface;
        }

        internal static DelegateDeclarationSyntax PrototypeDelegateDeclaration(PrototypeDelegateDeclarationAspect prototypeDelegateDeclaration)
        {
            var @delegate = SyntaxFactory.DelegateDeclaration((TypeSyntax)prototypeDelegateDeclaration.ReturnType.GetSyntaxNode(), prototypeDelegateDeclaration.Identifier.TokenValue).WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)));
            if (!prototypeDelegateDeclaration.IsNested)
                @delegate = @delegate.WithAttributeLists(_GetPrototypeTypeDeclarationAttribute(prototypeDelegateDeclaration.Id));
            if (prototypeDelegateDeclaration.TypeParameterList != null)
                @delegate = @delegate.WithTypeParameterList((TypeParameterListSyntax)(prototypeDelegateDeclaration.TypeParameterList.GetSyntaxNode()));
            if (prototypeDelegateDeclaration.FormalParameterList != null)
                @delegate = @delegate.WithParameterList((ParameterListSyntax)(prototypeDelegateDeclaration.FormalParameterList.GetSyntaxNode()));
            return @delegate;
        }

        internal static TypeParameterSyntax PrototypeTypeParameter(PrototypeTypeParameterAspect prototypeTypeParameter)
        {
            return SyntaxFactory.TypeParameter(prototypeTypeParameter.Identifier.TokenValue);
        }

#endregion
#region PrototypetypeMapping
        internal static SyntaxList<AttributeListSyntax> PrototypeTypeMapping(PrototypeTypeMappingAspect prototypeTypeMappingDeclaration)
        {
            var syntaxList = new List<AttributeListSyntax>();
            prototypeTypeMappingDeclaration.PrototypeTypeMappingItems.ToList().ForEach(t => syntaxList.Add(((AttributeListSyntax)t.GetSyntaxNode()).WithTarget(SyntaxFactory.AttributeTargetSpecifier(SyntaxFactory.Token(SyntaxKind.AssemblyKeyword)))));
            return new SyntaxList<AttributeListSyntax>(syntaxList);
        }

        internal static AttributeListSyntax PrototypeTypeMappingItem(PrototypeTypeMappingItemAspect prototypeTypeMappingMember)
        {
            var typeofExpression = (NameSyntax)prototypeTypeMappingMember.PrototypeTypeName.GetSyntaxNode();
            var prototypeTypeExpression = SyntaxFactory.TypeOfExpression(typeofExpression);
            var list = new List<AttributeSyntax>();
            list.Add(SyntaxFactory.Attribute(_IConcernQualifiedName(nameof(PrototypeTypeMappingAttribute))).WithArgumentList(SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList<AttributeArgumentSyntax>(new AttributeArgumentSyntax[]{SyntaxFactory.AttributeArgument(prototypeTypeExpression), SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(prototypeTypeMappingMember.TargetTypeNameAspect.GetSyntaxNode().ToString())))}))));
            var mappings = SeparatedSyntaxList<AttributeSyntax>(list.ToArray());
            return SyntaxFactory.AttributeList(mappings);
        }

#endregion
#region PrototypeItemMapping
        internal static AttributeListSyntax ThisDeclarationAttribute(string fullPrototypeTypename)
        {
            var attribute = SyntaxFactory.Attribute(GetNameSyntax(typeof(ThisDeclarationAttribute))).WithArgumentList(SyntaxFactory.AttributeArgumentList(SyntaxFactory.SingletonSeparatedList<AttributeArgumentSyntax>(SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(fullPrototypeTypename))))));
            return SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(attribute));
        }

        internal static AttributeListSyntax PrototypeMappingMember(PrototypeMappingMemberAspect prototypeMemberMappingAspect)
        {
            var source = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(prototypeMemberMappingAspect.Source.TokenValue));
            var prototypeItemMappingSourceKind = nameof(PrototypeItemMappingSourceKinds.Member);
            string prototypeItemTargetKind = null;
            string prototypeItemTargetName = "";
            switch (prototypeMemberMappingAspect.Target)
            {
                case PrototypeTargetThisMemberDeclarationAspect prototypeTargetThis:
                    prototypeItemTargetKind = nameof(PrototypeItemMappingTargetKinds.ThisMember);
                    prototypeItemTargetName = prototypeTargetThis.IdentifierValue;
                    break;
                case PrototypeTargetBaseMemberDeclarationAspect prototypeTargetBase:
                    prototypeItemTargetKind = nameof(PrototypeItemMappingTargetKinds.BaseMember);
                    prototypeItemTargetName = prototypeTargetBase.IdentifierValue;
                    break;
                case PrototypeTargetMemberDeclarationAspect prototypeTargetMember:
                    switch (prototypeTargetMember.TokenValue)
                    {
                        default:
                            prototypeItemTargetKind = nameof(PrototypeItemMappingTargetKinds.Member);
                            prototypeItemTargetName = prototypeTargetMember.TokenValue;
                            break;
                    }

                    break;
                default:
                    throw new NotSupportedException();
            }

            return PrototypeItemMappingAttributeList(prototypeItemMappingSourceKind, source, prototypeItemTargetKind, prototypeItemTargetName);
        }

        internal static AttributeListSyntax PrototypeMappingTypeParameter(PrototypeMappingTypeParameterAspect prototypeTypeParamterMappingAspect)
        {
            var source = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(prototypeTypeParamterMappingAspect.Source.TokenValue));
            var prototypeItemTargetKind = "";
            var prototypeItemTargetName = "";
            switch (prototypeTypeParamterMappingAspect.Target)
            {
                case PrototypeTypeGenericParameterTargetAspect typeGenericParameter:
                    prototypeItemTargetKind = nameof(PrototypeItemMappingTargetKinds.TypeGenericParameter);
                    prototypeItemTargetName = typeGenericParameter.Target.TokenValue;
                    break;
                case PrototypeMethodGenericParameterTargetAspect methodGenericParameter:
                    prototypeItemTargetKind = nameof(PrototypeItemMappingTargetKinds.MethodGenericParameter);
                    prototypeItemTargetName = methodGenericParameter.Target.TokenValue;
                    break;
                default:
                    throw new NotSupportedException();
            }

            string prototypeItemMappingSourceKind = nameof(PrototypeItemMappingSourceKinds.GenericParameter);
            return PrototypeItemMappingAttributeList(prototypeItemMappingSourceKind, source, prototypeItemTargetKind, prototypeItemTargetName);
        }

        internal static AttributeListSyntax PrototypeMappingTypeReference(PrototypeMappingTypeReferenceAspect prototypeTypeReferenceMappingAspect)
        {
            var source = SyntaxFactory.TypeOfExpression((TypeSyntax)prototypeTypeReferenceMappingAspect.PrototypeTypeNameReference.GetSyntaxNode());
            var prototypeItemMappingSourceKind = nameof(PrototypeItemMappingSourceKinds.AdviceType);
            string prototypeItemTargetKind = nameof(PrototypeItemMappingTargetKinds.NamespaceOrClass);
            string prototypeItemTargetName = prototypeTypeReferenceMappingAspect.NamespaceOrTypename.GetName();
            return PrototypeItemMappingAttributeList(prototypeItemMappingSourceKind, source, prototypeItemTargetKind, prototypeItemTargetName);
        }

        internal static AttributeSyntax _GetPrototypeMappingTypeReferenceAttribute(PrototypeMappingTypeReferenceAspect prototypeTypeReferenceMappingAspect)
        {
            var source = SyntaxFactory.TypeOfExpression((TypeSyntax)prototypeTypeReferenceMappingAspect.PrototypeTypeNameReference.GetSyntaxNode());
            var prototypeItemMappingSourceKind = nameof(PrototypeItemMappingSourceKinds.AdviceType);
            string prototypeItemTargetKind = nameof(PrototypeItemMappingTargetKinds.NamespaceOrClass);
            string prototypeItemTargetName = prototypeTypeReferenceMappingAspect.NamespaceOrTypename.GetName();
            return PrototypeItemMappingAttribute(prototypeItemMappingSourceKind, source, prototypeItemTargetKind, prototypeItemTargetName);
        }

        internal static AttributeListSyntax PrototypeItemMappingAttributeList(string prototypeItemMappingSourceKind, ExpressionSyntax source, string prototypeItemTargetKind, string prototypeItemTargetName)
        {
            var attibute = PrototypeItemMappingAttribute(prototypeItemMappingSourceKind, source, prototypeItemTargetKind, prototypeItemTargetName);
            return SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(attibute));
        }

        internal static AttributeSyntax PrototypeItemMappingAttribute(string prototypeItemMappingSourceKind, ExpressionSyntax source, string prototypeItemTargetKind, string prototypeItemTargetName)
        {
            var arguments = new SyntaxNodeOrToken[]{SyntaxFactory.AttributeArgument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, _GetIConcernMemberAccess(nameof(PrototypeItemMappingSourceKinds)), SyntaxFactory.IdentifierName(prototypeItemMappingSourceKind))), SyntaxFactory.Token(SyntaxKind.CommaToken), SyntaxFactory.AttributeArgument(source), SyntaxFactory.Token(SyntaxKind.CommaToken), SyntaxFactory.AttributeArgument(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, _GetIConcernMemberAccess(nameof(PrototypeItemMappingTargetKinds)), SyntaxFactory.IdentifierName(prototypeItemTargetKind))), SyntaxFactory.Token(SyntaxKind.CommaToken), SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(prototypeItemTargetName))), };
            var attribute = SyntaxFactory.Attribute(GetNameSyntax(typeof(PrototypeItemMappingAttribute))).WithArgumentList(SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList<AttributeArgumentSyntax>(arguments)));
            return attribute;
        }

#endregion
#region OverloadingConstructor
        internal static IEnumerable<MethodDeclarationSyntax> OverrideConstructors(IEnumerable<OverrideSpecificConstructorDeclarationAspect> overrideSpecificConstructorDeclarations)
        {
            var list = new List<MethodDeclarationSyntax>();
            foreach (var overrideSpecificConstructorDeclaration in overrideSpecificConstructorDeclarations)
            {
                var argumentList = (ArgumentListSyntax)overrideSpecificConstructorDeclaration.BaseConstructorParameters.GetSyntaxNode();
                foreach (var formalParameterList in overrideSpecificConstructorDeclaration.OverrideSpecficConstructors)
                {
                    var ctor = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), SyntaxFactory.Identifier("ctor")).WithParameterList((ParameterListSyntax)formalParameterList.GetSyntaxNode()).WithAttributeLists(_GetIConcernAttributeList(typeof(OverloadingConstructorAttribute)));
                    var expSttmts = new List<StatementSyntax>();
                    foreach (var argument in argumentList.Arguments.Select(a => a.Expression))
                    {
                        var expStmt = SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var")).WithVariables(SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier((expSttmts.Count + 1).ToString())).WithInitializer(SyntaxFactory.EqualsValueClause(argument)))));
                        expSttmts.Add(expStmt);
                    }

                    ctor = ctor.WithBody(SyntaxFactory.Block(expSttmts.ToArray()));
                    list.Add(ctor);
                }
            }

            return list;
        }

#endregion
        internal static CompilationUnitSyntax GetAspectRepository(byte[] bytes)
        {
            return SyntaxFactory.CompilationUnit().WithUsings(SyntaxFactory.SingletonList<UsingDirectiveSyntax>(SyntaxFactory.UsingDirective(SyntaxFactory.IdentifierName("System")))).WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(SyntaxFactory.NamespaceDeclaration(SyntaxFactory.QualifiedName(SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("AspectDN"), SyntaxFactory.IdentifierName("Aspect")), SyntaxFactory.IdentifierName("Concerns"))).WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(SyntaxFactory.ClassDeclaration("AspectRepository").WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword))).WithBaseList(SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(SyntaxFactory.SimpleBaseType(SyntaxFactory.IdentifierName("AspectRepositoryVisitor"))))).WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(SyntaxFactory.ConstructorDeclaration(SyntaxFactory.Identifier("AspectRepository")).WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword))).WithBody(SyntaxFactory.Block(SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName("Bytes"), SyntaxFactory.ArrayCreationExpression(SyntaxFactory.ArrayType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ByteKeyword))).WithRankSpecifiers(SyntaxFactory.SingletonList<ArrayRankSpecifierSyntax>(SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(SyntaxFactory.OmittedArraySizeExpression()))))).WithInitializer(GetBytesArrayInitialize(bytes)))))))))))));
        }

        internal static InitializerExpressionSyntax GetBytesArrayInitialize(byte[] bytes)
        {
            var syntaxNodes = new ExpressionSyntax[bytes.Length];
            for (int i = 0; i < bytes.Length; i++)
                syntaxNodes[i] = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(bytes[i]));
            var list = CSAspectCompilerHelper.SeparatedSyntaxList<ExpressionSyntax>(syntaxNodes);
            return SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression, list);
        }

        static SeparatedSyntaxList<AttributeArgumentSyntax> _GetTypeofAttributeArguemntList(string[] typenames)
        {
            var list = new List<AttributeArgumentSyntax>(typenames.Length);
            foreach (var @typeof in _GetTypeofSyntaxList(typenames))
                list.Add(SyntaxFactory.AttributeArgument(@typeof));
            return SeparatedSyntaxList<AttributeArgumentSyntax>(list.ToArray());
        }

        static IEnumerable<TypeOfExpressionSyntax> _GetTypeofSyntaxList(string[] typenames)
        {
            var list = new List<TypeOfExpressionSyntax>(typenames.Length);
            foreach (var typename in typenames)
            {
                var nameSyntax = ParseName(typename);
                list.Add(SyntaxFactory.TypeOfExpression(nameSyntax));
            }

            return list;
        }

        static BlockSyntax _BlockSyntaxNotImplementedException()
        {
            return SyntaxFactory.Block().AddStatements(new StatementSyntax[]{SyntaxFactory.ThrowStatement(SyntaxFactory.ObjectCreationExpression(SyntaxFactory.QualifiedName(SyntaxFactory.IdentifierName("System"), SyntaxFactory.IdentifierName("NotImplementedException"))).WithArgumentList(SyntaxFactory.ArgumentList()))});
        }

        static MemberAccessExpressionSyntax _IConcernEnumMemberAccess(string memberName, string enumValue)
        {
            return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, _GetIConcernMemberAccess(memberName), SyntaxFactory.IdentifierName(enumValue));
        }

        static MemberAccessExpressionSyntax _GetIConcernMemberAccess(string memberName)
        {
            var memberAccessSyntax = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("AspectDN"), SyntaxFactory.IdentifierName("Aspect")), SyntaxFactory.IdentifierName("Weaving")), SyntaxFactory.IdentifierName("IConcerns")), SyntaxFactory.IdentifierName(memberName));
            return memberAccessSyntax;
        }

        static QualifiedNameSyntax _IConcernQualifiedName(string memberName)
        {
            var memberAccessName = $"{typeof(ICodeAdviceDeclaration).Namespace}.{memberName}";
            return (QualifiedNameSyntax)SyntaxFactory.ParseName(memberAccessName);
        }

        static SyntaxList<AttributeListSyntax> _GetIConcernAttributeList(Type attributeType)
        {
            return new SyntaxList<AttributeListSyntax>(SyntaxFactory.AttributeList().AddAttributes(_GetAttribute(attributeType)));
        }

        static AttributeSyntax _GetAttribute(Type attributeType)
        {
            return SyntaxFactory.Attribute(SyntaxFactory.ParseName(attributeType.FullName));
        }

        internal static NameSyntax GetNameSyntax(Type type)
        {
            return SyntaxFactory.ParseName(type.FullName);
        }

        internal static IEnumerable<NodeType> GetDescendingNodesOfType<NodeType>(CSAspectNode fromParent, bool allLevels = false, CSAspectNode fromNext = null)
            where NodeType : CSAspectNode
        {
            foreach (var node in fromParent.ChildCSAspectNodes)
            {
                if (fromNext == null || (fromNext != null && fromNext.SynToken.End < node.SynToken.Start))
                {
                    var isTypeof = IsTypeOf<NodeType>(node);
                    if (isTypeof)
                        yield return (NodeType)node;
                }

                if (allLevels)
                {
                    foreach (var childNode in GetDescendingNodesOfType<NodeType>((CSAspectNode)node, allLevels))
                        yield return childNode;
                }
            }
        }

        internal static IEnumerable<CSAspectNode> GetDescendingNodesOfType<NodeType1, NodeType2>(CSAspectNode from, bool allLevels = false, CSAspectNode fromNext = null)
            where NodeType1 : CSAspectNode where NodeType2 : CSAspectNode
        {
            foreach (var node in GetDescendingNodesOfType<NodeType1>(from, allLevels, fromNext))
                yield return node;
            foreach (var node in GetDescendingNodesOfType<NodeType2>(from, allLevels, fromNext))
                yield return node;
        }

        internal static IEnumerable<NodeType> GetAscendingNodesOfType<NodeType>(CSAspectNode from, bool allLevels = true)
            where NodeType : CSAspectNode
        {
            if (from.ParentAspectNode != null && IsTypeOf<NodeType>(from.ParentAspectNode))
                yield return (NodeType)from.ParentAspectNode;
            if (allLevels && from.ParentAspectNode != null)
            {
                foreach (var parent in GetAscendingNodesOfType<NodeType>((CSAspectNode)from.ParentAspectNode))
                    yield return (NodeType)parent;
            }
        }

        internal static bool IsTypeOf<TypeOf>(AspectNode aspectNode)
        {
            var nodeType = aspectNode.GetType();
            while (nodeType != null)
            {
                if (nodeType.ToString() == typeof(TypeOf).ToString())
                    return true;
                nodeType = nodeType.BaseType;
            }

            return false;
        }

        internal static CSAspectNode GetAspectNode(CSAspectNode aspectNode, string id)
        {
            CSAspectNode returnNode = null;
            foreach (var childAspectNode in aspectNode.ChildCSAspectNodes)
            {
                if (childAspectNode.Id == id)
                {
                    returnNode = childAspectNode;
                    break;
                }
                else
                {
                    returnNode = GetAspectNode(childAspectNode, id);
                    if (returnNode != null)
                        break;
                }
            }

            return returnNode;
        }

        internal static NameSyntax ParseName(string name)
        {
            var names = name.Split(new char[]{'.'});
            NameSyntax nameSyntax = null;
            for (int i = 0; i < names.Length; i++)
            {
                NameSyntax namei = null;
                if (names[i].IndexOf("<") >= 0)
                {
                    int nbArgs = names[i].Count(t => t == ',') + 1;
                    var typename = names[i].Substring(0, names[i].IndexOf("<"));
                    namei = (GenericNameSyntax)RoslynHelper.GetUnboundTypeName(typename, nbArgs);
                }
                else
                    namei = SyntaxFactory.IdentifierName(names[i]);
                if (nameSyntax == null)
                    nameSyntax = namei;
                else
                {
                    nameSyntax = SyntaxFactory.QualifiedName(nameSyntax, (SimpleNameSyntax)namei);
                }
            }

            return nameSyntax;
        }

        internal static NameSyntax ParseTypename(string typename)
        {
            var typenamesTemplateRegex = @"
				^
				(

					(?<typename>
						[^\<\>\.]*
						(
							\<
							(?>
								(?<open>\<)|
								(?<-open>\>)|
								.?
							)*
							(?(open)(?!))
							\>
						)?
					)
				)
				(
					\.
					(?<typename>
						[^\<\>\.]*
						(
							\<
							(?>
								(?<open>\<)|
								(?<-open>\>)|
								.?
							)*
							(?(open)(?!))
							\>
						)?
					)
				)*
				$
                ";
            var typenameTemplateRegex = @"
					^
					(?<typename>[^\<\>]*)
					(
						\<
						(?<arguments>
							(?>
							(?<open>\<)|
							(?<-open>\>)|
							.?
							)*
							(?(open)(?!))
						)
						\>
					)?
					$
				";
            var argumentsTemplateRegex = @"
		
					^
					(
						(?<typename>
							[^\<\>,]*
							(
								\<
								(?>
									(?<open>\<)|
									(?<-open>\>)|
									.?
								)*
								(?(open)(?!))
								\>
							)?
						)
					)
					(
						,
						(?<typename>
							[^\<\>,]*
							(
								\<
								(?>
									(?<open>\<)|
									(?<-open>\>)|
									.?
								)*
								(?(open)(?!))
								\>
							)?
						)
					)*
					$
				";
            var typenamesRegex = new Regex(typenamesTemplateRegex, RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var typenameRegex = new Regex(typenameTemplateRegex, RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var argumentsRegex = new Regex(argumentsTemplateRegex, RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var types = new List<(string typename, List<string> arguments)>();
            var matchTypeNames = typenamesRegex.Match(typename);
            if (matchTypeNames.Success)
            {
                for (int i = 0; i < matchTypeNames.Groups["typename"].Captures.Count; i++)
                {
                    var matchTypeName = typenameRegex.Match(matchTypeNames.Groups["typename"].Captures[i].Value);
                    if (matchTypeName.Success)
                    {
                        List<string> arguments = null;
                        var singleTypename = matchTypeName.Groups["typename"].Captures[0].Value;
                        if (matchTypeName.Groups["arguments"].Success)
                        {
                            arguments = new List<string>();
                            var matchArgs = argumentsRegex.Match(matchTypeName.Groups["arguments"].Captures[0].Value);
                            if (matchArgs.Success)
                            {
                                for (int j = 0; j < matchArgs.Groups["typename"].Captures.Count; j++)
                                {
                                    arguments.Add(matchArgs.Groups["typename"].Captures[j].Value);
                                }
                            }
                        }

                        types.Add((singleTypename, arguments));
                    }
                }
            }
            else
                throw new NotImplementedException();
            NameSyntax nameSyntax = null;
            foreach (var type in types)
            {
                var arguments = type.arguments != null ? type.arguments.ToArray() : null;
                if (nameSyntax == null)
                    nameSyntax = ParseTypename(type.typename, arguments);
                else
                    nameSyntax = SyntaxFactory.QualifiedName(nameSyntax, (SimpleNameSyntax)ParseTypename(type.typename, arguments));
            }

            return nameSyntax;
        }

        internal static NameSyntax ParseTypename(string typename, string[] arguments)
        {
            if (arguments == null || arguments.Length == 0)
                return SyntaxFactory.IdentifierName(typename);
            var argumentNodes = new List<SyntaxNodeOrToken>();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (i != 0)
                    argumentNodes.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
                argumentNodes.Add(ParseTypename(arguments[i]));
            }

            var nameSyntax = SyntaxFactory.GenericName(SyntaxFactory.Identifier(typename)).WithTypeArgumentList(SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList<TypeSyntax>(argumentNodes)));
            return nameSyntax;
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

        internal static IdentifierNameSyntax IdentifierName(IdentifierNameAspect identifier)
        {
            return SyntaxFactory.IdentifierName(identifier.SynToken.Value);
        }

        internal static UsingDirectiveSyntax UsingNamespaceDirective(UsingNamespaceDirectiveAspect usingDirective)
        {
            var nameSyntax = (NameSyntax)usingDirective.ChildAspectNodes[0].GetSyntaxNode();
            return SyntaxFactory.UsingDirective(nameSyntax);
        }

        internal static UsingDirectiveSyntax UsingAliasDirective(UsingAliasDirectiveAspect usingAliasDirective)
        {
            var nameSyntax = SyntaxFactory.NameEquals((IdentifierNameSyntax)usingAliasDirective.IdentiferName.GetSyntaxNode());
            var namespaceOrTypename = (NameSyntax)usingAliasDirective.NamespaceOrTypename.GetSyntaxNode();
            return SyntaxFactory.UsingDirective(namespaceOrTypename).WithAlias(nameSyntax);
        }

        internal static QualifiedNameSyntax QualifiedName(QualifiedIdentifierAspect qualifiedIdentifier)
        {
            return QualifiedName(qualifiedIdentifier.Left, qualifiedIdentifier.Right);
        }

        internal static QualifiedNameSyntax QualifiedName(CSAspectNode left, CSAspectNode right)
        {
            return SyntaxFactory.QualifiedName((NameSyntax)left.GetSyntaxNode(), (SimpleNameSyntax)right.GetSyntaxNode());
        }

        internal static NameSyntax NamespaceOrTypename(NamespaceOrTypenameAspect namespaceOrTypename)
        {
            NameSyntax nameSyntax;
            if (namespaceOrTypename.Right == null)
                nameSyntax = (NameSyntax)namespaceOrTypename.Left.GetSyntaxNode();
            else
                nameSyntax = QualifiedName(namespaceOrTypename.Left, namespaceOrTypename.Right);
            return nameSyntax;
        }

        internal static StatementSyntax AroundStatement(AroundStatementAspect aroundStatement)
        {
            return SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("_AroundStatement"))).WithSemicolonToken(SyntaxFactory.MissingToken(SyntaxKind.SemicolonToken));
        }

        internal static AliasQualifiedNameSyntax QualifiedAliasMember(QualifiedAliasMemberAspect qualifiedIdentifier)
        {
            return SyntaxFactory.AliasQualifiedName((IdentifierNameSyntax)qualifiedIdentifier.Left.GetSyntaxNode(), (SimpleNameSyntax)qualifiedIdentifier.Right.GetSyntaxNode());
        }

        internal static TypeArgumentListSyntax TypeArgumentListSyntax(TypeArgumentListAspect typeArgumentList)
        {
            switch (typeArgumentList.TypeArguments.Count())
            {
                case 0:
                    return SyntaxFactory.TypeArgumentList();
                case 1:
                    var single = SyntaxFactory.SingletonSeparatedList<TypeSyntax>((TypeSyntax)typeArgumentList.TypeArguments.First().GetSyntaxNode());
                    return SyntaxFactory.TypeArgumentList(single);
                default:
                    var tokens = new SyntaxNodeOrTokenList();
                    foreach (var type in typeArgumentList.TypeArguments)
                    {
                        if (tokens.Count() > 0)
                            tokens = tokens.Add(SyntaxFactory.Token(SyntaxKind.CommaToken));
                        tokens = tokens.Add((TypeSyntax)type.GetSyntaxNode());
                    }

                    var separatedList = SyntaxFactory.SeparatedList<TypeSyntax>(new SyntaxNodeOrTokenList().AddRange(tokens));
                    return SyntaxFactory.TypeArgumentList(separatedList);
            }
        }

        internal static SyntaxToken Keyword(KeywordAspect keyword)
        {
            switch (keyword.Keyword)
            {
                case Keywords.ABSTRACT:
                    return SyntaxFactory.Token(SyntaxKind.AbstractKeyword);
                case Keywords.STATIC:
                    return SyntaxFactory.Token(SyntaxKind.StaticKeyword);
                case Keywords.AS:
                    return SyntaxFactory.Token(SyntaxKind.AsKeyword);
                case Keywords.OUT:
                    return SyntaxFactory.Token(SyntaxKind.OutKeyword);
                case Keywords.REF:
                    return SyntaxFactory.Token(SyntaxKind.RefKeyword);
                case Keywords.BOOL:
                    return SyntaxFactory.Token(SyntaxKind.BoolKeyword);
                case Keywords.BYTE:
                    return SyntaxFactory.Token(SyntaxKind.ByteKeyword);
                case Keywords.CHAR:
                    return SyntaxFactory.Token(SyntaxKind.CharKeyword);
                case Keywords.DECIMAL:
                    return SyntaxFactory.Token(SyntaxKind.DecimalKeyword);
                case Keywords.DOUBLE:
                    return SyntaxFactory.Token(SyntaxKind.DoubleKeyword);
                case Keywords.FLOAT:
                    return SyntaxFactory.Token(SyntaxKind.FloatKeyword);
                case Keywords.LONG:
                    return SyntaxFactory.Token(SyntaxKind.LongKeyword);
                case Keywords.SBYTE:
                    return SyntaxFactory.Token(SyntaxKind.SByteKeyword);
                case Keywords.OBJECT:
                    return SyntaxFactory.Token(SyntaxKind.ObjectKeyword);
                case Keywords.INT:
                    return SyntaxFactory.Token(SyntaxKind.IntKeyword);
                case Keywords.USHORT:
                    return SyntaxFactory.Token(SyntaxKind.UShortKeyword);
                case Keywords.SHORT:
                    return SyntaxFactory.Token(SyntaxKind.ShortKeyword);
                case Keywords.STRING:
                    return SyntaxFactory.Token(SyntaxKind.StringKeyword);
                case Keywords.UINT:
                    return SyntaxFactory.Token(SyntaxKind.UIntKeyword);
                case Keywords.ULONG:
                    return SyntaxFactory.Token(SyntaxKind.ULongKeyword);
                case Keywords.VOID:
                    return SyntaxFactory.Token(SyntaxKind.VoidKeyword);
                case Keywords.BASE:
                case Keywords.BREAK:
                case Keywords.CASE:
                case Keywords.CATCH:
                case Keywords.CHECKED:
                case Keywords.CONST:
                case Keywords.CONTINUE:
                case Keywords.DEFAULT:
                case Keywords.DELEGATE:
                case Keywords.DO:
                case Keywords.ELSE:
                case Keywords.ENUM:
                case Keywords.EVENT:
                case Keywords.EXPLICIT:
                case Keywords.EXTERN:
                case Keywords.FINALLY:
                case Keywords.FIXED:
                case Keywords.FOR:
                case Keywords.FOREACH:
                case Keywords.GOTO:
                case Keywords.IF:
                case Keywords.IMPLICIT:
                case Keywords.IN:
                case Keywords.INTERFACE:
                case Keywords.IS:
                case Keywords.LOCK:
                case Keywords.NAMESPACE:
                case Keywords.NEW:
                case Keywords.OPERATOR:
                case Keywords.OVERRIDE:
                case Keywords.PARAMS:
                case Keywords.PRIVATE:
                case Keywords.PROTECTED:
                case Keywords.PUBLIC:
                case Keywords.READONLY:
                case Keywords.RETURN:
                case Keywords.SEALED:
                case Keywords.SIZEOF:
                case Keywords.STACKALLOC:
                case Keywords.STRUCT:
                case Keywords.SWITCH:
                case Keywords.THIS:
                case Keywords.THROW:
                case Keywords.TRY:
                case Keywords.TYPEOF:
                case Keywords.UNCHECKED:
                case Keywords.UNSAFE:
                case Keywords.USING:
                case Keywords.VIRTUAL:
                case Keywords.VOLATILE:
                case Keywords.WHILE:
                default:
                    throw new NotImplementedException();
            }
        }

        internal static S[] SyntaxNodeArray<S, A>(IEnumerable<A> list)
            where A : CSAspectNode where S : SyntaxNode
        {
            S[] arr = new S[list.Count()];
            int i = 0;
            foreach (var item in list)
                arr[i++] = (S)item.GetSyntaxNode();
            return arr;
        }

        internal static SyntaxTokenList SyntaxTokenList(params SyntaxToken[] tokens)
        {
            switch (tokens.Length)
            {
                case 0:
                    return default(SyntaxTokenList);
                case 1:
                    return new SyntaxTokenList().Add(tokens[0]);
                default:
                    return new SyntaxTokenList().AddRange(tokens);
            }
        }

        internal static SyntaxTokenList SyntaxTokenList<A>(IEnumerable<A> aspectNodes)
            where A : CSAspectNode
        {
            return SyntaxTokenList(SyntaxTokens<A>(aspectNodes));
        }

        internal static SyntaxToken[] SyntaxTokens<A>(IEnumerable<A> aspectNodes)
            where A : CSAspectNode
        {
            var tokens = new SyntaxToken[aspectNodes.Count()];
            int i = 0;
            foreach (var item in aspectNodes)
                tokens[i++] = (SyntaxToken)item.GetSyntaxNode();
            return tokens;
        }

        internal static SeparatedSyntaxList<S> SeparatedSyntaxList<S, A>(IEnumerable<A> list)
            where S : SyntaxNode where A : CSAspectNode
        {
            var tokens = SyntaxNodeArray<S, A>(list);
            return SeparatedSyntaxList<S>(tokens);
        }

        internal static SeparatedSyntaxList<T> SeparatedSyntaxList<T>(params T[] tokens)
            where T : SyntaxNode
        {
            switch (tokens.Length)
            {
                case 0:
                    return SyntaxFactory.SeparatedList<T>();
                case 1:
                    return SyntaxFactory.SingletonSeparatedList<T>(tokens[0]);
                default:
                    var items = new SyntaxNodeOrToken[tokens.Count() * 2 - 1];
                    for (int i = 0; i < tokens.Count(); i++)
                    {
                        items[i * 2] = tokens[i];
                        if (i < tokens.Count() - 1)
                            items[i * 2 + 1] = SyntaxFactory.Token(SyntaxKind.CommaToken);
                    }

                    return SyntaxFactory.SeparatedList<T>(items);
            }
        }

        internal static SeparatedSyntaxList<T> SeparatedSyntaxList<T>(T token, int count)
            where T : SyntaxNode
        {
            var tokens = new T[count];
            for (int i = 0; i < count; i++)
                tokens[i] = token;
            return SeparatedSyntaxList<T>(tokens);
        }

        internal static SyntaxToken Identifier(IdentifierNameAspect identifier)
        {
            return SyntaxFactory.Identifier(identifier.TokenValue);
        }

        internal static SyntaxList<S> SyntaxList<S, A>(IEnumerable<A> list)
            where S : SyntaxNode where A : CSAspectNode
        {
            var syntaxes = new S[list.Count()];
            var i = 0;
            foreach (var item in list)
                syntaxes[i++] = (S)item.GetSyntaxNode();
            return new SyntaxList<S>().AddRange(syntaxes);
        }

        internal static SeparatedSyntaxList<ExpressionSyntax> SyntaxListLiteralExpression(IEnumerable<string> strings)
        {
            var array = strings.ToArray();
            switch (array.Length)
            {
                case 0:
                    throw new NotImplementedException();
                case 1:
                    return SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(GetLiteralExpression(array.First()));
                default:
                    var items = new SyntaxNodeOrToken[array.Count() * 2 - 1];
                    for (int i = 0; i < array.Count(); i++)
                    {
                        items[i * 2] = GetLiteralExpression(array[i]);
                        if (i < array.Count() - 1)
                            items[i * 2 + 1] = SyntaxFactory.Token(SyntaxKind.CommaToken);
                    }

                    return SyntaxFactory.SeparatedList<ExpressionSyntax>(items);
            }
        }

        internal static LiteralExpressionSyntax GetLiteralExpression(string s)
        {
            return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(s));
        }

        internal static ExternAliasDirectiveSyntax ExternAliasDirective(ExternAliasDirectiveAspect externalAlias)
        {
            throw new NotImplementedException();
        }

        internal static AnonymousMethodExpressionSyntax AnonymousMethodExpression(AnonymousMethodExpressionAspect anonymousMethodExpression)
        {
            var method = SyntaxFactory.AnonymousMethodExpression((BlockSyntax)anonymousMethodExpression.Body.GetSyntaxNode());
            if (anonymousMethodExpression.Parameters != null && anonymousMethodExpression.Parameters.Any())
            {
                var parameterList = SyntaxFactory.ParameterList(SeparatedSyntaxList<ParameterSyntax>(SyntaxNodeArray<ParameterSyntax, ExplicitAnonymousFunctionParameterAspect>(anonymousMethodExpression.Parameters)));
                method = method.WithParameterList(parameterList);
            }

            return method;
        }

        internal static ParameterSyntax ExplicitAnonymousFunctionParameter(ExplicitAnonymousFunctionParameterAspect parameter)
        {
            var param = SyntaxFactory.Parameter(Identifier(parameter.Identifier));
            if (parameter.Modifier != null)
                param = param.WithModifiers(SyntaxTokenList(new SyntaxToken[]{(SyntaxToken)parameter.Modifier.GetSyntaxNode()}));
            if (parameter.Type != null)
                param = param.WithType((TypeSyntax)parameter.Type.GetSyntaxNode());
            return param;
        }

        internal static ParameterSyntax ImplicitAnonymousFunctionParameter(ImplicitAnonymousFunctionParameterAspect parameter)
        {
            return SyntaxFactory.Parameter(Identifier(parameter.Identifier));
        }

        internal static CSharpSyntaxNode AnonymousFunctionBody(AnonymousFunctionBodyAspect anonymousFunctionBody)
        {
            return (CSharpSyntaxNode)anonymousFunctionBody.Expression.GetSyntaxNode();
        }

        internal static LambdaExpressionSyntax SimpleLambdaExpression(SimpleLambdaExpressionAspect lambdaExpressionAspect)
        {
            return SyntaxFactory.SimpleLambdaExpression((ParameterSyntax)lambdaExpressionAspect.Parameter.GetSyntaxNode(), (CSharpSyntaxNode)lambdaExpressionAspect.Body.GetSyntaxNode());
        }

        internal static LambdaExpressionSyntax ParenthesisLambdaExpression(ParenthesisLambdaExpressionAspect lambdaExpressionAspect)
        {
            var expressionSyntax = (CSharpSyntaxNode)lambdaExpressionAspect.Body.GetSyntaxNode();
            if (lambdaExpressionAspect.Parameters == null)
                return SyntaxFactory.ParenthesizedLambdaExpression((CSharpSyntaxNode)lambdaExpressionAspect.Body.GetSyntaxNode());
            var parameters = SyntaxFactory.ParameterList(SeparatedSyntaxList<ParameterSyntax>(SyntaxNodeArray<ParameterSyntax, AnonymousFunctionParameterAspect>(lambdaExpressionAspect.Parameters)));
            return SyntaxFactory.ParenthesizedLambdaExpression(parameters, (CSharpSyntaxNode)lambdaExpressionAspect.Body.GetSyntaxNode());
        }

        internal static InvocationExpressionSyntax InvocationExpression(InvocationExpressionAspect invocationExpression)
        {
            var expression = SyntaxFactory.InvocationExpression((ExpressionSyntax)invocationExpression.PrimaryExpression.GetSyntaxNode());
            if (invocationExpression.ArgumentList != null)
                expression = expression.WithArgumentList((ArgumentListSyntax)invocationExpression.ArgumentList.GetSyntaxNode());
            return expression;
        }

        internal static ArgumentListSyntax ArgumentList(ArgumentListAspect argumentList)
        {
            return SyntaxFactory.ArgumentList(SeparatedSyntaxList<ArgumentSyntax>(SyntaxNodeArray<ArgumentSyntax, ArgumentAspect>(argumentList.Arguments)));
        }

        internal static ArgumentListSyntax ArgumentList(IEnumerable<ExpressionAspect> expressions)
        {
            return SyntaxFactory.ArgumentList(SeparatedSyntaxList<ArgumentSyntax>(SyntaxNodeArray<ArgumentSyntax, ExpressionAspect>(expressions)));
        }

        internal static ArgumentListSyntax ArgumentList(IEnumerable<ArgumentAspect> arguments)
        {
            return SyntaxFactory.ArgumentList(SeparatedSyntaxList<ArgumentSyntax>(SyntaxNodeArray<ArgumentSyntax, ArgumentAspect>(arguments)));
        }

        internal static ElementAccessExpressionSyntax ElementExpression(ElementAccessExpressionAspect invocationExpression)
        {
            var arguments = SyntaxFactory.BracketedArgumentList(SeparatedSyntaxList<ArgumentSyntax>(SyntaxNodeArray<ArgumentSyntax, ArgumentAspect>(invocationExpression.Arguments)));
            return SyntaxFactory.ElementAccessExpression((ExpressionSyntax)invocationExpression.PrimaryExpression.GetSyntaxNode(), arguments);
        }

        internal static ArgumentSyntax Argument(ArgumentAspect argument)
        {
            var arg = SyntaxFactory.Argument((ExpressionSyntax)argument.Expression.GetSyntaxNode());
            if (argument.Modifier != null)
                arg = arg.WithRefOrOutKeyword((SyntaxToken)argument.Modifier.GetSyntaxNode());
            if (argument.Name != null)
                arg = arg.WithNameColon((NameColonSyntax)argument.Name.GetSyntaxNode());
            return arg;
        }

        internal static NameColonSyntax ArgumentName(ArgumentNameAspect name)
        {
            return SyntaxFactory.NameColon((IdentifierNameSyntax)name.GetSyntaxNode());
        }

        internal static ExpressionSyntax VariableReference(VariableReferenceAspect variableRef)
        {
            return (ExpressionSyntax)variableRef.Expression.GetSyntaxNode();
        }

        internal static BinaryExpressionSyntax BinaryExpression(BinaryExpressionAspect binaryExpression)
        {
            return SyntaxFactory.BinaryExpression(SyntaxKindExpression(binaryExpression.Operator), (ExpressionSyntax)binaryExpression.Left.GetSyntaxNode(), (ExpressionSyntax)binaryExpression.Right.GetSyntaxNode());
        }

        internal static BinaryExpressionSyntax BinaryConditionalExpression(BinaryConditionalExpressionAspect conditionalExpression)
        {
            return SyntaxFactory.BinaryExpression(SyntaxKindExpression(conditionalExpression.Operator), (ExpressionSyntax)conditionalExpression.LeftExpression.GetSyntaxNode(), (ExpressionSyntax)conditionalExpression.RightExpression.GetSyntaxNode());
        }

        internal static ConditionalExpressionSyntax ConditionalExpression(ConditionalExpressionAspect conditionalExpression)
        {
            return SyntaxFactory.ConditionalExpression((BinaryExpressionSyntax)conditionalExpression.Expression.GetSyntaxNode(), (ExpressionSyntax)conditionalExpression.LeftExpression.GetSyntaxNode(), (ExpressionSyntax)conditionalExpression.RightExpression.GetSyntaxNode());
        }

        internal static SyntaxKind SyntaxKindExpression(OperatorAspect @operator)
        {
            switch (@operator.TokenValue)
            {
                case "+":
                    return SyntaxKind.AddExpression;
                case "-":
                    return SyntaxKind.SubtractExpression;
                case "/":
                    return SyntaxKind.DivideExpression;
                case "*":
                    return SyntaxKind.MultiplyExpression;
                case "%":
                    return SyntaxKind.ModuloExpression;
                case ">>":
                    return SyntaxKind.RightShiftExpression;
                case "<<":
                    return SyntaxKind.LeftShiftExpression;
                case "<":
                    return SyntaxKind.LessThanExpression;
                case ">":
                    return SyntaxKind.GreaterThanExpression;
                case "==":
                    return SyntaxKind.EqualsExpression;
                case "<=":
                    return SyntaxKind.LessThanOrEqualExpression;
                case ">=":
                    return SyntaxKind.GreaterThanOrEqualExpression;
                case "is":
                    return SyntaxKind.IsExpression;
                case "as":
                    return SyntaxKind.AsExpression;
                case "=":
                    return SyntaxKind.SimpleAssignmentExpression;
                case "!=":
                    return SyntaxKind.NotEqualsExpression;
                case "&":
                    return SyntaxKind.BitwiseAndExpression;
                case "^":
                    return SyntaxKind.ExclusiveOrExpression;
                case "|":
                    return SyntaxKind.BitwiseOrExpression;
                case "&&":
                    return SyntaxKind.LogicalAndExpression;
                case "||":
                    return SyntaxKind.LogicalOrExpression;
                case "??":
                    return SyntaxKind.CoalesceExpression;
                case "!":
                    return SyntaxKind.LogicalNotExpression;
                default:
                    throw new NotImplementedException();
            }

            throw new NotImplementedException();
        }

        internal static ParenthesizedExpressionSyntax ParenthesizedExpression(ParenthesizedExpressionAspect parenthesizedExpression)
        {
            return SyntaxFactory.ParenthesizedExpression((ExpressionSyntax)parenthesizedExpression.Expression.GetSyntaxNode());
        }

        internal static MemberAccessExpressionSyntax MemberAccesssExpression(MemberAccessExpressionAspect memberAccessExpression)
        {
            return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, (ExpressionSyntax)memberAccessExpression.Left.GetSyntaxNode(), (SimpleNameSyntax)memberAccessExpression.Right.GetSyntaxNode());
        }

        internal static ThisExpressionSyntax ThisExpression(ThisElementAccessExpressionAspect thisAccess)
        {
            return SyntaxFactory.ThisExpression();
        }

        internal static ExpressionSyntax BaseAccessExpression(BaseElementAccessExpressionAspect baseAccess)
        {
            ExpressionSyntax memberAccess = SyntaxFactory.BaseExpression();
            if (baseAccess.Identifier != null)
            {
                memberAccess = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, (BaseExpressionSyntax)memberAccess, (SimpleNameSyntax)baseAccess.Identifier.GetSyntaxNode());
            }

            if (baseAccess.Arguments.Any())
                memberAccess = SyntaxFactory.InvocationExpression(memberAccess).WithArgumentList(ArgumentList(baseAccess.Arguments));
            return memberAccess;
        }

        internal static ObjectCreationExpressionSyntax ObjectCreationExpression(ObjectCreationExpressionAspect objectCreationExpression)
        {
            TypeSyntax typeSyntax = null;
            if (objectCreationExpression.Type == null && CSAspectCompilerHelper.GetAscendingNodesOfType<AdviceTypeMembersDeclarationAspect>(objectCreationExpression).FirstOrDefault() != null)
            {
                var adviceTypeMember = CSAspectCompilerHelper.GetAscendingNodesOfType<AdviceTypeMembersDeclarationAspect>(objectCreationExpression).First();
                typeSyntax = SyntaxFactory.ParseTypeName(adviceTypeMember.Name);
            }
            else
                typeSyntax = (TypeSyntax)objectCreationExpression.Type.GetSyntaxNode();
            var objectCreation = SyntaxFactory.ObjectCreationExpression(typeSyntax);
            if (objectCreationExpression.ArgumentList != null)
                objectCreation = objectCreation.WithArgumentList((ArgumentListSyntax)objectCreationExpression.ArgumentList.GetSyntaxNode());
            if (objectCreationExpression.Initializer != null)
                objectCreation = objectCreation.WithInitializer((InitializerExpressionSyntax)objectCreationExpression.Initializer.GetSyntaxNode());
            return objectCreation;
        }

        internal static InitializerExpressionSyntax ObjectIntializerExpression(ObjectInitializerAspect objectInitializer)
        {
            var kind = objectInitializer.IsComplex ? SyntaxKind.ComplexElementInitializerExpression : SyntaxKind.ObjectInitializerExpression;
            return SyntaxFactory.InitializerExpression(kind, SeparatedSyntaxList<ExpressionSyntax, MemberInitializerAspect>(objectInitializer.MemberInitializers));
        }

        internal static ExpressionSyntax MemberInitializer(MemberInitializerAspect memberInitializer)
        {
            return SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, (ExpressionSyntax)memberInitializer.Left.GetSyntaxNode(), (ExpressionSyntax)memberInitializer.Right.GetSyntaxNode());
        }

        internal static InitializerExpressionSyntax CollectionInitializerExpression(CollectionInitializerAspect collectionInitializer)
        {
            var kind = collectionInitializer.IsComplex ? SyntaxKind.ComplexElementInitializerExpression : SyntaxKind.CollectionInitializerExpression;
            var expression = SyntaxFactory.InitializerExpression(kind);
            if (collectionInitializer.Elements.Any())
                expression = expression.WithExpressions(SeparatedSyntaxList<ExpressionSyntax, CSAspectNode>(collectionInitializer.Elements));
            return expression;
        }

        internal static ExpressionSyntax ElementInitializer(ElementInitializerAspect elementInitializer)
        {
            return (ExpressionSyntax)elementInitializer.GetSyntaxNode();
        }

        internal static ObjectCreationExpressionSyntax DelegateCreationExpression(DelegateCreationExpressionAspect delegateCreationExpression)
        {
            return SyntaxFactory.ObjectCreationExpression((TypeSyntax)delegateCreationExpression.Type.GetSyntaxNode()).WithArgumentList(ArgumentList(new List<ExpressionAspect>()
            {delegateCreationExpression.Argument}));
        }

        internal static AnonymousObjectCreationExpressionSyntax AnonymousObjectCreationExpression(AnonymousObjectCreationExpressionAspect anonymousObjectCreation)
        {
            return SyntaxFactory.AnonymousObjectCreationExpression(SeparatedSyntaxList<AnonymousObjectMemberDeclaratorSyntax, AnonymousMemberDeclaratorAspect>(anonymousObjectCreation.MemberDeclarators));
        }

        internal static AnonymousObjectMemberDeclaratorSyntax AnonymousMemberDeclarator(AnonymousMemberDeclaratorAspect anonymousMemberDeclarator)
        {
            AnonymousObjectMemberDeclaratorSyntax declaratorSyntax;
            if (anonymousMemberDeclarator.IdentifierName != null)
                declaratorSyntax = SyntaxFactory.AnonymousObjectMemberDeclarator(SyntaxFactory.NameEquals((IdentifierNameSyntax)anonymousMemberDeclarator.IdentifierName.GetSyntaxNode()), (ExpressionSyntax)anonymousMemberDeclarator.Expression.GetSyntaxNode());
            else
                declaratorSyntax = SyntaxFactory.AnonymousObjectMemberDeclarator((ExpressionSyntax)anonymousMemberDeclarator.Expression.GetSyntaxNode());
            ;
            return declaratorSyntax;
        }

        internal static ExpressionSyntax ArrayCreationExpression(ArrayCreationExpressionAspect arrayCreationExpression)
        {
            ExpressionSyntax expression = null;
            if (arrayCreationExpression.Type != null)
            {
                ArrayTypeSyntax type = null;
                if (arrayCreationExpression.Type is ArrayTypeAspect)
                    type = (ArrayTypeSyntax)arrayCreationExpression.Type.GetSyntaxNode();
                else
                {
                    type = SyntaxFactory.ArrayType((TypeSyntax)arrayCreationExpression.Type.GetSyntaxNode());
                    List<ArrayRankSpecifierSyntax> arrayRankSpecifierSyntaxes = new List<ArrayRankSpecifierSyntax>();
                    if (arrayCreationExpression.Expressions.Count() > 0)
                        arrayRankSpecifierSyntaxes.Add(SyntaxFactory.ArrayRankSpecifier(SeparatedSyntaxList<ExpressionSyntax, ExpressionAspect>(arrayCreationExpression.Expressions)));
                    foreach (var rankSpecifier in arrayCreationExpression.RankSpecifiers)
                        arrayRankSpecifierSyntaxes.Add((ArrayRankSpecifierSyntax)rankSpecifier.GetSyntaxNode());
                    if (arrayRankSpecifierSyntaxes.Any())
                        type = type.WithRankSpecifiers(new SyntaxList<ArrayRankSpecifierSyntax>(arrayRankSpecifierSyntaxes));
                }

                ArrayCreationExpressionSyntax arrayExpression = SyntaxFactory.ArrayCreationExpression(type);
                if (arrayCreationExpression.ArrayInitializer != null)
                    arrayExpression = arrayExpression.WithInitializer((InitializerExpressionSyntax)arrayCreationExpression.ArrayInitializer.GetSyntaxNode());
                expression = arrayExpression;
            }
            else
            {
                ImplicitArrayCreationExpressionSyntax arrayExpression = SyntaxFactory.ImplicitArrayCreationExpression((InitializerExpressionSyntax)arrayCreationExpression.ArrayInitializer.GetSyntaxNode());
                expression = arrayExpression;
            }

            return expression;
        }

        internal static TypeOfExpressionSyntax Typeof(TypeOfExpressionAspect @typeof)
        {
            return SyntaxFactory.TypeOfExpression((TypeSyntax)@typeof.Type.GetSyntaxNode());
        }

        internal static NameSyntax QualifiedUnboundTypeName(QualifiedUnboundTypeNameAspect unboundType)
        {
            NameSyntax nameSyntax = null;
            if (unboundType.Right == null)
            {
                if (unboundType.GenericDimensionSpecifier == null)
                    nameSyntax = (NameSyntax)unboundType.Left.GetSyntaxNode();
                else
                    nameSyntax = GenericName((IdentifierNameAspect)unboundType.Left, unboundType.GenericDimensionSpecifier);
            }
            else
            {
                var leftNameSyntax = (NameSyntax)unboundType.Left.GetSyntaxNode();
                SimpleNameSyntax rightNameSyntax = null;
                if (unboundType.GenericDimensionSpecifier == null)
                    rightNameSyntax = (SimpleNameSyntax)unboundType.Right.GetSyntaxNode();
                else
                    rightNameSyntax = (SimpleNameSyntax)GenericName((IdentifierNameAspect)unboundType.Right, unboundType.GenericDimensionSpecifier);
                nameSyntax = SyntaxFactory.QualifiedName(leftNameSyntax, rightNameSyntax);
            }

            return nameSyntax;
        }

        internal static NameSyntax AliasUnboundTypeName(AliasUnboundTypeNameAspect unboundType)
        {
            var generic = GenericName((IdentifierNameAspect)unboundType.Right, unboundType.GenericDimensionSpecifier);
            return SyntaxFactory.AliasQualifiedName((IdentifierNameSyntax)unboundType.Left.GetSyntaxNode(), (SimpleNameSyntax)generic);
        }

        internal static TypeArgumentListSyntax GenericDimensionSpecifier(GenericDimensionSpecifierAspect genericSpecifier)
        {
            var tokens = new SyntaxNodeOrToken[genericSpecifier.CountCommas() * 2 + 1];
            tokens[0] = SyntaxFactory.OmittedTypeArgument();
            for (int i = 0; i < genericSpecifier.CountCommas(); i++)
            {
                var p = i * 2 + 1;
                tokens[p] = SyntaxFactory.Token(SyntaxKind.CommaToken);
                tokens[p + 1] = SyntaxFactory.OmittedTypeArgument();
            }

            return SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList<TypeSyntax>(tokens));
        }

        internal static CheckedExpressionSyntax CheckedExpression(CheckedExpressionAspect checkedExpression)
        {
            return SyntaxFactory.CheckedExpression(SyntaxKind.CheckedExpression, (ExpressionSyntax)checkedExpression.Expression.GetSyntaxNode());
        }

        internal static CheckedExpressionSyntax UnCheckedExpression(UnCheckedExpressionAspect checkedExpression)
        {
            return SyntaxFactory.CheckedExpression(SyntaxKind.UncheckedExpression, (ExpressionSyntax)checkedExpression.Expression.GetSyntaxNode());
        }

        internal static DefaultExpressionSyntax DefaultValueType(DefaultValueTypeExpressionAspect defaultValueType)
        {
            return SyntaxFactory.DefaultExpression((TypeSyntax)defaultValueType.Type.GetSyntaxNode());
        }

        internal static CastExpressionSyntax CastExpression(CastExpressionAspect castExpression)
        {
            return SyntaxFactory.CastExpression((TypeSyntax)castExpression.Type.GetSyntaxNode(), (ExpressionSyntax)castExpression.Expression.GetSyntaxNode());
        }

        internal static PrefixUnaryExpressionSyntax PreExpression(PreExpressionAspect expression)
        {
            SyntaxKind kind = expression.IncrDecrOperator == IncrDecrOperators.Increment ? SyntaxKind.PreIncrementExpression : SyntaxKind.PreDecrementExpression;
            return SyntaxFactory.PrefixUnaryExpression(kind, (ExpressionSyntax)expression.Expression.GetSyntaxNode());
        }

        internal static PostfixUnaryExpressionSyntax PostExpression(PostExpressionAspect expression)
        {
            SyntaxKind kind = expression.IncrDecrOperator == IncrDecrOperators.Increment ? SyntaxKind.PostIncrementExpression : SyntaxKind.PostDecrementExpression;
            return SyntaxFactory.PostfixUnaryExpression(kind, (ExpressionSyntax)expression.Expression.GetSyntaxNode());
        }

        internal static PrefixUnaryExpressionSyntax UnaryOperationExpression(UnaryOperationExpressionAspect unaryExpression)
        {
            var expression = (ExpressionSyntax)unaryExpression.Expression.GetSyntaxNode();
            return SyntaxFactory.PrefixUnaryExpression(UnaryOperator(unaryExpression.UnaryOperator), expression);
        }

        internal static SyntaxKind UnaryOperator(UnaryOperatorAspect @operator)
        {
            switch (@operator.TokenValue)
            {
                case "+":
                    return SyntaxKind.UnaryPlusExpression;
                case "-":
                    return SyntaxKind.UnaryMinusExpression;
                case "!":
                    return SyntaxKind.LogicalNotExpression;
                case "~":
                    return SyntaxKind.BitwiseNotExpression;
                default:
                    throw new NotImplementedException();
            }
        }

        internal static AssignmentExpressionSyntax AssignmentExpression(AssignmentExpressionAspect assign)
        {
            return SyntaxFactory.AssignmentExpression(AssignmentOperator(assign.Operator), (ExpressionSyntax)assign.Left.GetSyntaxNode(), (ExpressionSyntax)assign.Right.GetSyntaxNode());
        }

        internal static SyntaxKind AssignmentOperator(OperatorAspect @operator)
        {
            switch (@operator.TokenValue)
            {
                case "=":
                    return SyntaxKind.SimpleAssignmentExpression;
                case "+=":
                    return SyntaxKind.AddAssignmentExpression;
                case "-=":
                    return SyntaxKind.SubtractAssignmentExpression;
                case "*=":
                    return SyntaxKind.MultiplyAssignmentExpression;
                case "/=":
                    return SyntaxKind.DivideAssignmentExpression;
                case "%=":
                    return SyntaxKind.ModuloAssignmentExpression;
                case "&=":
                    return SyntaxKind.AndAssignmentExpression;
                case "|=":
                    return SyntaxKind.OrAssignmentExpression;
                case "^=":
                    return SyntaxKind.ExclusiveOrAssignmentExpression;
                case "<<=":
                    return SyntaxKind.LeftShiftAssignmentExpression;
                case ">>=":
                    return SyntaxKind.RightShiftAssignmentExpression;
                default:
                    throw new NotImplementedException();
            }
        }

        internal static LiteralExpressionSyntax LiteralExpression(LiteralExpressionAspect literalExpression)
        {
            return LiteralExpression(literalExpression.LiteralExpressionType, literalExpression.TokenValue);
        }

        internal static LiteralExpressionSyntax LiteralExpression(LiteralExpressionTypes literalExpressionType, string tokenValue)
        {
            switch (literalExpressionType)
            {
                case LiteralExpressionTypes.Boolean:
                    return SyntaxFactory.LiteralExpression(tokenValue == "true" ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression);
                case LiteralExpressionTypes.Decimal:
                    return GetDecimalLiteralExpression(tokenValue);
                case LiteralExpressionTypes.Hexadecimal:
                    return GetHexaLiteralExpression(tokenValue);
                case LiteralExpressionTypes.Real:
                    return GetRealLiteralExpressoin(tokenValue);
                case LiteralExpressionTypes.Character:
                    return SyntaxFactory.LiteralExpression(SyntaxKind.CharacterLiteralExpression, SyntaxFactory.Literal(Char.Parse(tokenValue.Substring(1, tokenValue.Length - 2))));
                case LiteralExpressionTypes.String:
                    string value = null;
                    if (tokenValue.StartsWith("@\""))
                        value = tokenValue.Substring(2, tokenValue.Length - 3);
                    else
                        value = tokenValue.Substring(1, tokenValue.Length - 2);
                    return SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(value));
                case LiteralExpressionTypes.Null:
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression);
                default:
                    throw new NotImplementedException();
            }
        }

        internal static LiteralExpressionSyntax GetDecimalLiteralExpression(string @decimal)
        {
            Regex regex = new Regex("^(?<value>[^(UL)(Ul)(uL)(ul)(LU)(Lu)(lU)(lu)UuLl]*)(?<type>([(UL)(Ul)(uL)(ul)(LU)(Lu)(lU)(lu)UuLl]))?");
            var matches = regex.Match(@decimal).Groups;
            var sValue = matches["value"].ToString();
            var sType = matches["type"].ToString();
            var value = Int64.Parse(sValue);
            switch (sType.ToUpper())
            {
                case "L":
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(long.Parse(sValue)));
                case "U":
                    if (value <= short.MaxValue)
                        return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(ushort.Parse(sValue)));
                    else if (value <= int.MaxValue)
                        return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(uint.Parse(sValue)));
                    else
                        return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(ulong.Parse(sValue)));
                case "UL":
                case "LU":
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(ulong.Parse(sValue)));
                default:
                    if (value <= short.MaxValue)
                        return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(short.Parse(sValue)));
                    else if (value <= int.MaxValue)
                        return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(int.Parse(sValue)));
                    else
                        return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(long.Parse(sValue)));
            }
        }

        internal static LiteralExpressionSyntax GetHexaLiteralExpression(string hexa)
        {
            Regex regex = new Regex("^(?<value>[^(UL)(Ul)(uL)(ul)(LU)(Lu)(lU)(lu)UuLl]*)(?<type>([(UL)(Ul)(uL)(ul)(LU)(Lu)(lU)(lu)UuLl]))?");
            var matches = regex.Match(hexa).Groups;
            var sValue = matches["value"].ToString();
            var sType = matches["type"].ToString();
            var value = (Int64)new System.ComponentModel.Int64Converter().ConvertFromString(sValue);
            return GetDecimalLiteralExpression(value.ToString() + sType);
        }

        internal static LiteralExpressionSyntax GetRealLiteralExpressoin(string hexaValue)
        {
            Regex regex = new Regex(@"^(?<value>([0-9]*(\.[0-9]*)?((e|E)(\-|\+)?([0-9]+))?))(?<type>(F|f|D|d|m|M))?");
            var matches = regex.Match(hexaValue).Groups;
            var value = regex.Match(hexaValue).Groups["value"].ToString();
            var type = regex.Match(hexaValue).Groups["type"].ToString();
            switch (type)
            {
                case "F":
                case "f":
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(float.Parse(value)));
                case "D":
                case "d":
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(double.Parse(value, _CultureInfo)));
                case "M":
                case "m":
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(decimal.Parse(value, _CultureInfo)));
                default:
                    return SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(double.Parse(value, _CultureInfo)));
            }
        }

        internal static TypeSyntax TypeName(TypeNameAspect typeName)
        {
            return (TypeSyntax)typeName.ChildAspectNodes[0].GetSyntaxNode();
        }

        internal static TypeSyntax VoidType(VoidTypeAspect voidType)
        {
            return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
        }

        internal static ArrayTypeSyntax ArrayType(ArrayTypeAspect arrayType)
        {
            return SyntaxFactory.ArrayType((TypeSyntax)arrayType.TypeAspect.GetSyntaxNode(), ArrayRankSpecifierList(arrayType.RankSpecifiers));
        }

        internal static SyntaxList<ArrayRankSpecifierSyntax> ArrayRankSpecifierList(IEnumerable<RankSpecifierAspect> rankSpecifiers)
        {
            return SyntaxList<ArrayRankSpecifierSyntax, RankSpecifierAspect>(rankSpecifiers);
        }

        internal static ArrayRankSpecifierSyntax ArrayRankSpecifier(RankSpecifierAspect rankSpecifier)
        {
            ArrayRankSpecifierSyntax specifier = null;
            switch (rankSpecifier.DimSeperators.Count())
            {
                case 0:
                    specifier = SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(SyntaxFactory.OmittedArraySizeExpression()));
                    break;
                default:
                    OmittedArraySizeExpressionSyntax[] tokens = new OmittedArraySizeExpressionSyntax[rankSpecifier.DimSeperators.Count() + 1];
                    for (int i = 0; i < tokens.Count(); i++)
                        tokens[i] = SyntaxFactory.OmittedArraySizeExpression();
                    specifier = SyntaxFactory.ArrayRankSpecifier(SeparatedSyntaxList<ExpressionSyntax>(tokens));
                    break;
            }

            return specifier;
        }

        internal static InitializerExpressionSyntax ArrayInitializerExpression(ArrayInitializerAspect arrayInitializer)
        {
            return SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression, SeparatedSyntaxList<ExpressionSyntax, VariableInitializerAspect>(arrayInitializer.VariableInitializers));
        }

        internal static ExpressionSyntax VariableInitalizer(VariableInitializerAspect variable)
        {
            return (ExpressionSyntax)variable.Expression.GetSyntaxNode();
        }

        internal static TypeSyntax PredifinedType(PredefinedTypeAspect predefinedType)
        {
            if (predefinedType.Keyword.Keyword == Keywords.DYNAMIC)
                return SyntaxFactory.IdentifierName(predefinedType.TokenValue);
            else
                return SyntaxFactory.PredefinedType(Keyword(predefinedType.Keyword));
        }

        internal static NullableTypeSyntax NullableType(NullableTypeAspect nullableType)
        {
            return SyntaxFactory.NullableType((TypeSyntax)nullableType.Type.GetSyntaxNode());
        }

        internal static BlockSyntax GetPrototypeItemsBlock(IEnumerable<string> prototypeItemsDeclarations)
        {
            var arraySyntax = SyntaxFactory.ArrayCreationExpression(SyntaxFactory.ArrayType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword))).WithRankSpecifiers(SyntaxFactory.SingletonList<ArrayRankSpecifierSyntax>(SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(SyntaxFactory.OmittedArraySizeExpression())))));
            if (prototypeItemsDeclarations.Count() == 0)
                arraySyntax = arraySyntax.WithInitializer(SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression));
            else
                arraySyntax = arraySyntax.WithInitializer(SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression, SyntaxListLiteralExpression(prototypeItemsDeclarations)));
            SyntaxFactory.ArrayCreationExpression(SyntaxFactory.ArrayType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword))).WithRankSpecifiers(SyntaxFactory.SingletonList<ArrayRankSpecifierSyntax>(SyntaxFactory.ArrayRankSpecifier(SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(SyntaxFactory.OmittedArraySizeExpression()))))).WithInitializer(SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression));
            return SyntaxFactory.Block(SyntaxFactory.SingletonList<StatementSyntax>(SyntaxFactory.ReturnStatement((arraySyntax))));
        }

        internal static IdentifierNameSyntax AnonymousTypeAspect(AnonymousTypeAspect anonymousType)
        {
            return SyntaxFactory.IdentifierName("var");
        }

        internal static QueryExpressionSyntax QueryExpression(QueryExpressionAspect queryExpression)
        {
            return SyntaxFactory.QueryExpression((FromClauseSyntax)queryExpression.FromClause.GetSyntaxNode(), (QueryBodySyntax)queryExpression.QueryBody.GetSyntaxNode());
        }

        internal static QueryBodySyntax QueryBody(QueryBodyAspect queryBody)
        {
            var body = SyntaxFactory.QueryBody((SelectOrGroupClauseSyntax)queryBody.SelectOrGroupClause.GetSyntaxNode());
            if (queryBody.QueryBodyClauses.Any())
                body = body.WithClauses(SyntaxList<QueryClauseSyntax, QueryBodyClauseAspect>(queryBody.QueryBodyClauses));
            if (queryBody.QueryContinuation != null)
                body = body.WithContinuation((QueryContinuationSyntax)queryBody.QueryContinuation.GetSyntaxNode());
            return body;
        }

        internal static SelectClauseSyntax SelectClause(SelectClauseAspect selectClause)
        {
            return SyntaxFactory.SelectClause((ExpressionSyntax)selectClause.Expression.GetSyntaxNode());
        }

        internal static GroupClauseSyntax GroupClause(GroupClauseAspect groupClause)
        {
            return SyntaxFactory.GroupClause((ExpressionSyntax)groupClause.GroupExpression.GetSyntaxNode(), (ExpressionSyntax)groupClause.ByExpression.GetSyntaxNode());
        }

        internal static QueryContinuationSyntax QueryContinuation(QueryContinuationAspect queryContinuation)
        {
            return SyntaxFactory.QueryContinuation(queryContinuation.Identifier.TokenValue, (QueryBodySyntax)queryContinuation.QueryBody.GetSyntaxNode());
        }

        internal static FromClauseSyntax FromClause(FromClauseAspect fromClause)
        {
            var clause = SyntaxFactory.FromClause(Identifier(fromClause.Identifier), (ExpressionSyntax)fromClause.InExpression.GetSyntaxNode());
            if (fromClause.Type != null)
                clause = clause.WithType((TypeSyntax)fromClause.Type.GetSyntaxNode());
            return clause;
        }

        internal static LetClauseSyntax LetClause(LetClauseAspect letClause)
        {
            return SyntaxFactory.LetClause(Identifier(letClause.Identifier), (ExpressionSyntax)letClause.Expression.GetSyntaxNode());
        }

        internal static WhereClauseSyntax WhereClause(WhereClauseAspect whereClause)
        {
            return SyntaxFactory.WhereClause((ExpressionSyntax)whereClause.Expression.GetSyntaxNode());
        }

        internal static JoinClauseSyntax JoinClause(JoinClauseAspect joinClause)
        {
            var clause = SyntaxFactory.JoinClause(Identifier(joinClause.Identifier), (ExpressionSyntax)joinClause.InExpression.GetSyntaxNode(), (ExpressionSyntax)joinClause.LeftExpression.GetSyntaxNode(), (ExpressionSyntax)joinClause.RightExpression.GetSyntaxNode());
            if (joinClause.Type != null)
                clause = clause.WithType((TypeSyntax)joinClause.Type.GetSyntaxNode());
            return clause;
        }

        internal static JoinClauseSyntax JoinIntoClause(JoinIntoClauseAspect joinIntoClause)
        {
            var clause = SyntaxFactory.JoinClause(Identifier(joinIntoClause.Identifier), (ExpressionSyntax)joinIntoClause.InExpression.GetSyntaxNode(), (ExpressionSyntax)joinIntoClause.LeftExpression.GetSyntaxNode(), (ExpressionSyntax)joinIntoClause.RightExpression.GetSyntaxNode()).WithInto(SyntaxFactory.JoinIntoClause(joinIntoClause.IntoIdentifier.TokenValue));
            if (joinIntoClause.Type != null)
                clause = clause.WithType((TypeSyntax)joinIntoClause.Type.GetSyntaxNode());
            return clause;
        }

        internal static OrderByClauseSyntax OrderByClause(OrderByClauseAspect orderByClause)
        {
            return SyntaxFactory.OrderByClause(SeparatedSyntaxList<OrderingSyntax, OrderingAspect>(orderByClause.Orderings));
        }

        internal static OrderingSyntax Ordering(OrderingAspect ordering)
        {
            var direction = SyntaxFactory.Token(SyntaxKind.None);
            if (ordering.Direction != null)
                direction = (SyntaxToken)ordering.Direction.GetSyntaxNode();
            var kind = SyntaxKind.AscendingOrdering;
            if (direction.Kind() == SyntaxKind.DescendingKeyword)
                kind = SyntaxKind.DescendingOrdering;
            var value = SyntaxFactory.Ordering(kind, (ExpressionSyntax)ordering.Expression.GetSyntaxNode());
            if (direction.Kind() != SyntaxKind.None)
                value = value.WithAscendingOrDescendingKeyword(direction);
            return value;
        }

        internal static SyntaxToken OrderingDirection(OrderingDirectionAspect direction)
        {
            switch (direction.OrderingDifrection)
            {
                case OrderingDirections.ASCENDING:
                    return SyntaxFactory.Token(SyntaxKind.AscendingKeyword);
                case OrderingDirections.DESCENDING:
                    return SyntaxFactory.Token(SyntaxKind.DescendingKeyword);
                case OrderingDirections.NONE:
                default:
                    throw new NotImplementedException();
            }
        }

        internal static LocalDeclarationStatementSyntax LocalVariableDeclaration(LocalVariableDeclarationAspect declaration)
        {
            return SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration((TypeSyntax)declaration.TypeOrIdenfifier.GetSyntaxNode(), SeparatedSyntaxList<VariableDeclaratorSyntax, LocalVariableDeclaratorAspect>(declaration.VariablesDeclarators)));
        }

        internal static VariableDeclarationSyntax VariableDeclaration(VariableDeclarationAspect declaration)
        {
            return SyntaxFactory.VariableDeclaration((TypeSyntax)declaration.TypeOrIdenfifier.GetSyntaxNode(), SeparatedSyntaxList<VariableDeclaratorSyntax, LocalVariableDeclaratorAspect>(declaration.VariablesDeclarators));
        }

        internal static VariableDeclaratorSyntax VariableDeclarator(AbstractVariableDeclaratorAspect variableDeclarator)
        {
            var syntax = SyntaxFactory.VariableDeclarator(variableDeclarator.Identifier.TokenValue);
            if (variableDeclarator.VariableInitializer != null)
                syntax = syntax.WithInitializer(SyntaxFactory.EqualsValueClause((ExpressionSyntax)variableDeclarator.VariableInitializer.GetSyntaxNode()));
            return syntax;
        }

        internal static LocalDeclarationStatementSyntax LocalConstantDeclaration(LocalConstantDeclarationAspect declaration)
        {
            return SyntaxFactory.LocalDeclarationStatement(SyntaxFactory.VariableDeclaration((TypeSyntax)declaration.TypeOrIdenfifier.GetSyntaxNode(), SeparatedSyntaxList<VariableDeclaratorSyntax, ConstantDeclaratorAspect>(declaration.ConstantDeclarators))).WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ConstKeyword)));
        }

        internal static VariableDeclaratorSyntax ConstantDeclarator(ConstantDeclaratorAspect constant)
        {
            var syntax = SyntaxFactory.VariableDeclarator(constant.Identifier.TokenValue);
            if (constant.Expression != null)
                syntax = syntax.WithInitializer(SyntaxFactory.EqualsValueClause((ExpressionSyntax)constant.Expression.GetSyntaxNode()));
            return syntax;
        }

        internal static LabeledStatementSyntax LabelStatement(LabeledStatementAspect label)
        {
            return SyntaxFactory.LabeledStatement(label.Identifier.TokenValue, (StatementSyntax)label.Statement.GetSyntaxNode());
        }

        internal static BlockSyntax Block(BlockAspect block)
        {
            return SyntaxFactory.Block(SyntaxList<StatementSyntax, StatementAspect>(block.Statements));
        }

        internal static ExpressionStatementSyntax ExpressionStatement(ExpressionStatementAspect statement)
        {
            return SyntaxFactory.ExpressionStatement((ExpressionSyntax)statement.Expression.GetSyntaxNode());
        }

        internal static IfStatementSyntax IfStatement(IfStatementAspect statement)
        {
            var @if = SyntaxFactory.IfStatement((ExpressionSyntax)statement.Condition.GetSyntaxNode(), (StatementSyntax)statement.Then.GetSyntaxNode());
            if (statement.Else != null)
                @if = @if.WithElse(SyntaxFactory.ElseClause((StatementSyntax)statement.Else.GetSyntaxNode()));
            return @if;
        }

        internal static SwitchStatementSyntax SwitchStatement(SwitchStatementAspect switchStatement)
        {
            var @switch = SyntaxFactory.SwitchStatement((ExpressionSyntax)switchStatement.Expression.GetSyntaxNode()).WithOpenParenToken(SyntaxFactory.Token(SyntaxKind.OpenParenToken)).WithCloseParenToken(SyntaxFactory.Token(SyntaxKind.CloseParenToken));
            if (switchStatement.Sections.Any())
                @switch = @switch.WithSections(SyntaxList<SwitchSectionSyntax, SwitchSectionAspect>(switchStatement.Sections));
            return @switch;
        }

        internal static SwitchSectionSyntax SwitchSection(SwitchSectionAspect switchSection)
        {
            var labels = SyntaxList<SwitchLabelSyntax, SwitchLabelAspect>(switchSection.Labels);
            var statements = SyntaxList<StatementSyntax, StatementAspect>(switchSection.Statements);
            return SyntaxFactory.SwitchSection(labels, statements);
        }

        internal static SwitchLabelSyntax SwitchLabel(SwitchLabelAspect switchLabel)
        {
            if (switchLabel.Expression == null)
                return SyntaxFactory.DefaultSwitchLabel();
            else
                return SyntaxFactory.CaseSwitchLabel((ExpressionSyntax)switchLabel.Expression.GetSyntaxNode());
        }

        internal static WhileStatementSyntax WhileStatement(WhileStatementAspect whileStatement)
        {
            return SyntaxFactory.WhileStatement((ExpressionSyntax)whileStatement.Conditiion.GetSyntaxNode(), (StatementSyntax)whileStatement.Statement.GetSyntaxNode());
        }

        internal static DoStatementSyntax DoStatement(DoStatementAspect doStatement)
        {
            return SyntaxFactory.DoStatement((StatementSyntax)doStatement.Statement.GetSyntaxNode(), (ExpressionSyntax)doStatement.Conditiion.GetSyntaxNode());
        }

        internal static ForStatementSyntax ForStatement(ForStatementAspect forStatement)
        {
            var @for = SyntaxFactory.ForStatement((StatementSyntax)forStatement.Statement.GetSyntaxNode());
            if (forStatement.ForInitializer != null)
            {
                if (forStatement.ForInitializer.Expressions.Count() == 1 && forStatement.ForInitializer.Expressions.First() is VariableDeclarationAspect)
                {
                    @for = @for.WithDeclaration((VariableDeclarationSyntax)forStatement.ForInitializer.Expressions.First().GetSyntaxNode());
                }
                else
                    @for = @for.WithInitializers(SeparatedSyntaxList<ExpressionSyntax, CSAspectNode>(forStatement.ForInitializer.Expressions));
            }

            if (forStatement.ForCondition != null)
                @for = @for.WithCondition((ExpressionSyntax)forStatement.ForCondition.GetSyntaxNode());
            if (forStatement.ForIterator != null)
            {
                @for = @for.WithIncrementors(SeparatedSyntaxList<ExpressionSyntax, ExpressionAspect>(forStatement.ForIterator.Expressions));
            }

            return @for;
        }

        internal static ExpressionSyntax ForCondition(ForConditionAspect condition)
        {
            return (ExpressionSyntax)condition.Expression.GetSyntaxNode();
        }

        internal static ForEachStatementSyntax ForEachStatement(ForeachStatementAspect foreachStatement)
        {
            return SyntaxFactory.ForEachStatement((TypeSyntax)foreachStatement.TypeOrIdenfifier.GetSyntaxNode(), foreachStatement.Identifier.TokenValue, (ExpressionSyntax)foreachStatement.Expression.GetSyntaxNode(), (StatementSyntax)foreachStatement.Statement.GetSyntaxNode());
        }

        internal static BreakStatementSyntax BreakStatement(BreakStatementAspect breakStatement)
        {
            return SyntaxFactory.BreakStatement();
        }

        internal static ContinueStatementSyntax ContinueStatement(ContinueStatementAspect continueStatement)
        {
            return SyntaxFactory.ContinueStatement();
        }

        internal static GotoStatementSyntax SimpleGotoStatement(SimpleGotoStatementAspect simpleGoto)
        {
            return SyntaxFactory.GotoStatement(SyntaxKind.GotoStatement, (IdentifierNameSyntax)simpleGoto.Identifier.GetSyntaxNode());
        }

        internal static GotoStatementSyntax SwitchGotoStatement(SwitchGotoStatementAspect switchGoto)
        {
            if (switchGoto.Expression != null)
                return SyntaxFactory.GotoStatement(SyntaxKind.GotoCaseStatement, (ExpressionSyntax)switchGoto.Expression.GetSyntaxNode()).WithCaseOrDefaultKeyword(SyntaxFactory.Token(SyntaxKind.CaseKeyword));
            else
                return SyntaxFactory.GotoStatement(SyntaxKind.GotoDefaultStatement).WithCaseOrDefaultKeyword(SyntaxFactory.Token(SyntaxKind.DefaultKeyword));
        }

        internal static ReturnStatementSyntax ReturnStatement(ReturnStatementAspect returnStatement)
        {
            var @return = SyntaxFactory.ReturnStatement();
            if (returnStatement.Expression != null)
                @return = @return.WithExpression((ExpressionSyntax)returnStatement.Expression.GetSyntaxNode());
            return @return;
        }

        internal static ThrowStatementSyntax ThrowStatement(ThrowStatementAspect throwStatement)
        {
            var @throw = SyntaxFactory.ThrowStatement();
            if (throwStatement.Expression != null)
                @throw = @throw.WithExpression((ExpressionSyntax)throwStatement.Expression.GetSyntaxNode());
            return @throw;
        }

        internal static TryStatementSyntax TryStatement(TryStatementAspect tryStatement)
        {
            var @try = SyntaxFactory.TryStatement(SyntaxList<CatchClauseSyntax, CatchClauseAspect>(tryStatement.CatchClausesAspects));
            if (tryStatement.FinallyClause != null)
                @try = @try.WithFinally((FinallyClauseSyntax)tryStatement.FinallyClause.GetSyntaxNode());
            if (tryStatement.Block != null)
                @try = @try.WithBlock((BlockSyntax)tryStatement.Block.GetSyntaxNode());
            return @try;
        }

        internal static CatchClauseSyntax CatchClause(CatchClauseAspect catchClause)
        {
            var catchClauseDeclaration = SyntaxFactory.CatchClause();
            if (catchClause.Type != null)
            {
                var @catch = SyntaxFactory.CatchDeclaration((TypeSyntax)catchClause.Type.GetSyntaxNode());
                if (catchClause.Identifier != null)
                    @catch = @catch.WithIdentifier(SyntaxFactory.Identifier(catchClause.Identifier.TokenValue));
                catchClauseDeclaration = catchClauseDeclaration.WithDeclaration(@catch);
            }

            if (catchClause.Block != null)
                catchClauseDeclaration = catchClauseDeclaration.WithBlock((BlockSyntax)catchClause.Block.GetSyntaxNode());
            return catchClauseDeclaration;
        }

        internal static FinallyClauseSyntax FinallyClause(FinallyClauseAspect finallyClause)
        {
            return SyntaxFactory.FinallyClause((BlockSyntax)finallyClause.Block.GetSyntaxNode());
        }

        internal static CheckedStatementSyntax CheckedStatement(CheckedStatementAspect checkedStatement)
        {
            return SyntaxFactory.CheckedStatement(SyntaxKind.CheckedStatement, (BlockSyntax)checkedStatement.Block.GetSyntaxNode());
        }

        internal static CheckedStatementSyntax UnCheckedStatement(UnCheckedStatementAspect unCheckedStatement)
        {
            return SyntaxFactory.CheckedStatement(SyntaxKind.UncheckedStatement, (BlockSyntax)unCheckedStatement.Block.GetSyntaxNode());
        }

        internal static LockStatementSyntax LockStatement(LockStatementAspect lockStatement)
        {
            return SyntaxFactory.LockStatement((ExpressionSyntax)lockStatement.Expression.GetSyntaxNode(), (StatementSyntax)lockStatement.Statement.GetSyntaxNode());
        }

        internal static UsingStatementSyntax UsingStatement(UsingStatementAspect usingStatement)
        {
            var @using = SyntaxFactory.UsingStatement((StatementSyntax)usingStatement.Statement.GetSyntaxNode());
            switch (usingStatement.ResourceAcquisition)
            {
                case LocalVariableDeclarationAspect localVariableDeclaration:
                    @using = @using.WithDeclaration(((LocalDeclarationStatementSyntax)localVariableDeclaration.GetSyntaxNode()).Declaration);
                    break;
                case VariableDeclarationAspect variableDeclaration:
                    @using = @using.WithDeclaration((VariableDeclarationSyntax)variableDeclaration.GetSyntaxNode());
                    break;
                case ExpressionAspect expression:
                    @using = @using.WithExpression((ExpressionSyntax)expression.GetSyntaxNode());
                    break;
            }

            return @using;
        }

        internal static YieldStatementSyntax YieldStatement(YieldStatementAspect yieldStatement)
        {
            if (yieldStatement.Expression != null)
                return SyntaxFactory.YieldStatement(SyntaxKind.YieldReturnStatement, (ExpressionSyntax)yieldStatement.Expression.GetSyntaxNode());
            else
                return SyntaxFactory.YieldStatement(SyntaxKind.YieldBreakStatement);
        }

        internal static EmptyStatementSyntax EmptyStatement(EmptyStatementAspect emptyStatement)
        {
            return SyntaxFactory.EmptyStatement();
        }

        internal static AttributeListSyntax AttributeSection(AttributeSectionAspect attributeSection)
        {
            var attributeList = SyntaxFactory.AttributeList(SeparatedSyntaxList<AttributeSyntax, AttributeAspect>(attributeSection.Attributes));
            if (attributeSection.TargetSpecifier != null)
                attributeList = attributeList.WithTarget((AttributeTargetSpecifierSyntax)attributeSection.TargetSpecifier.GetSyntaxNode());
            return attributeList;
        }

        internal static AttributeTargetSpecifierSyntax AttributeTargetSpecifier(AttributeTargetSpecifierAspect attributeTargetSpecifier)
        {
            SyntaxKind? target = null;
            switch (attributeTargetSpecifier.TokenValue)
            {
                case "field":
                    target = SyntaxKind.FieldKeyword;
                    break;
                case "event":
                    target = SyntaxKind.EventKeyword;
                    break;
                case "method":
                    target = SyntaxKind.MethodKeyword;
                    break;
                case "param":
                    target = SyntaxKind.ParamKeyword;
                    break;
                case "property":
                    target = SyntaxKind.PropertyKeyword;
                    break;
                case "return":
                    target = SyntaxKind.ReturnKeyword;
                    break;
                case "type":
                    target = SyntaxKind.TypeKeyword;
                    break;
                default:
                    throw new NotImplementedException();
            }

            return SyntaxFactory.AttributeTargetSpecifier(SyntaxFactory.Token((SyntaxKind)target));
        }

        internal static AttributeSyntax Attribute(AttributeAspect attribute)
        {
            var syntax = SyntaxFactory.Attribute((NameSyntax)attribute.Type.GetSyntaxNode());
            if (attribute.Arguments != null)
                syntax = syntax.WithArgumentList((AttributeArgumentListSyntax)attribute.Arguments.GetSyntaxNode());
            return syntax;
        }

        internal static AttributeArgumentListSyntax AttributeArgumentList(AttributeArgumentListAspect argumentList)
        {
            var syntax = SyntaxFactory.AttributeArgumentList(SeparatedSyntaxList<AttributeArgumentSyntax, AttributeArgumentAspect>(argumentList.AttributeArguments));
            return syntax;
        }

        internal static AttributeArgumentSyntax PositionalAttributeArgument(AttributeArgumentPositionalAspect positional)
        {
            var syntax = SyntaxFactory.AttributeArgument((ExpressionSyntax)positional.Expression.GetSyntaxNode());
            if (positional.IdentifierName != null)
                syntax = syntax.WithNameColon(SyntaxFactory.NameColon((IdentifierNameSyntax)positional.IdentifierName.GetSyntaxNode()));
            return syntax;
        }

        internal static AttributeArgumentSyntax NamedAttributeArgument(AttributeArgumentNamedAspect named)
        {
            return SyntaxFactory.AttributeArgument((ExpressionSyntax)named.Expression.GetSyntaxNode()).WithNameEquals(SyntaxFactory.NameEquals((IdentifierNameSyntax)named.IdentifierName.GetSyntaxNode()));
        }

        internal static ClassDeclarationSyntax ClassDeclaration(ClassDeclarationAspect classDeclaration)
        {
            var @class = SyntaxFactory.ClassDeclaration(Identifier(classDeclaration.IdentifierName)).WithMembers(SyntaxList<MemberDeclarationSyntax, TypeMemberDeclarationAspect>(classDeclaration.Members));
            if (classDeclaration.AttributeSections.Any())
                @class = @class.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(classDeclaration.AttributeSections));
            if (classDeclaration.ClassModifiers.Any())
                @class = @class.WithModifiers(SyntaxTokenList<ModifierAspect>(classDeclaration.ClassModifiers));
            if (classDeclaration.TypeParameterList != null)
                @class = @class.WithTypeParameterList((TypeParameterListSyntax)classDeclaration.TypeParameterList.GetSyntaxNode());
            if (classDeclaration.BaseList != null)
                @class = @class.WithBaseList((BaseListSyntax)classDeclaration.BaseList.GetSyntaxNode());
            if (classDeclaration.TypeParameterConstraintsClauses.Any())
                @class = @class.WithConstraintClauses(SyntaxList<TypeParameterConstraintClauseSyntax, TypeParameterConstraintsClauseAspect>(classDeclaration.TypeParameterConstraintsClauses));
            return @class;
        }

        internal static SyntaxToken Modifier(ModifierAspect modifier)
        {
            SyntaxKind? kind = null;
            switch (modifier.TokenValue)
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

            return SyntaxFactory.Token((SyntaxKind)kind);
        }

        internal static TypeParameterListSyntax TypeParameterList(TypeParameterListAspect typeParameterList)
        {
            return SyntaxFactory.TypeParameterList(SeparatedSyntaxList<TypeParameterSyntax, TypeParameterItemAspect>(typeParameterList.TypeParameters));
        }

        internal static TypeParameterListSyntax TypeParameterList(IEnumerable<string> typeParameterNames)
        {
            var list = new SeparatedSyntaxList<TypeParameterSyntax>().AddRange(typeParameterNames.Select(t => SyntaxFactory.TypeParameter(t)));
            return SyntaxFactory.TypeParameterList(list);
        }

        internal static TypeParameterSyntax TypeParameterItem(TypeParameterItemAspect parameterItem)
        {
            var typeParameter = SyntaxFactory.TypeParameter(Identifier(parameterItem.IdentifierName));
            if (parameterItem.AttributeSections.Any())
                typeParameter.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(parameterItem.AttributeSections));
            return typeParameter;
        }

        internal static BaseListSyntax BaseList(BaseListAspect classBase)
        {
            return SyntaxFactory.BaseList(SeparatedSyntaxList<BaseTypeSyntax, BaseTypeAspect>(classBase.BaseTypes));
        }

        internal static TypeParameterSyntax TypeParameter(IdentifierNameAspect parameter)
        {
            return SyntaxFactory.TypeParameter(Identifier(parameter));
        }

        internal static BaseTypeSyntax BaseType(BaseTypeAspect baseType)
        {
            var baseTypeSyntax = SyntaxFactory.SimpleBaseType((TypeSyntax)baseType.Type.GetSyntaxNode());
            return baseTypeSyntax;
        }

        internal static TypeParameterConstraintClauseSyntax TypeParameterConstraintsClause(TypeParameterConstraintsClauseAspect constraint)
        {
            return SyntaxFactory.TypeParameterConstraintClause((IdentifierNameSyntax)constraint.Identifier.GetSyntaxNode(), SeparatedSyntaxList<TypeParameterConstraintSyntax, TypeParameterConstraintAspect>(constraint.TypeParameterConstraints));
        }

        internal static TypeParameterConstraintSyntax TypeParameterConstraintContsructor(TypeParameterConstraintConstructutorAspect constructor)
        {
            return SyntaxFactory.ConstructorConstraint();
        }

        internal static TypeParameterConstraintSyntax TypeParameterConstraintClassOrStruct(TypeParameterConstraintClassOrStructAspect constraint)
        {
            SyntaxKind syntaxKind = SyntaxKind.None;
            switch (constraint.KeyWord.Keyword)
            {
                case Keywords.CLASS:
                    syntaxKind = SyntaxKind.ClassConstraint;
                    break;
                case Keywords.STRUCT:
                    syntaxKind = SyntaxKind.StructConstraint;
                    break;
                default:
                    throw new NotImplementedException();
            }

            return SyntaxFactory.ClassOrStructConstraint(syntaxKind);
        }

        internal static TypeParameterConstraintSyntax TypeParameterConstraintType(TypeParameterConstraintTypeAspect constraint)
        {
            return SyntaxFactory.TypeConstraint((TypeSyntax)constraint.Type.GetSyntaxNode());
        }

        internal static FieldDeclarationSyntax ConstantDeclaration(ConstantDeclarationAspect constantDeclaration)
        {
            var modifiers = SyntaxTokenList<ModifierAspect>(constantDeclaration.Modifiers);
            modifiers = modifiers.Add(SyntaxFactory.Token(SyntaxKind.ConstKeyword));
            var constant = SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration((TypeSyntax)constantDeclaration.Type.GetSyntaxNode()).WithVariables(SeparatedSyntaxList<VariableDeclaratorSyntax, ConstantDeclaratorAspect>(constantDeclaration.Declarators))).WithModifiers(modifiers);
            if (constantDeclaration.AttributeSections.Any())
                constant = constant.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(constantDeclaration.AttributeSections));
            return constant;
        }

        internal static FieldDeclarationSyntax FieldDeclaration(FieldDeclarationAspect fieldDeclaration)
        {
            var field = SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration((TypeSyntax)fieldDeclaration.Type.GetSyntaxNode()).WithVariables(SeparatedSyntaxList<VariableDeclaratorSyntax, VariableDeclaratorAspect>(fieldDeclaration.Declarators))).WithModifiers(SyntaxTokenList<ModifierAspect>(fieldDeclaration.Modifiers));
            if (fieldDeclaration.AttributeSections.Any())
                field = field.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(fieldDeclaration.AttributeSections));
            return field;
        }

        internal static ConstructorDeclarationSyntax ConstructorDeclaration(ConstructorDeclarationAspect constructorDeclaration)
        {
            var constructor = SyntaxFactory.ConstructorDeclaration(Identifier(constructorDeclaration.Identifier));
            if (constructorDeclaration.AttributeSections.Any())
                constructor = constructor.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(constructorDeclaration.AttributeSections));
            if (constructorDeclaration.Modifiers.Any())
                constructor = constructor.WithModifiers(SyntaxTokenList<ModifierAspect>(constructorDeclaration.Modifiers));
            if (constructorDeclaration.ParameterList != null)
                constructor = constructor.WithParameterList((ParameterListSyntax)(constructorDeclaration.ParameterList.GetSyntaxNode()));
            if (constructorDeclaration.ConstructorInitializer != null)
                constructor = constructor.WithInitializer(((ConstructorInitializerSyntax)constructorDeclaration.ConstructorInitializer.GetSyntaxNode()));
            if (constructorDeclaration.Block != null)
                constructor = constructor.WithBody(((BlockSyntax)constructorDeclaration.Block.GetSyntaxNode()));
            return constructor;
        }

        internal static ParameterListSyntax FormalParameterList(FormalParameterListAspect parameeters)
        {
            return SyntaxFactory.ParameterList(SeparatedSyntaxList<ParameterSyntax, ParameterAspect>(parameeters.Parameters));
        }

        internal static ParameterSyntax FixedParameter(FixedParameterAspect parameter)
        {
            var param = SyntaxFactory.Parameter(Identifier(parameter.Identifier)).WithType((TypeSyntax)parameter.Type.GetSyntaxNode());
            if (parameter.AttributeSections.Any())
                param = param.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(parameter.AttributeSections));
            if (parameter.Modifiers.Any())
                param = param.WithModifiers(SyntaxTokenList<ParameterModifierAspect>(parameter.Modifiers));
            if (parameter.DefaultArgument != null)
                param = param.WithDefault(SyntaxFactory.EqualsValueClause((ExpressionSyntax)parameter.DefaultArgument.GetSyntaxNode()));
            return param;
        }

        internal static ParameterSyntax ArrayParameter(ParameterArrayAspect arrayParameter)
        {
            var param = SyntaxFactory.Parameter(Identifier(arrayParameter.Identifier)).WithType((TypeSyntax)arrayParameter.ArrayType.GetSyntaxNode()).WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ParamsKeyword)));
            if (arrayParameter.AttributeSections.Any())
                param = param.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(arrayParameter.AttributeSections));
            return param;
        }

        internal static SyntaxToken ParameterModifier(ParameterModifierAspect modifier)
        {
            switch (modifier.TokenValue)
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

        internal static ConstructorInitializerSyntax ConstructorInitializer(ConstructorInitializerAspect constructorInitializer)
        {
            return SyntaxFactory.ConstructorInitializer(ConstructorInitializerModifier(constructorInitializer.InitializerModifier), (ArgumentListSyntax)constructorInitializer.ArgumentList.GetSyntaxNode());
        }

        internal static SyntaxKind ConstructorInitializerModifier(ConstructorInitializerModifierAspect constructorInitializer)
        {
            switch (constructorInitializer.TokenValue)
            {
                case "base":
                    return SyntaxKind.BaseConstructorInitializer;
                case "this":
                    return SyntaxKind.ThisConstructorInitializer;
                default:
                    throw new NotImplementedException();
            }
        }

        internal static ConstructorDeclarationSyntax StaticConstructorDeclaration(StaticConstructorDeclarationAspect constructorDeclaration)
        {
            var constructor = SyntaxFactory.ConstructorDeclaration(Identifier(constructorDeclaration.Identifier));
            if (constructorDeclaration.AttributeSections.Any())
                constructor = constructor.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(constructorDeclaration.AttributeSections));
            if (constructorDeclaration.Modifiers.Any())
                constructor = constructor.WithModifiers(SyntaxTokenList<ModifierAspect>(constructorDeclaration.Modifiers));
            if (constructorDeclaration.Block != null)
                constructor = constructor.WithBody(((BlockSyntax)constructorDeclaration.Block.GetSyntaxNode()));
            return constructor;
        }

        internal static DelegateDeclarationSyntax DelegateDeclaration(DelegateDeclarationAspect delegateDeclaration)
        {
            var @delegate = SyntaxFactory.DelegateDeclaration((TypeSyntax)delegateDeclaration.ReturnType.GetSyntaxNode(), delegateDeclaration.Identifier.TokenValue);
            if (delegateDeclaration.AttributeSections.Any())
                @delegate = @delegate.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(delegateDeclaration.AttributeSections));
            if (delegateDeclaration.Modifiers.Any())
                @delegate = @delegate.WithModifiers(SyntaxTokenList<ModifierAspect>(delegateDeclaration.Modifiers));
            if (delegateDeclaration.VariantParameterList != null)
                @delegate = @delegate.WithTypeParameterList((TypeParameterListSyntax)(delegateDeclaration.VariantParameterList.GetSyntaxNode()));
            if (delegateDeclaration.FormalParameterList != null)
                @delegate = @delegate.WithParameterList((ParameterListSyntax)(delegateDeclaration.FormalParameterList.GetSyntaxNode()));
            if (delegateDeclaration.ConstrainstsClauses != null)
                @delegate = @delegate.WithConstraintClauses(SyntaxList<TypeParameterConstraintClauseSyntax, TypeParameterConstraintsClauseAspect>(delegateDeclaration.ConstrainstsClauses));
            return @delegate;
        }

        internal static TypeParameterListSyntax VariantTypeParameterList(VariantTypeParameterListAspect variantTypeParameterList)
        {
            return SyntaxFactory.TypeParameterList(SeparatedSyntaxList<TypeParameterSyntax, VariantTypeParameterAspect>(variantTypeParameterList.VariantTypeParameters));
        }

        internal static TypeParameterSyntax VariantTypeParameter(VariantTypeParameterAspect variantTypeParameter)
        {
            var typeParam = SyntaxFactory.TypeParameter(variantTypeParameter.Identifier.TokenValue);
            if (variantTypeParameter.AttributeSections.Any())
                typeParam = typeParam.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(variantTypeParameter.AttributeSections));
            if (variantTypeParameter.VarianceAnnotation != null)
                typeParam = typeParam.WithVarianceKeyword((SyntaxToken)variantTypeParameter.VarianceAnnotation.GetSyntaxNode());
            return typeParam;
        }

        internal static SyntaxToken VarianceAnnotation(VarianceAnnotationAspect varianceAnnotation)
        {
            switch (varianceAnnotation.TokenValue)
            {
                case "in":
                    return SyntaxFactory.Token(SyntaxKind.InKeyword);
                case "out":
                    return SyntaxFactory.Token(SyntaxKind.OutKeyword);
                default:
                    throw new NotImplementedException();
            }
        }

        internal static DestructorDeclarationSyntax DestructorDeclaration(DestructorDeclarationAspect destructorDeclaration)
        {
            var destructor = SyntaxFactory.DestructorDeclaration(Identifier(destructorDeclaration.Identifier));
            if (destructorDeclaration.AttributeSections.Any())
                destructor = destructor.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(destructorDeclaration.AttributeSections));
            if (destructorDeclaration.Modifiers.Any())
                destructor = destructor.WithModifiers(SyntaxTokenList<ModifierAspect>(destructorDeclaration.Modifiers));
            if (destructorDeclaration.Block != null)
                destructor = destructor.WithBody(((BlockSyntax)destructorDeclaration.Block.GetSyntaxNode()));
            return destructor;
        }

        internal static EnumDeclarationSyntax EnumDeclaration(EnumDeclarationAspect enumDeclaration)
        {
            var @enum = SyntaxFactory.EnumDeclaration(Identifier(enumDeclaration.Identifier)).WithMembers(SeparatedSyntaxList<EnumMemberDeclarationSyntax, EnumMemberDeclarationApsect>(enumDeclaration.EnumMembers));
            if (enumDeclaration.AttributeSections.Any())
                @enum = @enum.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(enumDeclaration.AttributeSections));
            if (enumDeclaration.Modifiers.Any())
                @enum = @enum.WithModifiers(SyntaxTokenList<ModifierAspect>(enumDeclaration.Modifiers));
            if (enumDeclaration.EnumBase != null)
                @enum = @enum.WithBaseList((BaseListSyntax)enumDeclaration.EnumBase.GetSyntaxNode());
            return @enum;
        }

        internal static BaseListSyntax EnumBase(EnumBaseAspect enumBase)
        {
            return SyntaxFactory.BaseList(SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(SyntaxFactory.SimpleBaseType((PredefinedTypeSyntax)enumBase.Type.GetSyntaxNode())));
        }

        internal static EnumMemberDeclarationSyntax EnumMemberDeclaration(EnumMemberDeclarationApsect enumMember)
        {
            var enumMemberSyntax = SyntaxFactory.EnumMemberDeclaration(Identifier(enumMember.Identifier));
            if (enumMember.AttributeSections.Any())
                enumMemberSyntax = enumMemberSyntax.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(enumMember.AttributeSections));
            if (enumMember.Expression != null)
                enumMemberSyntax = enumMemberSyntax.WithEqualsValue(SyntaxFactory.EqualsValueClause((ExpressionSyntax)enumMember.Expression.GetSyntaxNode()));
            return enumMemberSyntax;
        }

        internal static EventFieldDeclarationSyntax EventFieldDeclaration(EventFieldDeclarationAspect eventDeclaration)
        {
            var @event = SyntaxFactory.EventFieldDeclaration(SyntaxFactory.VariableDeclaration((TypeSyntax)eventDeclaration.Type.GetSyntaxNode(), SeparatedSyntaxList<VariableDeclaratorSyntax, VariableDeclaratorAspect>(eventDeclaration.VariableDeclarators)));
            if (eventDeclaration.AttributeSections.Any())
                @event = @event.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(eventDeclaration.AttributeSections));
            if (eventDeclaration.Modifiers.Any())
                @event = @event.WithModifiers(SyntaxTokenList<ModifierAspect>(eventDeclaration.Modifiers));
            return @event;
        }

        internal static EventDeclarationSyntax EventPropertyDeclaration(EventPropertyDeclarationAspect eventDeclaration)
        {
            var @event = SyntaxFactory.EventDeclaration((TypeSyntax)eventDeclaration.Type.GetSyntaxNode(), eventDeclaration.MemberName.Identifier.TokenValue).WithAccessorList(SyntaxFactory.AccessorList(SyntaxList<AccessorDeclarationSyntax, EventAccessorAspect>(eventDeclaration.EventAccessors)));
            if (eventDeclaration.MemberName.Type != null)
                @event = @event.WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier((NameSyntax)eventDeclaration.MemberName.Type.GetSyntaxNode()));
            if (eventDeclaration.AttributeSections.Any())
                @event = @event.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(eventDeclaration.AttributeSections));
            if (eventDeclaration.Modifiers.Any())
                @event = @event.WithModifiers(SyntaxTokenList<ModifierAspect>(eventDeclaration.Modifiers));
            return @event;
        }

        internal static AccessorDeclarationSyntax EventAccessor(EventAccessorAspect eventAccessor)
        {
            SyntaxKind kind = SyntaxKind.None;
            switch (eventAccessor.AccessorType)
            {
                case EventAccessorTypesAspect.Add:
                    kind = SyntaxKind.AddAccessorDeclaration;
                    break;
                case EventAccessorTypesAspect.Remove:
                    kind = SyntaxKind.RemoveAccessorDeclaration;
                    break;
                default:
                    throw new NotImplementedException();
            }

            var accessor = SyntaxFactory.AccessorDeclaration(kind, (BlockSyntax)eventAccessor.Block.GetSyntaxNode());
            if (eventAccessor.AttributeSections.Any())
                accessor = accessor.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(eventAccessor.AttributeSections));
            return accessor;
        }

        internal static MethodDeclarationSyntax MethodDeclaration(MethodDeclarationAspect methodDeclaration)
        {
            var method = SyntaxFactory.MethodDeclaration((TypeSyntax)methodDeclaration.ReturnType.GetSyntaxNode(), methodDeclaration.MemberName.Identifier.TokenValue);
            if (methodDeclaration.MemberName.Identifier is GenericNameAspect)
            {
                var typeParameterList = TypeParameterList(((GenericNameAspect)methodDeclaration.MemberName.Identifier).TypeArgumentList.TypeArguments.Select(t => t.TokenValue));
                method = method.WithTypeParameterList(typeParameterList);
            }

            if (methodDeclaration.MemberName.Type != null)
                method = method.WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier((NameSyntax)methodDeclaration.MemberName.Type.GetSyntaxNode()));
            if (methodDeclaration.AttributeSections.Any())
                method = method.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(methodDeclaration.AttributeSections));
            if (methodDeclaration.Modifiers.Any())
                method = method.WithModifiers(SyntaxTokenList<ModifierAspect>(methodDeclaration.Modifiers));
            if (methodDeclaration.TypeParameterList != null)
                method = method.WithTypeParameterList((TypeParameterListSyntax)(methodDeclaration.TypeParameterList.GetSyntaxNode()));
            if (methodDeclaration.ParameterList != null)
                method = method.WithParameterList((ParameterListSyntax)(methodDeclaration.ParameterList.GetSyntaxNode()));
            if (methodDeclaration.TypeParameterConstraintsClauses.Any())
                method = method.WithConstraintClauses(SyntaxList<TypeParameterConstraintClauseSyntax, TypeParameterConstraintsClauseAspect>(methodDeclaration.TypeParameterConstraintsClauses));
            if (methodDeclaration.Block != null)
                method = method.WithBody(((BlockSyntax)methodDeclaration.Block.GetSyntaxNode()));
            return method;
        }

        internal static TypeSyntax ReturnType(ReturnTypeAspect returnType)
        {
            if (returnType.Type == null)
                return SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword));
            else
                return (TypeSyntax)returnType.Type.GetSyntaxNode();
        }

        internal static PropertyDeclarationSyntax PropertyDeclaration(PropertyDeclarationAspect propertyDeclaration)
        {
            var property = SyntaxFactory.PropertyDeclaration((TypeSyntax)propertyDeclaration.Type.GetSyntaxNode(), propertyDeclaration.MemberName.Identifier.TokenValue).WithAccessorList(SyntaxFactory.AccessorList(SyntaxList<AccessorDeclarationSyntax, PropertyAccessorAspect>(propertyDeclaration.Accessors)));
            ;
            if (propertyDeclaration.AttributeSections.Any())
                property = property.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(propertyDeclaration.AttributeSections));
            if (propertyDeclaration.Modifiers.Any())
                property = property.WithModifiers(SyntaxTokenList<ModifierAspect>(propertyDeclaration.Modifiers));
            if (propertyDeclaration.MemberName.Type != null)
                property = property.WithExplicitInterfaceSpecifier(SyntaxFactory.ExplicitInterfaceSpecifier((NameSyntax)propertyDeclaration.MemberName.Type.GetSyntaxNode()));
            return property;
        }

        internal static AccessorDeclarationSyntax PropertyAccessor(PropertyAccessorAspect propertyAccessor)
        {
            SyntaxKind kind = SyntaxKind.None;
            switch (propertyAccessor.AccessorType)
            {
                case PropertyAccessorTypesAspect.Get:
                    kind = SyntaxKind.GetAccessorDeclaration;
                    break;
                case PropertyAccessorTypesAspect.Set:
                    kind = SyntaxKind.SetAccessorDeclaration;
                    break;
                default:
                    throw new NotImplementedException();
            }

            var accessor = SyntaxFactory.AccessorDeclaration(kind);
            if (propertyAccessor.Block == null)
                accessor = accessor.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            else
                accessor = accessor.WithBody((BlockSyntax)propertyAccessor.Block.GetSyntaxNode());
            if (propertyAccessor.AttributeSections.Any())
                accessor = accessor.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(propertyAccessor.AttributeSections));
            if (propertyAccessor.Modifiers.Any())
                accessor = accessor.WithModifiers(SyntaxTokenList<ModifierAspect>(propertyAccessor.Modifiers));
            return accessor;
        }

        internal static IndexerDeclarationSyntax IndexerDeclaration(IndexerDeclarationAspect indexerDeclaration)
        {
            var indexer = SyntaxFactory.IndexerDeclaration((TypeSyntax)indexerDeclaration.Type.GetSyntaxNode()).WithAccessorList(SyntaxFactory.AccessorList(SyntaxList<AccessorDeclarationSyntax, PropertyAccessorAspect>(indexerDeclaration.Accessors)));
            ;
            if (indexerDeclaration.AttributeSections.Any())
                indexer = indexer.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(indexerDeclaration.AttributeSections));
            if (indexerDeclaration.Modifiers.Any())
                indexer = indexer.WithModifiers(SyntaxTokenList<ModifierAspect>(indexerDeclaration.Modifiers));
            if (indexerDeclaration.FormalParameterList != null)
                indexer = indexer.WithParameterList(BracketedParameterList(indexerDeclaration.FormalParameterList));
            return indexer;
        }

        internal static BracketedParameterListSyntax BracketedParameterList(FormalParameterListAspect formalParamters)
        {
            return SyntaxFactory.BracketedParameterList(SeparatedSyntaxList<ParameterSyntax, ParameterAspect>(formalParamters.Parameters));
        }

        internal static InterfaceDeclarationSyntax InterfaceDeclaration(InterfaceDeclarationAspect interfaceDeclaration)
        {
            var @interface = SyntaxFactory.InterfaceDeclaration(Identifier(interfaceDeclaration.Identifier)).WithMembers(SyntaxList<MemberDeclarationSyntax, InterfaceMemberAspect>(interfaceDeclaration.Members));
            if (interfaceDeclaration.AttributeSections.Any())
                @interface = @interface.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(interfaceDeclaration.AttributeSections));
            if (interfaceDeclaration.Modifiers.Any())
                @interface = @interface.WithModifiers(SyntaxTokenList<ModifierAspect>(interfaceDeclaration.Modifiers));
            if (interfaceDeclaration.VariantTypeParameterList != null)
                @interface = @interface.WithTypeParameterList((TypeParameterListSyntax)interfaceDeclaration.VariantTypeParameterList.GetSyntaxNode());
            if (interfaceDeclaration.BaseTypes.Any())
            {
                var baseList = SyntaxFactory.BaseList(SeparatedSyntaxList<BaseTypeSyntax, BaseTypeAspect>(interfaceDeclaration.BaseTypes));
                @interface = @interface.WithBaseList(baseList);
            }

            if (interfaceDeclaration.TypeParameterConstraintsClauses.Any())
                @interface = @interface.WithConstraintClauses(SyntaxList<TypeParameterConstraintClauseSyntax, TypeParameterConstraintsClauseAspect>(interfaceDeclaration.TypeParameterConstraintsClauses));
            return @interface;
        }

        internal static MemberDeclarationSyntax InterfaceMethod(InterfaceMethodAspect methodDeclaration)
        {
            var method = SyntaxFactory.MethodDeclaration((TypeSyntax)methodDeclaration.ReturnType.GetSyntaxNode(), methodDeclaration.Identifier.TokenValue).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            ;
            if (methodDeclaration.AttributeSections.Any())
                method = method.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(methodDeclaration.AttributeSections));
            if (methodDeclaration.Modifiers.Any())
                method = method.WithModifiers(SyntaxTokenList<ModifierAspect>(methodDeclaration.Modifiers));
            if (methodDeclaration.TypeParameterList != null)
                method = method.WithTypeParameterList((TypeParameterListSyntax)(methodDeclaration.TypeParameterList.GetSyntaxNode()));
            if (methodDeclaration.ParameterList != null)
                method = method.WithParameterList((ParameterListSyntax)(methodDeclaration.ParameterList.GetSyntaxNode()));
            if (methodDeclaration.TypeParameterConstraintsClauses.Any())
                method = method.WithConstraintClauses(SyntaxList<TypeParameterConstraintClauseSyntax, TypeParameterConstraintsClauseAspect>(methodDeclaration.TypeParameterConstraintsClauses));
            return method;
        }

        internal static PropertyDeclarationSyntax InterfaceProperty(InterfacePropertyAspect propertyDeclaration)
        {
            var property = SyntaxFactory.PropertyDeclaration((TypeSyntax)propertyDeclaration.Type.GetSyntaxNode(), propertyDeclaration.Identifier.TokenValue).WithAccessorList(SyntaxFactory.AccessorList(SyntaxList<AccessorDeclarationSyntax, InterfaceAccessorAspect>(propertyDeclaration.Accessors)));
            if (propertyDeclaration.AttributeSections.Any())
                property = property.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(propertyDeclaration.AttributeSections));
            if (propertyDeclaration.Modifiers.Any())
                property = property.WithModifiers(SyntaxTokenList<ModifierAspect>(propertyDeclaration.Modifiers));
            return property;
        }

        internal static AccessorDeclarationSyntax InterfaceAccessor(InterfaceAccessorAspect interfaceAccessor)
        {
            SyntaxKind kind = SyntaxKind.None;
            switch (interfaceAccessor.AccessorType)
            {
                case PropertyAccessorTypesAspect.Get:
                    kind = SyntaxKind.GetAccessorDeclaration;
                    break;
                case PropertyAccessorTypesAspect.Set:
                    kind = SyntaxKind.SetAccessorDeclaration;
                    break;
                default:
                    throw new NotImplementedException();
            }

            var accessor = SyntaxFactory.AccessorDeclaration(kind).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            if (interfaceAccessor.AttributeSections.Any())
                accessor = accessor.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(interfaceAccessor.AttributeSections));
            return accessor;
        }

        internal static EventFieldDeclarationSyntax InterfaceEvent(InterfaceEventAspect eventDeclaration)
        {
            var @event = SyntaxFactory.EventFieldDeclaration(SyntaxFactory.VariableDeclaration((TypeSyntax)eventDeclaration.Type.GetSyntaxNode()).WithVariables(SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(eventDeclaration.Identifier.TokenValue))))).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            if (eventDeclaration.AttributeSections.Any())
                @event = @event.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(eventDeclaration.AttributeSections));
            if (eventDeclaration.Modifiers.Any())
                @event = @event.WithModifiers(SyntaxTokenList<ModifierAspect>(eventDeclaration.Modifiers));
            return @event;
        }

        internal static IndexerDeclarationSyntax InterfaceIndexer(InterfaceIndexerAspect indexerDeclaration)
        {
            var indexer = SyntaxFactory.IndexerDeclaration((TypeSyntax)indexerDeclaration.Type.GetSyntaxNode()).WithAccessorList(SyntaxFactory.AccessorList(SyntaxList<AccessorDeclarationSyntax, InterfaceAccessorAspect>(indexerDeclaration.Accessors)));
            if (indexerDeclaration.AttributeSections.Any())
                indexer = indexer.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(indexerDeclaration.AttributeSections));
            if (indexerDeclaration.Modifiers.Any())
                indexer = indexer.WithModifiers(SyntaxTokenList<ModifierAspect>(indexerDeclaration.Modifiers));
            if (indexerDeclaration.FormalParameterList != null)
                indexer = indexer.WithParameterList(BracketedParameterList(indexerDeclaration.FormalParameterList));
            return indexer;
        }

        internal static OperatorDeclarationSyntax UnaryOperatorDeclarator(UnaryOperatorDeclaratorAspect operatorDeclaration)
        {
            var @operator = SyntaxFactory.OperatorDeclaration((TypeSyntax)operatorDeclaration.Type.GetSyntaxNode(), UnaryOverloadedOperator(operatorDeclaration.Operator)).WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList<ParameterSyntax>(SyntaxFactory.Parameter(SyntaxFactory.Identifier(operatorDeclaration.Identifier1.Identifier.TokenValue)).WithType((TypeSyntax)operatorDeclaration.Type1.Type.GetSyntaxNode()))));
            if (operatorDeclaration.AttributeSections.Any())
                @operator = @operator.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(operatorDeclaration.AttributeSections));
            if (operatorDeclaration.Modifiers.Any())
                @operator = @operator.WithModifiers(SyntaxTokenList<ModifierAspect>(operatorDeclaration.Modifiers));
            if (operatorDeclaration.Block != null)
                @operator = @operator.WithBody(((BlockSyntax)operatorDeclaration.Block.GetSyntaxNode()));
            return @operator;
        }

        internal static SyntaxToken UnaryOverloadedOperator(OverloadableUnaryOperatorAspect @operator)
        {
            switch (@operator.TokenValue)
            {
                case "+":
                    return SyntaxFactory.Token(SyntaxKind.PlusToken);
                case "-":
                    return SyntaxFactory.Token(SyntaxKind.MinusToken);
                case "!":
                    return SyntaxFactory.Token(SyntaxKind.ExclamationToken);
                case "~":
                    return SyntaxFactory.Token(SyntaxKind.TildeToken);
                case "++":
                    return SyntaxFactory.Token(SyntaxKind.PlusPlusToken);
                case "--":
                    return SyntaxFactory.Token(SyntaxKind.MinusMinusToken);
                case "true":
                    return SyntaxFactory.Token(SyntaxKind.TrueKeyword);
                case "false":
                    return SyntaxFactory.Token(SyntaxKind.FalseKeyword);
                default:
                    throw new NotImplementedException();
            }
        }

        internal static SyntaxToken BinaryOverloadedOperator(OverloadableBinaryOperatorAspect @operator)
        {
            switch (@operator.TokenValue)
            {
                case "+":
                    return SyntaxFactory.Token(SyntaxKind.PlusToken);
                case "-":
                    return SyntaxFactory.Token(SyntaxKind.MinusToken);
                case "*":
                    return SyntaxFactory.Token(SyntaxKind.AsteriskToken);
                case "/":
                    return SyntaxFactory.Token(SyntaxKind.SlashToken);
                case "%":
                    return SyntaxFactory.Token(SyntaxKind.PercentToken);
                case "&":
                    return SyntaxFactory.Token(SyntaxKind.AmpersandToken);
                case "|":
                    return SyntaxFactory.Token(SyntaxKind.BarToken);
                case "^":
                    return SyntaxFactory.Token(SyntaxKind.CaretToken);
                case "<<":
                    return SyntaxFactory.Token(SyntaxKind.LessThanLessThanToken);
                case ">>":
                    return SyntaxFactory.Token(SyntaxKind.GreaterThanGreaterThanToken);
                case "==":
                    return SyntaxFactory.Token(SyntaxKind.EqualsEqualsToken);
                case "!=":
                    return SyntaxFactory.Token(SyntaxKind.ExclamationEqualsToken);
                case ">":
                    return SyntaxFactory.Token(SyntaxKind.GreaterThanToken);
                case "<":
                    return SyntaxFactory.Token(SyntaxKind.LessThanToken);
                case ">=":
                    return SyntaxFactory.Token(SyntaxKind.GreaterThanEqualsToken);
                case "<=":
                    return SyntaxFactory.Token(SyntaxKind.LessThanEqualsToken);
                default:
                    throw new NotImplementedException();
            }
        }

        internal static SyntaxToken ConversionOperatorType(ConvertsionOperatorType @operator)
        {
            switch (@operator.TokenValue)
            {
                case "explicit":
                    return SyntaxFactory.Token(SyntaxKind.ExplicitKeyword);
                case "implicit":
                    return SyntaxFactory.Token(SyntaxKind.ImplicitKeyword);
                default:
                    throw new NotImplementedException();
            }
        }

        internal static OperatorDeclarationSyntax BinaryOperatorDeclaratorAspect(BinaryOperatorDeclaratorAspect operatorDeclaration)
        {
            var parameters = new ParameterSyntax[]{SyntaxFactory.Parameter(SyntaxFactory.Identifier(operatorDeclaration.Identifier1.Identifier.TokenValue)).WithType((TypeSyntax)operatorDeclaration.Type1.Type.GetSyntaxNode()), SyntaxFactory.Parameter(SyntaxFactory.Identifier(operatorDeclaration.Identifier2.Identifier.TokenValue)).WithType((TypeSyntax)operatorDeclaration.Type2.Type.GetSyntaxNode())};
            var @operator = SyntaxFactory.OperatorDeclaration((TypeSyntax)operatorDeclaration.Type.GetSyntaxNode(), BinaryOverloadedOperator(operatorDeclaration.Operator)).WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList<ParameterSyntax>(parameters)));
            if (operatorDeclaration.AttributeSections.Any())
                @operator = @operator.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(operatorDeclaration.AttributeSections));
            if (operatorDeclaration.Modifiers.Any())
                @operator = @operator.WithModifiers(SyntaxTokenList<ModifierAspect>(operatorDeclaration.Modifiers));
            if (operatorDeclaration.Block != null)
                @operator = @operator.WithBody(((BlockSyntax)operatorDeclaration.Block.GetSyntaxNode()));
            return @operator;
        }

        internal static ConversionOperatorDeclarationSyntax ConversionOperationDeclaratpr(ConversionOperationDeclaratorAspect operatorDeclaration)
        {
            var @operator = SyntaxFactory.ConversionOperatorDeclaration(ConversionOperatorType(operatorDeclaration.Operator), (TypeSyntax)operatorDeclaration.Type.GetSyntaxNode()).WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList<ParameterSyntax>(SyntaxFactory.Parameter(SyntaxFactory.Identifier(operatorDeclaration.Identifier1.Identifier.TokenValue)).WithType((TypeSyntax)operatorDeclaration.Type1.Type.GetSyntaxNode()))));
            if (operatorDeclaration.AttributeSections.Any())
                @operator = @operator.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(operatorDeclaration.AttributeSections));
            if (operatorDeclaration.Modifiers.Any())
                @operator = @operator.WithModifiers(SyntaxTokenList<ModifierAspect>(operatorDeclaration.Modifiers));
            if (operatorDeclaration.Block != null)
                @operator = @operator.WithBody(((BlockSyntax)operatorDeclaration.Block.GetSyntaxNode()));
            return @operator;
        }

        internal static StructDeclarationSyntax StructDeclaration(StructDeclarationAspect structDeclaration)
        {
            var @struct = SyntaxFactory.StructDeclaration(Identifier(structDeclaration.IdentifierName)).WithMembers(SyntaxList<MemberDeclarationSyntax, TypeMemberDeclarationAspect>(structDeclaration.Members));
            if (structDeclaration.AttributeSections.Any())
                @struct = @struct.WithAttributeLists(SyntaxList<AttributeListSyntax, AttributeSectionAspect>(structDeclaration.AttributeSections));
            if (structDeclaration.Modifiers.Any())
                @struct = @struct.WithModifiers(SyntaxTokenList<ModifierAspect>(structDeclaration.Modifiers));
            if (structDeclaration.TypeParameterList != null)
                @struct = @struct.WithTypeParameterList((TypeParameterListSyntax)structDeclaration.TypeParameterList.GetSyntaxNode());
            if (structDeclaration.StructInterfaces != null)
                @struct = @struct.WithBaseList((BaseListSyntax)structDeclaration.StructInterfaces.GetSyntaxNode());
            if (structDeclaration.TypeParameterConstraintsClauses.Any())
                @struct = @struct.WithConstraintClauses(SyntaxList<TypeParameterConstraintClauseSyntax, TypeParameterConstraintsClauseAspect>(structDeclaration.TypeParameterConstraintsClauses));
            return @struct;
        }

        internal static BaseListSyntax StructInterfaces(StructInterfacesAspect structInterfaces)
        {
            return SyntaxFactory.BaseList(SeparatedSyntaxList<BaseTypeSyntax, BaseTypeAspect>(structInterfaces.BaseTypes));
        }

        internal static string GetReferencedName(NameAspect nameAspect)
        {
            string name = null;
            if (nameAspect != null)
                name = CSAspectCompilerHelper.GetFullName(nameAspect);
            return name;
        }

        internal static string GetFullName(NameAspect nameAspect)
        {
            switch (nameAspect)
            {
                case QualifiedIdentifierAspect qualifiedIdentifier:
                    return GetFullName(qualifiedIdentifier);
                case IdentifierNameAspect identifierName:
                    return identifierName.TokenValue;
                case QualifiedAliasMemberAspect qualifiedAliasMember:
                    return qualifiedAliasMember.GetName();
                default:
                    throw new NotImplementedException();
            }
        }

        internal static string GetFullName(QualifiedIdentifierAspect qualifiedIdentifierAspect)
        {
            string name = "";
            if (qualifiedIdentifierAspect.Left is QualifiedIdentifierAspect)
                name += GetFullName((QualifiedIdentifierAspect)qualifiedIdentifierAspect.Left);
            else
                name += qualifiedIdentifierAspect.Left.TokenValue;
            name += "." + qualifiedIdentifierAspect.Right.TokenValue;
            return name;
        }

        internal static SyntaxList<AttributeListSyntax> GetAttributeListOfAttributeType(Type type)
        {
            var attributeSyntax = SyntaxFactory.Attribute(SyntaxFactory.ParseName(type.FullName)).WithArgumentList(SyntaxFactory.AttributeArgumentList(SyntaxFactory.SingletonSeparatedList<AttributeArgumentSyntax>(SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(""))))));
            return SyntaxFactory.SingletonList<AttributeListSyntax>(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(attributeSyntax)));
        }
    }
}