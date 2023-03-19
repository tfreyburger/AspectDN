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
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Runtime.Remoting.Messaging;

namespace AspectDN.Aspect.Concerns
{
    internal class AdviceDefinition : IAdviceDefinition
    {
        protected List<AdviceMemberDefinition> _AdviceMemberDefinitions;
        protected HashSet<TypeDefinition> _CompilerGeneratedTypes;
        protected List<TypeReference> _ReferencedPrototypeTypes;
        internal bool IsThisDeclaration
        {
            get
            {
                return CecilHelper.HasCustomAttributesOfType(AdviceDeclaration, typeof(ThisDeclarationAttribute));
            }
        }

        internal bool IsCompiledGenerated { get; }

        internal List<AdviceMemberDefinition> CompiledGeneratedAdviceMemberDefinitions { get; }

        internal TypeDefinition AdviceDeclaration { get; }

        internal AdviceKinds AdviceKind { get; }

        internal IEnumerable<AdviceMemberDefinition> AdviceMemberDefinitions => _AdviceMemberDefinitions;
        internal string Name
        {
            get
            {
                var name = AdviceDeclaration.Name;
                if (IsCompiledGenerated)
                    name += (AdviceKind == AdviceKinds.Type ? "/<>c" : "/<>M");
                return name;
            }
        }

        internal string FullAssemblyName
        {
            get
            {
                return (AdviceDeclaration.DeclaringType ?? (TypeDefinition)AdviceDeclaration).Module.Assembly.FullName;
            }
        }

        internal string FullAdviceName
        {
            get
            {
                var name = AdviceDeclaration.FullName;
                if (IsCompiledGenerated)
                    name += (AdviceKind == AdviceKinds.Type ? "/<>c" : "/<>M");
                return name;
            }
        }

        internal IEnumerable<TypeReference> ReferencedPrototypeTypes { get => _ReferencedPrototypeTypes; }

        internal IEnumerable<TypeDefinition> CompilerGeneratedTypes => _CompilerGeneratedTypes;
        internal IEnumerable<IMemberDefinition> ThisDeclarationMembers
        {
            get
            {
                return CecilHelper.GetTypeMembers(AdviceDeclaration).Where(t => CecilHelper.HasCustomAttributesOfType(t, typeof(ExludedMemberAttribute)) && CecilHelper.GetCustomAttributeOfType(t, typeof(ExludedMemberAttribute)).ConstructorArguments.Any());
            }
        }

        internal AdviceDefinition(TypeDefinition adviceDeclaration, AdviceKinds adviceKind, bool isCompiledGenerated = false)
        {
            AdviceDeclaration = adviceDeclaration;
            AdviceKind = adviceKind;
            IsCompiledGenerated = isCompiledGenerated;
            _AdviceMemberDefinitions = new List<AdviceMemberDefinition>();
            CompiledGeneratedAdviceMemberDefinitions = new List<AdviceMemberDefinition>();
            _CompilerGeneratedTypes = new HashSet<TypeDefinition>();
            _SetInternalReferencedPrototypeTypes();
        }

        internal AdviceMemberDefinition Add(IMemberDefinition member, AdviceMemberKinds memberKind)
        {
            var adviceMemberDefinition = new AdviceTypeMemberDefinition(this, member, memberKind);
            _AdviceMemberDefinitions.Add(adviceMemberDefinition);
            return adviceMemberDefinition;
        }

        internal AdviceMemberDefinition Add(CustomAttribute customAttribute, AdviceMemberKinds memberKind)
        {
            var adviceMemberDefinition = new AdviceAttributeDefinition(this, customAttribute, memberKind);
            _AdviceMemberDefinitions.Add(adviceMemberDefinition);
            return adviceMemberDefinition;
        }

        internal AdviceTypeReferenceDefinition Add(TypeReference typeReference, AdviceMemberKinds memberKind)
        {
            var adviceTypeReferenceDefinition = new AdviceTypeReferenceDefinition(this, typeReference, memberKind);
            _AdviceMemberDefinitions.Add(adviceTypeReferenceDefinition);
            return adviceTypeReferenceDefinition;
        }

        internal AdviceMemberDefinition Add(TypeDefinition type, AdviceMemberKinds memberKind)
        {
            var adviceTypeDefinition = new AdviceTypeMemberDefinition(this, type, memberKind);
            _AdviceMemberDefinitions.Add(adviceTypeDefinition);
            return adviceTypeDefinition;
        }

        internal void AddCompilerGeneratedTypes(IEnumerable<TypeDefinition> compilerGeneratedTypes)
        {
            foreach (var compilerGeneratedType in compilerGeneratedTypes)
                _CompilerGeneratedTypes.Add(compilerGeneratedType);
        }

        internal void AddReferencedPrototypeTypes(IEnumerable<TypeReference> referencedPrototypeTypes)
        {
            foreach (var referencedPrototypeType in referencedPrototypeTypes)
            {
                if (_ReferencedPrototypeTypes.Exists(t => t.FullName == referencedPrototypeType.FullName))
                    continue;
                _ReferencedPrototypeTypes.Add(referencedPrototypeType);
            }
        }

        void _SetInternalReferencedPrototypeTypes()
        {
            _ReferencedPrototypeTypes = new List<TypeReference>();
            var attribute = CecilHelper.GetCustomAttributeOfType(AdviceDeclaration, typeof(ReferencedPrototypeTypesAttribute));
            if (attribute != null)
            {
                var typeReferences = ((Mono.Cecil.CustomAttributeArgument[])attribute.ConstructorArguments.First().Value).Select(t => (TypeReference)t.Value);
                _ReferencedPrototypeTypes.AddRange(typeReferences);
            }
        }

#region IAdviceDefinition
        bool IAdviceDefinition.IsCompilerGenerated => IsCompiledGenerated;
        IEnumerable<IAdviceMemberDefinition> IAdviceDefinition.CompiledGeneratedAdviceMemberDefinitions => CompiledGeneratedAdviceMemberDefinitions;
        string IAdviceDefinition.Name => Name;
        TypeDefinition IAdviceDefinition.AdviceDeclaration => AdviceDeclaration;
        AdviceKinds IAdviceDefinition.AdviceKind => AdviceKind;
        string IAdviceDefinition.FullAssemblyName => FullAdviceName;
        IEnumerable<IAdviceMemberDefinition> IAdviceDefinition.AdviceMemberDefinitions => AdviceMemberDefinitions;
        string IAdviceDefinition.FullAdviceName => FullAdviceName;
        IEnumerable<TypeReference> IAdviceDefinition.ReferencedPrototypeTypes => ReferencedPrototypeTypes;
        IEnumerable<TypeDefinition> IAdviceDefinition.CompilerGeneratedTypes => _CompilerGeneratedTypes;
        bool IAdviceDefinition.IsThisDeclaration => IsThisDeclaration;
#endregion
    }

    internal abstract class AdviceMemberDefinition : IAdviceMemberDefinition
    {
        internal AdviceKinds AdviceKind => ParentAdviceDefinion.AdviceKind;
        internal AdviceMemberKinds AdviceMemberKind { get; }

        internal AdviceDefinition ParentAdviceDefinion { get; }

        internal TypeDefinition AdviceDeclaration => ParentAdviceDefinion.AdviceDeclaration;
        internal IEnumerable<int> PrototypeReferenceInstructionOffsets { get; set; }

        internal virtual object Member { get; }

        internal virtual string MemberName
        {
            get
            {
                switch (Member)
                {
                    case TypeSpecification typeSpecification:
                        return typeSpecification.Name;
                    case TypeDefinition typeDefinition:
                        return typeDefinition.Name;
                    case IMemberDefinition member:
                        return member.Name;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        internal AdviceMemberDefinition(AdviceDefinition parentAdviceDefinition, AdviceMemberKinds adviceMemberKind, object member)
        {
            ParentAdviceDefinion = parentAdviceDefinition;
            AdviceMemberKind = adviceMemberKind;
            Member = member;
        }

#region IAdviceMemberDefinition
        IAdviceDefinition IAdviceMemberDefinition.ParentAdviceDefinition => ParentAdviceDefinion;
        AdviceMemberKinds IAdviceMemberDefinition.AdviceMemberKind => AdviceMemberKind;
        object IAdviceMemberDefinition.Member => Member;
        string IAdviceMemberDefinition.MemberName => MemberName;
#endregion
#region IAdviceDefinition
        bool IAdviceDefinition.IsCompilerGenerated => ParentAdviceDefinion.IsCompiledGenerated;
        IEnumerable<IAdviceMemberDefinition> IAdviceDefinition.CompiledGeneratedAdviceMemberDefinitions => ParentAdviceDefinion.CompiledGeneratedAdviceMemberDefinitions;
        string IAdviceDefinition.Name => ParentAdviceDefinion.Name;
        TypeDefinition IAdviceDefinition.AdviceDeclaration => ParentAdviceDefinion.AdviceDeclaration;
        AdviceKinds IAdviceDefinition.AdviceKind => ParentAdviceDefinion.AdviceKind;
        string IAdviceDefinition.FullAssemblyName => ParentAdviceDefinion.FullAssemblyName;
        IEnumerable<IAdviceMemberDefinition> IAdviceDefinition.AdviceMemberDefinitions => ParentAdviceDefinion.AdviceMemberDefinitions;
        string IAdviceDefinition.FullAdviceName => ParentAdviceDefinion.FullAssemblyName;
        IEnumerable<TypeReference> IAdviceDefinition.ReferencedPrototypeTypes => ParentAdviceDefinion.ReferencedPrototypeTypes;
        IEnumerable<TypeDefinition> IAdviceDefinition.CompilerGeneratedTypes => ParentAdviceDefinion.CompilerGeneratedTypes;
        bool IAdviceDefinition.IsThisDeclaration => ParentAdviceDefinion.IsThisDeclaration;
#endregion
    }

    internal class AdviceTypeMemberDefinition : AdviceMemberDefinition
    {
        internal IMemberDefinition MemberDefinition { get; }

        internal string Name
        {
            get
            {
                return MemberDefinition.Name;
            }
        }

        internal string FullAssemblyName
        {
            get
            {
                return (AdviceDeclaration.DeclaringType ?? (TypeDefinition)AdviceDeclaration).Module.Assembly.FullName;
            }
        }

        internal AdviceTypeMemberDefinition(AdviceDefinition parentAdviceDefinition, IMemberDefinition memberDefinition, AdviceMemberKinds adviceMemberKind) : base(parentAdviceDefinition, adviceMemberKind, memberDefinition)
        {
            MemberDefinition = memberDefinition;
        }
    }

    internal class AdviceTypeReferenceDefinition : AdviceMemberDefinition
    {
        internal TypeReference TypeReference { get; }

        internal string Name
        {
            get
            {
                return TypeReference.Name;
            }
        }

        internal AdviceTypeReferenceDefinition(AdviceDefinition parentAdviceDefinition, TypeReference typeReference, AdviceMemberKinds adviceMemberKind) : base(parentAdviceDefinition, adviceMemberKind, typeReference)
        {
            TypeReference = typeReference;
        }
    }

    internal class AdviceAttributeDefinition : AdviceMemberDefinition
    {
        internal CustomAttribute CustomAttribute { get; }

        internal AdviceAttributeDefinition(AdviceDefinition parentAdviceDefinition, CustomAttribute customAttribute, AdviceMemberKinds adviceMemberKind) : base(parentAdviceDefinition, adviceMemberKind, customAttribute)
        {
            CustomAttribute = customAttribute;
        }
    }

    internal class AdviceCodeDefinition : AdviceDefinition
    {
        internal AdviceCodeDefinition(TypeDefinition adviceDeclaration) : base(adviceDeclaration, AdviceKinds.Code)
        {
            var method = ((TypeDefinition)AdviceDeclaration).Methods.FirstOrDefault(t => CecilHelper.HasCustomAttributesOfType((IMemberDefinition)t, typeof(ShadowMethod)));
            Add(method, AdviceMemberKinds.Method);
        }
    }

    internal class AdviceStackDefinition : AdviceDefinition
    {
        internal MethodDefinition Method => AdviceDeclaration.Methods.Where(m => !CecilHelper.HasCustomAttributesOfType((IMemberDefinition)m, typeof(PrototypeItemDeclarationAttribute))).FirstOrDefault();
        internal AdviceStackDefinition(TypeDefinition adviceDeclaration) : base(adviceDeclaration, AdviceKinds.ChangeValue)
        {
            var method = ((TypeDefinition)AdviceDeclaration).Methods.Where(t => CecilHelper.HasCustomAttributesOfType((IMemberDefinition)t, typeof(ShadowMethod))).FirstOrDefault();
            Add(method, AdviceMemberKinds.Method);
        }
    }

    internal class AdviceAttributesDefinition : AdviceDefinition
    {
        internal AdviceAttributesDefinition(TypeDefinition adviceDeclaration) : base(adviceDeclaration, AdviceKinds.Attributes)
        {
        }
    }

    internal class AdviceEnumMembersDefinition : AdviceDefinition
    {
        internal AdviceEnumMembersDefinition(TypeDefinition adviceDeclaration) : base(adviceDeclaration, AdviceKinds.EnumMembers)
        {
        }
    }
}