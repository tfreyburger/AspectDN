// Author:
//
// T. Freyburger (t.freyburger@gmail.com)
//
// Copyright (c)  Thierry Freyburger
//
// Licensed under the GPLV3 license.
////
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;
using AspectDN.Aspect.Weaving.IJoinpoints;

namespace AspectDN.Aspect.Weaving.IConcerns
{
    public enum AspectKinds
    {
        CodeAspect,
        ChangeValueAspect,
        TypeMembersApsect,
        TypesAspect,
        EnumMembersAspect,
        AttributesAspect,
        InterfaceMembersAspect,
        InheritedTypesAspect
    }

    public enum ExecutionTimes
    {
        none,
        before,
        after,
        around
    }

    public enum ControlFlows : short
    {
        none = 0,
        set = 1,
        get = 2,
        call = 4,
        body = 8,
        @throw = 16,
        add = 32,
        remove = 64,
    }

    public interface IAspectDefinition
    {
        long Id { get; }

        AspectKinds AspectKind { get; }

        string AspectDeclarationName { get; }

        string FullAspectDeclarationName { get; }

        string FullAspectRepositoryName { get; }

        IPointcutDefinition Pointcut { get; }

        IAdviceDefinition AdviceDefinition { get; }

        IEnumerable<IAspectMemberDefinition> AspectMemberDefinitions { get; }

        IEnumerable<IPrototypeItemMappingDefinition> PrototypeItemMappingDefinitions { get; }

        bool IsCodeAspect { get; }

        void Join(IJoinpoint joinpoint);
    }

    public interface IAspectMemberDefinition : IAspectDefinition
    {
        IAspectDefinition ParentAspectDefinition { get; }

        IAdviceMemberDefinition AdviceMemberDefinition { get; }
    }

    public interface ICodeAspectDefinition : IAspectMemberDefinition
    {
        ControlFlows ControlFlow { get; }

        ExecutionTimes ExecutionTime { get; }
    }

    public interface IInheritanceAspectDefinition : IAspectDefinition
    {
        IEnumerable<IOverrideConstructorDefinition> ConstructorOverloads { get; }
    }

    public interface IOverrideConstructorDefinition
    {
        IEnumerable<ParameterDefinition> OverrideConstructorParameters { get; }

        IEnumerable<TypeReference> BaseConstructorParameterValueTypes { get; }

        IEnumerable<Instruction>[] BaseConstructeurParameterValues { get; }
    }

    public interface IChangeValueAspectDefinition : IAspectMemberDefinition
    {
        ControlFlows ControlFlow { get; }
    }

    public interface ITypeMembersAspectDefinition : IAspectMemberDefinition
    {
        AspectMemberModifiers MemberModifers { get; }
    }

    public interface ITypesAspectDefinition : IAspectMemberDefinition
    {
        string Namespace { get; }
    }
}