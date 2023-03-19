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
using Mono.Cecil;
using AspectDN.Aspect.Weaving.IConcerns;
using AspectDN.Common;
using System.IO;
using System.Runtime.CompilerServices;

namespace AspectDN.Aspect.Concerns
{
    internal class AspectDeclaration
    {
        TypeDefinition _AspectDeclaration;
        internal string FullPointcutName => ((TypeReference)_AspectDeclaration.CustomAttributes.First(t => t.AttributeType.FullName == typeof(AspectPointcutAttribute).FullName).ConstructorArguments[0].Value).FullName;
        internal string FullAdviceName => ((TypeReference)_AspectDeclaration.CustomAttributes.First(t => t.AttributeType.FullName == typeof(AspectAdviceAttribute).FullName).ConstructorArguments[0].Value).FullName;
        internal string FullName => _AspectDeclaration.FullName;
        internal string Name => _AspectDeclaration.Name;
        internal AspectKinds AspectKind
        {
            get
            {
                switch (_AspectDeclaration.Interfaces.FirstOrDefault().InterfaceType.Name)
                {
                    case nameof(ICodeAspectDeclaration):
                        return AspectKinds.CodeAspect;
                    case nameof(IChangeValueAspectDeclaration):
                        return AspectKinds.ChangeValueAspect;
                    case nameof(IAspectInheritanceDeclaration):
                        return AspectKinds.InheritedTypesAspect;
                    case nameof(IAspectInterfaceMembersDeclaration):
                        return AspectKinds.InterfaceMembersAspect;
                    case nameof(IAspectTypeDeclaration):
                        return AspectKinds.TypesAspect;
                    case nameof(IAspectEnumMembersDeclaration):
                        return AspectKinds.EnumMembersAspect;
                    case nameof(IAspectTypeMembersDeclaration):
                        return AspectKinds.TypeMembersApsect;
                    case nameof(IAspectAttributesDeclaration):
                        return AspectKinds.AttributesAspect;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        internal string FullAspectRepositoryName => Path.GetFileNameWithoutExtension(_AspectDeclaration.Module.Name);
        internal ExecutionTimes ExecutionTime => (ExecutionTimes)CecilHelper.GetCustomAttributeOfType(_AspectDeclaration, typeof(ExecutionTimeAttribute)).ConstructorArguments[0].Value;
        internal ControlFlows ControlFlow => (ControlFlows)CecilHelper.GetCustomAttributeOfType(_AspectDeclaration, typeof(ControlFlowsAttribute)).ConstructorArguments[0].Value;
        internal string NamespaceOrTypename
        {
            get
            {
                var namespaceAttribute = CecilHelper.GetCustomAttributeOfType(_AspectDeclaration, typeof(NamespaceOrTypeNameAttribute));
                var namespaceOrTypeName = "";
                if (namespaceAttribute != null)
                    namespaceOrTypeName = (string)namespaceAttribute.ConstructorArguments[0].Value;
                return namespaceOrTypeName;
            }
        }

        internal AspectMemberModifiers Modifiers
        {
            get
            {
                var memberModifiers = AspectMemberModifiers.none;
                var memberModifiersAttribute = CecilHelper.GetCustomAttributeOfType(_AspectDeclaration, typeof(AspectTypeMemberModifersAttribute));
                if (memberModifiersAttribute != null)
                {
                    foreach (var attributeArgument in (CustomAttributeArgument[])memberModifiersAttribute.ConstructorArguments[0].Value)
                    {
                        var value = (AspectTypeMemberModifers)attributeArgument.Value;
                        switch (value)
                        {
                            case AspectTypeMemberModifers.@new:
                                memberModifiers |= AspectMemberModifiers.@new;
                                break;
                            case AspectTypeMemberModifers.@override:
                                memberModifiers |= AspectMemberModifiers.@override;
                                break;
                        }
                    }
                }

                return memberModifiers;
            }
        }

        internal AspectDeclaration(TypeDefinition typeDefinition)
        {
            _AspectDeclaration = typeDefinition;
        }

        internal IEnumerable<PrototypeItemMappingDefinition> GetPrototypeItemMappingDefinitions(AdviceDefinition adviceDefinition)
        {
            var prototypeItemMappingDefinitions = new List<PrototypeItemMappingDefinition>();
            var prototypeMappingItemAttributes = CecilHelper.GetCustomAttributesOfType(_AspectDeclaration, typeof(PrototypeItemMappingAttribute));
            foreach (var prototypeMappingItemAttribute in prototypeMappingItemAttributes)
            {
                var prototypeItemMappingTargetKind = (PrototypeItemMappingTargetKinds)prototypeMappingItemAttribute.ConstructorArguments[2].Value;
                var targetName = (string)prototypeMappingItemAttribute.ConstructorArguments[3].Value;
                var prototypeItemMappingSourceKind = (PrototypeItemMappingSourceKinds)prototypeMappingItemAttribute.ConstructorArguments[0].Value;
                switch (prototypeItemMappingSourceKind)
                {
                    case PrototypeItemMappingSourceKinds.Member:
                        object constructorArgument = prototypeMappingItemAttribute.ConstructorArguments[1].Value;
                        if (constructorArgument is CustomAttributeArgument)
                            constructorArgument = ((CustomAttributeArgument)((CustomAttributeArgument)constructorArgument)).Value;
                        var prototypeMemnberName = (string)constructorArgument;
                        foreach (var prototypeField in adviceDefinition.AdviceDeclaration.Fields.Where(t => t.Name == prototypeMemnberName && !CecilHelper.HasCustomAttributesOfType(t, typeof(CompilerGeneratedAttribute))))
                            prototypeItemMappingDefinitions.Add(new PrototypeItemMappingDefinition(prototypeItemMappingSourceKind, prototypeField, prototypeItemMappingTargetKind, targetName));
                        foreach (var prototypeMethod in adviceDefinition.AdviceDeclaration.Methods.Where(t => t.Name == prototypeMemnberName && !CecilHelper.HasCustomAttributesOfType(t, typeof(CompilerGeneratedAttribute))))
                            prototypeItemMappingDefinitions.Add(new PrototypeItemMappingDefinition(prototypeItemMappingSourceKind, prototypeMethod, prototypeItemMappingTargetKind, targetName));
                        foreach (var prototypeEvent in adviceDefinition.AdviceDeclaration.Events.Where(t => t.Name == prototypeMemnberName && !CecilHelper.HasCustomAttributesOfType(t, typeof(CompilerGeneratedAttribute))))
                        {
                            var fieldEvent = adviceDefinition.AdviceDeclaration.Fields.FirstOrDefault(t => t.Name == prototypeEvent.Name && CecilHelper.HasCustomAttributesOfType(t, typeof(CompilerGeneratedAttribute)) && CecilHelper.IsDelegate(t.FieldType) && t.FieldType.FullName == prototypeEvent.EventType.FullName);
                            if (fieldEvent != null)
                                prototypeItemMappingDefinitions.Add(new PrototypeItemMappingDefinition(prototypeItemMappingSourceKind, fieldEvent, prototypeItemMappingTargetKind, targetName));
                            else
                                prototypeItemMappingDefinitions.Add(new PrototypeItemMappingDefinition(prototypeItemMappingSourceKind, prototypeEvent, prototypeItemMappingTargetKind, targetName));
                        }

                        foreach (var prototypeProperty in adviceDefinition.AdviceDeclaration.Properties.Where(t => t.Name == prototypeMemnberName && CecilHelper.HasCustomAttributesOfType(t, typeof(PrototypeItemDeclarationAttribute)) && !CecilHelper.HasCustomAttributesOfType(t, typeof(CompilerGeneratedAttribute))))
                            prototypeItemMappingDefinitions.Add(new PrototypeItemMappingDefinition(prototypeItemMappingSourceKind, prototypeProperty, prototypeItemMappingTargetKind, targetName));
                        break;
                    case PrototypeItemMappingSourceKinds.GenericParameter:
                        var typeParameterName = (string)((CustomAttributeArgument)prototypeMappingItemAttribute.ConstructorArguments[1].Value).Value;
                        var genericParameter = adviceDefinition.AdviceDeclaration.GenericParameters.FirstOrDefault(t => t.Name == typeParameterName);
                        if (genericParameter == null && _AspectDeclaration.Interfaces.Any(i => i.InterfaceType.FullName == typeof(IAspectInheritanceDeclaration).FullName))
                        {
                            throw new NotImplementedException();
                        }

                        prototypeItemMappingDefinitions.Add(new PrototypeItemMappingDefinition(prototypeItemMappingSourceKind, genericParameter, prototypeItemMappingTargetKind, targetName));
                        break;
                    case PrototypeItemMappingSourceKinds.AdviceType:
                    case PrototypeItemMappingSourceKinds.PrototypeType:
                        object typeArugment = prototypeMappingItemAttribute.ConstructorArguments[1].Value;
                        if (typeArugment is CustomAttributeArgument)
                            typeArugment = ((CustomAttributeArgument)((CustomAttributeArgument)typeArugment)).Value;
                        prototypeItemMappingDefinitions.Add(new PrototypeItemMappingDefinition(prototypeItemMappingSourceKind, typeArugment, prototypeItemMappingTargetKind, targetName));
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }

            foreach (var compiledGeneratedAdviceMemberDefinition in adviceDefinition.CompiledGeneratedAdviceMemberDefinitions)
            {
                PrototypeItemMappingSourceKinds prototypeItemMappingSourceKind;
                var sourceMember = compiledGeneratedAdviceMemberDefinition.Member;
                PrototypeItemMappingTargetKinds prototypeItemMappingTargetKind;
                var targetName = "";
                if (compiledGeneratedAdviceMemberDefinition.AdviceKind == AdviceKinds.Type)
                {
                    prototypeItemMappingTargetKind = PrototypeItemMappingTargetKinds.CompiledGeneratedMember;
                    targetName = "";
                    prototypeItemMappingSourceKind = PrototypeItemMappingSourceKinds.AdviceType;
                }
                else
                {
                    prototypeItemMappingTargetKind = PrototypeItemMappingTargetKinds.CompiledGeneratedMember;
                    targetName = "";
                    prototypeItemMappingSourceKind = PrototypeItemMappingSourceKinds.Member;
                }

                prototypeItemMappingDefinitions.Add(new PrototypeItemMappingDefinition(prototypeItemMappingSourceKind, sourceMember, prototypeItemMappingTargetKind, targetName));
            }

            foreach (var indexer in adviceDefinition.AdviceDeclaration.Properties.Where(t => CecilHelper.HasCustomAttributesOfType(t, typeof(PrototypeItemDeclarationAttribute))))
            {
                var methodPropertyNames = new List<string>();
                if (indexer.GetMethod != null)
                    methodPropertyNames.Add(indexer.GetMethod.FullName);
                if (indexer.SetMethod != null)
                    methodPropertyNames.Add(indexer.SetMethod.FullName);
                if (indexer.HasOtherMethods)
                    methodPropertyNames.AddRange(indexer.OtherMethods.Select(m => m.FullName));
                if (adviceDefinition.AdviceDeclaration.Methods.Where(m => m.HasBody).SelectMany(t => t.Body.Instructions.Where(i => i.Operand is MemberReference)).Any(t => methodPropertyNames.Any(mp => mp == ((MemberReference)t.Operand).Resolve().FullName)))
                {
                    prototypeItemMappingDefinitions.Add(new PrototypeItemMappingDefinition(PrototypeItemMappingSourceKinds.Member, indexer, PrototypeItemMappingTargetKinds.Member, null));
                }
            }

            foreach (var constructor in adviceDefinition.AdviceDeclaration.Methods.Where(t => t.IsConstructor && CecilHelper.HasCustomAttributesOfType(t, typeof(PrototypeItemDeclarationAttribute))))
            {
                if (adviceDefinition.AdviceDeclaration.Methods.Where(m => m.HasBody).SelectMany(t => t.Body.Instructions.Where(i => i.Operand is MemberReference)).Any(t => ((MemberReference)t.Operand).Resolve().FullName == constructor.FullName))
                {
                    prototypeItemMappingDefinitions.Add(new PrototypeItemMappingDefinition(PrototypeItemMappingSourceKinds.Member, constructor, PrototypeItemMappingTargetKinds.Member, constructor.Name));
                }
            }

            return prototypeItemMappingDefinitions;
        }

        internal void Join(AssemblyDefinition joinpointAssembly)
        {
            ((AspectDNAssemblyResolver)joinpointAssembly.MainModule.AssemblyResolver).Join((AspectDNAssemblyResolver)_AspectDeclaration.Module.AssemblyResolver);
        }
    }
}