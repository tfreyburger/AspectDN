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

namespace AspectDN.Aspect.Weaving.IConcerns
{
    public class AspectDNAssemblyAttribute : Attribute
    {
    }

    public class ExludedMemberAttribute : Attribute
    {
        public string FullPrototypeTypeName { get; }

        public ExludedMemberAttribute()
        {
        }

        public ExludedMemberAttribute(string fullPrototypeTypeName) => FullPrototypeTypeName = fullPrototypeTypeName;
    }

    public class PrototypeItemDeclarationAttribute : ExludedMemberAttribute
    {
        public string Id { get; }

        public PrototypeItemDeclarationAttribute(string id)
        {
            Id = id;
        }
    }

    public class PrototypeTypeDeclarationAttribute : ExludedMemberAttribute
    {
        public string Id { get; }

        public PrototypeTypeDeclarationAttribute(string id)
        {
            Id = id;
        }
    }

    public class PrototypeTypeParameterMappingAttribute : ExludedMemberAttribute
    {
        public string SourceName { get; }

        public string TargetName { get; }

        public PrototypeTypeParameterMappingAttribute(string sourceName, string targetName)
        {
            SourceName = sourceName;
            TargetName = targetName;
        }
    }

    public class AdviceMemberOrign : Attribute
    {
        public Type AdviceDeclaration { get; }

        public string SourceMemberName { get; }

        public AdviceMemberOrign(Type adviceDeclaration, string sourceMemberName)
        {
            AdviceDeclaration = adviceDeclaration;
            SourceMemberName = sourceMemberName;
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true, Inherited = true)]
    public class PrototypeTypeMappingAttribute : ExludedMemberAttribute
    {
        public Type PrototypeType { get; }

        public string TargetTypename { get; }

        public PrototypeTypeMappingAttribute(Type virtualType, string targetTypename)
        {
            PrototypeType = virtualType;
            TargetTypename = targetTypename;
        }
    }

    [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class PrototypeItemMappingAttribute : ExludedMemberAttribute
    {
        public PrototypeItemMappingSourceKinds SourceType { get; }

        public object Source { get; }

        public PrototypeItemMappingTargetKinds TargetType { get; }

        public string TargetName { get; }

        public PrototypeItemMappingAttribute(PrototypeItemMappingSourceKinds sourceKind, object source, PrototypeItemMappingTargetKinds targetKind, string targetName)
        {
            Source = source;
            SourceType = sourceKind;
            TargetType = targetKind;
            TargetName = targetName;
        }
    }

    public enum PrototypeItemMappingSourceKinds
    {
        Member,
        AdviceType,
        PrototypeType,
        GenericParameter
    }

    public enum PrototypeItemMappingTargetKinds
    {
        This,
        ThisMember,
        BaseMember,
        Member,
        NamespaceOrClass,
        TypeGenericParameter,
        MethodGenericParameter,
        CompiledGeneratedMember,
        CompiledGeneratedType
    }

    [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class ThisDeclarationAttribute : ExludedMemberAttribute
    {
        public string PrototypeTypeName { get; }

        public ThisDeclarationAttribute(string prototypeTypeName)
        {
            PrototypeTypeName = prototypeTypeName;
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class OverloadingConstructorAttribute : ExludedMemberAttribute
    {
    }

    public class ShadowMethod : ExludedMemberAttribute
    {
        public ShadowMethod()
        {
        }
    }

    public class PointcutAttribute : ExludedMemberAttribute
    {
        public PointcutTypes PointcutType { get; }

        public PointcutAttribute(PointcutTypes pointcutType)
        {
            PointcutType = pointcutType;
        }
    }

    public class AspectPointcutAttribute : ExludedMemberAttribute
    {
        public Type PointcutType { get; }

        public AspectPointcutAttribute(Type pointcutType)
        {
            PointcutType = pointcutType;
        }
    }

    public class AspectAdviceAttribute : ExludedMemberAttribute
    {
        public Type AdviceType { get; }

        public AspectAdviceAttribute(Type adviceTYpe)
        {
            AdviceType = adviceTYpe;
        }
    }

    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class AdviceBaseTypeAttribute : ExludedMemberAttribute
    {
        public object BaseType { get; }

        public object[] TypeArguments { get; }

        public AdviceBaseTypeAttribute(Type baseType, params object[] typeArguments)
        {
            BaseType = baseType;
            TypeArguments = typeArguments;
        }
    }

    public class ExecutionTimeAttribute : ExludedMemberAttribute
    {
        public ExecutionTimes ExecutionTime { get; }

        public ExecutionTimeAttribute(ExecutionTimes executionTime)
        {
            ExecutionTime = executionTime;
        }
    }

    public class ControlFlowsAttribute : ExludedMemberAttribute
    {
        public ControlFlows ControlFlow { get; }

        public ControlFlowsAttribute(ControlFlows controlFlow)
        {
            ControlFlow = controlFlow;
        }
    }

    public class PointcutTypeAttribute : ExludedMemberAttribute
    {
        public PointcutTypes PointcutType { get; }

        public PointcutTypeAttribute(PointcutTypes pointcutType)
        {
            PointcutType = pointcutType;
        }
    }

    public class NamespaceOrTypeNameAttribute : ExludedMemberAttribute
    {
        public string NamespaceOrTypeName { get; }

        public NamespaceOrTypeNameAttribute(string namespaceOrTypeName)
        {
            NamespaceOrTypeName = namespaceOrTypeName;
        }
    }

    public class AspectTypeMemberModifersAttribute : ExludedMemberAttribute
    {
        public AspectTypeMemberModifers[] TypeMemberModifers { get; }

        public AspectTypeMemberModifersAttribute(params AspectTypeMemberModifers[] typeMemberModifers)
        {
            TypeMemberModifers = typeMemberModifers;
        }
    }

    public class AspectParentAttribute : ExludedMemberAttribute
    {
        public Type AspectParent { get; }

        public AspectParentAttribute(Type aspectParent)
        {
            AspectParent = aspectParent;
        }
    }

    public enum AspectTypeMemberModifers
    {
        @new,
        @override
    }

    public class ReferencedPrototypeTypesAttribute : ExludedMemberAttribute
    {
        public Type[] PrototypeTypes { get; }

        public ReferencedPrototypeTypesAttribute(params Type[] virtualTypes)
        {
            PrototypeTypes = virtualTypes;
        }
    }

    public class ReferencedAdviceTypesAttribute : ExludedMemberAttribute
    {
        public Type[] AdviceTypes { get; }

        public ReferencedAdviceTypesAttribute(params Type[] adviceTypes)
        {
            AdviceTypes = adviceTypes;
        }
    }

    public class AdviceConstructorAttribute : Attribute
    {
    }
}