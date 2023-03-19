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

namespace AspectDN.Aspect.Weaving.IConcerns
{
    public interface IAdviceDefinition
    {
        bool IsCompilerGenerated { get; }

        bool IsThisDeclaration { get; }

        string Name { get; }

        TypeDefinition AdviceDeclaration { get; }

        AdviceKinds AdviceKind { get; }

        string FullAssemblyName { get; }

        IEnumerable<IAdviceMemberDefinition> AdviceMemberDefinitions { get; }

        IEnumerable<IAdviceMemberDefinition> CompiledGeneratedAdviceMemberDefinitions { get; }

        IEnumerable<TypeReference> ReferencedPrototypeTypes { get; }

        string FullAdviceName { get; }

        IEnumerable<TypeDefinition> CompilerGeneratedTypes { get; }
    }

    public interface IAdviceMemberDefinition : IAdviceDefinition
    {
        IAdviceDefinition ParentAdviceDefinition { get; }

        AdviceMemberKinds AdviceMemberKind { get; }

        object Member { get; }

        string MemberName { get; }
    }

    public interface IAdviceAttributesDefinition : IAdviceDefinition
    {
        IEnumerable<CustomAttribute> CustomAttributes { get; }
    }

    public interface IAdviceEnumMembersDefinition : IAdviceDefinition
    {
        IEnumerable<FieldDefinition> Fields { get; }
    }

    public enum AdviceKinds
    {
        Attributes,
        Type,
        BaseTypeList,
        EnumMembers,
        TypeMembers,
        InterfaceMembers,
        ChangeValue,
        Code
    }

    public enum AdviceMemberKinds
    {
        None,
        Field,
        Property,
        Method,
        Event,
        Constructor,
        Operator,
        Type,
        Attribute,
        EnumMember
    }
}