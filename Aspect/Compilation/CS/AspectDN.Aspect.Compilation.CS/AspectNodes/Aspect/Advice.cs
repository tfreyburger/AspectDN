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
using AspectDN.Aspect.Compilation.Foundation;
using TokenizerDN.Common.SourceAnalysis;
using Microsoft.CodeAnalysis;

namespace AspectDN.Aspect.Compilation.CS
{
    internal abstract class AdviceDeclarationAspect : PackageMemberAspect
    {
        internal IEnumerable<PrototypeMemberDeclarationAspect> PrototypeMembers => CSAspectCompilerHelper.GetDescendingNodesOfType<PrototypeMemberDeclarationAspect>(this);
        internal IdentifierNameAspect Identifier { get => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this).FirstOrDefault(); }

        internal List<AspectMemberDeclarationAspect> AspectReferences { get; }

        internal List<string> PrototypeItems { get; }

        internal string Fullname
        {
            get
            {
                StringBuilder sb = new StringBuilder(Name);
                var package = CSAspectCompilerHelper.GetAscendingNodesOfType<PackageDeclarationAspect>(this).FirstOrDefault();
                if (package != null)
                    sb.Insert(0, '.').Insert(0, package.PackageFullName);
                return sb.ToString();
            }
        }

        internal string Name
        {
            get
            {
                var identifier = ChildAspectNodes.OfType<NameAspect>().FirstOrDefault();
                if (identifier != null)
                    return identifier.TokenValue;
                return null;
            }
        }

        internal override CSAspectNodeTypes CSAspectNodeType => CSAspectNodeTypes.Advice;
        internal AdviceDeclarationAspect(ISynToken token) : base(token)
        {
            AspectReferences = new List<AspectMemberDeclarationAspect>();
            PrototypeItems = new List<string>();
        }
    }

    internal class AdviceTypeMembersDeclarationAspect : AdviceDeclarationAspect
    {
        internal IEnumerable<TypeMemberDeclarationAspect> Members { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeMemberDeclarationAspect>(this); }

        internal AdviceTypeMembersDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AdviceTypeMembersDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AdviceCodeDeclarationAspect : AdviceDeclarationAspect
    {
        internal IEnumerable<StatementAspect> Statements { get => CSAspectCompilerHelper.GetDescendingNodesOfType<StatementAspect>(this); }

        internal AdviceCodeDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AdviceCodeDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AdviceChangeValueDeclarationAspect : AdviceDeclarationAspect
    {
        internal TypeAspect Type { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeAspect>(this).FirstOrDefault(); }

        internal IEnumerable<StatementAspect> Statements { get => CSAspectCompilerHelper.GetDescendingNodesOfType<StatementAspect>(this); }

        internal AdviceChangeValueDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AdviceChangeValueDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AdviceInterfaceMembersDeclarationAspect : AdviceDeclarationAspect
    {
        internal IEnumerable<InterfaceMemberAspect> Members { get => CSAspectCompilerHelper.GetDescendingNodesOfType<InterfaceMemberAspect>(this); }

        internal AdviceInterfaceMembersDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AdviceInterfaceMembersDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AdviceEnumMembersDeclarationAspect : AdviceDeclarationAspect
    {
        internal IEnumerable<EnumMemberDeclarationApsect> Members { get => CSAspectCompilerHelper.GetDescendingNodesOfType<EnumMemberDeclarationApsect>(this); }

        internal AdviceEnumMembersDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AdviceEnumMembersDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AdviceConstructorDeclarationAspect : TypeMemberDeclarationAspect
    {
        internal IEnumerable<AttributeSectionAspect> AttributeSections { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeSectionAspect>(this); }

        internal IEnumerable<ModifierAspect> Modifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ModifierAspect>(this, false).ToList(); }

        internal FormalParameterListAspect ParameterList { get => CSAspectCompilerHelper.GetDescendingNodesOfType<FormalParameterListAspect>(this).FirstOrDefault(); }

        internal ConstructorInitializerAspect ConstructorInitializer { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ConstructorInitializerAspect>(this).FirstOrDefault(); }

        internal BlockAspect Block { get => CSAspectCompilerHelper.GetDescendingNodesOfType<BlockAspect>(this).FirstOrDefault(); }

        internal AdviceConstructorDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AdviceConstructorDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AdviceDestructorDeclarationAspect : TypeMemberDeclarationAspect
    {
        internal IEnumerable<AttributeSectionAspect> AttributeSections { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeSectionAspect>(this, false); }

        internal IEnumerable<ModifierAspect> Modifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ModifierAspect>(this, false).ToList(); }

        internal BlockAspect Block { get => CSAspectCompilerHelper.GetDescendingNodesOfType<BlockAspect>(this).FirstOrDefault(); }

        internal AdviceDestructorDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AdviceDestructorDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AdviceStaticConstructorDeclarationAspect : TypeMemberDeclarationAspect
    {
        internal IEnumerable<AttributeSectionAspect> AttributeSections { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeSectionAspect>(this); }

        internal IEnumerable<ModifierAspect> Modifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ModifierAspect>(this, false).ToList(); }

        internal BlockAspect Block { get => CSAspectCompilerHelper.GetDescendingNodesOfType<BlockAspect>(this).FirstOrDefault(); }

        internal AdviceStaticConstructorDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AdviceStaticConstructorDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AdviceTypesDeclarationAspect : AdviceDeclarationAspect
    {
        internal IEnumerable<TypeMemberDeclarationAspect> TypeMembers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeMemberDeclarationAspect>(this, false); }

        internal AdviceTypesDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AdviceTypeDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AdviceAttributesDeclarationAspect : AdviceDeclarationAspect
    {
        internal IEnumerable<AttributeSectionAspect> Attributes { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeSectionAspect>(this, false); }

        internal AdviceAttributesDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AdviceAttributesDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AdviceUnaryOperatorDeclaratorAspect : OperatorDeclarationAspect
    {
        internal IEnumerable<AttributeSectionAspect> AttributeSections { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeSectionAspect>(this); }

        internal IEnumerable<ModifierAspect> Modifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ModifierAspect>(this, false).ToList(); }

        internal ReturnTypeAspect ReturnType { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ReturnTypeAspect>(this).FirstOrDefault(); }

        internal OverloadableUnaryOperatorAspect Operator { get => CSAspectCompilerHelper.GetDescendingNodesOfType<OverloadableUnaryOperatorAspect>(this).FirstOrDefault(); }

        internal AdviceOperatorDeclaratorParameterAspect Parameter { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AdviceOperatorDeclaratorParameterAspect>(this).FirstOrDefault(); }

        internal BlockAspect Block { get => CSAspectCompilerHelper.GetDescendingNodesOfType<BlockAspect>(this).FirstOrDefault(); }

        internal AdviceUnaryOperatorDeclaratorAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AdviceUnaryOperatorDeclarator(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AdviceBinaryOperatorDeclaratorAspect : OperatorDeclarationAspect
    {
        internal IEnumerable<AttributeSectionAspect> AttributeSections { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeSectionAspect>(this); }

        internal IEnumerable<ModifierAspect> Modifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ModifierAspect>(this, false).ToList(); }

        internal ReturnTypeAspect ReturnType { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ReturnTypeAspect>(this).FirstOrDefault(); }

        internal OverloadableBinaryOperatorAspect Operator { get => CSAspectCompilerHelper.GetDescendingNodesOfType<OverloadableBinaryOperatorAspect>(this).FirstOrDefault(); }

        internal AdviceOperatorDeclaratorParameterAspect Parameter1 { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AdviceOperatorDeclaratorParameterAspect>(this).FirstOrDefault(); }

        internal AdviceOperatorDeclaratorParameterAspect Parameter2 { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AdviceOperatorDeclaratorParameterAspect>(this, false, Parameter1).FirstOrDefault(); }

        internal BlockAspect Block { get => CSAspectCompilerHelper.GetDescendingNodesOfType<BlockAspect>(this).FirstOrDefault(); }

        internal AdviceBinaryOperatorDeclaratorAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AdviceBinaryOperatorDeclaratorAspect(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AdviceConversionOperatorDeclaratorAspect : OperatorDeclarationAspect
    {
        internal IEnumerable<AttributeSectionAspect> AttributeSections { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeSectionAspect>(this); }

        internal IEnumerable<ModifierAspect> Modifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ModifierAspect>(this, false).ToList(); }

        internal ReturnTypeAspect ReturnType { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ReturnTypeAspect>(this).FirstOrDefault(); }

        internal ConvertsionOperatorType Operator { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ConvertsionOperatorType>(this).FirstOrDefault(); }

        internal AdviceOperatorDeclaratorParameterAspect Parameter { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AdviceOperatorDeclaratorParameterAspect>(this).FirstOrDefault(); }

        internal BlockAspect Block { get => CSAspectCompilerHelper.GetDescendingNodesOfType<BlockAspect>(this).FirstOrDefault(); }

        internal AdviceConversionOperatorDeclaratorAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AdviceConversionOperatorDeclaratorAspect(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AdviceOperatorDeclaratorParameterAspect : CSAspectNode
    {
        internal NameAspect Type => ChildAspectNodes.Count() > 1 ? (NameAspect)ChildAspectNodes[0] : null;
        internal IdentifierNameAspect Identifier => (IdentifierNameAspect)(ChildAspectNodes.Count() > 1 ? ChildAspectNodes[1] : ChildAspectNodes.FirstOrDefault());
        internal AdviceOperatorDeclaratorParameterAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AdviceOperatorDeclaratorParameterAspect(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }
}