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
using TokenizerDN.Common.SourceAnalysis;
using AspectDN.Aspect.Compilation.Foundation;
using Microsoft.CodeAnalysis;

namespace AspectDN.Aspect.Compilation.CS
{
    internal abstract class AspectMemberDeclarationAspect : PackageMemberAspect
    {
        internal string Fullname
        {
            get
            {
                var packageDeclaration = CSAspectCompilerHelper.GetAscendingNodesOfType<PackageDeclarationAspect>(this, true).LastOrDefault();
                if (packageDeclaration != null)
                {
                    return $"{packageDeclaration.PackageFullName}.{Identifier.TokenValue}";
                }
                else
                    return Identifier.TokenValue;
            }
        }

        internal IdentifierNameAspect Identifier { get => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this).FirstOrDefault(); }

        internal IEnumerable<PrototypeMappingItemAspect> PrototypeMappingItems { get => CSAspectCompilerHelper.GetDescendingNodesOfType<PrototypeMappingItemAspect>(this); }

        internal AspectPointcutAspect Pointcut { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AspectPointcutAspect>(this).FirstOrDefault(); }

        internal AspectAdviceAspect Advice => CSAspectCompilerHelper.GetDescendingNodesOfType<AspectAdviceAspect>(this).FirstOrDefault();
        internal AspectMemberDeclarationAspect(ISynToken token) : base(token)
        {
        }
    }

    internal interface IAspectAdviceAnnonymousAspect
    {
        AspectMemberDeclarationAspect ParentAspectMember { get; }
    }

    ;
    internal abstract class AspectAdviceAspect : CSAspectNode
    {
        internal AspectMemberDeclarationAspect ParentAspectMember { get => CSAspectCompilerHelper.GetAscendingNodesOfType<AspectMemberDeclarationAspect>(this).FirstOrDefault(); }

        internal AspectAdviceAspect(ISynToken token) : base(token)
        {
        }
    }

#region InheritAspect
    internal class InheritDeclarationAspect : AspectMemberDeclarationAspect
    {
        internal IEnumerable<BaseTypeAspect> BaseTypes { get => CSAspectCompilerHelper.GetDescendingNodesOfType<BaseTypeAspect>(this); }

        internal IEnumerable<OverrideSpecificConstructorDeclarationAspect> OverrideConstructorsDeclarations { get => CSAspectCompilerHelper.GetDescendingNodesOfType<OverrideSpecificConstructorDeclarationAspect>(this, false); }

        internal InheritDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AspectInheritDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

#endregion
#region CodeAspect
    internal abstract class AspectAdviceCodeAspect : AspectAdviceAspect
    {
        internal AspectAdviceCodeAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class AspectAdviceCodeNamedAspect : AspectAdviceCodeAspect
    {
        internal NameAspect Name => (NameAspect)ChildAspectNodes.FirstOrDefault();
        internal AspectAdviceCodeNamedAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AspectAdviceCodeNamed(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AspectAdviceCodeAnonymousAspect : AspectAdviceCodeAspect, IAspectAdviceAnnonymousAspect
    {
        internal AspectCodeDeclarationAspect ParentDeclarator { get => CSAspectCompilerHelper.GetAscendingNodesOfType<AspectCodeDeclarationAspect>(this, false).FirstOrDefault(); }

        internal IEnumerable<PrototypeMemberDeclarationAspect> PrototypeMembers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<PrototypeMemberDeclarationAspect>(this); }

        internal IEnumerable<StatementAspect> Statements { get => CSAspectCompilerHelper.GetDescendingNodesOfType<StatementAspect>(this); }

        internal AspectPointcutThisCodeAnonymousAspect ThisPointcut => CSAspectCompilerHelper.GetDescendingNodesOfType<AspectPointcutThisCodeAnonymousAspect>(ParentAspectMember, true).FirstOrDefault();
        internal AspectAdviceCodeAnonymousAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AspectAdviceCodeAnonymous(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }

        AspectMemberDeclarationAspect IAspectAdviceAnnonymousAspect.ParentAspectMember => ParentAspectMember;
    }

    internal class AspectAdviceInheritAnonymousAspect : AspectAdviceCodeAspect, IAspectAdviceAnnonymousAspect
    {
        internal InheritDeclarationAspect ParentDeclarator { get => CSAspectCompilerHelper.GetAscendingNodesOfType<InheritDeclarationAspect>(this, false).FirstOrDefault(); }

        internal IEnumerable<BaseTypeAspect> BaseTypes { get => CSAspectCompilerHelper.GetDescendingNodesOfType<BaseTypeAspect>(this); }

        internal IEnumerable<PrototypeMemberDeclarationAspect> PrototypeMembers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<PrototypeMemberDeclarationAspect>(this); }

        internal AspectAdviceInheritAnonymousAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AspectAdviceInheritAnonymous(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }

        AspectMemberDeclarationAspect IAspectAdviceAnnonymousAspect.ParentAspectMember => ParentAspectMember;
    }

    internal class AspectCodeDeclarationAspect : AspectMemberDeclarationAspect
    {
        internal ExecutionTimeAspect ExecutionTime { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ExecutionTimeAspect>(this).FirstOrDefault(); }

        internal ControlFlowsAspect ControlFlows { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ControlFlowsAspect>(this).FirstOrDefault(); }

        internal AspectCodeDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AspectCodeDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

#endregion
#region AttributeAspect
    internal class AspectAttributesDeclarationAspect : AspectMemberDeclarationAspect
    {
        internal AspectAdviceAttributesAspect AdviceAttributes { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AspectAdviceAttributesAspect>(this).FirstOrDefault(); }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AspectAttributesDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }

        internal AspectAttributesDeclarationAspect(ISynToken token) : base(token)
        {
        }
    }

    internal abstract class AspectAdviceAttributesAspect : AspectAdviceAspect
    {
        internal AspectAdviceAttributesAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class AspectAdviceAttributesNamedAspect : AspectAdviceAttributesAspect
    {
        internal NameAspect Name => (NameAspect)ChildAspectNodes.FirstOrDefault();
        internal AspectTypesDeclarationAspect ParentDeclarator { get => CSAspectCompilerHelper.GetAscendingNodesOfType<AspectTypesDeclarationAspect>(this, false).FirstOrDefault(); }

        internal AspectAdviceAttributesNamedAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AspectAdviceAttributesNamed(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AspectAdviceAsttributesAnonymousAspect : AspectAdviceAttributesAspect, IAspectAdviceAnnonymousAspect
    {
        internal IEnumerable<AttributeSectionAspect> Attributes { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeSectionAspect>(this, false); }

        internal IEnumerable<PrototypeMemberDeclarationAspect> PrototypeMembers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<PrototypeMemberDeclarationAspect>(this); }

        internal AspectAdviceAsttributesAnonymousAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AspectAdviceAttributesAnonymous(this);
        }

        AspectMemberDeclarationAspect IAspectAdviceAnnonymousAspect.ParentAspectMember => ParentAspectMember;
    }

#endregion
#region ConstructorOverloading
    internal class OverrideSpecificConstructorDeclarationAspect : CSAspectNode
    {
        internal IEnumerable<FormalParameterListAspect> OverrideSpecficConstructors => CSAspectCompilerHelper.GetDescendingNodesOfType<FormalParameterListAspect>(this, false);
        internal ArgumentListAspect BaseConstructorParameters => CSAspectCompilerHelper.GetDescendingNodesOfType<ArgumentListAspect>(this, false).FirstOrDefault();
        internal OverrideSpecificConstructorDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            throw new NotSupportedException();
        }
    }

#endregion
#region ChangeValue
    internal class AspectChangeValueDeclarationAspect : AspectMemberDeclarationAspect
    {
        internal ExecutionTimeAspect ExecutionTime { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ExecutionTimeAspect>(this).FirstOrDefault(); }

        internal ControlFlowsAspect ControlFlows { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ControlFlowsAspect>(this).FirstOrDefault(); }

        internal AspectAdviceChangeValueAspect AdviceChangeValue { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AspectAdviceChangeValueAspect>(this).FirstOrDefault(); }

        internal AspectChangeValueDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AspectChangeValueDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal abstract class AspectAdviceChangeValueAspect : AspectAdviceAspect
    {
        internal AspectAdviceChangeValueAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class AspectAdviceChangeValueNamedAspect : AspectAdviceChangeValueAspect
    {
        internal NameAspect Name => (NameAspect)ChildCSAspectNodes.FirstOrDefault();
        internal AspectAdviceChangeValueNamedAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AspectAdviceChangeValueNamed(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AspectAdviceChangeValueAnonymousAspect : AspectAdviceChangeValueAspect, IAspectAdviceAnnonymousAspect
    {
        internal AspectChangeValueDeclarationAspect ParentDeclarator { get => CSAspectCompilerHelper.GetAscendingNodesOfType<AspectChangeValueDeclarationAspect>(this, false).FirstOrDefault(); }

        internal TypeAspect Type { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeAspect>(this).FirstOrDefault(); }

        internal IEnumerable<PrototypeMemberDeclarationAspect> PrototypeItems { get => CSAspectCompilerHelper.GetDescendingNodesOfType<PrototypeMemberDeclarationAspect>(this); }

        internal IEnumerable<StatementAspect> Statements { get => CSAspectCompilerHelper.GetDescendingNodesOfType<StatementAspect>(this); }

        internal AspectPointcutThisCodeAnonymousAspect ThisPointcut => CSAspectCompilerHelper.GetDescendingNodesOfType<AspectPointcutThisCodeAnonymousAspect>(ParentAspectMember, true).FirstOrDefault();
        internal AspectAdviceChangeValueAnonymousAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AspectAdviceChangeValueAnonymous(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }

        AspectMemberDeclarationAspect IAspectAdviceAnnonymousAspect.ParentAspectMember => ParentAspectMember;
    }

#endregion
    internal class AspectTypeMembersDeclarationAspect : AspectMemberDeclarationAspect
    {
        internal NameAspect Name => (NameAspect)ChildCSAspectNodes.FirstOrDefault();
        internal AspectAdviceTypeMembersDeclarationAspect AdivceMember { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AspectAdviceTypeMembersDeclarationAspect>(this).FirstOrDefault(); }

        internal IEnumerable<AspectMemberModifier> AspectMemberModifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AspectMemberModifier>(this); }

        internal AspectTypeMembersDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AspectTypeMembersDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AspectMemberModifier : CSAspectNode
    {
        internal AspectMemberModifier(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AspectMemberModifier(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AspectInterfaceMembersDeclarationAspect : AspectMemberDeclarationAspect
    {
        internal AspectAdviceInterfaceMembersAspect AdivceMember { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AspectAdviceInterfaceMembersAspect>(this).FirstOrDefault(); }

        internal AspectInterfaceMembersDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AspectInterfaceMembersDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AspectEnumMembersDeclarationAspect : AspectMemberDeclarationAspect
    {
        internal AspectAdviceEnumMemberAspect AdivceMember { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AspectAdviceEnumMemberAspect>(this).FirstOrDefault(); }

        internal AspectEnumMembersDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AspectEnumMembersDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class ExecutionTimeAspect : CSAspectNode
    {
        internal ExecutionTimeAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ExecutionTime(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class ControlFlowsAspect : CSAspectNode
    {
        internal IEnumerable<ControlFlowAspect> ControlFlowItems { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ControlFlowAspect>(this, true); }

        internal ControlFlowsAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ControlsFlow(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class ControlFlowAspect : CSAspectNode
    {
        internal ControlFlowAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ControlFlow(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal abstract class AspectPointcutAspect : CSAspectNode
    {
        internal AspectPointcutAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class AspectPointcutNamedAspect : AspectPointcutAspect
    {
        internal NameAspect PointcutName => CSAspectCompilerHelper.GetDescendingNodesOfType<NameAspect>(this, false).FirstOrDefault();
        internal AspectPointcutNamedAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AspectPointcutNamed(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal abstract class AspectPointcutAnonymousAspect : AspectPointcutAspect
    {
        internal AspectMemberDeclarationAspect AspectMember { get => CSAspectCompilerHelper.GetAscendingNodesOfType<AspectMemberDeclarationAspect>(this).FirstOrDefault(); }

        internal AspectPointcutAnonymousAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class AspectPointcutCommonAnonymousAspect : AspectPointcutAnonymousAspect
    {
        internal PointcutTypeAspect PointcutType { get => CSAspectCompilerHelper.GetDescendingNodesOfType<PointcutTypeAspect>(this).FirstOrDefault(); }

        internal PointcutExpressionAspect Expression { get => CSAspectCompilerHelper.GetDescendingNodesOfType<PointcutExpressionAspect>(this).FirstOrDefault(); }

        internal AspectPointcutCommonAnonymousAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AspectPointcutAttributeAnonymous(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AspectPointcutThisTypeMembersAnonymousAspect : AspectPointcutAnonymousAspect
    {
        internal NameAspect PrototypeFullName { get => CSAspectCompilerHelper.GetDescendingNodesOfType<NameAspect>(this).FirstOrDefault(); }

        internal AspectPointcutThisTypeMembersAnonymousAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AspectPointcutAttributeAnonymous(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AspectPointcutThisCodeAnonymousAspect : AspectPointcutAnonymousAspect
    {
        internal NameAspect PrototypeFullName { get => CSAspectCompilerHelper.GetDescendingNodesOfType<NameAspect>(this).FirstOrDefault(); }

        internal PointcutTypeAspect PointcutType { get => CSAspectCompilerHelper.GetDescendingNodesOfType<PointcutTypeAspect>(this).FirstOrDefault(); }

        internal PointcutExpressionAspect Expression { get => CSAspectCompilerHelper.GetDescendingNodesOfType<PointcutExpressionAspect>(this).FirstOrDefault(); }

        internal AspectPointcutThisCodeAnonymousAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AspectPointcutAttributeAnonymous(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal abstract class AspectAdviceTypeMembersDeclarationAspect : AspectAdviceAspect
    {
        internal AspectAdviceTypeMembersDeclarationAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class AspectAdviceTypeMembersNamedAspect : AspectAdviceTypeMembersDeclarationAspect
    {
        internal NameAspect Name => (NameAspect)ChildCSAspectNodes.FirstOrDefault();
        internal AspectAdviceTypeMembersNamedAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AspectAdviceTypeMembersNamed(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AspectAdviceTypeMembersAnonymousAspect : AspectAdviceTypeMembersDeclarationAspect, IAspectAdviceAnnonymousAspect
    {
        internal AspectTypeMembersDeclarationAspect ParentDeclarator { get => CSAspectCompilerHelper.GetAscendingNodesOfType<AspectTypeMembersDeclarationAspect>(this, false).FirstOrDefault(); }

        internal IEnumerable<PrototypeMemberDeclarationAspect> PrototypeMembers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<PrototypeMemberDeclarationAspect>(this); }

        internal IEnumerable<TypeMemberDeclarationAspect> TypeMembers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeMemberDeclarationAspect>(this); }

        internal AspectPointcutThisTypeMembersAnonymousAspect ThisPointcut => CSAspectCompilerHelper.GetDescendingNodesOfType<AspectPointcutThisTypeMembersAnonymousAspect>(ParentDeclarator, true).FirstOrDefault();
        internal AspectAdviceTypeMembersAnonymousAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AspectAdviceTypeMembersAnonymous(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }

        AspectMemberDeclarationAspect IAspectAdviceAnnonymousAspect.ParentAspectMember => ParentAspectMember;
    }

    internal abstract class AspectAdviceInterfaceMembersAspect : AspectAdviceAspect
    {
        internal AspectAdviceInterfaceMembersAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class AspectAdviceInterfaceMembersNamedAspect : AspectAdviceInterfaceMembersAspect
    {
        internal NameAspect Name => (NameAspect)ChildCSAspectNodes.FirstOrDefault();
        internal AspectAdviceInterfaceMembersNamedAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AspectAdviceInterfaceMembersNamed(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AspectAdviceInterfaceMembersAnonymousAspect : AspectAdviceInterfaceMembersAspect, IAspectAdviceAnnonymousAspect
    {
        internal AspectInterfaceMembersDeclarationAspect ParentDeclarator { get => CSAspectCompilerHelper.GetAscendingNodesOfType<AspectInterfaceMembersDeclarationAspect>(this, false).FirstOrDefault(); }

        internal IEnumerable<InterfaceMemberAspect> Members { get => CSAspectCompilerHelper.GetDescendingNodesOfType<InterfaceMemberAspect>(this); }

        internal IEnumerable<PrototypeMemberDeclarationAspect> PrototypeMembers => CSAspectCompilerHelper.GetDescendingNodesOfType<PrototypeMemberDeclarationAspect>(this);
        internal AspectPointcutThisTypeMembersAnonymousAspect ThisPointcut => CSAspectCompilerHelper.GetDescendingNodesOfType<AspectPointcutThisTypeMembersAnonymousAspect>(ParentDeclarator, true).FirstOrDefault();
        internal AspectAdviceInterfaceMembersAnonymousAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AspectAdviceInterfaceMembersAnonymous(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }

        AspectMemberDeclarationAspect IAspectAdviceAnnonymousAspect.ParentAspectMember => ParentAspectMember;
    }

    internal abstract class AspectAdviceEnumMemberAspect : AspectAdviceAspect
    {
        internal AspectAdviceEnumMemberAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class AspectAdviceEnumMembersNamedAspect : AspectAdviceEnumMemberAspect
    {
        internal NameAspect Name => (NameAspect)ChildCSAspectNodes.FirstOrDefault();
        internal AspectAdviceEnumMembersNamedAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AspectAdviceEnumMembersNamed(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AspectAdviceEnumMembersAnonymousAspect : AspectAdviceEnumMemberAspect, IAspectAdviceAnnonymousAspect
    {
        internal AspectEnumMembersDeclarationAspect ParentDeclarator { get => CSAspectCompilerHelper.GetAscendingNodesOfType<AspectEnumMembersDeclarationAspect>(this, false).FirstOrDefault(); }

        internal IEnumerable<EnumMemberDeclarationApsect> Members { get => CSAspectCompilerHelper.GetDescendingNodesOfType<EnumMemberDeclarationApsect>(this); }

        internal AspectAdviceEnumMembersAnonymousAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AspectAdviceEnumMembersAnonymous(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }

        AspectMemberDeclarationAspect IAspectAdviceAnnonymousAspect.ParentAspectMember => ParentAspectMember;
    }

    internal class AspectTypesDeclarationAspect : AspectMemberDeclarationAspect
    {
        internal AspectAdviceTypeMembersDeclarationAspect AdivceMember { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AspectAdviceTypeMembersDeclarationAspect>(this).FirstOrDefault(); }

        internal NameAspect Namespace { get => CSAspectCompilerHelper.GetDescendingNodesOfType<NameAspect>(this, false, Pointcut).FirstOrDefault(); }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AspectTypesDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }

        internal AspectTypesDeclarationAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class AspectAdviceTypeNamedAspect : AspectAdviceTypeMembersDeclarationAspect
    {
        internal NameAspect Name => (NameAspect)ChildCSAspectNodes.FirstOrDefault();
        internal AspectTypesDeclarationAspect ParentDeclarator { get => CSAspectCompilerHelper.GetAscendingNodesOfType<AspectTypesDeclarationAspect>(this, false).FirstOrDefault(); }

        internal AspectAdviceTypeNamedAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AspectAdviceTypeNamed(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AspectAdviceTypeAnonymousAspect : AspectAdviceTypeMembersDeclarationAspect, IAspectAdviceAnnonymousAspect
    {
        internal AspectTypesDeclarationAspect ParentDeclarator { get => CSAspectCompilerHelper.GetAscendingNodesOfType<AspectTypesDeclarationAspect>(this, false).FirstOrDefault(); }

        internal IEnumerable<TypeMemberDeclarationAspect> TypeMembers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeMemberDeclarationAspect>(this, false); }

        internal IEnumerable<PrototypeMemberDeclarationAspect> PrototypeMembers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<PrototypeMemberDeclarationAspect>(this, false); }

        internal AspectAdviceTypeAnonymousAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AspectAdviceTypeAnonymous(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }

        AspectMemberDeclarationAspect IAspectAdviceAnnonymousAspect.ParentAspectMember => ParentAspectMember;
    }

    internal class AroundStatementAspect : EmbeddedStatementAspect
    {
        internal AroundStatementAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AroundStatement(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

#region PrototypeItemDeclaration
#endregion
#region PrototypeTypeDeclaration
    internal abstract class PrototypeTypeDeclarationAspect : PackageMemberAspect
    {
        internal string Fullname => CSAspectCompilerHelper.GetAscendingNodesOfType<PackageDeclarationAspect>(this, true).Last().PackageFullName;
        internal PrototypeTypeDeclarationAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class PrototypeClassDeclarationAspect : PrototypeTypeDeclarationAspect
    {
        internal IdentifierNameAspect IdentifierName { get => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this, false).FirstOrDefault(); }

        internal TypeParameterListAspect TypeParameterList { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeParameterListAspect>(this, false).FirstOrDefault(); }

        internal PrototypeBaseListAspect BaseList { get => CSAspectCompilerHelper.GetDescendingNodesOfType<PrototypeBaseListAspect>(this, false).FirstOrDefault(); }

        internal IEnumerable<TypeParameterConstraintsClauseAspect> TypeParameterConstraintsClauses { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeParameterConstraintsClauseAspect>(this); }

        internal IEnumerable<CSAspectNode> Members
        {
            get
            {
                return CSAspectCompilerHelper.GetDescendingNodesOfType<PrototypeMemberDeclarationAspect>(this).Cast<CSAspectNode>().Union(CSAspectCompilerHelper.GetDescendingNodesOfType<TypeMemberDeclarationAspect>(this).Cast<CSAspectNode>()).Union(CSAspectCompilerHelper.GetDescendingNodesOfType<PrototypeTypeDeclarationAspect>(this).Cast<CSAspectNode>());
            }
        }

        internal IEnumerable<KeywordAspect> Modifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<KeywordAspect>(this); }

        internal bool IsNested { get; }

        internal PrototypeClassDeclarationAspect(ISynToken token, bool isNested) : base(token)
        {
            IsNested = isNested;
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PrototypeClassDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class PrototypeBaseListAspect : CSAspectNode
    {
        internal IEnumerable<PrototypeBaseTypeAspect> BaseTyoes { get => CSAspectCompilerHelper.GetDescendingNodesOfType<PrototypeBaseTypeAspect>(this, false); }

        internal PrototypeBaseListAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PrototypeBaseList(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class PrototypeBaseTypeAspect : CSAspectNode
    {
        internal TypeAspect BaseTyoe { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeAspect>(this, false).FirstOrDefault(); }

        internal PrototypeBaseTypeAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PrototypeBaseType(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal abstract class PrototypeMemberDeclarationAspect : PackageMemberAspect
    {
        internal PrototypeMemberDeclarationAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class PrototypeFieldDeclarationAspect : PrototypeMemberDeclarationAspect
    {
        internal TypeAspect Type { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeAspect>(this, false).FirstOrDefault(); }

        internal IdentifierNameAspect Identifier { get => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this).FirstOrDefault(); }

        internal IEnumerable<KeywordAspect> Modifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<KeywordAspect>(this); }

        internal bool FromPrototypeType { get; }

        internal PrototypeFieldDeclarationAspect(ISynToken token, bool fromPrototypeType) : base(token)
        {
            FromPrototypeType = fromPrototypeType;
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PrototypeFieldDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal abstract class PrototypePropertyBaseDeclarationAspect : PrototypeMemberDeclarationAspect
    {
        internal TypeAspect Type { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeAspect>(this).FirstOrDefault(); }

        internal IEnumerable<KeywordAspect> Modifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<KeywordAspect>(this); }

        internal IEnumerable<PrototypeAccessorAspect> Accessors { get => CSAspectCompilerHelper.GetDescendingNodesOfType<PrototypeAccessorAspect>(this, false); }

        internal bool FromPrototypeType { get; set; }

        internal PrototypePropertyBaseDeclarationAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class PrototypePropertyDeclarationAspect : PrototypePropertyBaseDeclarationAspect
    {
        internal MemberNameAspect MemberName { get => CSAspectCompilerHelper.GetDescendingNodesOfType<MemberNameAspect>(this, false).FirstOrDefault(); }

        internal PrototypePropertyDeclarationAspect(ISynToken token, bool fromPrototypeType) : base(token)
        {
            FromPrototypeType = fromPrototypeType;
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PrototypePropertyDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class PrototypeIndexerDeclarationtAspect : PrototypePropertyBaseDeclarationAspect
    {
        internal FormalParameterListAspect FormalParameterList { get => CSAspectCompilerHelper.GetDescendingNodesOfType<FormalParameterListAspect>(this, false).FirstOrDefault(); }

        internal PrototypeIndexerDeclarationtAspect(ISynToken token, bool fromPrototypeType) : base(token)
        {
            FromPrototypeType = fromPrototypeType;
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PrototypeIndexerDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal abstract class PrototypeAccessorAspect : CSAspectNode
    {
        internal PrototypeAccessorAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class PrototypeGetAccessorAspect : PrototypeAccessorAspect
    {
        internal PrototypeGetAccessorAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PrototypeGetAccessorDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class PrototypeSetAccessorAspect : PrototypeAccessorAspect
    {
        internal PrototypeSetAccessorAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PrototypeSetAccessorDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class PrototypeMethodDeclarationAspect : PrototypeMemberDeclarationAspect
    {
        internal IEnumerable<KeywordAspect> Modifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<KeywordAspect>(this); }

        internal ReturnTypeAspect ReturnType { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ReturnTypeAspect>(this).FirstOrDefault(); }

        internal IdentifierNameAspect Identifier { get => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this).FirstOrDefault(); }

        internal TypeParameterListAspect TypeParameterList { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeParameterListAspect>(this, false).FirstOrDefault(); }

        internal FormalParameterListAspect ParameterList { get => CSAspectCompilerHelper.GetDescendingNodesOfType<FormalParameterListAspect>(this).FirstOrDefault(); }

        internal bool FromPrototypeType { get; }

        internal PrototypeMethodDeclarationAspect(ISynToken token, bool fromPrototypeType) : base(token)
        {
            FromPrototypeType = fromPrototypeType;
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PrototypeMethodDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class PrototypeEventFieldDeclarationAspect : PrototypeMemberDeclarationAspect
    {
        internal IEnumerable<KeywordAspect> Modifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<KeywordAspect>(this); }

        internal CSAspectNode Type { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeAspect, NameAspect>(this, false).FirstOrDefault(); }

        internal IdentifierNameAspect Identifier { get => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this, false).FirstOrDefault(); }

        internal bool FromPrototypeType { get; }

        internal PrototypeEventFieldDeclarationAspect(ISynToken token, bool fromPrototypeType) : base(token)
        {
            FromPrototypeType = fromPrototypeType;
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PrototypeEventFieldDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class PrototypeEventPropertyDeclarationAspect : PrototypeEventFieldDeclarationAspect
    {
        internal IEnumerable<KeywordAspect> Modifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<KeywordAspect>(this); }

        internal IEnumerable<PrototypeEventccessorAspect> Accessors { get => CSAspectCompilerHelper.GetDescendingNodesOfType<PrototypeEventccessorAspect>(this, false); }

        internal PrototypeEventPropertyDeclarationAspect(ISynToken token, bool fromPrototypeType) : base(token, fromPrototypeType)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PrototypeEventPropertyDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal abstract class PrototypeEventccessorAspect : CSAspectNode
    {
        internal PrototypeEventccessorAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class PrototypeAddAccessorAspect : PrototypeEventccessorAspect
    {
        internal PrototypeAddAccessorAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PrototypeAddAccessorDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class PrototypeRemoveAccessorAspect : PrototypeEventccessorAspect
    {
        internal PrototypeRemoveAccessorAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PrototypeRemoveAccessorDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class PrototypeConstructorDeclarationtAspect : PrototypeMemberDeclarationAspect
    {
        internal IdentifierNameAspect Identifier { get => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this).FirstOrDefault(); }

        internal FormalParameterListAspect ParameterList { get => CSAspectCompilerHelper.GetDescendingNodesOfType<FormalParameterListAspect>(this).FirstOrDefault(); }

        internal ConstructorInitializerAspect ConstructorInitializer { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ConstructorInitializerAspect>(this).FirstOrDefault(); }

        internal bool FromPrototypeType { get; }

        internal PrototypeConstructorDeclarationtAspect(ISynToken token, bool fromPrototypeType) : base(token)
        {
            FromPrototypeType = fromPrototypeType;
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PrototypeConstructorDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class PrototypeStructDeclarationAspect : PrototypeMemberDeclarationAspect
    {
        internal IdentifierNameAspect IdentifierName { get => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this, false).FirstOrDefault(); }

        internal VariantTypeParameterListAspect TypeParameterList { get => CSAspectCompilerHelper.GetDescendingNodesOfType<VariantTypeParameterListAspect>(this, false).FirstOrDefault(); }

        internal PrototypeBaseListAspect BaseList { get => CSAspectCompilerHelper.GetDescendingNodesOfType<PrototypeBaseListAspect>(this, false).FirstOrDefault(); }

        internal IEnumerable<CSAspectNode> Members
        {
            get
            {
                return CSAspectCompilerHelper.GetDescendingNodesOfType<PrototypeMemberDeclarationAspect>(this).Cast<CSAspectNode>().Union(CSAspectCompilerHelper.GetDescendingNodesOfType<PrototypeTypeDeclarationAspect>(this).Cast<CSAspectNode>());
            }
        }

        internal bool IsNested { get; }

        internal PrototypeStructDeclarationAspect(ISynToken token, bool isNested) : base(token)
        {
            IsNested = isNested;
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PrototypeStructDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class PrototypeInterfaceDeclarationAspect : PrototypeMemberDeclarationAspect
    {
        internal IdentifierNameAspect Identifier { get => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this).FirstOrDefault(); }

        internal VariantTypeParameterListAspect TypeParameterList { get => CSAspectCompilerHelper.GetDescendingNodesOfType<VariantTypeParameterListAspect>(this, false).FirstOrDefault(); }

        internal BaseListAspect BaseList { get => CSAspectCompilerHelper.GetDescendingNodesOfType<BaseListAspect>(this).FirstOrDefault(); }

        internal IEnumerable<InterfaceMemberAspect> Members { get => CSAspectCompilerHelper.GetDescendingNodesOfType<InterfaceMemberAspect>(this, false); }

        internal bool IsNested { get; }

        internal PrototypeInterfaceDeclarationAspect(ISynToken token, bool isNested) : base(token)
        {
            IsNested = isNested;
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PrototypeInterfaceDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class PrototypeDelegateDeclarationAspect : PrototypeMemberDeclarationAspect
    {
        internal ReturnTypeAspect ReturnType { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ReturnTypeAspect>(this, false).FirstOrDefault(); }

        internal CSAspectNode Identifier
        {
            get
            {
                CSAspectNode identifier = CSAspectCompilerHelper.GetDescendingNodesOfType<MemberNameAspect>(this, false).FirstOrDefault();
                if (identifier == null)
                    identifier = CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this, false).FirstOrDefault();
                return identifier;
            }
        }

        internal VariantTypeParameterListAspect TypeParameterList { get => CSAspectCompilerHelper.GetDescendingNodesOfType<VariantTypeParameterListAspect>(this, false).FirstOrDefault(); }

        internal FormalParameterListAspect FormalParameterList { get => CSAspectCompilerHelper.GetDescendingNodesOfType<FormalParameterListAspect>(this, false).FirstOrDefault(); }

        internal bool IsNested { get; }

        internal PrototypeDelegateDeclarationAspect(ISynToken token, bool isNested) : base(token)
        {
            IsNested = isNested;
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PrototypeDelegateDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class PrototypeEnumDeclarationAspect : PrototypeMemberDeclarationAspect
    {
        internal IdentifierNameAspect Identifier { get => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this).FirstOrDefault(); }

        internal IEnumerable<EnumMemberDeclarationApsect> EnumMembers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<EnumMemberDeclarationApsect>(this, false).ToList(); }

        internal EnumBaseAspect EnumBase => CSAspectCompilerHelper.GetDescendingNodesOfType<EnumBaseAspect>(this, false).FirstOrDefault();
        internal bool IsNested { get; }

        internal PrototypeEnumDeclarationAspect(ISynToken token, bool isNested) : base(token)
        {
            IsNested = isNested;
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PrototypeEnumDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class PrototypeTypeParameterAspect : PrototypeMemberDeclarationAspect
    {
        internal IdentifierNameAspect Identifier { get => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this).FirstOrDefault(); }

        internal PrototypeTypeParameterAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PrototypeTypeParameter(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

#endregion
#region PrototypeMappingType
    internal class PrototypeTypeMappingAspect : CSAspectNode
    {
        internal IEnumerable<PrototypeTypeMappingItemAspect> PrototypeTypeMappingItems { get => CSAspectCompilerHelper.GetDescendingNodesOfType<PrototypeTypeMappingItemAspect>(this, false).ToList(); }

        internal PrototypeTypeMappingAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            throw new NotImplementedException();
        }
    }

    internal class PrototypeTypeMappingItemAspect : CSAspectNode
    {
        internal QualifiedUnboundTypeNameAspect PrototypeTypeName { get => (QualifiedUnboundTypeNameAspect)ChildCSAspectNodes.FirstOrDefault(); }

        internal TargetTypeNameAspect TargetTypeNameAspect { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TargetTypeNameAspect>(this, false).FirstOrDefault(); }

        internal PrototypeTypeMappingItemAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PrototypeTypeMappingItem(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class TargetTypeNameAspect : TypeNameAspect
    {
        internal CSAspectNode TypeName { get => ChildCSAspectNodes.FirstOrDefault(); }

        internal TargetTypeNameAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return TypeName.GetSyntaxNode();
        }
    }

#endregion
#region PrototypeMapping
    internal abstract class PrototypeMappingItemAspect : CSAspectNode
    {
        internal PrototypeMappingItemAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class PrototypeMappingMemberAspect : PrototypeMappingItemAspect
    {
        internal IdentifierNameAspect Source { get => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this, false).FirstOrDefault(); }

        internal PrototypeTargetDeclarationAspect Target { get => CSAspectCompilerHelper.GetDescendingNodesOfType<PrototypeTargetDeclarationAspect>(this, false).FirstOrDefault(); }

        internal PrototypeMappingMemberAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PrototypeMappingMember(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class PrototypeMappingTypeParameterAspect : PrototypeMappingItemAspect
    {
        internal IdentifierNameAspect Source { get => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this, false).FirstOrDefault(); }

        internal CSAspectNode Target => CSAspectCompilerHelper.GetDescendingNodesOfType<CSAspectNode>(this, false).Last();
        internal PrototypeMappingTypeParameterAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PrototypeMappingTypeParameter(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal abstract class PrototypeGenericParameterTargetAspect : CSAspectNode
    {
        internal CSAspectNode Target => CSAspectCompilerHelper.GetDescendingNodesOfType<CSAspectNode>(this, false).Last();
        internal PrototypeGenericParameterTargetAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class PrototypeTypeGenericParameterTargetAspect : PrototypeGenericParameterTargetAspect
    {
        internal PrototypeTypeGenericParameterTargetAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            throw new NotImplementedException();
        }
    }

    internal class PrototypeMethodGenericParameterTargetAspect : PrototypeGenericParameterTargetAspect
    {
        internal PrototypeMethodGenericParameterTargetAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            throw new NotImplementedException();
        }
    }

    internal abstract class PrototypeTargetDeclarationAspect : CSAspectNode
    {
        internal PrototypeTargetDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            throw new NotImplementedException();
        }
    }

    internal class PrototypeTargetThisMemberDeclarationAspect : PrototypeTargetDeclarationAspect
    {
        internal string IdentifierValue { get => this.ChildAspectNodes.Count() > 0 ? CSAspectCompilerHelper.GetDescendingNodesOfType<NameAspect>(this, false).First().TokenValue : null; }

        internal PrototypeTargetThisMemberDeclarationAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class PrototypeTargetBaseMemberDeclarationAspect : PrototypeTargetDeclarationAspect
    {
        internal string IdentifierValue { get => this.ChildAspectNodes.Count() > 0 ? CSAspectCompilerHelper.GetDescendingNodesOfType<NameAspect>(this, false).First().TokenValue : null; }

        internal PrototypeTargetBaseMemberDeclarationAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class PrototypeTargetMemberDeclarationAspect : PrototypeTargetDeclarationAspect
    {
        internal string IdentifierValue { get => this.ChildAspectNodes.Count() > 0 ? CSAspectCompilerHelper.GetDescendingNodesOfType<NameAspect>(this, false).First().TokenValue : null; }

        internal PrototypeTargetMemberDeclarationAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class PrototypeMappingTypeReferenceAspect : PrototypeMappingItemAspect
    {
        internal NameAspect PrototypeTypeNameReference => (NameAspect)ChildCSAspectNodes.FirstOrDefault();
        internal NameAspect NamespaceOrTypename => CSAspectCompilerHelper.GetDescendingNodesOfType<NameAspect>(this, false, PrototypeTypeNameReference).FirstOrDefault();
        internal PrototypeMappingTypeReferenceAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PrototypeMappingTypeReference(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }
#endregion PrototypeMapping
}