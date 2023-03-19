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
using System.IO;
using System.Reflection;
using AspectDN.Aspect.Compilation.CS;
using AspectDN.Common;
using AspectDN.Aspect.Compilation.Foundation;
using TokenizerDN.Common;
using TokenizerDN.Common.SourceAnalysis;

namespace AspectDN.Aspect.Syntax.CS.CS5Syntax
{
    internal class CS5SyntaxTokenVisitor : ITokenVisitor
    {
        List<ICompilerError> _Errors;
        CSAspectTree _Tree;
        internal CSAspectTree _AspectTree
        {
            get
            {
                return (CSAspectTree)_Tree;
            }
        }

        internal CS5SyntaxTokenVisitor(CSAspectTree tree, List<ICompilerError> errors)
        {
            _Tree = tree;
            _Errors = errors;
        }

        internal object Visit(ISynToken token)
        {
            var result = _VisitToken(token);
            return result;
        }

        internal AspectNode VisitToken(ISynToken token)
        {
            return _VisitToken(token);
        }

        AspectNode _VisitToken(ISynToken token)
        {
            if (!token.IsTokenError)
            {
                switch (token.Name)
                {
#region aspect
                    case "aspect-compilation-unit":
                        return _DefaultVisitChildTokens(_AspectTree.CreateRoot(token));
                    case "package-declaration":
                        return _DefaultVisitChildTokens(new PackageDeclarationAspect(token));
                    case "pointcut-declaration":
                        return _DefaultVisitChildTokens(new PointcutDeclarationAspect(token));
                    case "pointcut-expression":
                        return _DefaultVisitChildTokens(new PointcutExpressionAspect(token));
                    case "pointcut-type":
                        return _DefaultVisitChildTokens(new PointcutTypeAspect(token));
                    case "advice-interface-members-declaration":
                        return _DefaultVisitChildTokens(new AdviceInterfaceMembersDeclarationAspect(token));
                    case "advice-type-members-declaration":
                        return _DefaultVisitChildTokens(new AdviceTypeMembersDeclarationAspect(token));
                    case "advice-code-declaration":
                        return _DefaultVisitChildTokens(new AdviceCodeDeclarationAspect(token));
                    case "advice-change-value-declaration":
                        return _DefaultVisitChildTokens(new AdviceChangeValueDeclarationAspect(token));
                    case "advice-constructor-declaration":
                        return _DefaultVisitChildTokens(new AdviceConstructorDeclarationAspect(token));
                    case "advice-static-constructor-declaration":
                        return _DefaultVisitChildTokens(new AdviceStaticConstructorDeclarationAspect(token));
                    case "advice-destructor-declaration":
                        return _DefaultVisitChildTokens(new AdviceDestructorDeclarationAspect(token));
                    case "advice-unary-operator-declarator":
                        return _DefaultVisitChildTokens(new AdviceUnaryOperatorDeclaratorAspect(token));
                    case "advice-binary-operator-declarator":
                        return _DefaultVisitChildTokens(new AdviceBinaryOperatorDeclaratorAspect(token));
                    case "advice-conversion-operator-declarator":
                        return _DefaultVisitChildTokens(new AdviceConversionOperatorDeclaratorAspect(token));
                    case "advice-operator-declarator-parameter":
                        return _DefaultVisitChildTokens(new AdviceOperatorDeclaratorParameterAspect(token));
                    case "aspect-code-declaration":
                        return _DefaultVisitChildTokens(new AspectCodeDeclarationAspect(token));
                    case "aspect-change-value-declaration":
                        return _DefaultVisitChildTokens(new AspectChangeValueDeclarationAspect(token));
                    case "aspect-types-declaration":
                        return _DefaultVisitChildTokens(new AspectTypesDeclarationAspect(token));
                    case "aspect-type-members-declaration":
                        return _DefaultVisitChildTokens(new AspectTypeMembersDeclarationAspect(token));
                    case "aspect-type-member-modifier":
                        return _DefaultVisitChildTokens(new AspectMemberModifier(token));
                    case "aspect-interface-members-declaration":
                        return _DefaultVisitChildTokens(new AspectInterfaceMembersDeclarationAspect(token));
                    case "aspect-enum-members-declaration":
                        return _DefaultVisitChildTokens(new AspectEnumMembersDeclarationAspect(token));
                    case "aspect-inherit-declaration":
                        return _DefaultVisitChildTokens(new InheritDeclarationAspect(token));
                    case "execution-time":
                        return _DefaultVisitChildTokens(new ExecutionTimeAspect(token));
                    case "control-flow":
                        return _DefaultVisitChildTokens(new ControlFlowAspect(token));
                    case "control-flows":
                        return _DefaultVisitChildTokens(new ControlFlowsAspect(token));
                    case "aspect-pointcut-common-anonymous":
                        return _DefaultVisitChildTokens(new AspectPointcutCommonAnonymousAspect(token));
                    case "aspect-pointcut-this-type-members-anonymous":
                        return _DefaultVisitChildTokens(new AspectPointcutThisTypeMembersAnonymousAspect(token));
                    case "aspect-pointcut-this-code-anonymous":
                        return _DefaultVisitChildTokens(new AspectPointcutThisCodeAnonymousAspect(token));
                    case "aspect-pointcut-named":
                        return _DefaultVisitChildTokens(new AspectPointcutNamedAspect(token));
                    case "aspect-advice-code-named":
                        return _DefaultVisitChildTokens(new AspectAdviceCodeNamedAspect(token));
                    case "aspect-advice-code-anonymous":
                        return _DefaultVisitChildTokens(new AspectAdviceCodeAnonymousAspect(token));
                    case "aspect-advice-inherit-anonymous":
                        return _DefaultVisitChildTokens(new AspectAdviceInheritAnonymousAspect(token));
                    case "aspect-advice-changevalue-named":
                        return _DefaultVisitChildTokens(new AspectAdviceChangeValueNamedAspect(token));
                    case "aspect-advice-changevalue-anonymous":
                        return _DefaultVisitChildTokens(new AspectAdviceChangeValueAnonymousAspect(token));
                    case "prototype-member-mapping":
                        return _DefaultVisitChildTokens(new PrototypeMappingMemberAspect(token));
                    case "prototype-type-parameter-mapping":
                        return _DefaultVisitChildTokens(new PrototypeMappingTypeParameterAspect(token));
                    case "prototype-type-generic-parameter-target":
                        return _DefaultVisitChildTokens(new PrototypeTypeGenericParameterTargetAspect(token));
                    case "prototype-method-generic-parameter-target":
                        return _DefaultVisitChildTokens(new PrototypeMethodGenericParameterTargetAspect(token));
                    case "prototype-type-reference-mapping":
                        return _DefaultVisitChildTokens(new PrototypeMappingTypeReferenceAspect(token));
                    case "prototype-target-this-member-declaration":
                        return _DefaultVisitChildTokens(new PrototypeTargetThisMemberDeclarationAspect(token));
                    case "prototype-target-this-declaration":
                        return _DefaultVisitChildTokens(new PrototypeTargetThisMemberDeclarationAspect(token));
                    case "prototype-target-base-member-declaration":
                        return _DefaultVisitChildTokens(new PrototypeTargetBaseMemberDeclarationAspect(token));
                    case "prototype-target-member-declaration":
                        return _DefaultVisitChildTokens(new PrototypeTargetMemberDeclarationAspect(token));
                    case "override-specific-constructor-declaration":
                        return _DefaultVisitChildTokens(new OverrideSpecificConstructorDeclarationAspect(token));
                    case "aspect-advice-type-members-named":
                        return _DefaultVisitChildTokens(new AspectAdviceTypeMembersNamedAspect(token));
                    case "aspect-advice-type-members-anonymous":
                        return _DefaultVisitChildTokens(new AspectAdviceTypeMembersAnonymousAspect(token));
                    case "aspect-advice-interface-members-named":
                        return _DefaultVisitChildTokens(new AspectAdviceInterfaceMembersNamedAspect(token));
                    case "aspect-advice-interface-members-anonymous":
                        return _DefaultVisitChildTokens(new AspectAdviceInterfaceMembersAnonymousAspect(token));
                    case "around-statement":
                        return _DefaultVisitChildTokens(new AroundStatementAspect(token));
                    case "advice-enum-members-declaration":
                        return _DefaultVisitChildTokens(new AdviceEnumMembersDeclarationAspect(token));
                    case "aspect-advice-enum-members-named":
                        return _DefaultVisitChildTokens(new AspectAdviceEnumMembersNamedAspect(token));
                    case "aspect-advice-enum-members-anonymous":
                        return _DefaultVisitChildTokens(new AspectAdviceEnumMembersAnonymousAspect(token));
                    case "advice-types-declaration":
                        return _DefaultVisitChildTokens(new AdviceTypesDeclarationAspect(token));
                    case "aspect-advice-type-named":
                        return _DefaultVisitChildTokens(new AspectAdviceTypeNamedAspect(token));
                    case "aspect-advice-type-anonymous":
                        return _DefaultVisitChildTokens(new AspectAdviceTypeAnonymousAspect(token));
                    case "advice-attributes-declaration":
                        return _DefaultVisitChildTokens(new AdviceAttributesDeclarationAspect(token));
                    case "aspect-attributes-declaration":
                        return _DefaultVisitChildTokens(new AspectAttributesDeclarationAspect(token));
                    case "aspect-advice-attributes-named":
                        return _DefaultVisitChildTokens(new AspectAdviceAttributesNamedAspect(token));
                    case "aspect-advice-attributes-anonymous":
                        return _DefaultVisitChildTokens(new AspectAdviceAsttributesAnonymousAspect(token));
#endregion aspect
#region PrototypeDeclaration
                    case "prototype-nested-class-declaration":
                        return _DefaultVisitChildTokens(new PrototypeClassDeclarationAspect(token, true));
                    case "prototype-nested-interface-declaration":
                        return _DefaultVisitChildTokens(new PrototypeInterfaceDeclarationAspect(token, true));
                    case "prototype-nested-struct-declaration":
                        return _DefaultVisitChildTokens(new PrototypeStructDeclarationAspect(token, true));
                    case "prototype-nested-delegate-declaration":
                        return _DefaultVisitChildTokens(new PrototypeDelegateDeclarationAspect(token, true));
                    case "prototype-nested-enum-declaration":
                        return _DefaultVisitChildTokens(new PrototypeEnumDeclarationAspect(token, true));
                    case "prototype-class-declaration":
                        return _DefaultVisitChildTokens(new PrototypeClassDeclarationAspect(token, false));
                    case "prototype-base-list":
                        return _DefaultVisitChildTokens(new PrototypeBaseListAspect(token));
                    case "prototype-base-type":
                        return _DefaultVisitChildTokens(new PrototypeBaseTypeAspect(token));
                    case "prototype-struct-declaration":
                        return _DefaultVisitChildTokens(new PrototypeStructDeclarationAspect(token, false));
                    case "prototype-interface-declaration":
                        return _DefaultVisitChildTokens(new PrototypeInterfaceDeclarationAspect(token, false));
                    case "prototype-delegate-declaration":
                        return _DefaultVisitChildTokens(new PrototypeDelegateDeclarationAspect(token, false));
                    case "prototype-member-name":
                        return _DefaultVisitChildTokens(new MemberNameAspect(token));
                    case "prototype-enum-declaration":
                        return _DefaultVisitChildTokens(new PrototypeEnumDeclarationAspect(token, false));
                    case "prototype-type-field-declaration":
                        return _DefaultVisitChildTokens(new PrototypeFieldDeclarationAspect(token, true));
                    case "prototype-field-declaration":
                        return _DefaultVisitChildTokens(new PrototypeFieldDeclarationAspect(token, false));
                    case "prototype-type-property-declaration":
                        return _DefaultVisitChildTokens(new PrototypePropertyDeclarationAspect(token, true));
                    case "prototype-property-declaration":
                        return _DefaultVisitChildTokens(new PrototypePropertyDeclarationAspect(token, false));
                    case "prototype-get-accessor-declaration":
                        return _DefaultVisitChildTokens(new PrototypeGetAccessorAspect(token));
                    case "prototype-set-accessor-declaration":
                        return _DefaultVisitChildTokens(new PrototypeSetAccessorAspect(token));
                    case "prototype-type-indexer-declaration":
                        return _DefaultVisitChildTokens(new PrototypeIndexerDeclarationtAspect(token, true));
                    case "prototype-indexer-declaration":
                        return _DefaultVisitChildTokens(new PrototypeIndexerDeclarationtAspect(token, false));
                    case "prototype-type-method-declaration":
                        return _DefaultVisitChildTokens(new PrototypeMethodDeclarationAspect(token, true));
                    case "prototype-method-declaration":
                        return _DefaultVisitChildTokens(new PrototypeMethodDeclarationAspect(token, false));
                    case "prototype-type-constructor-declaration":
                        return _DefaultVisitChildTokens(new PrototypeConstructorDeclarationtAspect(token, true));
                    case "prototype-type-constructor-initializer":
                        return _DefaultVisitChildTokens(new ConstructorInitializerAspect(token));
                    case "prototype-type-constructor-initializer-modifier":
                        return _DefaultVisitChildTokens(new ConstructorInitializerModifierAspect(token));
                    case "prototype-event-field-declaration":
                        return _DefaultVisitChildTokens(new PrototypeEventFieldDeclarationAspect(token, false));
                    case "prototype-type-event-field-declaration":
                        return _DefaultVisitChildTokens(new PrototypeEventFieldDeclarationAspect(token, true));
                    case "prototype-type-event-property-declaration":
                        return _DefaultVisitChildTokens(new PrototypeEventPropertyDeclarationAspect(token, true));
                    case "prototype-event-property-declaration":
                        return _DefaultVisitChildTokens(new PrototypeEventPropertyDeclarationAspect(token, false));
                    case "prototype-add-accessor-declaration":
                        return _DefaultVisitChildTokens(new PrototypeAddAccessorAspect(token));
                    case "prototype-remove-accessor-declaration":
                        return _DefaultVisitChildTokens(new PrototypeRemoveAccessorAspect(token));
                    case "prototype-type-parameter-declaration":
                        return _DefaultVisitChildTokens(new PrototypeTypeParameterAspect(token));
                    case "prototype-mapping-types-declaration":
                        return _DefaultVisitChildTokens(new PrototypeTypeMappingAspect(token));
                    case "prototype-type-name":
                        return _DefaultVisitChildTokens(new QualifiedUnboundTypeNameAspect(token));
                    case "target-type-name":
                        return _DefaultVisitChildTokens(new TargetTypeNameAspect(token));
                    case "prototype-member-method-declaration":
                        return _DefaultVisitChildTokens(new PrototypeMethodDeclarationAspect(token, false));
                    case "prototype-constructor-declaration":
                        return _DefaultVisitChildTokens(new PrototypeConstructorDeclarationtAspect(token, false));
                    case "prototype-fixed-parameter":
                        return _DefaultVisitChildTokens(new FixedParameterAspect(token));
                    case "prototype-parameter-array":
                        return _DefaultVisitChildTokens(new ParameterArrayAspect(token));
                    case "prototype-map-type-member":
                        return _DefaultVisitChildTokens(new PrototypeTypeMappingItemAspect(token));
                    case "prototype-member-modifier":
                        return _DefaultVisitChildTokens(new KeywordAspect(token, (Keywords)Enum.Parse(typeof(Keywords), token.Value.ToUpper())));
#endregion
#region Misc
                    case "argument-list":
                        return _DefaultVisitChildTokens(new ArgumentListAspect(token));
                    case "argument-name":
                        return _DefaultVisitChildTokens(new ArgumentNameAspect(token));
                    case "argument":
                        return _DefaultVisitChildTokens(new ArgumentAspect(token));
                    case "type-argument-list":
                        return _DefaultVisitChildTokens(new TypeArgumentListAspect(token));
                    case "anonymous-function-parameter-modifier":
                    case "prototype-class-modifier":
                    case "argument-modifier":
                    case "prototype-type-member-modifier":
                        return _VisitKeyword(token);
                    case "using-alias-directive":
                        return _DefaultVisitChildTokens(new UsingAliasDirectiveAspect(token));
                    case "using-namespace-directive":
                        return _DefaultVisitChildTokens(new UsingNamespaceDirectiveAspect(token));
                    case "qualified-identifier":
                        if (token.Children.Count() == 1)
                            return _VisitToken(token.Children[0]);
                        else
                            return _DefaultVisitChildTokens(new QualifiedIdentifierAspect(token));
                    case "identifier":
                        return new IdentifierNameAspect(token);
                    case "prototype-identifier":
                        return new IdentifierNameAspect(token);
                    case "namespace-or-type-name":
                        return _DefaultVisitChildTokens(new NamespaceOrTypenameAspect(token));
                    case "positional-argument":
                        return _DefaultVisitChildTokens(new AttributeArgumentPositionalAspect(token));
                    case "named-argument":
                        return _DefaultVisitChildTokens(new AttributeArgumentNamedAspect(token));
                    case "namespace-declaration":
                        return _DefaultVisitChildTokens(new NamespaceDeclarationAspect(token));
                    case "qualified-alias-member":
                        return _DefaultVisitChildTokens(new QualifiedAliasMemberAspect(token));
#endregion Misc
#region type
                    case "nullable-type":
                        return _DefaultVisitChildTokens(new NullableTypeAspect(token));
                    case "array-type":
                        return _DefaultVisitChildTokens(new ArrayTypeAspect(token));
                    case "rank-specifier":
                        return _DefaultVisitChildTokens(new RankSpecifierAspect(token));
                    case "dim-separator":
                        return _DefaultVisitChildTokens(new DimSeparator(token));
                    case "predefined-type":
                    case "none-array-type":
                    case "void":
                        return _VisitNoneArrayType(token);
                    case "anonymous-type":
                        return _DefaultVisitChildTokens(new AnonymousTypeAspect(token));
                    case "type-name":
                        return _DefaultVisitChildTokens(new TypeNameAspect(token));
                    case "void-type":
                        return _DefaultVisitChildTokens(new VoidTypeAspect(token));
#endregion type
#region expression
                    case "block":
                        return _DefaultVisitChildTokens(new BlockAspect(token));
                    case "labeled-statement":
                        return _DefaultVisitChildTokens(new LabeledStatementAspect(token));
                    case "local-variable-declaration":
                        return _DefaultVisitChildTokens(new LocalVariableDeclarationAspect(token));
                    case "variable-declaration":
                        return _DefaultVisitChildTokens(new VariableDeclarationAspect(token));
                    case "local-variable-declarator":
                        return _DefaultVisitChildTokens(new LocalVariableDeclaratorAspect(token));
                    case "local-constant-declaration":
                        return _DefaultVisitChildTokens(new LocalConstantDeclarationAspect(token));
                    case "statement-expression":
                        throw new NotImplementedException();
                    case "this-expression":
                        return _DefaultVisitChildTokens(new OperatorAspect(token));
                    case "post-increment-expression":
                        return _DefaultVisitChildTokens(new PostExpressionAspect(token, IncrDecrOperators.Increment));
                    case "post-decrement-expression":
                        return _DefaultVisitChildTokens(new PostExpressionAspect(token, IncrDecrOperators.Decrement));
                    case "object-creation-expression":
                        return _DefaultVisitChildTokens(new ObjectCreationExpressionAspect(token));
                    case "generic-name":
                        return _DefaultVisitChildTokens(new GenericNameAspect(token));
                    case "parenthesized-expression":
                        return _DefaultVisitChildTokens(new ParenthesizedExpressionAspect(token));
                    case "member-access":
                        return _DefaultVisitChildTokens(new MemberAccessExpressionAspect(token));
                    case "invocation-expression":
                        return _DefaultVisitChildTokens(new InvocationExpressionAspect(token));
                    case "element-access":
                        return _DefaultVisitChildTokens(new ElementAccessExpressionAspect(token));
                    case "this-access":
                        return _DefaultVisitChildTokens(new ThisElementAccessExpressionAspect(token));
                    case "base-access":
                        return _DefaultVisitChildTokens(new BaseElementAccessExpressionAspect(token));
                    case "object-initializer":
                        return _DefaultVisitChildTokens(new ObjectInitializerAspect(token));
                    case "member-initializer":
                        return _DefaultVisitChildTokens(new MemberInitializerAspect(token));
                    case "collection-initializer":
                        return _DefaultVisitChildTokens(new CollectionInitializerAspect(token));
                    case "element-initializer":
                        return _DefaultVisitChildTokens(new ElementInitializerAspect(token));
                    case "array-creation-expression":
                        return _DefaultVisitChildTokens(new ArrayCreationExpressionAspect(token));
                    case "delegate-creation-expression":
                        return _DefaultVisitChildTokens(new DelegateCreationExpressionAspect(token));
                    case "anonymous-object-creation-expression":
                        return _DefaultVisitChildTokens(new AnonymousObjectCreationExpressionAspect(token));
                    case "member-declarator":
                        return _DefaultVisitChildTokens(new AnonymousMemberDeclaratorAspect(token));
                    case "typeof-expression":
                        return _DefaultVisitChildTokens(new TypeOfExpressionAspect(token));
                    case "qualified-unbound-type-name":
                        return _DefaultVisitChildTokens(new QualifiedUnboundTypeNameAspect(token));
                    case "alias-unbound-type-name":
                        return _DefaultVisitChildTokens(new AliasUnboundTypeNameAspect(token));
                    case "generic-dimension-specifier":
                        return _DefaultVisitChildTokens(new GenericDimensionSpecifierAspect(token));
                    case "comma":
                        return _DefaultVisitChildTokens(new CommaAspect(token));
                    case "checked-expression":
                        return _DefaultVisitChildTokens(new CheckedExpressionAspect(token));
                    case "unchecked-expression":
                        return _DefaultVisitChildTokens(new UnCheckedExpressionAspect(token));
                    case "default-value-expression":
                        return _DefaultVisitChildTokens(new DefaultValueTypeExpressionAspect(token));
                    case "unary-operation-expression":
                        return _DefaultVisitChildTokens(new UnaryOperationExpressionAspect(token));
                    case "unary-operator":
                        return _DefaultVisitChildTokens(new UnaryOperatorAspect(token));
                    case "pre-increment-expression":
                        return _DefaultVisitChildTokens(new PreExpressionAspect(token, IncrDecrOperators.Increment));
                    case "pre-decrement-expression":
                        return _DefaultVisitChildTokens(new PreExpressionAspect(token, IncrDecrOperators.Decrement));
                    case "cast-expression":
                        return _DefaultVisitChildTokens(new CastExpressionAspect(token));
                    case "additive-expression":
                        return _VisitBinaryExpression(token);
                    case "additive-operator":
                        return _DefaultVisitChildTokens(new OperatorAspect(token));
                    case "multiplicative-expression":
                        return _VisitBinaryExpression(token);
                    case "multiplicative-operator":
                        return _DefaultVisitChildTokens(new OperatorAspect(token));
                    case "shift-expression":
                        return _VisitBinaryExpression(token);
                    case "shift-operator":
                        return _DefaultVisitChildTokens(new OperatorAspect(token));
                    case "shift-relational-expression":
                        return _VisitBinaryExpression(token);
                    case "type-relational-expression":
                        return _VisitBinaryExpression(token);
                    case "shift-relational-operator":
                        return _DefaultVisitChildTokens(new OperatorAspect(token));
                    case "type-relational-operator":
                        return _DefaultVisitChildTokens(new OperatorAspect(token));
                    case "equality-expression":
                        return _VisitBinaryConditionalExpression(token);
                    case "equality-operator":
                        return _DefaultVisitChildTokens(new OperatorAspect(token));
                    case "and-expression":
                        return _VisitBinaryConditionalExpression(token);
                    case "exclusive-or-expression":
                        return _VisitBinaryConditionalExpression(token);
                    case "inclusive-or-expression":
                        return _VisitBinaryConditionalExpression(token);
                    case "conditional-and-expression":
                        return _VisitBinaryConditionalExpression(token);
                    case "conditional-or-expression":
                        return _VisitBinaryConditionalExpression(token);
                    case "null-coalescing-expression":
                        return _VisitBinaryConditionalExpression(token);
                    case "conditional-expression":
                        return _VisitConditionalExpression(token);
                    case "and-expression-op":
                    case "exclusive-or-expression-op":
                    case "inclusive-or-expression-op":
                    case "conditional-and-expression-op":
                    case "conditional-or-expression-op":
                    case "null-coalescing-expression-op":
                        return _DefaultVisitChildTokens(new OperatorAspect(token));
                    case "anonymous-method-expression":
                        return _DefaultVisitChildTokens(new AnonymousMethodExpressionAspect(token));
                    case "simple-lambda-expression":
                        return _DefaultVisitChildTokens(new SimpleLambdaExpressionAspect(token));
                    case "parenthesis-lambda-expression":
                        return _DefaultVisitChildTokens(new ParenthesisLambdaExpressionAspect(token));
                    case "explicit-anonymous-function-parameter":
                        return _DefaultVisitChildTokens(new ExplicitAnonymousFunctionParameterAspect(token));
                    case "implicit-anonymous-function-parameter":
                        return _DefaultVisitChildTokens(new ImplicitAnonymousFunctionParameterAspect(token));
                    case "anonymous-function-body":
                        return _DefaultVisitChildTokens(new AnonymousFunctionBodyAspect(token));
                    case "query-expression":
                        return _DefaultVisitChildTokens(new QueryExpressionAspect(token));
                    case "from-clause":
                        return _DefaultVisitChildTokens(new FromClauseAspect(token));
                    case "query-body":
                        return _DefaultVisitChildTokens(new QueryBodyAspect(token));
                    case "let-clause":
                        return _DefaultVisitChildTokens(new LetClauseAspect(token));
                    case "where-clause":
                        return _DefaultVisitChildTokens(new WhereClauseAspect(token));
                    case "join-clause":
                        return _DefaultVisitChildTokens(new JoinClauseAspect(token));
                    case "join-into-clause":
                        return _DefaultVisitChildTokens(new JoinIntoClauseAspect(token));
                    case "orderby-clause":
                        return _DefaultVisitChildTokens(new OrderByClauseAspect(token));
                        ;
                    case "ordering":
                        return _DefaultVisitChildTokens(new OrderingAspect(token));
                    case "ordering-direction":
                        return _VisitKeyword(token);
                    case "select-clause":
                        return _DefaultVisitChildTokens(new SelectClauseAspect(token));
                    case "group-clause":
                        return _DefaultVisitChildTokens(new GroupClauseAspect(token));
                    case "query-continuation":
                        return _DefaultVisitChildTokens(new QueryContinuationAspect(token));
                    case "assignment":
                        return _DefaultVisitChildTokens(new AssignmentExpressionAspect(token));
                    case "assignment-operator":
                        return new OperatorAspect(token);
                    case "boolean-literal":
                        return _DefaultVisitChildTokens(new LiteralExpressionAspect(token, LiteralExpressionTypes.Boolean));
                    case "hexadecimal-integer-literal":
                        return _DefaultVisitChildTokens(new LiteralExpressionAspect(token, LiteralExpressionTypes.Hexadecimal));
                    case "decimal-integer-literal":
                        return _DefaultVisitChildTokens(new LiteralExpressionAspect(token, LiteralExpressionTypes.Decimal));
                    case "real-literal":
                        return _DefaultVisitChildTokens(new LiteralExpressionAspect(token, LiteralExpressionTypes.Real));
                    case "string-literal":
                        return _DefaultVisitChildTokens(new LiteralExpressionAspect(token, LiteralExpressionTypes.String));
                    case "null-literal":
                        return _DefaultVisitChildTokens(new LiteralExpressionAspect(token, LiteralExpressionTypes.Null));
                    case "character-literal":
                        return _DefaultVisitChildTokens(new LiteralExpressionAspect(token, LiteralExpressionTypes.Character));
#endregion expression
#region namespace
#endregion namespace
#region statements
                    case "yield-statement":
                        return _DefaultVisitChildTokens(new YieldStatementAspect(token));
                    case "empty-statement":
                        return _DefaultVisitChildTokens(new EmptyStatementAspect(token));
                    case "checked-statement":
                        return _DefaultVisitChildTokens(new CheckedStatementAspect(token));
                    case "unchecked-statement":
                        return _DefaultVisitChildTokens(new UnCheckedStatementAspect(token));
                    case "lock-statement":
                        return _DefaultVisitChildTokens(new LockStatementAspect(token));
                    case "expression-statement":
                        return _DefaultVisitChildTokens(new ExpressionStatementAspect(token));
                    case "using-statement":
                        return _DefaultVisitChildTokens(new UsingStatementAspect(token));
                    case "break-statement":
                        return _DefaultVisitChildTokens(new BreakStatementAspect(token));
                    case "continue-statement":
                        return _DefaultVisitChildTokens(new ContinueStatementAspect(token));
                    case "simple-goto-statement":
                        return _DefaultVisitChildTokens(new SimpleGotoStatementAspect(token));
                    case "switch-goto-statement":
                        return _DefaultVisitChildTokens(new SwitchGotoStatementAspect(token));
                    case "return-statement":
                        return _DefaultVisitChildTokens(new ReturnStatementAspect(token));
                    case "throw-statement":
                        return _DefaultVisitChildTokens(new ThrowStatementAspect(token));
                    case "do-statement":
                        return _DefaultVisitChildTokens(new DoStatementAspect(token));
                    case "while-statement":
                        return _DefaultVisitChildTokens(new WhileStatementAspect(token));
                    case "for-statement":
                        return _DefaultVisitChildTokens(new ForStatementAspect(token));
                    case "for-initializer":
                        return _DefaultVisitChildTokens(new ForInitializerAspect(token));
                    case "for-condition":
                        return _DefaultVisitChildTokens(new ForConditionAspect(token));
                    case "for-iterator":
                        return _DefaultVisitChildTokens(new ForIteratorAspect(token));
                    case "foreach-statement":
                        return _DefaultVisitChildTokens(new ForeachStatementAspect(token));
                    case "if-statement":
                        return _DefaultVisitChildTokens(new IfStatementAspect(token));
                    case "switch-statement":
                        return _DefaultVisitChildTokens(new SwitchStatementAspect(token));
                    case "switch-section":
                        return _DefaultVisitChildTokens(new SwitchSectionAspect(token));
                    case "switch-label":
                        return _DefaultVisitChildTokens(new SwitchLabelAspect(token));
                    case "try-statement":
                        return _DefaultVisitChildTokens(new TryStatementAspect(token));
                    case "general-catch-clause":
                    case "specific-catch-clause":
                        return _DefaultVisitChildTokens(new CatchClauseAspect(token));
                    case "finally-clause":
                        return _DefaultVisitChildTokens(new FinallyClauseAspect(token));
#endregion statements
#region declaration
                    case "type-parameter-list":
                        return _DefaultVisitChildTokens(new TypeParameterListAspect(token));
                    case "array-initializer":
                        return _DefaultVisitChildTokens(new ArrayInitializerAspect(token));
                    case "variable-initializer":
                        return _DefaultVisitChildTokens(new VariableInitializerAspect(token));
                    case "attribute-section":
                        return _DefaultVisitChildTokens(new AttributeSectionAspect(token));
                    case "attribute-target":
                        return _DefaultVisitChildTokens(new AttributeTargetSpecifierAspect(token));
                    case "attribute":
                        return _DefaultVisitChildTokens(new AttributeAspect(token));
                    case "attribute-arguments":
                        return _DefaultVisitChildTokens(new AttributeArgumentListAspect(token));
                    case "type-parameter-constraints-clause":
                        return _DefaultVisitChildTokens(new TypeParameterConstraintsClauseAspect(token));
                    case "accessor-modifier":
                    case "delegate-modifier":
                    case "field-modifier":
                    case "constant-modifier":
                    case "class-modifier":
                    case "method-modifier":
                    case "property-modifier":
                    case "constructor-modifier":
                    case "event-modifier":
                    case "indexer-modifier":
                    case "operator-modifier":
                    case "struct-modifier":
                    case "interface-modifier":
                    case "enum-modifier":
                    case "destructor-modifiers":
                    case "static-constructor-extern-modifier":
                    case "static-constructor-static-modifier":
                    case "interface-member-modifier":
                        return _DefaultVisitChildTokens(new ModifierAspect(token));
                    case "type-parameter-item":
                        return _DefaultVisitChildTokens(new TypeParameterItemAspect(token));
                    case "type-parameter-constraint-type":
                        return _DefaultVisitChildTokens(new TypeParameterConstraintTypeAspect(token));
                    case "type-parameter-constraint-class-or-struct":
                        return _DefaultVisitChildTokens(new TypeParameterConstraintClassOrStructAspect(token, (KeywordAspect)_VisitKeyword(token)));
                    case "constructor-constraint":
                        return _DefaultVisitChildTokens(new TypeParameterConstraintConstructutorAspect(token));
                    case "field-declaration":
                        return _DefaultVisitChildTokens(new FieldDeclarationAspect(token));
                    case "variable-declarator":
                        return _DefaultVisitChildTokens(new VariableDeclaratorAspect(token));
                    case "constant-declaration":
                        return _DefaultVisitChildTokens(new ConstantDeclarationAspect(token));
                    case "constant-declarator":
                        return _DefaultVisitChildTokens(new ConstantDeclaratorAspect(token));
                    case "method-declaration":
                        return _DefaultVisitChildTokens(new MethodDeclarationAspect(token));
                    case "return-type":
                        return _DefaultVisitChildTokens(new ReturnTypeAspect(token));
                    case "member-name":
                        return _DefaultVisitChildTokens(new MemberNameAspect(token));
                    case "formal-parameter-list":
                        return _DefaultVisitChildTokens(new FormalParameterListAspect(token));
                    case "parameter-modifier":
                        return _DefaultVisitChildTokens(new ParameterModifierAspect(token));
                    case "fixed-parameter":
                        return _DefaultVisitChildTokens(new FixedParameterAspect(token));
                    case "parameter-array":
                        return _DefaultVisitChildTokens(new ParameterArrayAspect(token));
                    case "indexer-declaration":
                        return _DefaultVisitChildTokens(new IndexerDeclarationAspect(token));
                    case "property-declaration":
                        return _DefaultVisitChildTokens(new PropertyDeclarationAspect(token));
                    case "get-accessor-declaration":
                        return _DefaultVisitChildTokens(new PropertyAccessorAspect(token, PropertyAccessorTypesAspect.Get));
                    case "set-accessor-declaration":
                        return _DefaultVisitChildTokens(new PropertyAccessorAspect(token, PropertyAccessorTypesAspect.Set));
                    case "constructor-declaration":
                        return _DefaultVisitChildTokens(new ConstructorDeclarationAspect(token));
                    case "constructor-initializer":
                        return _DefaultVisitChildTokens(new ConstructorInitializerAspect(token));
                    case "constructor-initializer-modifier":
                        return _DefaultVisitChildTokens(new ConstructorInitializerModifierAspect(token));
                    case "static-constructor-declaration":
                        return _DefaultVisitChildTokens(new StaticConstructorDeclarationAspect(token));
                    case "delegate-declaration":
                        return _DefaultVisitChildTokens(new DelegateDeclarationAspect(token));
                    case "destructor-declaration":
                        return _DefaultVisitChildTokens(new DestructorDeclarationAspect(token));
                    case "event-field-declaration":
                        return _DefaultVisitChildTokens(new EventFieldDeclarationAspect(token));
                    case "event-property-declaration":
                        return _DefaultVisitChildTokens(new EventPropertyDeclarationAspect(token));
                    case "add-accessor-declaration":
                        return _DefaultVisitChildTokens(new EventAccessorAspect(token, EventAccessorTypesAspect.Add));
                    case "remove-accessor-declaration":
                        return _DefaultVisitChildTokens(new EventAccessorAspect(token, EventAccessorTypesAspect.Remove));
                    case "indexer-explicit-type":
                        return _DefaultVisitChildTokens(new IndexerExplicitTypeAspect(token));
                    case "unary-operator-declarator":
                        return _DefaultVisitChildTokens(new UnaryOperatorDeclaratorAspect(token));
                    case "binary-operator-declarator":
                        return _DefaultVisitChildTokens(new BinaryOperatorDeclaratorAspect(token));
                    case "conversion-operator-declarator":
                        return _DefaultVisitChildTokens(new ConversionOperationDeclaratorAspect(token));
                    case "overloadable-unary-operator":
                        return _DefaultVisitChildTokens(new OverloadableUnaryOperatorAspect(token));
                    case "overloadable-binary-operator":
                        return _DefaultVisitChildTokens(new OverloadableBinaryOperatorAspect(token));
                    case "conversion-operator-type":
                        return _DefaultVisitChildTokens(new ConvertsionOperatorType(token));
                    case "operator-param-type1":
                        return _DefaultVisitChildTokens(new OperatorType1Aspect(token));
                    case "operator-param-identifier1":
                        return _DefaultVisitChildTokens(new OperatorIdentifier1Aspect(token));
                    case "operator-param-type2":
                        return _DefaultVisitChildTokens(new OperatorType2Aspect(token));
                    case "operator-param-identifier2":
                        return _DefaultVisitChildTokens(new OperatorIdentifier2Aspect(token));
                    case "class-declaration":
                        return _DefaultVisitChildTokens(new ClassDeclarationAspect(token));
                    case "class-modifiers":
                        return _DefaultVisitChildTokens(new ModifierAspect(token));
                    case "constant-keyword":
                        return _DefaultVisitChildTokens(new ModifierAspect(token));
                    case "class-base":
                        return _DefaultVisitChildTokens(new BaseListAspect(token));
                    case "base-type":
                    case "interface-type":
                        return _DefaultVisitChildTokens(new BaseTypeAspect(token));
                    case "struct-declaration":
                        return _DefaultVisitChildTokens(new StructDeclarationAspect(token));
                    case "struct-interfaces":
                        return _DefaultVisitChildTokens(new StructInterfacesAspect(token));
                    case "interface-declaration":
                        return _DefaultVisitChildTokens(new InterfaceDeclarationAspect(token));
                    case "variant-type-parameter-list":
                        return _DefaultVisitChildTokens(new VariantTypeParameterListAspect(token));
                    case "variant-type-parameter":
                        return _DefaultVisitChildTokens(new VariantTypeParameterAspect(token));
                    case "variance-annotation":
                        return _DefaultVisitChildTokens(new VarianceAnnotationAspect(token));
                    case "interface-method-declaration":
                        return _DefaultVisitChildTokens(new InterfaceMethodAspect(token));
                    case "interface-property-declaration":
                        return _DefaultVisitChildTokens(new InterfacePropertyAspect(token));
                    case "interface-get-accessor":
                        return _DefaultVisitChildTokens(new InterfaceAccessorAspect(token, PropertyAccessorTypesAspect.Get));
                    case "interface-set-accessor":
                        return _DefaultVisitChildTokens(new InterfaceAccessorAspect(token, PropertyAccessorTypesAspect.Set));
                    case "interface-event-declaration":
                        return _DefaultVisitChildTokens(new InterfaceEventAspect(token));
                    case "interface-indexer-declaration":
                        return _DefaultVisitChildTokens(new InterfaceIndexerAspect(token));
                    case "enum-declaration":
                        return _DefaultVisitChildTokens(new EnumDeclarationAspect(token));
                    case "enum-base":
                        return _DefaultVisitChildTokens(new EnumBaseAspect(token));
                    case "enum-base-type":
                        return _VisitNoneArrayType(token);
                    case "enum-modifiers":
                        return _DefaultVisitChildTokens(new EnumBaseAspect(token));
                    case "enum-member-declaration":
                        return _DefaultVisitChildTokens(new EnumMemberDeclarationApsect(token));
#endregion declaration
                    case null:
                        return _DefaultVisitChildTokens(_CreateTokenError(token, (string)token.Value));
                    default:
                        return _CreateTokenError(token, token.Name);
                }
            }
            else
            {
                var tokenError = new TokenErrorAspect(token, AspectDNErrorFactory.GetCompilerError("CS5TokenError", token.GetSourceLocation(), token.Value));
                _Errors.Add(tokenError);
                return tokenError;
            }
        }

        TokenErrorAspect _CreateTokenError(ISynToken synToken, string value)
        {
            var tokenError = new TokenErrorAspect(synToken, AspectDNErrorFactory.GetCompilerError("CS5TokenError", synToken.GetSourceLocation(), value));
            _Errors.Add(tokenError);
            return tokenError;
        }

        AspectNode _VisitBinaryExpression(ISynToken token)
        {
            AspectNode expression = null;
            if (token.Children.Count() == 1)
                expression = _VisitToken(token.Children[0]);
            else
            {
                expression = new BinaryExpressionAspect(token);
                foreach (ISynToken child in token.Children)
                    expression.AddNode(_VisitToken(child));
            }

            return expression;
        }

        AspectNode _VisitBinaryConditionalExpression(ISynToken token)
        {
            AspectNode expression = null;
            if (token.Children.Count() == 1)
                expression = _VisitToken(token.Children[0]);
            else
            {
                expression = new BinaryConditionalExpressionAspect(token);
                foreach (ISynToken child in token.Children)
                    expression.AddNode(_VisitToken(child));
            }

            return expression;
        }

        AspectNode _VisitConditionalExpression(ISynToken token)
        {
            AspectNode expression = null;
            if (token.Children.Count() == 1)
                expression = _VisitToken(token.Children[0]);
            else
            {
                expression = new ConditionalExpressionAspect(token);
                foreach (ISynToken child in token.Children)
                    expression.AddNode(_VisitToken(child));
            }

            return expression;
        }

        ISynToken _TruncateToken(ISynToken token)
        {
            while (token.Children.Count() == 1)
                token = token[0];
            return token;
        }

        IdentifierNameAspect _VisitIdentifier(ISynToken token)
        {
            return new IdentifierNameAspect(token);
        }

        LiteralExpressionAspect _VisitPrototypeArgumentIndex(ISynToken token)
        {
            return new LiteralExpressionAspect(token.Children.First(), LiteralExpressionTypes.Integer);
        }

        AspectNode _VisitNoneArrayType(ISynToken token)
        {
            switch (token.Value)
            {
                case "object":
                case "dynamic":
                case "string":
                case "bool":
                case "decimal":
                case "sbyte":
                case "byte":
                case "short":
                case "ushort":
                case "int":
                case "uint":
                case "long":
                case "ulong":
                case "char":
                case "float":
                case "double":
                    PredefinedTypeAspect predefinedType = new PredefinedTypeAspect(token, new KeywordAspect(token, (Keywords)Enum.Parse(typeof(Keywords), token.Value.ToUpper())));
                    return predefinedType;
                case "void":
                    predefinedType = new PredefinedTypeAspect(token, new KeywordAspect(token, (Keywords)Enum.Parse(typeof(Keywords), token.Value.ToUpper())));
                    return predefinedType;
                default:
                    return _VisitToken(token.Children[0]);
            }
        }

        AspectNode _DefaultVisitChildTokens(AspectNode parent)
        {
            foreach (ISynToken token in parent.SynToken.Children)
                parent.AddNode(_VisitToken(token));
            return parent;
        }

        AspectNode _VisitKeyword(ISynToken token)
        {
            switch (token.Value)
            {
                case "abstract":
                case "as":
                case "base":
                case "bool":
                case "break":
                case "byte":
                case "case":
                case "catch":
                case "char":
                case "checked":
                case "class":
                case "const":
                case "continue":
                case "decimal":
                case "default":
                case "delegate":
                case "do":
                case "double":
                case "else":
                case "enum":
                case "event":
                case "explicit":
                case "extern":
                case "finally":
                case "fixed":
                case "float":
                case "for":
                case "foreach":
                case "goto":
                case "if":
                case "implicit":
                case "in":
                case "int":
                case "interface":
                case "is":
                case "lock":
                case "long":
                case "namespace":
                case "new":
                case "object":
                case "operator":
                case "out":
                case "override":
                case "params":
                case "private":
                case "protected":
                case "public":
                case "readonly":
                case "ref":
                case "return":
                case "sbyte":
                case "sealed":
                case "short":
                case "sizeof":
                case "stackalloc":
                case "static":
                case "string":
                case "struct":
                case "switch":
                case "this":
                case "throw":
                case "try":
                case "typeof":
                case "uint":
                case "ulong":
                case "unchecked":
                case "unsafe":
                case "ushort":
                case "using":
                case "virtual":
                case "volatile":
                case "while":
                    return new KeywordAspect(token, (Keywords)Enum.Parse(typeof(Keywords), token.Value.ToUpper()));
                case "ascending":
                case "descending":
                    return new OrderingDirectionAspect(token, (OrderingDirections)Enum.Parse(typeof(OrderingDirections), token.Value.ToUpper()));
                default:
                    throw new NotImplementedException();
            }
        }

#region ITokenVisitor
        object ITokenVisitor.Visit(ISynToken token) => Visit(token);
#endregion
    }
}