// Author:
//
// T. Freyburger (t.freyburger@gmail.com)
//
// Copyright (c)  Thierry Freyburger
//
// Licensed under the GPLV3 license.
////
using AspectDN.Aspect.Weaving.IConcerns;
using AspectDN.Aspect.Weaving.IJoinpoints;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;
using System.Linq;
using System;
using AspectDN.Common;
using System.Runtime.Remoting.Messaging;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Foundation.Common.Error;

namespace AspectDN.Aspect.Weaving
{
    internal class WeaveItem
    {
        List<(MethodDefinition source, MethodDefinition target)> _MappedMethods;
        List<WeaveItemMember> _WeaveItemMembers;
        List<PrototypeItemMapping> _PrototypeItemMappings;
        List<PrototypeTypeMapping> _PrototypeTypeMappings;
        List<(TypeDefinition sourceCompilerTypeDefinition, TypeDefinition targetCompilerGeneratedTypeDefinition)> _CompilerGeneratedTypeMappings;
        List<WeaveItem> _WeaveItemNeeds;
        List<WeaveItem> _UsedByWeaveItems;
        bool _OnError = false;
        internal long Id { get; }

        internal Weaver Weaver { get; }

        internal IAspectDefinition Aspect { get; }

        internal IJoinpoint Joinpoint { get; }

        internal IEnumerable<WeaveItemMember> WeaveItemMembers => _WeaveItemMembers;
        internal IEnumerable<(TypeDefinition sourceCompilerTypeDefinition, TypeDefinition targetCompilerGeneratedTypeDefinition)> CompilerGeneratedTypeMappings => _CompilerGeneratedTypeMappings;
        internal IEnumerable<IPrototypeItemMappingDefinition> PrototypeItemMappingDefinitions => Aspect.PrototypeItemMappingDefinitions;
        internal List<(MethodDefinition source, MethodDefinition target)> MappedMethods => _MappedMethods;
        internal string FullAspectDeclarationName => this.Aspect.FullAspectDeclarationName;
        internal string FullAspectRepositoryName => this.Aspect.FullAspectRepositoryName;
        internal bool HasPrototypeItemMappings => Aspect.PrototypeItemMappingDefinitions.Any();
        internal bool OnError => _OnError;
        internal bool IsVerified { get; set; }

        internal IEnumerable<WeaveItem> WeaveItemNeeds => _WeaveItemNeeds;
        internal IEnumerable<WeaveItem> UsedByWeaveItems => _UsedByWeaveItems.Where(t => !t.OnError);
        internal IEnumerable<PrototypeItemMapping> PrototypeItemMappings => _PrototypeItemMappings;
        internal AssemblyDefinition TargetAssembly
        {
            get
            {
                switch (Joinpoint)
                {
                    case IAssemblyJoinpoint assemblyJoinpoint:
                        return assemblyJoinpoint.Assembly;
                    case IInstructionJoinpoint instructionJoinpoint:
                        return instructionJoinpoint.DeclaringType.Module.Assembly;
                    case IMemberJoinpoint memberJoinpoint:
                        return memberJoinpoint.DeclaringType.Module.Assembly;
                    case ITypeJoinpoint typeJoinpoint:
                        return typeJoinpoint.Assembly;
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        internal IEnumerable<PrototypeTypeMapping> PrototypeTypeMappings => _PrototypeTypeMappings;
        internal WeaveItem(Weaver weaver, long id, IAspectDefinition aspect, IJoinpoint joinpoint)
        {
            this.Aspect = aspect;
            this.Joinpoint = joinpoint;
            this.Weaver = weaver;
            this.Id = id;
            _PrototypeItemMappings = new List<PrototypeItemMapping>();
            _WeaveItemMembers = new List<WeaveItemMember>();
            _UsedByWeaveItems = new List<WeaveItem>();
            _PrototypeTypeMappings = new List<PrototypeTypeMapping>();
            _WeaveItemNeeds = new List<WeaveItem>();
            _MappedMethods = new List<(MethodDefinition source, MethodDefinition target)>();
            _CompilerGeneratedTypeMappings = new List<(TypeDefinition sourceCompilerTypeDefinition, TypeDefinition targetCompilerGeneratedTypeDefinition)>();
            aspect.Join(joinpoint);
        }

        internal virtual object Resolve(object @object)
        {
            switch (@object)
            {
                case TypeDefinition typeDefinition:
                    return Resolve(typeDefinition);
                default:
                    return @object;
            }
        }

        internal virtual TypeDefinition Resolve(TypeDefinition sourceTypeDefinition)
        {
            TypeDefinition resolvedType = sourceTypeDefinition;
            if (string.IsNullOrEmpty(sourceTypeDefinition.Namespace) && PrototypeItemMappings.Any(t => t.FullPrototypeItemName == sourceTypeDefinition.FullName))
            {
                var mapping = PrototypeItemMappings.FirstOrDefault(t => t.FullPrototypeItemName == sourceTypeDefinition.FullName && t.TargetKind == PrototypeItemMappingTargetKinds.CompiledGeneratedMember);
                if (mapping != null && CecilHelper.HasCustomAttributesOfType(sourceTypeDefinition, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute)) && mapping.Target != null)
                {
                    resolvedType = ((INewType)mapping.Target).ClonedType;
                    return resolvedType;
                }
            }

            if (Aspect.AspectKind == AspectKinds.EnumMembersAspect && sourceTypeDefinition.DeclaringType != null && sourceTypeDefinition.DeclaringType.FullName == Aspect.AdviceDefinition.FullAdviceName)
            {
                resolvedType = (TypeDefinition)Joinpoint.Member;
                return resolvedType;
            }

            if (Aspect.AspectKind == AspectKinds.TypeMembersApsect && sourceTypeDefinition.DeclaringType != null && sourceTypeDefinition.DeclaringType.FullName == Aspect.AdviceDefinition.FullAdviceName)
            {
                resolvedType = (TypeDefinition)Joinpoint.Member;
                return resolvedType;
            }

            if (Aspect.AspectKind == AspectKinds.TypeMembersApsect && (sourceTypeDefinition.FullName == Aspect.AdviceDefinition.FullAdviceName || sourceTypeDefinition.FullName == Aspect.AdviceDefinition.AdviceDeclaration.FullName))
            {
                resolvedType = (TypeDefinition)Joinpoint.Member;
                return resolvedType;
            }

            if (Aspect.AspectKind == AspectKinds.InterfaceMembersAspect && sourceTypeDefinition.DeclaringType != null && sourceTypeDefinition.DeclaringType.FullName == Aspect.AdviceDefinition.FullAdviceName)
            {
                resolvedType = (TypeDefinition)Joinpoint.Member;
                return resolvedType;
            }

            if (Aspect.AspectKind == AspectKinds.InterfaceMembersAspect && sourceTypeDefinition.FullName == Aspect.AdviceDefinition.FullAdviceName)
            {
                resolvedType = (TypeDefinition)Joinpoint.Member;
                return resolvedType;
            }

            var declaringType = CecilHelper.GetHisghestDeclaringType(sourceTypeDefinition, typeof(IAdviceDeclaration));
            if (string.IsNullOrEmpty(declaringType.Namespace) && CecilHelper.HasCustomAttributesOfType(declaringType, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute)) && _CompilerGeneratedTypeMappings.Any(t => t.sourceCompilerTypeDefinition == declaringType))
            {
                resolvedType = _CompilerGeneratedTypeMappings.First(t => t.sourceCompilerTypeDefinition == declaringType).targetCompilerGeneratedTypeDefinition;
                if (sourceTypeDefinition.IsNested)
                    resolvedType = CecilHelper.GetNestedType(resolvedType, sourceTypeDefinition.FullName);
                return resolvedType;
            }

            if ((Aspect.AspectKind == AspectKinds.CodeAspect || Aspect.AspectKind == AspectKinds.ChangeValueAspect) && sourceTypeDefinition.FullName == Aspect.AdviceDefinition.AdviceDeclaration.FullName)
            {
                resolvedType = null;
                switch (Joinpoint)
                {
                    case IInstructionJoinpoint instructionJoinpoint:
                        resolvedType = instructionJoinpoint.CallingMethod.DeclaringType;
                        break;
                    default:
                        resolvedType = ((IMemberDefinition)Joinpoint.Member).DeclaringType;
                        break;
                }

                return resolvedType;
            }

            if (Aspect.AspectKind == AspectKinds.TypesAspect && WeaveItemMembers.OfType<INewType>().Any(t => t.SourceType.FullName == declaringType.FullName))
            {
                var newType = WeaveItemMembers.OfType<INewType>().First(t => t.SourceType.FullName == declaringType.FullName);
                resolvedType = newType.ClonedType;
                if (sourceTypeDefinition.FullName != newType.SourceType.FullName)
                    resolvedType = _GetNewAdviceTypeFromSourceType(resolvedType, sourceTypeDefinition, false);
                return resolvedType;
            }

            var prototypeItemMapping = PrototypeItemMappings.FirstOrDefault(t => t.FullPrototypeItemName == declaringType.FullName);
            if (prototypeItemMapping != null)
            {
                if (prototypeItemMapping.Target == null)
                    throw new NotSupportedException("Should not happened");
                var targetType = ((INewType)prototypeItemMapping.Target).ClonedType;
                targetType = _GetNewAdviceTypeFromSourceType(targetType, sourceTypeDefinition, prototypeItemMapping.TargetKind == PrototypeItemMappingTargetKinds.CompiledGeneratedMember);
                return targetType;
            }

            var prototypeTypeMapping = PrototypeTypeMappings.FirstOrDefault(t => t.PrototypeType.FullName == declaringType.FullName);
            if (prototypeTypeMapping != null)
            {
                var targetType = (TypeDefinition)prototypeTypeMapping.TargetType;
                if (targetType == null)
                    throw new NotSupportedException("Should not happened");
                if (sourceTypeDefinition.IsNested)
                    targetType = _GetNewPrototypeTypeFromSourceType(targetType, sourceTypeDefinition);
                return targetType;
            }

            prototypeTypeMapping = Weaver.PrototypeTypeMappings.Select(t => t.Value).FirstOrDefault(t => t.PrototypeType.FullName == declaringType.FullName);
            if (prototypeTypeMapping != null)
            {
                var targetType = (TypeDefinition)prototypeTypeMapping.TargetType;
                if (targetType == null)
                    throw new NotSupportedException("Should not happened");
                if (sourceTypeDefinition.IsNested)
                    targetType = _GetNewPrototypeTypeFromSourceType(targetType, sourceTypeDefinition);
                return targetType;
            }

            if (sourceTypeDefinition.Module != null)
                resolvedType = TargetAssembly.MainModule.ImportReference(sourceTypeDefinition).Resolve();
            return resolvedType;
        }

        internal virtual object Resolve(FieldDefinition sourceFieldDefinition)
        {
            var prototypeItemMapping = PrototypeItemMappings.FirstOrDefault(t => t.FullPrototypeItemName == sourceFieldDefinition.FullName);
            if (prototypeItemMapping != null)
            {
                if (prototypeItemMapping.Target == null)
                    throw new NotSupportedException("Should not happened");
                return prototypeItemMapping.Target;
            }

            if (Aspect.AspectKind == AspectKinds.TypeMembersApsect && sourceFieldDefinition.DeclaringType.FullName == Aspect.AdviceDefinition.AdviceDeclaration.FullName && !CecilHelper.HasCustomAttributesOfType(sourceFieldDefinition, typeof(ExludedMemberAttribute)))
            {
                var resolvedField = WeaveItemMembers.OfType<NewFieldMember>().Where(t => t.SourceField.FullName == sourceFieldDefinition.FullName).First().ClonedField;
                return resolvedField;
            }

            if (Aspect.AspectKind == AspectKinds.EnumMembersAspect && sourceFieldDefinition.DeclaringType.FullName == Aspect.AdviceDefinition.AdviceDeclaration.FullName && !CecilHelper.HasCustomAttributesOfType(sourceFieldDefinition, typeof(ExludedMemberAttribute)))
            {
                var resolvedField = WeaveItemMembers.OfType<NewEnumMember>().Where(t => t.SourceField.FullName == sourceFieldDefinition.FullName).First().ClonedField;
                return resolvedField;
            }

            var declaringType = CecilHelper.GetHisghestDeclaringType(sourceFieldDefinition.DeclaringType, typeof(IAdviceDeclaration));
            if (string.IsNullOrEmpty(declaringType.Namespace) && CecilHelper.HasCustomAttributesOfType(declaringType, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute)) && _CompilerGeneratedTypeMappings.Any(t => t.sourceCompilerTypeDefinition == declaringType))
            {
                declaringType = _CompilerGeneratedTypeMappings.First(t => t.sourceCompilerTypeDefinition == declaringType).targetCompilerGeneratedTypeDefinition;
                var fieldTarget = declaringType.Fields.First(t => t.Name == sourceFieldDefinition.Name);
                return fieldTarget;
            }

            if (Aspect.AspectKind == AspectKinds.TypesAspect && WeaveItemMembers.OfType<INewType>().Any(t => t.SourceType.FullName == declaringType.FullName))
            {
                var newType = WeaveItemMembers.OfType<INewType>().First(t => t.SourceType.FullName == declaringType.FullName).ClonedType;
                var nestedType = _GetNewAdviceTypeFromSourceType(newType, sourceFieldDefinition.DeclaringType, Aspect.AdviceDefinition.IsCompilerGenerated);
                var fieldTarget = nestedType.Fields.First(t => t.Name == sourceFieldDefinition.Name);
                return fieldTarget;
            }

            prototypeItemMapping = PrototypeItemMappings.FirstOrDefault(t => t.FullPrototypeItemName == declaringType.FullName);
            if (prototypeItemMapping != null)
            {
                if (prototypeItemMapping.Target == null)
                    throw new NotSupportedException("Should not happened");
                var prototypeItemType = ((INewType)prototypeItemMapping.Target).ClonedType;
                var nestedType = _GetNewAdviceTypeFromSourceType(prototypeItemType, sourceFieldDefinition.DeclaringType, prototypeItemMapping.TargetKind == PrototypeItemMappingTargetKinds.CompiledGeneratedMember);
                var fieldTarget = nestedType.Fields.First(t => t.Name == sourceFieldDefinition.Name);
                return fieldTarget;
            }

            var prototypeTypeMapping = PrototypeTypeMappings.FirstOrDefault(t => t.PrototypeType.FullName == declaringType.FullName);
            if (prototypeTypeMapping == null)
                prototypeTypeMapping = Weaver.PrototypeTypeMappings.Select(t => t.Value).FirstOrDefault(t => t.PrototypeType.FullName == declaringType.FullName);
            if (prototypeTypeMapping != null)
            {
                var nestedType = _GetNewPrototypeTypeFromSourceType((TypeDefinition)prototypeTypeMapping.TargetType, sourceFieldDefinition.DeclaringType);
                var fieldTarget = nestedType.Fields.FirstOrDefault(t => t.Name == sourceFieldDefinition.Name);
                if (fieldTarget == null)
                    fieldTarget = Weaver.SafeWeaveItemMembers.OfType<NewTypeMember>().Where(t => t.ClonedMember is FieldDefinition && t.ClonedMember.Name == sourceFieldDefinition.Name).Select(t => (FieldDefinition)t.ClonedMember).First();
                return fieldTarget;
            }

            return TargetAssembly.MainModule.ImportReference(sourceFieldDefinition).Resolve();
        }

        internal virtual object Resolve(MethodDefinition sourceMethodDefinition)
        {
            var prototypeItemMapping = PrototypeItemMappings.FirstOrDefault(t => t.FullPrototypeItemName == sourceMethodDefinition.FullName);
            if (prototypeItemMapping != null)
            {
                if (prototypeItemMapping.Target == null)
                    throw new NotSupportedException("Should not happened");
                return prototypeItemMapping.Target;
            }

            if (Aspect.AspectKind == AspectKinds.TypeMembersApsect && sourceMethodDefinition.DeclaringType.FullName == Aspect.AdviceDefinition.AdviceDeclaration.FullName)
            {
                var mappedMethod = MappedMethods.First(t => t.source == sourceMethodDefinition).target;
                return mappedMethod;
            }

            if (Aspect.AspectKind == AspectKinds.InterfaceMembersAspect && sourceMethodDefinition.DeclaringType.FullName == Aspect.AdviceDefinition.AdviceDeclaration.FullName)
            {
                var resolvedMethod = WeaveItemMembers.OfType<NewAbstractMethodMember>().Where(t => t.SourceMethod.FullName == sourceMethodDefinition.FullName).First().ClonedMethod;
                return resolvedMethod;
            }

            var declaringType = CecilHelper.GetHisghestDeclaringType(sourceMethodDefinition.DeclaringType, typeof(IAdviceDeclaration));
            if (string.IsNullOrEmpty(declaringType.Namespace) && CecilHelper.HasCustomAttributesOfType(declaringType, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute)) && _CompilerGeneratedTypeMappings.Any(t => t.sourceCompilerTypeDefinition == declaringType))
            {
                declaringType = _CompilerGeneratedTypeMappings.First(t => t.sourceCompilerTypeDefinition == declaringType).targetCompilerGeneratedTypeDefinition;
                var resolvedParameters = Resolve(sourceMethodDefinition.Parameters.Select(t => t.ParameterType));
                var resolvedMethod = declaringType.Methods.FirstOrDefault(m => m.Name == sourceMethodDefinition.Name && WeaverHelper.IsSame(resolvedParameters, m.Parameters.Select(t => t.ParameterType)));
                return resolvedMethod;
            }

            if ((Aspect.AspectKind == AspectKinds.CodeAspect || Aspect.AspectKind == AspectKinds.ChangeValueAspect) && sourceMethodDefinition.DeclaringType.FullName == Aspect.AdviceDefinition.AdviceDeclaration.FullName)
            {
                MethodDefinition resolvedMethod = null;
                switch (Joinpoint)
                {
                    case IInstructionJoinpoint instructionJoinpoint:
                        resolvedMethod = instructionJoinpoint.CallingMethod;
                        break;
                    default:
                        resolvedMethod = (MethodDefinition)Joinpoint.Member;
                        break;
                }

                return resolvedMethod;
            }

            if (Aspect.AspectKind == AspectKinds.TypesAspect && WeaveItemMembers.OfType<INewType>().Any(t => t.SourceType.FullName == declaringType.FullName))
            {
                var newMethodMember = WeaveItemMembers.OfType<INewType>().First(t => t.SourceType.FullName == declaringType.FullName);
                var resolvedMethod = ((WeaveItemMember)newMethodMember).MappedMethods.First(t => t.source == sourceMethodDefinition).target;
                return resolvedMethod;
            }

            prototypeItemMapping = PrototypeItemMappings.FirstOrDefault(t => t.FullPrototypeItemName == declaringType.FullName);
            if (prototypeItemMapping != null)
            {
                if (prototypeItemMapping.Target == null)
                    throw new NotSupportedException("Should not happened");
                var prototypeItemType = ((INewType)prototypeItemMapping.Target).ClonedType;
                var nestedType = _GetNewAdviceTypeFromSourceType(prototypeItemType, sourceMethodDefinition.DeclaringType, prototypeItemMapping.TargetKind == PrototypeItemMappingTargetKinds.CompiledGeneratedMember);
                var method = WeaverHelper.GetMethodCompatible(sourceMethodDefinition, nestedType, _PrototypeTypeMappings, Weaver.SafeWeaveItemMembers);
                return method;
            }

            var prototypeTypeMapping = PrototypeTypeMappings.FirstOrDefault(t => t.PrototypeType.FullName == declaringType.FullName);
            if (prototypeTypeMapping == null)
                prototypeTypeMapping = Weaver.PrototypeTypeMappings.Select(t => t.Value).FirstOrDefault(t => t.PrototypeType.FullName == declaringType.FullName);
            if (prototypeTypeMapping != null)
            {
                var nestedType = _GetNewPrototypeTypeFromSourceType((TypeDefinition)prototypeTypeMapping.TargetType, sourceMethodDefinition.DeclaringType);
                var method = WeaverHelper.GetMethodCompatible(sourceMethodDefinition, nestedType, _PrototypeTypeMappings, Weaver.SafeWeaveItemMembers);
                if (method == null && nestedType != null)
                    return ErrorFactory.GetError("PrototypeTargetTypeNotFound", prototypeTypeMapping.PrototypeType.FullName, prototypeTypeMapping.TargetType.FullName);
                return method;
            }

            foreach (var prototypTypeMapping in PrototypeTypeMappings)
            {
                foreach (var member in CecilHelper.GetTypeMembers(prototypTypeMapping.PrototypeType))
                {
                    foreach (var adviceMemberOrignAttribute in CecilHelper.GetCustomAttributeOfTypes(member, typeof(AdviceMemberOrign)))
                    {
                        var derivedWeaveItem = Weaver.SafeWeaveItems.FirstOrDefault(t => t.Aspect.AdviceDefinition.FullAdviceName == (string)adviceMemberOrignAttribute.ConstructorArguments[1].Value);
                        if (derivedWeaveItem != null)
                        {
                            prototypeItemMapping = derivedWeaveItem.PrototypeItemMappings.FirstOrDefault(t => t.FullPrototypeItemName == declaringType.FullName);
                            if (prototypeItemMapping != null)
                            {
                                if (prototypeItemMapping.Target == null)
                                    throw new NotSupportedException("Should not happened");
                                var prototypeItemType = ((INewType)prototypeItemMapping.Target).ClonedType;
                                var nestedType = _GetNewAdviceTypeFromSourceType(prototypeItemType, sourceMethodDefinition.DeclaringType, prototypeItemMapping.TargetKind == PrototypeItemMappingTargetKinds.CompiledGeneratedMember);
                                var method = WeaverHelper.GetMethodCompatible(sourceMethodDefinition, nestedType, _PrototypeTypeMappings, Weaver.SafeWeaveItemMembers);
                                return method;
                            }
                        }
                    }
                }
            }

            if (Weaver.SafeWeaveItemMembers.OfType<INewType>().Any(t => t.SourceType.FullName == declaringType.FullName))
            {
                var weeveItemMember = Weaver.SafeWeaveItemMembers.OfType<INewType>().FirstOrDefault(t => t.SourceType.FullName == declaringType.DeclaringType.FullName);
            }

            var target = TargetAssembly.MainModule.ImportReference(sourceMethodDefinition).Resolve();
            if (target == null)
                target = sourceMethodDefinition;
            return target;
        }

        internal virtual PropertyDefinition Resolve(PropertyDefinition sourcePropertyDefinition)
        {
            var resolvedProperty = sourcePropertyDefinition;
            var prototypeItemMapping = PrototypeItemMappings.FirstOrDefault(t => t.FullPrototypeItemName == sourcePropertyDefinition.FullName);
            if (prototypeItemMapping != null)
            {
                if (prototypeItemMapping.Target == null)
                    throw new NotSupportedException("Should not happened");
                resolvedProperty = (PropertyDefinition)prototypeItemMapping.Target;
            }

            var declaringType = CecilHelper.GetHisghestDeclaringType(sourcePropertyDefinition.DeclaringType, typeof(IAdviceDeclaration));
            if (string.IsNullOrEmpty(declaringType.Namespace) && CecilHelper.HasCustomAttributesOfType(declaringType, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute)) && _CompilerGeneratedTypeMappings.Any(t => t.sourceCompilerTypeDefinition == declaringType))
            {
                declaringType = _CompilerGeneratedTypeMappings.First(t => t.sourceCompilerTypeDefinition == declaringType).targetCompilerGeneratedTypeDefinition;
                var propertyTarget = declaringType.Properties.FirstOrDefault(t => WeaverHelper.HasPropertyCompatible(t, sourcePropertyDefinition.DeclaringType, _PrototypeTypeMappings));
                return propertyTarget;
            }

            prototypeItemMapping = PrototypeItemMappings.FirstOrDefault(t => t.FullPrototypeItemName == declaringType.FullName);
            if (prototypeItemMapping != null)
            {
                if (prototypeItemMapping.Target == null)
                    throw new NotSupportedException("Should not happened");
                var prototypeItemType = ((INewType)prototypeItemMapping.Target).ClonedType;
                var nestedType = _GetNewAdviceTypeFromSourceType(prototypeItemType, sourcePropertyDefinition.DeclaringType, prototypeItemMapping.TargetKind == PrototypeItemMappingTargetKinds.CompiledGeneratedMember);
                var propertyTarget = nestedType.Properties.FirstOrDefault(t => WeaverHelper.HasPropertyCompatible(t, sourcePropertyDefinition.DeclaringType, _PrototypeTypeMappings));
                return propertyTarget;
            }

            var prototypeTypeMapping = PrototypeTypeMappings.FirstOrDefault(t => t.PrototypeType.FullName == declaringType.FullName);
            if (prototypeTypeMapping == null)
                prototypeTypeMapping = Weaver.PrototypeTypeMappings.Select(t => t.Value).FirstOrDefault(t => t.PrototypeType.FullName == declaringType.FullName);
            if (prototypeTypeMapping != null)
            {
                var nestedType = _GetNewPrototypeTypeFromSourceType((TypeDefinition)prototypeTypeMapping.TargetType, sourcePropertyDefinition.DeclaringType);
                var propertyTarget = WeaverHelper.GetPropertyCompatible(sourcePropertyDefinition, nestedType, _PrototypeTypeMappings);
                return propertyTarget;
            }

            return resolvedProperty;
        }

        internal virtual EventDefinition Resolve(EventDefinition sourceEventDefinition)
        {
            EventDefinition resolvedEvent = sourceEventDefinition;
            var prototypeItemMapping = PrototypeItemMappings.FirstOrDefault(t => t.FullPrototypeItemName == sourceEventDefinition.FullName);
            if (prototypeItemMapping != null)
            {
                if (prototypeItemMapping.Target == null)
                    throw new NotSupportedException("Should not happened");
                resolvedEvent = (EventDefinition)prototypeItemMapping.Target;
            }

            if (Aspect.AspectKind == AspectKinds.InterfaceMembersAspect && sourceEventDefinition.DeclaringType.FullName == Aspect.AdviceDefinition.AdviceDeclaration.FullName)
            {
                resolvedEvent = (EventDefinition)Joinpoint.Member;
                return resolvedEvent;
            }

            var declaringType = CecilHelper.GetHisghestDeclaringType(sourceEventDefinition.DeclaringType, typeof(IAdviceDeclaration));
            if (string.IsNullOrEmpty(declaringType.Namespace) && CecilHelper.HasCustomAttributesOfType(declaringType, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute)) && _CompilerGeneratedTypeMappings.Any(t => t.sourceCompilerTypeDefinition == declaringType))
            {
                declaringType = _CompilerGeneratedTypeMappings.First(t => t.sourceCompilerTypeDefinition == declaringType).targetCompilerGeneratedTypeDefinition;
                var eventTarget = declaringType.Events.First(t => t.Name == sourceEventDefinition.Name);
                return eventTarget;
            }

            prototypeItemMapping = PrototypeItemMappings.FirstOrDefault(t => t.FullPrototypeItemName == declaringType.FullName);
            if (prototypeItemMapping != null)
            {
                if (prototypeItemMapping.Target == null)
                    throw new NotSupportedException("Should not happened");
                var prototypeItemType = ((INewType)prototypeItemMapping.Target).ClonedType;
                var nestedType = _GetNewAdviceTypeFromSourceType(prototypeItemType, sourceEventDefinition.DeclaringType, prototypeItemMapping.TargetKind == PrototypeItemMappingTargetKinds.CompiledGeneratedMember);
                var eventTarget = nestedType.Events.First(t => t.Name == sourceEventDefinition.Name);
                return eventTarget;
            }

            var prototypeTypeMapping = PrototypeTypeMappings.FirstOrDefault(t => t.PrototypeType.FullName == declaringType.FullName);
            if (prototypeTypeMapping == null)
                prototypeTypeMapping = Weaver.PrototypeTypeMappings.Select(t => t.Value).FirstOrDefault(t => t.PrototypeType.FullName == declaringType.FullName);
            if (prototypeTypeMapping != null)
            {
                var nestedType = _GetNewPrototypeTypeFromSourceType((TypeDefinition)prototypeTypeMapping.TargetType, sourceEventDefinition.DeclaringType);
                var eventTarget = nestedType.Events.First(t => t.Name == sourceEventDefinition.Name);
                return eventTarget;
            }

            return resolvedEvent;
        }

        internal virtual CustomAttribute Resolve(CustomAttribute sourceCustomAttribute)
        {
            var resolvedConstructor = (MethodReference)Resolve(sourceCustomAttribute.Constructor);
            var targetCustomAttribute = new CustomAttribute(resolvedConstructor, sourceCustomAttribute.GetBlob());
            return targetCustomAttribute;
        }

        internal virtual IGenericParameterProvider Resolve(IGenericParameterProvider sourceGenericParameterProvider)
        {
            switch (sourceGenericParameterProvider)
            {
                case MethodReference sourceMethodReference:
                    return (MethodReference)Resolve(sourceMethodReference);
                case TypeReference sourceTypeReference:
                    return Resolve(sourceTypeReference);
                default:
                    throw new NotFiniteNumberException();
            }
        }

        internal virtual CallSite Resolve(CallSite sourceCallSite)
        {
            var targetCallSite = new CallSite(Resolve(sourceCallSite.ReturnType));
            targetCallSite.CallingConvention = sourceCallSite.CallingConvention;
            targetCallSite.ExplicitThis = sourceCallSite.ExplicitThis;
            targetCallSite.HasThis = sourceCallSite.HasThis;
            targetCallSite.Name = sourceCallSite.Name;
            return targetCallSite;
        }

        internal virtual TypeReference Resolve(TypeReference sourceTypeReference, GenericResolutionContext genericResolutionContext = null)
        {
            TypeReference resolvedTypeReference = null;
            var typeReferenceKind = CecilHelper.GetTypeReferenceKind(sourceTypeReference);
            switch (typeReferenceKind)
            {
                case TypeReferenceKinds.GenericInstance:
                    var sourceGenericInstance = (GenericInstanceType)sourceTypeReference;
                    var resolvedElementType = Resolve(sourceGenericInstance.GetElementType());
                    var resolvedGenericInstance = new GenericInstanceType(resolvedElementType);
                    if (sourceGenericInstance.HasGenericArguments)
                    {
                        List<TypeReference> resolvedGenericArguments = null;
                        if (Aspect.IsCodeAspect && WeaverHelper.ResolveDefinition(resolvedElementType, this).FullName == Joinpoint.DeclaringType.FullName)
                            resolvedGenericArguments = WeaverHelper.ResolveDefinition(resolvedElementType, this).GenericParameters.Cast<TypeReference>().ToList();
                        else
                            resolvedGenericArguments = Resolve(sourceGenericInstance.GenericArguments, genericResolutionContext).ToList();
                        resolvedGenericArguments.ForEach(t => resolvedGenericInstance.GenericArguments.Add(t));
                    }

                    if (sourceGenericInstance.HasGenericParameters)
                    {
                        var resolvedParameterArguments = Resolve(sourceGenericInstance.GenericParameters, sourceGenericInstance).ToList();
                        resolvedParameterArguments.ForEach(t => resolvedGenericInstance.GenericParameters.Add(t));
                    }

                    resolvedGenericInstance.IsValueType = sourceGenericInstance.IsValueType;
                    resolvedTypeReference = resolvedGenericInstance;
                    break;
                case TypeReferenceKinds.SimpleTypeReference:
                    if (sourceTypeReference.IsArray)
                    {
                        TypeReference targetElementTypeDefinitionItem = Resolve(sourceTypeReference.GetElementType(), genericResolutionContext);
                        targetElementTypeDefinitionItem = CecilHelper.ImportReference(TargetAssembly, targetElementTypeDefinitionItem);
                        resolvedTypeReference = new ArrayType(targetElementTypeDefinitionItem, ((ArrayType)sourceTypeReference).Rank);
                    }
                    else
                    {
                        resolvedTypeReference = CecilHelper.Resolve(sourceTypeReference.GetElementType());
                        resolvedTypeReference = Resolve((TypeDefinition)resolvedTypeReference);
                    }

                    break;
                case TypeReferenceKinds.ByReferenceType:
                    var sourceByReferenceType = (ByReferenceType)sourceTypeReference;
                    var targetReferenceType = Resolve(sourceTypeReference.GetElementType());
                    var resolvedByReferenceType = new ByReferenceType(targetReferenceType);
                    resolvedTypeReference = resolvedByReferenceType;
                    break;
                case TypeReferenceKinds.GenericParameter:
                    var sourceGenericParameter = (GenericParameter)sourceTypeReference;
                    switch (sourceGenericParameter.Owner)
                    {
                        case TypeReference typeReferenceOwner:
                            var resolvedOwnerType = Resolve(typeReferenceOwner);
                            TypeReference resolvedGenericParamater = null;
                            if (!PrototypeItemMappings.Any(t => t.FullPrototypeItemName == sourceTypeReference.FullName))
                                resolvedGenericParamater = resolvedOwnerType.GenericParameters[sourceGenericParameter.Position];
                            else
                            {
                                var prototypeItemMapping = PrototypeItemMappings.Where(t => t.FullPrototypeItemName == sourceTypeReference.FullName).FirstOrDefault();
                                if (prototypeItemMapping != null)
                                {
                                    if (prototypeItemMapping.Target == null)
                                        throw new NotSupportedException("No virtual mapping target");
                                    resolvedGenericParamater = (TypeReference)prototypeItemMapping.Target;
                                }
                                else
                                    throw new NotSupportedException("No mapping");
                            }

                            resolvedTypeReference = CecilHelper.ImportReference(TargetAssembly, resolvedGenericParamater);
                            break;
                        case MethodReference methodReferenceOwner:
                            MethodReference resolvedOwnerMethod = null;
                            if (genericResolutionContext != null && genericResolutionContext.SourceMethodReference.FullName == methodReferenceOwner.FullName)
                                resolvedOwnerMethod = genericResolutionContext.TargetMethodReference;
                            else
                            {
                                if (methodReferenceOwner.IsDefinition)
                                    resolvedOwnerMethod = (MethodDefinition)Resolve((MethodDefinition)methodReferenceOwner);
                                else
                                    resolvedOwnerMethod = (MethodReference)Resolve(methodReferenceOwner);
                            }

                            resolvedTypeReference = resolvedOwnerMethod.GenericParameters[sourceGenericParameter.Position];
                            break;
                        default:
                            throw new NotSupportedException();
                    }

                    break;
                default:
                    throw new NotSupportedException();
            }

            if (resolvedTypeReference == null)
                throw new NotSupportedException();
            if (!resolvedTypeReference.IsGenericParameter)
                resolvedTypeReference = CecilHelper.ImportReference(TargetAssembly, resolvedTypeReference);
            return resolvedTypeReference;
        }

        internal virtual IEnumerable<TypeReference> Resolve(IEnumerable<TypeReference> sourceTypeReferences, GenericResolutionContext genericResolutionContext = null)
        {
            var targetTypeReferences = new List<TypeReference>();
            foreach (var sourceTypeReference in sourceTypeReferences)
                targetTypeReferences.Add(Resolve(sourceTypeReference, genericResolutionContext));
            return targetTypeReferences;
        }

        internal virtual GenericParameter Resolve(GenericParameter sourceGenericParameter, IGenericParameterProvider targetGenericParameterProvider)
        {
            var targetGenericParameter = new GenericParameter(targetGenericParameterProvider.GenericParameters[sourceGenericParameter.Position]);
            return targetGenericParameter;
        }

        internal virtual IEnumerable<GenericParameter> Resolve(Mono.Collections.Generic.Collection<GenericParameter> sourceGenericParameters, IGenericParameterProvider targetGenericParameterProvider)
        {
            var targetGenericParameters = new List<GenericParameter>();
            foreach (var sourceGenericParameter in sourceGenericParameters)
                targetGenericParameters.Add(Resolve(sourceGenericParameter, targetGenericParameterProvider));
            return targetGenericParameters;
        }

        internal virtual object Resolve(FieldReference sourceFieldReference, WeaveItemMember weaveItemMember)
        {
            var target = Resolve(WeaverHelper.ResolveDefinition(sourceFieldReference, this));
            if (!(target is FieldReference))
                return target;
            if (!(target is FieldDefinition))
                return target;
            var targetFieldDefinition = (FieldDefinition)target;
            TypeReference declaringType = null;
            TypeReference fieldType = targetFieldDefinition.FieldType;
            if ((Aspect.IsCodeAspect || Aspect.AspectKind == AspectKinds.EnumMembersAspect || Aspect.AspectKind == AspectKinds.TypeMembersApsect || Aspect.AspectKind == AspectKinds.TypesAspect) && sourceFieldReference.DeclaringType.GetElementType().FullName == Aspect.AdviceDefinition.FullAdviceName)
            {
                declaringType = targetFieldDefinition.DeclaringType;
                if (declaringType.HasGenericParameters)
                {
                    declaringType = new GenericInstanceType(declaringType);
                    var declaredTypedTypes = new TypedTypeVisitor().Visit(Joinpoint.DeclaringType, Weaver.SafeWeaveItemMembers);
                    var declaringTypedType = declaredTypedTypes.First(t => t.Type.FullName == targetFieldDefinition.DeclaringType.FullName);
                    declaringTypedType.GenericArguments.ToList().ForEach(t => ((GenericInstanceType)declaringType).GenericArguments.Add(t));
                    target = new FieldReference(targetFieldDefinition.Name, fieldType, declaringType);
                }
            }
            else
            {
                if (sourceFieldReference.DeclaringType != null)
                    declaringType = Resolve(sourceFieldReference.DeclaringType);
                if (!sourceFieldReference.IsDefinition)
                    target = new FieldReference(targetFieldDefinition.Name, Resolve(fieldType), declaringType);
            }

            target = CecilHelper.ImportReference(TargetAssembly, (FieldReference)target);
            return target;
        }

        internal virtual object Resolve(MethodReference sourceMethodReference)
        {
            if (sourceMethodReference.IsDefinition)
            {
                var targetMethodDefinition = Resolve((MethodDefinition)sourceMethodReference);
                return targetMethodDefinition;
            }

            var sourceElementMethodDefinition = sourceMethodReference.GetElementMethod().Resolve();
            var resolvedSourceElementMethodDefinition = Resolve(sourceElementMethodDefinition);
            if (resolvedSourceElementMethodDefinition is IError)
                return resolvedSourceElementMethodDefinition;
            if (resolvedSourceElementMethodDefinition is FieldDefinition)
                return resolvedSourceElementMethodDefinition;
            MethodReference targetMethodReference = null;
            if (sourceMethodReference is GenericInstanceMethod)
            {
                var sourceElementMethodReference = sourceMethodReference.GetElementMethod();
                var targetElementMethodReference = (MethodReference)Resolve(sourceElementMethodReference);
                var targetGenericInstanceMethod = new GenericInstanceMethod(targetElementMethodReference);
                foreach (var sourceGenericParameter in sourceMethodReference.GenericParameters)
                {
                    var targetGenericparemeter = new GenericParameter((string)sourceGenericParameter.Name, targetGenericInstanceMethod);
                    targetGenericparemeter.Attributes = sourceGenericParameter.Attributes;
                    targetGenericInstanceMethod.GenericParameters.Add(targetGenericparemeter);
                }

                foreach (var sourceGenericArgument in ((GenericInstanceMethod)sourceMethodReference).GenericArguments)
                {
                    var targetGenericArgument = Resolve(sourceGenericArgument);
                    targetGenericInstanceMethod.GenericArguments.Add(targetGenericArgument);
                }

                targetGenericInstanceMethod.ReturnType = Resolve(sourceMethodReference.ReturnType, sourceElementMethodReference, targetElementMethodReference);
                if (sourceMethodReference.HasParameters && !targetGenericInstanceMethod.HasParameters)
                {
                    foreach (var sourceMethodReferenceParameter in sourceMethodReference.Parameters)
                    {
                        var targetMethodReferenceParameter = Resolve(sourceMethodReferenceParameter, targetGenericInstanceMethod);
                        targetGenericInstanceMethod.Parameters.Add(targetMethodReferenceParameter);
                    }
                }

                targetMethodReference = CecilHelper.ImportReference(TargetAssembly, targetGenericInstanceMethod);
            }
            else
            {
                TypeReference targetDeclaringType = sourceMethodReference.DeclaringType;
                var resolvedTargetDeclaringType = Resolve(sourceMethodReference.DeclaringType);
                targetMethodReference = new MethodReference(((MethodDefinition)resolvedSourceElementMethodDefinition).Name, TargetAssembly.MainModule.ImportReference(typeof(void)), resolvedTargetDeclaringType);
                targetMethodReference.CallingConvention = sourceMethodReference.CallingConvention;
                targetMethodReference.ExplicitThis = sourceMethodReference.ExplicitThis;
                targetMethodReference.HasThis = sourceMethodReference.HasThis;
                targetMethodReference.MethodReturnType.MarshalInfo = sourceMethodReference.MethodReturnType.MarshalInfo;
                if (sourceMethodReference.ContainsGenericParameter)
                {
                    foreach (var sourceGenericParameter in sourceMethodReference.GenericParameters)
                    {
                        var targetGenericparemeter = new GenericParameter((string)sourceGenericParameter.Name, targetMethodReference);
                        targetGenericparemeter.Attributes = sourceGenericParameter.Attributes;
                        targetMethodReference.GenericParameters.Add(targetGenericparemeter);
                    }
                }

                TypeReference targetReturnType = null;
                if (sourceMethodReference.ReturnType.IsGenericParameter && ((GenericParameter)sourceMethodReference.ReturnType).Owner == sourceMethodReference)
                {
                    var methodElement = sourceMethodReference.GetElementMethod().Resolve();
                    var resolvedElementMethod = (MethodDefinition)Resolve(methodElement);
                    targetReturnType = resolvedElementMethod.GenericParameters[((GenericParameter)sourceMethodReference.ReturnType).Position];
                }
                else
                {
                    GenericResolutionContext genericResolutionContext = null;
                    if ((sourceMethodReference.CallingConvention & MethodCallingConvention.Generic) == MethodCallingConvention.Generic)
                        genericResolutionContext = new GenericResolutionContext(sourceMethodReference, targetMethodReference);
                    targetReturnType = Resolve(sourceMethodReference.ReturnType, genericResolutionContext);
                }

                targetMethodReference.ReturnType = targetReturnType;
                if (sourceMethodReference.HasParameters)
                {
                    foreach (var sourceMethodReferenceParameter in sourceMethodReference.Parameters)
                    {
                        var targetMethodReferenceParameter = Resolve(sourceMethodReferenceParameter, targetMethodReference);
                        targetMethodReference.Parameters.Add(targetMethodReferenceParameter);
                    }
                }

                targetMethodReference = CecilHelper.ImportReference(TargetAssembly, targetMethodReference);
            }

            return targetMethodReference;
        }

        internal virtual ParameterDefinition Resolve(ParameterDefinition sourceParameterDefinition, MethodReference targetMethodReference)
        {
            TypeReference targetParameterTypeReference = Resolve(sourceParameterDefinition.ParameterType, (MethodReference)sourceParameterDefinition.Method, targetMethodReference);
            if (sourceParameterDefinition.ParameterType.IsByReference && !targetParameterTypeReference.IsByReference)
                targetParameterTypeReference = new ByReferenceType(targetParameterTypeReference);
            var targetParameter = new ParameterDefinition(sourceParameterDefinition.Name, sourceParameterDefinition.Attributes, targetParameterTypeReference);
            targetParameter.Constant = Resolve(sourceParameterDefinition.Constant);
            targetParameter.MarshalInfo = sourceParameterDefinition.MarshalInfo;
            foreach (var customAttribute in sourceParameterDefinition.CustomAttributes)
                targetParameter.CustomAttributes.Add(Resolve(customAttribute));
            return targetParameter;
        }

        internal virtual TypeReference Resolve(TypeReference sourceTypeReference, MethodReference sourceMethodRefernce, MethodReference targetMethodReference)
        {
            TypeReference targetTypeReference = null;
            bool isByReference = false;
            bool isArray = false;
            var rank = -1;
            if (sourceTypeReference is ByReferenceType)
            {
                isByReference = true;
                sourceTypeReference = ((ByReferenceType)sourceTypeReference).ElementType;
            }

            if (sourceTypeReference is ArrayType)
            {
                isArray = true;
                rank = ((ArrayType)sourceTypeReference).Rank;
                sourceTypeReference = ((ArrayType)sourceTypeReference).ElementType;
            }

            if (sourceTypeReference.IsGenericParameter)
            {
                if (((GenericParameter)sourceTypeReference).DeclaringMethod != null && ((GenericParameter)sourceTypeReference).DeclaringMethod.FullName == sourceMethodRefernce.FullName)
                {
                    targetTypeReference = targetMethodReference.GenericParameters[((GenericParameter)sourceTypeReference).Position];
                    if (!((GenericParameter)targetTypeReference).DeclaringMethod.IsDefinition)
                        targetTypeReference.Name = "";
                }
                else
                {
                    if (((TypeReference)sourceTypeReference).DeclaringType != null)
                    {
                        targetTypeReference = CecilHelper.GetGenericParameter(targetMethodReference.DeclaringType, ((GenericParameter)sourceTypeReference).Position);
                        if (!targetTypeReference.DeclaringType.IsDefinition)
                            ((TypeReference)targetTypeReference).Name = "";
                    }
                    else
                        throw new NotSupportedException();
                }
            }
            else
                targetTypeReference = Resolve(sourceTypeReference, new GenericResolutionContext(sourceMethodRefernce, targetMethodReference));
            if (isByReference)
                targetTypeReference = new ByReferenceType(targetTypeReference);
            if (isArray)
                targetTypeReference = new ArrayType(targetTypeReference, rank);
            return targetTypeReference;
        }

        internal virtual EventDefinition Resolve(EventReference sourceEventReferernce)
        {
            throw new NotImplementedException();
        }

        internal virtual string ResolveMemberName(string memberName)
        {
            var memberNames = CecilHelper.GetMemberNames(memberName);
            if (!string.IsNullOrEmpty(memberNames.interfaceName))
            {
                var prototypeTypeMapping = PrototypeTypeMappings.FirstOrDefault(t => t.PrototypeType.FullName.EndsWith(memberNames.interfaceName));
                if (prototypeTypeMapping != null)
                {
                    var newInterfaceName = prototypeTypeMapping.TargetType.FullName;
                    memberName = $"{newInterfaceName}.{memberNames.simplename}";
                    return memberName;
                }

                var prototypeItemMapping = PrototypeItemMappings.FirstOrDefault(t => t.SourceKind == PrototypeItemMappingSourceKinds.AdviceType && t.PrototypeItemMember.FullName.Replace('/', '.') == memberNames.interfaceName);
                if (prototypeItemMapping != null)
                {
                    var newInterfaceName = ((INewType)prototypeItemMapping.Target).ClonedType.FullName;
                    memberName = $"{newInterfaceName}.{memberNames.simplename}";
                    return memberName;
                }

                if (Aspect.AspectKind == AspectKinds.TypesAspect && WeaveItemMembers.OfType<INewType>().Any(t => t.AdviceTypeDefinition.FullName.Replace('/', '.') == memberNames.interfaceName))
                {
                    var newInterfaceName = WeaveItemMembers.OfType<INewType>().First(t => t.AdviceTypeDefinition.FullName.Replace('/', '.') == memberNames.interfaceName).ClonedType.FullName;
                    memberName = $"{newInterfaceName}.{memberNames.simplename}";
                    return memberName;
                }
            }

            return memberName;
        }

        internal void AddError(IError error)
        {
            _OnError = true;
            Weaver.AddError(error);
            System.Diagnostics.Debug.WriteLine(FullAspectDeclarationName);
            var consumeds = _UsedByWeaveItems.Where(t => t.FullAspectDeclarationName != this.FullAspectDeclarationName).Distinct().ToList();
            while (consumeds.Any(p => p._UsedByWeaveItems.Any(c => !consumeds.Any(t => t != c))))
            {
                var news = consumeds.SelectMany(p => p._UsedByWeaveItems.Where(c => !consumeds.Any(t => t != c))).Distinct();
                consumeds.AddRange(news);
            }

            foreach (var consumer in consumeds)
            {
                consumer._OnError = true;
                Weaver.AddError(AspectDNErrorFactory.GetError("WeaveItemConsumerError", Aspect.AspectDeclarationName, Joinpoint.Member != null ? Joinpoint.Member.FullName : Joinpoint.Assembly.FullName, consumer.Aspect.AspectDeclarationName, consumer.Joinpoint.Member != null ? consumer.Joinpoint.Member.FullName : consumer.Joinpoint.Assembly.FullName));
            }
        }

        internal void AddPrototypeItemMapping(IPrototypeItemMappingDefinition prototypeItemMappingDefinition, object target)
        {
            _PrototypeItemMappings.Add(new DefinedPrototypeItemMapping(prototypeItemMappingDefinition, this, target));
            if (target is WeaveItemMember && ((WeaveItemMember)target).WeaveItem.Id != Id)
            {
                _WeaveItemNeeds.Add(((WeaveItemMember)target).WeaveItem);
                ((WeaveItemMember)target).WeaveItem._UsedByWeaveItems.Add(this);
            }

            if (target is null)
                AddError(AspectDNErrorFactory.GetError("WeaveItemItemMappingTargetNull", prototypeItemMappingDefinition.FullPrototypeItemName));
            else
            {
                if (target is IMemberDefinition)
                {
                    var baseTargetTypes = WeaverHelper.GetBaseTypes(this.Joinpoint.DeclaringType, Weaver.SafeWeaveItemMembers);
                    var access = WeaverHelper.IsMemberModifierCompatible(this, baseTargetTypes, (IMemberDefinition)target);
                    if (!access)
                    {
                        AddError(AspectDNErrorFactory.GetError("PrototypememberAccessModifierError", prototypeItemMappingDefinition.FullPrototypeItemName, Aspect.AspectDeclarationName, prototypeItemMappingDefinition.TargetName));
                    }
                }
            }
        }

        internal void AddPrototypeItemMapping(PrototypeItemMapping prototypeItemMapping, string targetNameError = null)
        {
            _PrototypeItemMappings.Add(prototypeItemMapping);
            if (prototypeItemMapping.Target is WeaveItemMember && ((WeaveItemMember)prototypeItemMapping.Target).WeaveItem.Id != Id)
            {
                _WeaveItemNeeds.Add(((WeaveItemMember)prototypeItemMapping.Target).WeaveItem);
                ((WeaveItemMember)prototypeItemMapping.Target).WeaveItem._UsedByWeaveItems.Add(this);
            }

            if (prototypeItemMapping.Target == null)
            {
                AddError(AspectDNErrorFactory.GetError("WeaveItemItemMappingTargetNull", targetNameError, prototypeItemMapping.FullPrototypeItemName, prototypeItemMapping.ParentAspectDefinition.AspectDeclarationName, Joinpoint.Member.FullName));
            }
            else
            {
                var baseTargetTypes = WeaverHelper.GetBaseTypes(this.Joinpoint.DeclaringType, Weaver.SafeWeaveItemMembers);
                var access = WeaverHelper.IsMemberModifierCompatible(this, baseTargetTypes, (IMemberDefinition)prototypeItemMapping.Target);
                if (!access)
                {
                    AddError(AspectDNErrorFactory.GetError("PrototypememberAccessModifierError", prototypeItemMapping.FullPrototypeItemName, prototypeItemMapping.ParentAspectDefinition.AspectDeclarationName, prototypeItemMapping.TargetName));
                }
            }
        }

        internal WeaveItemMember AddMember(WeaveItemMember weaveItemMember)
        {
            _WeaveItemMembers.Add(weaveItemMember);
            return weaveItemMember;
        }

        internal void AddPrototypeTypeMapping(PrototypeTypeMapping prototypeTypeMapping)
        {
            if (prototypeTypeMapping.TargetType == null)
            {
            }

            _PrototypeTypeMappings.Add(prototypeTypeMapping);
        }

        internal void AddCompilerGeneratedTypeMapping(TypeDefinition sourceCompilerGeneratedType, TypeDefinition targetCompilerTypeDefinition)
        {
            _CompilerGeneratedTypeMappings.Add((sourceCompilerGeneratedType, targetCompilerTypeDefinition));
        }

        TypeDefinition _GetNewAdviceTypeFromSourceType(TypeDefinition newType, TypeDefinition sourceType, bool isCompilatedGenerated)
        {
            var sourceName = sourceType.FullName.Substring(sourceType.FullName.IndexOf("/") + 1, sourceType.FullName.Length - sourceType.FullName.IndexOf("/") - 1);
            if (isCompilatedGenerated)
                sourceName = $"{sourceType.DeclaringType.FullName}.{sourceName}";
            if (newType.Name == sourceName)
                return newType;
            var nestedFullNames = sourceName.Split('.').Last().Split('/');
            TypeDefinition nestedType = null;
            var fromFullNames = newType.FullName.Split('.').Last().Split('/');
            var index = 0;
            while (index <= fromFullNames.Length - 1)
            {
                if (index == 0)
                    nestedType = newType;
                else
                    nestedType = nestedType.NestedTypes.First(t => t.Name == fromFullNames[index]);
                if (nestedType.Name == nestedFullNames.First())
                    break;
                index++;
            }

            if (nestedType.Name != nestedFullNames[0])
                return null;
            for (int i = 1; i < nestedFullNames.Length; i++)
            {
                nestedType = nestedType.NestedTypes.FirstOrDefault(t => t.Name == nestedFullNames[i]);
                if (nestedType == null)
                    break;
            }

            return nestedType;
        }

        TypeDefinition _GetNewAdviceTypeFromSourceType(INewType newType, TypeDefinition sourceType, bool isCompilatedGenerated)
        {
            var sourceName = sourceType.FullName.Substring(sourceType.FullName.IndexOf("/") + 1, sourceType.FullName.Length - sourceType.FullName.IndexOf("/") - 1);
            if (isCompilatedGenerated)
                sourceName = $"{newType.ClonedType.DeclaringType.FullName}.{sourceName}";
            if (newType.ClonedType.Name == sourceName)
                return newType.ClonedType;
            var nestedFullNames = sourceName.Split('.').Last().Split('/');
            TypeDefinition nestedType = null;
            var fromFullNames = newType.ClonedType.FullName.Split('.').Last().Split('/');
            var index = 0;
            while (index <= fromFullNames.Length - 1)
            {
                if (index == 0)
                    nestedType = newType.ClonedType;
                else
                    nestedType = nestedType.NestedTypes.First(t => t.Name == fromFullNames[index]);
                if (nestedType.Name == nestedFullNames.First())
                    break;
                index++;
            }

            if (nestedType.Name != nestedFullNames[0])
                return null;
            for (int i = 1; i < nestedFullNames.Length; i++)
            {
                nestedType = nestedType.NestedTypes.FirstOrDefault(t => t.Name == nestedFullNames[i]);
                if (nestedType == null)
                    break;
            }

            return nestedType;
        }

        TypeDefinition _GetNewPrototypeTypeFromSourceType(TypeDefinition newType, TypeDefinition sourceType)
        {
            var sourceFullName = sourceType.FullName.Split('.').Last();
            var nestedType = newType;
            if (sourceFullName.IndexOf(".") < 0)
            {
                var nestedNames = sourceType.FullName.Split('/');
                for (int i = 1; i < nestedNames.Length; i++)
                {
                    nestedType = nestedType.NestedTypes.Where(t => t.Name == nestedNames[i]).FirstOrDefault();
                    if (nestedType == null)
                        break;
                }
            }

            return nestedType;
        }
    }

    internal abstract class WeaveItemMember
    {
        List<IMemberDefinition> _OverloadedTypeMembers;
        internal WeaveItem WeaveItem { get; }

        internal IAdviceMemberDefinition AdviceMember { get; }

        internal IEnumerable<PrototypeItemMapping> PrototypeItemMappings => WeaveItem.PrototypeItemMappings;
        internal bool OnError => WeaveItem.OnError;
        internal AssemblyDefinition TargetAssembly => WeaveItem.TargetAssembly;
        internal IEnumerable<IMemberDefinition> OverloadedTypeMembers => _OverloadedTypeMembers;
        internal List<(MethodDefinition source, MethodDefinition target)> MappedMethods => WeaveItem.MappedMethods;
        internal string FullAspectDeclarationName => WeaveItem.FullAspectDeclarationName;
        internal string FullAspectRepositoryName => WeaveItem.FullAspectRepositoryName;
        internal WeaveItemMember(WeaveItem weaveItem, IAdviceMemberDefinition adviceMember)
        {
            WeaveItem = weaveItem;
            this.AdviceMember = adviceMember;
            _OverloadedTypeMembers = new List<IMemberDefinition>();
        }

        internal virtual object Resolve(object @object) => WeaveItem.Resolve(@object);
        internal virtual TypeDefinition Resolve(TypeDefinition sourceType) => WeaveItem.Resolve(sourceType);
        internal virtual object Resolve(FieldDefinition fieldDefinition) => WeaveItem.Resolve(fieldDefinition);
        internal virtual object Resolve(MethodDefinition sourceMethod) => WeaveItem.Resolve(sourceMethod);
        internal virtual PropertyDefinition Resolve(PropertyDefinition sourceProperty) => WeaveItem.Resolve(sourceProperty);
        internal virtual EventDefinition Resolve(EventDefinition sourceEvent) => WeaveItem.Resolve(sourceEvent);
        internal virtual TypeReference Resolve(TypeReference sourceTypeReference) => WeaveItem.Resolve(sourceTypeReference);
        internal virtual IEnumerable<TypeReference> Resolve(IEnumerable<TypeReference> typeReferences) => WeaveItem.Resolve(typeReferences);
        internal virtual object Resolve(MethodReference sourceMethodReference) => WeaveItem.Resolve(sourceMethodReference);
        internal virtual object Resolve(FieldReference sourceFieldReference) => WeaveItem.Resolve(sourceFieldReference, this);
        internal virtual string ResolveMemberName(string memberName) => WeaveItem.ResolveMemberName(memberName);
        internal virtual EventDefinition Resolve(EventReference sourceEventReferernce) => WeaveItem.Resolve(sourceEventReferernce);
        internal virtual IGenericParameterProvider Resolve(IGenericParameterProvider sourceGenericParameterProvider) => WeaveItem.Resolve(sourceGenericParameterProvider);
        internal virtual CallSite Resolve(CallSite sourceCallSite) => WeaveItem.Resolve(sourceCallSite);
        internal virtual TypeReference Resolve(TypeReference sourceGenericParameter, IGenericParameterProvider targetGenericParameterProvider) => Resolve(sourceGenericParameter, targetGenericParameterProvider);
        internal virtual IEnumerable<TypeReference> Resolve(Mono.Collections.Generic.Collection<TypeReference> sourceGenericParameters, IGenericParameterProvider targetGenericParameterProvider) => Resolve(sourceGenericParameters, targetGenericParameterProvider);
        internal void AddError(IError error) => WeaveItem.AddError(error);
        internal void AddOverloadedTypeMember(IMemberDefinition overloadedTypeMember)
        {
            if (!_OverloadedTypeMembers.Any(t => t.FullName == overloadedTypeMember.FullName))
                _OverloadedTypeMembers.Add(overloadedTypeMember);
        }

        internal void AddOverloadedFlatTypeMember(FlatTypeMember overloadedFlatTypeMember)
        {
            if (overloadedFlatTypeMember != null)
            {
                var overloadedTypeMember = overloadedFlatTypeMember.MemberDefinition;
                if (overloadedFlatTypeMember.NewTypeMember != null)
                    overloadedTypeMember = overloadedFlatTypeMember.NewTypeMember.ClonedMember;
                AddOverloadedTypeMember(overloadedTypeMember);
            }
        }
    }

    internal abstract class WeaveItemMemberNewType : WeaveItemMember
    {
        internal abstract TypeDefinition ClonedType { get; set; }

        internal abstract TypeDefinition SourceType { get; }

        internal virtual string FullTargetTypeName => $"{WeaveItem.Joinpoint.Member.FullName}\\{SourceType.Name}";
        public WeaveItemMemberNewType(WeaveItem weaveItem, IAdviceMemberDefinition adviceMember) : base(weaveItem, adviceMember)
        {
        }
    }

    internal class NewAssemblyType : WeaveItemMemberNewType, INewType
    {
        internal string Namespace => ((ITypesAspectDefinition)WeaveItem.Aspect).Namespace;
        internal string ResolvedFullTypeName => $"{Namespace}.{Name}";
        internal string Name => SourceType.Name;
        internal AssemblyDefinition JoinpointAssembly => ((IAssemblyJoinpoint)WeaveItem.Joinpoint).Assembly;
        internal override TypeDefinition ClonedType { get; set; }

        internal override TypeDefinition SourceType => (TypeDefinition)AdviceMember.Member;
        internal NewAssemblyType(WeaveItem weaveItem, IAdviceMemberDefinition adviceMember) : base(weaveItem, adviceMember)
        {
        }

#region INewType
        TypeDefinition INewType.AdviceTypeDefinition => SourceType;
        TypeDefinition INewType.ClonedType { get => ClonedType; set => ClonedType = value; }

        IEnumerable<PrototypeItemMapping> INewType.PrototypeItemMappings => WeaveItem.PrototypeItemMappings;
        TypeDefinition INewType.SourceType { get => SourceType; }

        WeaveItem INewType.WeaveItem => WeaveItem;
        bool INewType.IsCompilerGenerated => WeaveItem.Aspect.AdviceDefinition.IsCompilerGenerated;
#endregion
    }

    internal class NewEnumMember : WeaveItemMember
    {
        FieldDefinition _ClonedField;
        internal FieldDefinition SourceField => (FieldDefinition)AdviceMember.Member;
        internal FieldDefinition ClonedField { get => (FieldDefinition)_ClonedField; set => _ClonedField = value; }

        internal TypeDefinition ResolvedDeclaringType => ((ITypeJoinpoint)WeaveItem.Joinpoint).TypeDefinition;
        internal NewEnumMember(WeaveItem weaveItem, IAdviceMemberDefinition adviceMember) : base(weaveItem, adviceMember)
        {
        }
    }

    internal class NewNestedType : WeaveItemMemberNewType, INewType
    {
        internal string TargetName
        {
            get
            {
                var targetName = SourceType.Name;
                if (AdviceMember.IsCompilerGenerated)
                    targetName = $"{SourceType.DeclaringType}.{SourceType.Name}";
                return targetName;
            }
        }

        internal string Name => AdviceTypeDefinition.Name;
        internal TypeDefinition AdviceTypeDefinition => (TypeDefinition)AdviceMember.Member;
        internal TypeDefinition JoinpointType => ((ITypeJoinpoint)WeaveItem.Joinpoint).TypeDefinition;
        internal override TypeDefinition ClonedType { get; set; }

        internal override TypeDefinition SourceType => (TypeDefinition)AdviceMember.Member;
        internal override string FullTargetTypeName => $"{WeaveItem.Joinpoint.Member.FullName}\\{TargetName}";
        internal NewNestedType(WeaveItem weaveItem, IAdviceMemberDefinition adviceMember) : base(weaveItem, adviceMember)
        {
        }

#region INewType
        WeaveItem INewType.WeaveItem => WeaveItem;
        TypeDefinition INewType.AdviceTypeDefinition => SourceType;
        TypeDefinition INewType.ClonedType { get => ClonedType; set => ClonedType = value; }

        IEnumerable<PrototypeItemMapping> INewType.PrototypeItemMappings => WeaveItem.PrototypeItemMappings;
        TypeDefinition INewType.SourceType { get => SourceType; }

        bool INewType.IsCompilerGenerated => WeaveItem.Aspect.AdviceDefinition.IsCompilerGenerated;
#endregion
    }

    internal interface INewType
    {
        TypeDefinition AdviceTypeDefinition { get; }

        TypeDefinition SourceType { get; }

        TypeDefinition ClonedType { get; set; }

        IEnumerable<PrototypeItemMapping> PrototypeItemMappings { get; }

        WeaveItem WeaveItem { get; }

        bool IsCompilerGenerated { get; }
    }

    internal abstract class NewTypeMember : WeaveItemMember
    {
        bool _Override = false;
        bool _New = false;
        internal TypeDefinition JoinpointDeclaringType => (TypeDefinition)((ITypeJoinpoint)WeaveItem.Joinpoint).Member;
        internal string FullJoinpointDeclaringTypeName => JoinpointDeclaringType.FullName;
        internal abstract string FullNewMemberName { get; }

        internal AspectMemberModifiers MemberModifiers => ((ITypeMembersAspectDefinition)WeaveItem.Aspect).MemberModifers;
        internal IMemberDefinition Member => (IMemberDefinition)AdviceMember.Member;
        internal IMemberDefinition ClonedMember { get; set; }

        internal bool Override => _Override;
        internal bool IsNew => _New;
        internal NewTypeMember(WeaveItem weaveItem, IAdviceMemberDefinition adviceMember) : base(weaveItem, adviceMember)
        {
        }

        internal void SetOverride() => _Override = true;
        internal void SetNew() => _New = true;
    }

    internal class NewFieldMember : NewTypeMember
    {
        internal ILBlockNode FieldInitILCode { get; }

        internal override string FullNewMemberName => CecilHelper.GetFullMemberName(FullJoinpointDeclaringTypeName, ((IMemberDefinition)AdviceMember.Member).Name);
        internal FieldDefinition SourceField => (FieldDefinition)Member;
        internal FieldDefinition ClonedField { get => (FieldDefinition)ClonedMember; set => ClonedMember = value; }

        internal ILBlockNode InitIlBlock { get; }

        internal NewFieldMember(WeaveItem weaveItem, IAdviceMemberDefinition adviceMember, ILBlockNode fieldInitILCode) : base(weaveItem, adviceMember)
        {
            FieldInitILCode = fieldInitILCode;
        }
    }

    internal class NewPropertyMember : NewTypeMember
    {
        internal override string FullNewMemberName => CecilHelper.GetFullMemberName(FullJoinpointDeclaringTypeName, ((IMemberDefinition)AdviceMember.Member).Name, ((PropertyDefinition)AdviceMember.Member).Parameters.Select(p => p.ParameterType));
        internal PropertyDefinition SourceProperty => (PropertyDefinition)Member;
        internal PropertyDefinition ClonedProperty { get => (PropertyDefinition)ClonedMember; set => ClonedMember = value; }

        internal NewPropertyMember(WeaveItem weaveItem, IAdviceMemberDefinition adviceMember) : base(weaveItem, adviceMember)
        {
        }
    }

    internal abstract class NewAbstractMethodMember : NewTypeMember
    {
        internal MethodDefinition SourceMethod => (MethodDefinition)Member;
        internal MethodDefinition ClonedMethod { get => (MethodDefinition)ClonedMember; set => ClonedMember = value; }

        internal NewAbstractMethodMember(WeaveItem weaveItem, IAdviceMemberDefinition adviceMember) : base(weaveItem, adviceMember)
        {
        }
    }

    internal class NewMethodMember : NewAbstractMethodMember
    {
        internal string NewMemberName => (AdviceMember.IsCompilerGenerated ? $"{((MethodDefinition)AdviceMember.Member).DeclaringType.FullName}." : "") + ((IMemberDefinition)AdviceMember.Member).Name;
        internal override string FullNewMemberName => CecilHelper.GetFullMemberName(FullJoinpointDeclaringTypeName, ((IMemberDefinition)AdviceMember.Member).Name);
        internal NewMethodMember(WeaveItem weaveItem, IAdviceMemberDefinition adviceMember) : base(weaveItem, adviceMember)
        {
        }
    }

    internal class NewConstructorMember : NewAbstractMethodMember
    {
        internal List<Instruction> FieldInitILs { get; }

        internal override string FullNewMemberName => CecilHelper.GetFullMemberName(FullJoinpointDeclaringTypeName, ((IMemberDefinition)AdviceMember.Member).Name, ((PropertyDefinition)AdviceMember.Member).Parameters.Select(p => p.ParameterType));
        internal NewConstructorMember(WeaveItem weaveItem, IAdviceMemberDefinition adviceMember) : base(weaveItem, adviceMember)
        {
            FieldInitILs = new List<Instruction>();
        }
    }

    internal class NewOperatorMember : NewAbstractMethodMember
    {
        internal override string FullNewMemberName => CecilHelper.GetFullMemberName(FullJoinpointDeclaringTypeName, ((IMemberDefinition)AdviceMember.Member).Name, ((PropertyDefinition)AdviceMember.Member).Parameters.Select(p => p.ParameterType));
        internal NewOperatorMember(WeaveItem weaveItem, IAdviceMemberDefinition adviceMember) : base(weaveItem, adviceMember)
        {
        }
    }

    internal class NewEventMember : NewTypeMember
    {
        internal override string FullNewMemberName => CecilHelper.GetFullMemberName(FullJoinpointDeclaringTypeName, ((IMemberDefinition)AdviceMember.Member).Name);
        internal EventDefinition SourceEvent => (EventDefinition)Member;
        internal EventDefinition ClonedEvent { get => (EventDefinition)ClonedMember; set => ClonedMember = value; }

        internal NewEventMember(WeaveItem weaveItem, IAdviceMemberDefinition adviceMember) : base(weaveItem, adviceMember)
        {
        }
    }

    internal class NewAttributeMember : WeaveItemMember
    {
        internal CustomAttribute ClonedAttribute { get; set; }

        internal IMemberDefinition TargetMember => (IMemberDefinition)WeaveItem.Joinpoint.Member;
        internal string FullTargetMemberName
        {
            get
            {
                var index = ((CustomAttribute)AdviceMember.Member).AttributeType.FullName.IndexOf("/");
                var name = $"{((IMemberJoinpoint)WeaveItem.Joinpoint).MemberDefinition.FullName}.{((CustomAttribute)AdviceMember.Member).AttributeType.FullName.Substring(index + 1)}";
                return name;
            }
        }

        internal CustomAttribute SourceAttribute => (CustomAttribute)AdviceMember.Member;
        internal NewAttributeMember(WeaveItem weaveItem, IAdviceMemberDefinition adviceMember) : base(weaveItem, adviceMember)
        {
        }
    }

    internal class NewInheritedType : WeaveItemMember
    {
        List<OverloadConstructor> _OverloadConstructors;
        internal IEnumerable<OverloadConstructor> OverloadConstructors => _OverloadConstructors;
        internal bool HasSpecificConstructorOverloads => OverrideConstructorDefinitions.Any();
        internal TypeReference SourceBaseType => (TypeReference)AdviceMember.Member;
        internal TypeDefinition JoinpointType => ((ITypeJoinpoint)WeaveItem.Joinpoint).TypeDefinition;
        internal bool IsInterface => SourceBaseType.GetElementType().Resolve().IsInterface;
        internal IEnumerable<IOverrideConstructorDefinition> OverrideConstructorDefinitions => ((IInheritanceAspectDefinition)WeaveItem.Aspect).ConstructorOverloads;
        internal TypeReference TargetBaseType { get; set; }

        internal InterfaceImplementation TargetInterfaceImplementation => new InterfaceImplementation(TargetBaseType);
        internal TypeDefinition NewJoinpointType { get; set; }

        internal IEnumerable<TypeReference> ResolvedGenericArguments { get; set; }

        internal NewInheritedType(WeaveItem weaveItem, IAdviceMemberDefinition adviceMember) : base(weaveItem, adviceMember)
        {
            _OverloadConstructors = new List<OverloadConstructor>();
        }

        internal IEnumerable<IMemberDefinition> BaseTypeMembers => CecilHelper.GetTypeMembers(SourceBaseType.Resolve());
        internal void Add(MethodDefinition joinpointConstructor, MethodDefinition baseTypeConstructor, IEnumerable<Instruction>[] baseArguments)
        {
            _OverloadConstructors.Add(new OverloadConstructor(joinpointConstructor, baseTypeConstructor, baseArguments));
        }
    }

    internal class OverloadConstructor
    {
        internal MethodBody NewJoinpointConstructorBody { get; set; }

        internal MethodDefinition JoinpointConstructor { get; }

        internal MethodDefinition BaseTypeConstructor { get; }

        internal IEnumerable<Instruction>[] BaseArguments { get; }

        internal OverloadConstructor(MethodDefinition joinpointConstructor, MethodDefinition baseTypeConstructor, IEnumerable<Instruction>[] baseArguments)
        {
            JoinpointConstructor = joinpointConstructor;
            BaseTypeConstructor = baseTypeConstructor;
            BaseArguments = baseArguments;
        }
    }

    internal abstract class NewCodeMember : WeaveItemMember
    {
        internal abstract MethodDefinition TargetMethod { get; }

        internal MethodBody NewMethodBody { get; set; }

        public NewCodeMember(WeaveItem weaveItem, IAdviceMemberDefinition adviceMember) : base(weaveItem, adviceMember)
        {
        }
    }

    internal abstract class NewCode : NewCodeMember
    {
        internal ICodeAspectDefinition CodeAspect => (ICodeAspectDefinition)WeaveItem.Aspect;
        internal ExecutionTimes ExecutionTime => CodeAspect.ExecutionTime;
        internal MethodDefinition SourceMethod => (MethodDefinition)CodeAspect.AdviceMemberDefinition.Member;
        internal IJoinpoint Joinpoint => WeaveItem.Joinpoint;
        internal NewCode(WeaveItem weaveItem, IAdviceMemberDefinition adviceMember) : base(weaveItem, adviceMember)
        {
        }

        internal override object Resolve(MethodDefinition sourceMethod)
        {
            if (sourceMethod.FullName == SourceMethod.FullName)
                return TargetMethod;
            return base.Resolve(sourceMethod);
        }
    }

    internal class NewCodeInstruction : NewCode
    {
        internal override MethodDefinition TargetMethod => ((IInstructionJoinpoint)WeaveItem.Joinpoint).CallingMethod;
        internal NewCodeInstruction(WeaveItem weaveItem, IAdviceMemberDefinition adviceMember) : base(weaveItem, adviceMember)
        {
        }
    }

    internal class NewCodeBody : NewCode
    {
        internal override MethodDefinition TargetMethod => (MethodDefinition)WeaveItem.Joinpoint.Member;
        internal NewCodeBody(WeaveItem weaveItem, IAdviceMemberDefinition adviceMember) : base(weaveItem, adviceMember)
        {
        }
    }

    internal class NewChangeValue : NewCodeMember
    {
        internal MethodDefinition SourceMethod => (MethodDefinition)AdviceMember.Member;
        internal override MethodDefinition TargetMethod => ((IInstructionJoinpoint)WeaveItem.Joinpoint).CallingMethod;
        internal IJoinpoint Joinpoint => WeaveItem.Joinpoint;
        internal NewChangeValue(WeaveItem weaveItem, IAdviceMemberDefinition adviceMember) : base(weaveItem, adviceMember)
        {
        }

        internal override object Resolve(MethodDefinition sourceMethod)
        {
            if (sourceMethod.FullName == SourceMethod.FullName)
                return TargetMethod;
            return base.Resolve(sourceMethod);
        }
    }

    internal class NewFieldInitCode : NewCodeMember
    {
        internal ILBlockNode FieldInitILCode { get; }

        internal MethodDefinition TargetConstructor { get; }

        internal NewFieldMember NewFieldMember => (NewFieldMember)AdviceMember;
        internal MethodDefinition SourceMethod => FieldInitILCode.Method;
        internal override MethodDefinition TargetMethod => TargetConstructor;
        internal IJoinpoint Joinpoint => WeaveItem.Joinpoint;
        internal NewFieldInitCode(WeaveItem weaveItem, IAdviceMemberDefinition adviceMember, ILBlockNode fieldInitILCode, MethodDefinition targtConstructor) : base(weaveItem, adviceMember)
        {
            FieldInitILCode = fieldInitILCode;
            TargetConstructor = targtConstructor;
        }
    }
}