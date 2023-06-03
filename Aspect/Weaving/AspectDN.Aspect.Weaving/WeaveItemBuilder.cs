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
using AspectDN.Common;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using MonoCollection = Mono.Collections.Generic;
using System.Runtime.CompilerServices;
using Foundation.Common.Error;
using AspectDN.Common;

namespace AspectDN.Aspect.Weaving
{
    internal class WeaveItemBuilder
    {
        Weaver _Weaver;
        Dictionary<TypeDefinition, TypeDefinition> _GlobalCompilerGeneratedTypes;
        internal IEnumerable<WeaveItemMember> WeaveItemMembers => _Weaver.WeaveItemMembers;
        internal IEnumerable<WeaveItem> WeaveItems => _Weaver.WeaveItems;
        internal IEnumerable<WeaveItem> SafeWeaveItems => _Weaver.SafeWeaveItems;
        internal IEnumerable<WeaveItemMember> SafeWeaveItemMembers => _Weaver.SafeWeaveItemMembers;
        internal WeaveItemBuilder(Weaver weaver)
        {
            _Weaver = weaver;
        }

        internal WeaveItemBuilder CreateWeaveItems()
        {
            _GlobalCompilerGeneratedTypes = new Dictionary<TypeDefinition, TypeDefinition>();
            _ResolvePrototypeTypeTargets();
            _CreateWeaveItems();
            _BuildPrototypeCompilerGeneratedTypePrototypeTypes();
            ;
            _CheckWeaveItemMemberRedundancies();
            _ResolvePrototypeAndAdviceTypeMappings();
            _CheckNewAssemblyType();
            _CloneAdviceTypesAndTypeMembers();
            _CheckNewbaseType();
            _CheckNewNestedTypes();
            _CheckTypeMembersIntegrity();
            _CheckNewEnumMembers();
            _CheckBaseInterfaces();
            _CheckNewAttributes();
            _CheckNewCode();
            _ResolvePrototypeItemMappings();
            _CheckPrototypeTypeTargets();
            _CloneMethodBodies();
            _BuildILOverloadConstructors();
            _BuildILOverloadNewConstructors();
            _SetOverloadedTypeMember();
            _MergeExtendedCode();
            return this;
        }

        void _AddWeaveItem(WeaveItem weaveItem) => _Weaver.WeaveItems.Add(weaveItem);
        void _AddWeaveItemMembers(IEnumerable<WeaveItemMember> weaveItemMembers) => _Weaver.WeaveItemMembers.AddRange(weaveItemMembers);
        void _BuildPrototypeCompilerGeneratedTypePrototypeTypes()
        {
            var weavesByJoinpointAssemblies = SafeWeaveItems.Where(t => t.Aspect.AdviceDefinition.CompilerGeneratedTypes.Any()).Select(t => (t.Joinpoint.Assembly, (WeaveItem)t, t.Aspect.AdviceDefinition.CompilerGeneratedTypes)).GroupBy(t => t.Assembly);
            foreach (var weavesByJoinpointAssembly in weavesByJoinpointAssemblies)
            {
                foreach (var weaveByJoinpointAssembly in weavesByJoinpointAssembly.Select(t => (t.Item2, t.CompilerGeneratedTypes)))
                {
                    foreach (var compilerGeneratedType in weaveByJoinpointAssembly.CompilerGeneratedTypes)
                    {
                        TypeDefinition cloneCompilerGeneratedType = null;
                        if (_GlobalCompilerGeneratedTypes.ContainsKey(compilerGeneratedType))
                            cloneCompilerGeneratedType = _GlobalCompilerGeneratedTypes[compilerGeneratedType];
                        else
                        {
                            cloneCompilerGeneratedType = _CloneCompilerGeneratedType(compilerGeneratedType, weaveByJoinpointAssembly.Item1.WeaveItemMembers.First());
                            _GlobalCompilerGeneratedTypes.Add(compilerGeneratedType, cloneCompilerGeneratedType);
                        }

                        weaveByJoinpointAssembly.Item1.AddCompilerGeneratedTypeMapping(compilerGeneratedType, cloneCompilerGeneratedType);
                    }
                }

                var weaveItemMember = weavesByJoinpointAssemblies.First().Select(t => t.Item2).First().WeaveItemMembers.First();
                foreach (var compilerGeneratedType in _GlobalCompilerGeneratedTypes.Keys.Distinct())
                {
                    var cloneCompilerGeneratedType = _GlobalCompilerGeneratedTypes[compilerGeneratedType];
                    _CloneTypeGenericParamters(weaveItemMember, compilerGeneratedType, cloneCompilerGeneratedType);
                    _CloneAddonType(compilerGeneratedType, cloneCompilerGeneratedType, weaveItemMember);
                }

                foreach (var compilerGeneratedType in _GlobalCompilerGeneratedTypes.Keys.Distinct())
                {
                    var cloneCompilerGeneratedType = _GlobalCompilerGeneratedTypes[compilerGeneratedType];
                    foreach (var sourceMethod in compilerGeneratedType.Methods.Where(t => t.HasBody))
                    {
                        var targetMethod = (MethodDefinition)weaveItemMember.Resolve(sourceMethod);
                        if (sourceMethod.HasBody)
                        {
                            var clonedBody = MethodBodyCloner.Create(sourceMethod, weaveItemMember).CloneBody();
                            if (clonedBody != null)
                                targetMethod.Body = clonedBody;
                        }
                    }
                }
            }
        }

        TypeDefinition _CloneCompilerGeneratedType(TypeDefinition compilerGeneratedType, WeaveItemMember weaveItemMember)
        {
            var clonedType = WeaverHelper.Clone(compilerGeneratedType, null, $"{compilerGeneratedType}@{compilerGeneratedType.Module.Mvid.ToString()}", weaveItemMember);
            return clonedType;
        }

        void _ResolvePrototypeTypeTargets()
        {
            foreach (var prototypeTypeMappingDefinition in _Weaver.AspectContainer.PrototypeTypeMappingDefinitions)
            {
                var prototypeTypeTarget = new PrototypeTypeMapping(prototypeTypeMappingDefinition);
                var @namespace = _Weaver.JointpointsContainer.Namespaces.Where(n => prototypeTypeMappingDefinition.TargetTypename.IndexOf(n) == 0).Aggregate("", (max, cur) => max.Length > cur.Length ? max : cur);
                if (string.IsNullOrEmpty(@namespace))
                    _Weaver.AddError(AspectDNErrorFactory.GetError("PrototypeTargetTypeNotFound", prototypeTypeMappingDefinition.PrototypeType.FullName, prototypeTypeMappingDefinition.TargetTypename));
                else
                {
                    var targetTypeName = $"{@namespace}.{prototypeTypeMappingDefinition.TargetTypename.Substring(@namespace.Length + 1).Replace(".", "/")}";
                    var index = targetTypeName.IndexOf("<");
                    if (index != -1)
                    {
                        var arguments = targetTypeName.Substring(index, targetTypeName.TrimEnd().Length - index);
                        var argumentNbr = arguments.Count(t => t == ',') + 1;
                        targetTypeName = targetTypeName.Replace(arguments, $"`{argumentNbr}");
                    }

                    var targetTypes = _Weaver.JointpointsContainer.TypeJoinpoints.Where(t => t.FullName == targetTypeName).Select(t => t.TypeDefinition);
                    if (!targetTypes.Any())
                        _Weaver.AddError(AspectDNErrorFactory.GetError("PrototypeTargetTypeNotFound", prototypeTypeMappingDefinition.PrototypeType.FullName, prototypeTypeMappingDefinition.TargetTypename));
                    else
                    {
                        if (targetTypes.Count() > 1)
                            _Weaver.AddError(AspectDNErrorFactory.GetError("PrototypeTargetTypeAmbiguity", prototypeTypeMappingDefinition.TargetTypename));
                        else
                            prototypeTypeTarget.TargetType = targetTypes.First();
                    }
                }

                _Weaver.PrototypeTypeMappings.Add(prototypeTypeTarget.PrototypeType.FullName, prototypeTypeTarget);
            }

            foreach (var prototypeTypeMappingTarget in _Weaver.PrototypeTypeMappings.Values)
            {
                if (prototypeTypeMappingTarget.TargetType == null)
                {
                    SetPrototypeTypeTargetOnError(prototypeTypeMappingTarget);
                    continue;
                }
            }
        }

        void _CheckPrototypeTypeTargets()
        {
            foreach (var prototypeTypeMappingTarget in _Weaver.PrototypeTypeMappings.Values.Where(t => t.TargetType != null))
            {
                var errors = WeaverHelper.IsTargetAndPrototypeTypeCompatible(prototypeTypeMappingTarget, _Weaver.PrototypeTypeMappings.Select(t => t.Value), SafeWeaveItemMembers);
                if (errors.Any())
                {
                    _Weaver.AddError(AspectDNErrorFactory.GetError("PrototypeTypeMappingMismatch", prototypeTypeMappingTarget.PrototypeType.FullName, prototypeTypeMappingTarget.TargetType.FullName));
                    foreach (var error in errors)
                        _Weaver.AddError(error);
                    SetPrototypeTypeTargetOnError(prototypeTypeMappingTarget);
                    continue;
                }
            }
        }

        internal void SetPrototypeTypeTargetOnError(PrototypeTypeMapping prototypeTypeMapping)
        {
            if (prototypeTypeMapping.OnError)
                return;
            prototypeTypeMapping.OnError = true;
            prototypeTypeMapping.TargetType = null;
            foreach (var prototypeType in prototypeTypeMapping.InternalReferencedPrototypeTypes)
            {
                var internlaPrototypeTypeMapping = _Weaver.PrototypeTypeMappings.Where(t => t.Key == prototypeType.FullName).Select(t => t.Value).FirstOrDefault();
                if (internlaPrototypeTypeMapping != null)
                    SetPrototypeTypeTargetOnError(internlaPrototypeTypeMapping);
            }

            foreach (var prototypeTypeMappingTarget in _Weaver.PrototypeTypeMappings.Values.Where(t => t.OnError))
            {
                var weaveitemMembersOrError = SafeWeaveItemMembers.Where(t => t.AdviceMember.ReferencedPrototypeTypes.Any(p => p.FullName == prototypeTypeMappingTarget.PrototypeType.FullName));
                foreach (var weaveItemMember in weaveitemMembersOrError)
                {
                    weaveItemMember.AddError(AspectDNErrorFactory.GetError("AspectWithPrototypeTypeTargetError", weaveItemMember.WeaveItem.Aspect.AspectDeclarationName, prototypeTypeMappingTarget.PrototypeType.FullName));
                }
            }
        }

        void _CreateWeaveItems()
        {
            long weaveItemId = 1;
            foreach (var aspectDefinition in _Weaver.AspectContainer.Aspects)
            {
                var joinpoints = WeaverHelper.GetTargets(aspectDefinition, _Weaver.JointpointsContainer);
                if (!joinpoints.Any())
                {
                    _Weaver.AddError(AspectDNErrorFactory.GetError("NoJointpointFound", aspectDefinition.FullAspectDeclarationName));
                    continue;
                }

                foreach (var item in joinpoints)
                {
                    var joinpoint = item;
                    if (aspectDefinition.AdviceDefinition.IsCompilerGenerated && aspectDefinition.AspectKind == AspectKinds.TypesAspect && joinpoint is IMemberJoinpoint)
                    {
                        joinpoint = _Weaver.JointpointsContainer.TypeJoinpoints.FirstOrDefault(t => t.Member.FullName == ((IMemberJoinpoint)joinpoint).DeclaringType.FullName);
                        ;
                    }

                    switch (aspectDefinition.AspectKind)
                    {
                        case AspectKinds.CodeAspect:
                            _AddNewCode(weaveItemId++, (IJoinpoint)joinpoint, (ICodeAspectDefinition)aspectDefinition);
                            break;
                        case AspectKinds.ChangeValueAspect:
                            _AddChangeValue(weaveItemId++, (IJoinpoint)joinpoint, (IChangeValueAspectDefinition)aspectDefinition);
                            break;
                        case AspectKinds.TypeMembersApsect:
                            _AddNewTypeMembers(weaveItemId++, (ITypeJoinpoint)joinpoint, (ITypeMembersAspectDefinition)aspectDefinition);
                            break;
                        case AspectKinds.TypesAspect:
                            switch (joinpoint)
                            {
                                case IAssemblyJoinpoint assemblyJoinpoint:
                                    _AddNewType(weaveItemId++, assemblyJoinpoint, (ITypesAspectDefinition)aspectDefinition);
                                    break;
                                case ITypeJoinpoint typeJoinpoint:
                                    _AddNewNestedType(weaveItemId++, typeJoinpoint, (ITypesAspectDefinition)aspectDefinition);
                                    break;
                                default:
                                    throw new NotSupportedException();
                            }

                            break;
                        case AspectKinds.EnumMembersAspect:
                            _AddEnumMembers(weaveItemId++, (ITypeJoinpoint)joinpoint, aspectDefinition);
                            break;
                        case AspectKinds.AttributesAspect:
                            _AddAttributes(weaveItemId++, (IJoinpoint)joinpoint, aspectDefinition);
                            break;
                        case AspectKinds.InterfaceMembersAspect:
                            _AddNewInterfaceMembers(weaveItemId++, (ITypeJoinpoint)joinpoint, aspectDefinition);
                            break;
                        case AspectKinds.InheritedTypesAspect:
                            _AddNewInheritedTypes(weaveItemId++, (ITypeJoinpoint)joinpoint, aspectDefinition);
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
        }

        void _AddNewType(long id, IAssemblyJoinpoint assemblyJoinpoint, ITypesAspectDefinition typesAspect)
        {
            var weaveItem = new WeaveItem(_Weaver, id, typesAspect, assemblyJoinpoint);
            foreach (var typeAdviceMember in typesAspect.AdviceDefinition.AdviceMemberDefinitions)
                weaveItem.AddMember(new NewAssemblyType(weaveItem, typeAdviceMember));
            _AddWeaveItem(weaveItem);
            _AddWeaveItemMembers(weaveItem.WeaveItemMembers);
        }

        void _AddNewNestedType(long id, ITypeJoinpoint typeJoinpoint, ITypesAspectDefinition typesAspect)
        {
            var weaveItem = new WeaveItem(_Weaver, id, typesAspect, typeJoinpoint);
            foreach (var typeAdviceMember in typesAspect.AdviceDefinition.AdviceMemberDefinitions)
                weaveItem.AddMember(new NewNestedType(weaveItem, typeAdviceMember));
            _AddWeaveItem(weaveItem);
            _AddWeaveItemMembers(weaveItem.WeaveItemMembers);
        }

        void _AddNewInheritedTypes(long id, ITypeJoinpoint typeJoinpoint, IAspectDefinition aspect)
        {
            var weaveItem = new WeaveItem(_Weaver, id, aspect, typeJoinpoint);
            List<NewInheritedType> newInheritedTypes = new List<NewInheritedType>();
            foreach (var adviceMember in aspect.AdviceDefinition.AdviceMemberDefinitions)
                weaveItem.AddMember(new NewInheritedType(weaveItem, adviceMember));
            _AddWeaveItem(weaveItem);
            _AddWeaveItemMembers(weaveItem.WeaveItemMembers);
        }

        void _AddNewTypeMembers(long id, ITypeJoinpoint typeJoinpoint, ITypeMembersAspectDefinition typeMembersAspect)
        {
            var weaveItem = new WeaveItem(_Weaver, id, typeMembersAspect, typeJoinpoint);
            foreach (var typeMemberAdvice in typeMembersAspect.AdviceDefinition.AdviceMemberDefinitions)
            {
                NewTypeMember newTypeMember = null;
                switch (typeMemberAdvice.AdviceMemberKind)
                {
                    case AdviceMemberKinds.Field:
                        var fieldInitCode = CecilHelper.GetFieldConstantInitILBlock((FieldDefinition)typeMemberAdvice.Member);
                        newTypeMember = new NewFieldMember(weaveItem, typeMemberAdvice, fieldInitCode);
                        break;
                    case AdviceMemberKinds.Property:
                        newTypeMember = new NewPropertyMember(weaveItem, typeMemberAdvice);
                        break;
                    case AdviceMemberKinds.Method:
                        newTypeMember = new NewMethodMember(weaveItem, typeMemberAdvice);
                        break;
                    case AdviceMemberKinds.Event:
                        newTypeMember = new NewEventMember(weaveItem, typeMemberAdvice);
                        break;
                    case AdviceMemberKinds.Constructor:
                        newTypeMember = new NewConstructorMember(weaveItem, typeMemberAdvice);
                        break;
                    case AdviceMemberKinds.Operator:
                        newTypeMember = new NewOperatorMember(weaveItem, typeMemberAdvice);
                        break;
                    case AdviceMemberKinds.Type:
                    case AdviceMemberKinds.None:
                    default:
                        throw ErrorFactory.GetException("NotImplementedException");
                }

                weaveItem.AddMember(newTypeMember);
                if (newTypeMember is NewFieldMember && ((NewFieldMember)newTypeMember).FieldInitILCode != null)
                {
                    foreach (var constructor in newTypeMember.JoinpointDeclaringType.Methods.Where(t => t.IsConstructor))
                        weaveItem.AddMember(new NewFieldInitCode(weaveItem, newTypeMember.AdviceMember, ((NewFieldMember)newTypeMember).FieldInitILCode, constructor));
                }
            }

            _AddWeaveItem(weaveItem);
            _AddWeaveItemMembers(weaveItem.WeaveItemMembers);
        }

        void _AddEnumMembers(long id, ITypeJoinpoint typeJoinpoint, IAspectDefinition enumMembersAspect)
        {
            var weaveItem = new WeaveItem(_Weaver, id, enumMembersAspect, typeJoinpoint);
            foreach (var enumAdviceMember in enumMembersAspect.AdviceDefinition.AdviceMemberDefinitions)
            {
                var newEnumMember = new NewEnumMember(weaveItem, enumAdviceMember);
                weaveItem.AddMember(newEnumMember);
            }

            _AddWeaveItem(weaveItem);
            _AddWeaveItemMembers(weaveItem.WeaveItemMembers);
        }

        void _AddNewInterfaceMembers(long id, ITypeJoinpoint typeJoinpoint, IAspectDefinition aspect)
        {
            foreach (var joinpoint in WeaverHelper.GetTargets(aspect, _Weaver.JointpointsContainer).Cast<IJoinpoint>())
            {
                var weaveItem = new WeaveItem(_Weaver, id, aspect, typeJoinpoint);
                foreach (var typeMemberAdvice in aspect.AdviceDefinition.AdviceMemberDefinitions)
                {
                    NewTypeMember newTypeMember = null;
                    switch (typeMemberAdvice.AdviceMemberKind)
                    {
                        case AdviceMemberKinds.Field:
                            newTypeMember = new NewFieldMember(weaveItem, typeMemberAdvice, null);
                            break;
                        case AdviceMemberKinds.Property:
                            newTypeMember = new NewPropertyMember(weaveItem, typeMemberAdvice);
                            break;
                        case AdviceMemberKinds.Method:
                            newTypeMember = new NewMethodMember(weaveItem, typeMemberAdvice);
                            break;
                        case AdviceMemberKinds.Event:
                            newTypeMember = new NewEventMember(weaveItem, typeMemberAdvice);
                            break;
                        case AdviceMemberKinds.Constructor:
                        case AdviceMemberKinds.Operator:
                        case AdviceMemberKinds.Type:
                        case AdviceMemberKinds.None:
                        default:
                            throw ErrorFactory.GetException("NotImplementedException");
                    }

                    weaveItem.AddMember(newTypeMember);
                }

                _AddWeaveItem(weaveItem);
                _AddWeaveItemMembers(weaveItem.WeaveItemMembers);
            }
        }

        void _AddNewCode(long id, IJoinpoint joinpoint, ICodeAspectDefinition codeAspect)
        {
            var weaveItem = new WeaveItem(_Weaver, id, codeAspect, joinpoint);
            WeaveItemMember weaveItemMember = null;
            if (joinpoint is IInstructionJoinpoint)
                weaveItemMember = new NewCodeInstruction(weaveItem, codeAspect.AdviceMemberDefinition);
            else
                weaveItemMember = new NewCodeBody(weaveItem, codeAspect.AdviceMemberDefinition);
            weaveItem.AddMember(weaveItemMember);
            _AddWeaveItem(weaveItem);
            _AddWeaveItemMembers(weaveItem.WeaveItemMembers);
        }

        void _AddChangeValue(long id, IJoinpoint joinpoint, IAspectDefinition changeValueaspect)
        {
            var weaveItem = new WeaveItem(_Weaver, id, changeValueaspect, joinpoint);
            weaveItem.AddMember(new NewChangeValue(weaveItem, changeValueaspect.AdviceDefinition.AdviceMemberDefinitions.First()));
            _AddWeaveItem(weaveItem);
            _AddWeaveItemMembers(weaveItem.WeaveItemMembers);
        }

        void _AddAttributes(long id, IJoinpoint joinpoint, IAspectDefinition aspect)
        {
            var weaveItem = new WeaveItem(_Weaver, id, aspect, joinpoint);
            var newAttributes = new List<NewAttributeMember>();
            foreach (var attribueAdviceMember in aspect.AdviceDefinition.AdviceMemberDefinitions)
                newAttributes.Add(new NewAttributeMember(weaveItem, attribueAdviceMember));
            _AddWeaveItem(weaveItem);
            _AddWeaveItemMembers(newAttributes);
        }

        void _CheckWeaveItemMemberRedundancies()
        {
            _CheckNewTypeRedundancoes();
            _CheckNewNestedTypeRedundancies();
            _CheckNewbaseTypeReduncdancies();
            _CheckNewTypeMemberRedundancies();
            _CheckNewEnumMemberRedundancies();
            _CheckNewInterfaceMemberRedundancies();
            _CheckNewbaseInterfaceRedundancies();
            _CheckNewCodeRedundancies();
            _CheckNewChangeValueRedundancies();
            _CheckNewAttributeRedundancies();
        }

        void _CheckNewTypeRedundancoes()
        {
            foreach (var newType in SafeWeaveItemMembers.OfType<NewAssemblyType>())
            {
                if (newType.WeaveItem.OnError)
                    continue;
                var weaveItemMembers = WeaveItemMembers.OfType<NewAssemblyType>().Where(t => t.WeaveItem.Joinpoint == newType.WeaveItem.Joinpoint && t.ResolvedFullTypeName == newType.ResolvedFullTypeName);
                if (weaveItemMembers.Count() > 1)
                    weaveItemMembers.ToList().ForEach(t => t.WeaveItem.AddError(WeaverHelper.GetError(t, "AspectMemberAlreadyApplied")));
            }
        }

        void _CheckNewNestedTypeRedundancies()
        {
            foreach (var newNestedType in SafeWeaveItems.OfType<NewNestedType>())
            {
                if (newNestedType.WeaveItem.OnError)
                    continue;
                var weaveItemMembers = WeaveItemMembers.OfType<NewNestedType>().Where(t => !t.AdviceMember.IsCompilerGenerated && t.WeaveItem.Joinpoint == newNestedType.WeaveItem.Joinpoint && t.FullTargetTypeName == newNestedType.FullTargetTypeName);
                if (weaveItemMembers.Count() > 1)
                    weaveItemMembers.ToList().ForEach(t => t.WeaveItem.AddError(WeaverHelper.GetError(t, "AspectMemberAlreadyApplied")));
            }
        }

        void _CheckNewbaseTypeReduncdancies()
        {
            foreach (var newBaseType in SafeWeaveItemMembers.OfType<NewInheritedType>().Where(t => !t.IsInterface))
            {
                if (newBaseType.WeaveItem.OnError)
                    continue;
                var weaveItemMembers = WeaveItemMembers.OfType<NewInheritedType>().Where(t => !t.IsInterface && t.WeaveItem.Joinpoint == newBaseType.WeaveItem.Joinpoint);
                if (weaveItemMembers.Count() > 1)
                    weaveItemMembers.ToList().ForEach(t => t.WeaveItem.AddError(WeaverHelper.GetError(t, "AspectMemberAlreadyApplied")));
            }
        }

        void _CheckNewTypeMemberRedundancies()
        {
            foreach (var newTypeMember in SafeWeaveItemMembers.OfType<NewTypeMember>().Where(t => !CecilHelper.HasCustomAttributesOfType((MemberReference)t.AdviceMember.Member, typeof(CompilerGeneratedAttribute))))
            {
                if (newTypeMember.WeaveItem.OnError)
                    continue;
                IEnumerable<NewTypeMember> newTypeMembers = null;
                switch (newTypeMember.AdviceMember.Member)
                {
                    case FieldDefinition fieldDefinition:
                        newTypeMembers = WeaveItemMembers.OfType<NewTypeMember>().Where(t => t != newTypeMember && t.WeaveItem.Joinpoint == newTypeMember.WeaveItem.Joinpoint && ((IMemberDefinition)t.AdviceMember.Member).Name == fieldDefinition.Name && t.AdviceMember.AdviceMemberKind == newTypeMember.AdviceMember.AdviceMemberKind && !CecilHelper.HasCustomAttributesOfType((MemberReference)t.AdviceMember.Member, typeof(CompilerGeneratedAttribute)));
                        break;
                    case MethodDefinition methodDefinition:
                        newTypeMembers = WeaveItemMembers.OfType<NewTypeMember>().Where(t => t != newTypeMember && !t.AdviceMember.IsCompilerGenerated && t.WeaveItem.Joinpoint == newTypeMember.WeaveItem.Joinpoint && t.AdviceMember.AdviceMemberKind == newTypeMember.AdviceMember.AdviceMemberKind && ((IMemberDefinition)t.AdviceMember.Member).Name == methodDefinition.Name && ((MethodDefinition)t.AdviceMember.Member).GenericParameters.Count == methodDefinition.GenericParameters.Count && CecilHelper.AreTypeReferencesEqual(((MethodDefinition)t.AdviceMember.Member).Parameters.Select(p => p.ParameterType), methodDefinition.Parameters.Select(p => p.ParameterType)) && !CecilHelper.HasCustomAttributesOfType((MemberReference)t.AdviceMember.Member, typeof(CompilerGeneratedAttribute)));
                        break;
                    case EventDefinition eventDefinition:
                        newTypeMembers = WeaveItemMembers.OfType<NewTypeMember>().Where(t => t != newTypeMember && t.WeaveItem.Joinpoint == newTypeMember.WeaveItem.Joinpoint && ((IMemberDefinition)t.AdviceMember.Member).Name == eventDefinition.Name && t.AdviceMember.AdviceMemberKind == newTypeMember.AdviceMember.AdviceMemberKind && !CecilHelper.HasCustomAttributesOfType((MemberReference)t.AdviceMember.Member, typeof(CompilerGeneratedAttribute)));
                        break;
                    case PropertyDefinition propertyDefinition:
                        if (propertyDefinition.Name == "Item")
                        {
                            newTypeMembers = WeaveItemMembers.OfType<NewTypeMember>().Where(t => t != newTypeMember && t.WeaveItem.Joinpoint == newTypeMember.WeaveItem.Joinpoint && ((IMemberDefinition)t.AdviceMember.Member).Name == propertyDefinition.Name && CecilHelper.AreTypeReferencesEqual(((PropertyDefinition)t.AdviceMember.Member).Parameters.Select(p => p.ParameterType), propertyDefinition.Parameters.Select(p => p.ParameterType)) && t.AdviceMember.AdviceMemberKind == newTypeMember.AdviceMember.AdviceMemberKind && !CecilHelper.HasCustomAttributesOfType((MemberReference)t.AdviceMember.Member, typeof(CompilerGeneratedAttribute)));
                        }
                        else
                            newTypeMembers = WeaveItemMembers.OfType<NewTypeMember>().Where(t => t != newTypeMember && t.WeaveItem.Joinpoint == newTypeMember.WeaveItem.Joinpoint && ((IMemberDefinition)t.AdviceMember.Member).Name == propertyDefinition.Name && t.AdviceMember.AdviceMemberKind == newTypeMember.AdviceMember.AdviceMemberKind && !CecilHelper.HasCustomAttributesOfType((MemberReference)t.AdviceMember.Member, typeof(CompilerGeneratedAttribute)));
                        break;
                    default:
                        throw new NotImplementedException();
                }

                if (newTypeMembers.Count() > 1)
                    newTypeMembers.ToList().ForEach(t => t.WeaveItem.AddError(WeaverHelper.GetError(t, "AspectMemberAlreadyApplied", ((MemberReference)newTypeMember.AdviceMember.Member).FullName)));
            }
        }

        void _CheckNewEnumMemberRedundancies()
        {
            foreach (var newEnumMember in SafeWeaveItemMembers.OfType<NewEnumMember>())
            {
                if (newEnumMember.WeaveItem.OnError)
                    continue;
                var weaveItemMembers = SafeWeaveItemMembers.OfType<NewEnumMember>().Where(t => t.WeaveItem.Joinpoint == newEnumMember.WeaveItem.Joinpoint && ((IMemberDefinition)t.AdviceMember.Member).Name == ((IMemberDefinition)newEnumMember.AdviceMember.Member).Name);
                if (weaveItemMembers.Count() > 1)
                {
                    weaveItemMembers.ToList().ForEach(t => t.WeaveItem.AddError(WeaverHelper.GetError(t, "AspectMemberAlreadyApplied")));
                    continue;
                }
            }
        }

        void _CheckNewInterfaceMemberRedundancies()
        {
            foreach (var newInterfaceMember in SafeWeaveItemMembers.OfType<NewTypeMember>().Where(t => t.Member.DeclaringType.IsInterface))
            {
                if (newInterfaceMember.WeaveItem.OnError)
                    continue;
                IEnumerable<NewTypeMember> newTypeMembers = null;
                switch (newInterfaceMember.AdviceMember.Member)
                {
                    case MethodDefinition methodDefinition:
                        newTypeMembers = WeaveItemMembers.OfType<NewTypeMember>().Where(t => t != newInterfaceMember && t.WeaveItem.Joinpoint == newInterfaceMember.WeaveItem.Joinpoint && t.AdviceMember.Member is MethodDefinition && ((MethodDefinition)t.AdviceMember.Member).Name == methodDefinition.Name && ((MethodDefinition)t.AdviceMember.Member).GenericParameters.Count == methodDefinition.GenericParameters.Count && CecilHelper.AreTypeReferencesEqual(((MethodDefinition)t.AdviceMember.Member).Parameters.Select(p => p.ParameterType), methodDefinition.Parameters.Select(p => p.ParameterType)));
                        break;
                    case EventDefinition eventDefinition:
                        newTypeMembers = WeaveItemMembers.OfType<NewTypeMember>().Where(t => t != newInterfaceMember && t.WeaveItem.Joinpoint == newInterfaceMember.WeaveItem.Joinpoint && t.AdviceMember.Member is EventDefinition && ((IMemberDefinition)t.AdviceMember.Member).Name == eventDefinition.Name);
                        break;
                    case PropertyDefinition propertyDefinition:
                        if (propertyDefinition.Name == "Item")
                            newTypeMembers = WeaveItemMembers.OfType<NewTypeMember>().Where(t => t != newInterfaceMember && t.WeaveItem.Joinpoint == newInterfaceMember.WeaveItem.Joinpoint && t.AdviceMember.Member is PropertyDefinition && ((IMemberDefinition)t.AdviceMember.Member).Name == propertyDefinition.Name && CecilHelper.AreTypeReferencesEqual(((PropertyDefinition)t.AdviceMember.Member).Parameters.Select(p => p.ParameterType), propertyDefinition.Parameters.Select(p => p.ParameterType)));
                        else
                            newTypeMembers = WeaveItemMembers.OfType<NewTypeMember>().Where(t => t != newInterfaceMember && t.WeaveItem.Joinpoint == newInterfaceMember.WeaveItem.Joinpoint && t.AdviceMember.Member is PropertyDefinition && ((IMemberDefinition)t.AdviceMember.Member).Name == propertyDefinition.Name);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                if (newTypeMembers.Count() > 1)
                    newTypeMembers.ToList().ForEach(t => t.WeaveItem.AddError(WeaverHelper.GetError(t, "AspectMemberAlreadyApplied")));
            }
        }

        void _CheckNewbaseInterfaceRedundancies()
        {
            foreach (var newBaseInterface in SafeWeaveItemMembers.OfType<NewInheritedType>().Where(t => t.IsInterface))
            {
                if (newBaseInterface.WeaveItem.OnError)
                    continue;
                var weaveItemMembers = WeaveItemMembers.OfType<NewInheritedType>().Where(t => t.IsInterface && t.WeaveItem.Joinpoint == newBaseInterface.WeaveItem.Joinpoint && t.AdviceMember.MemberName == newBaseInterface.AdviceMember.MemberName);
                if (weaveItemMembers.Count() > 1)
                {
                    weaveItemMembers.ToList().ForEach(t => t.WeaveItem.AddError(WeaverHelper.GetError(t, "AspectMemberAlreadyApplied")));
                    continue;
                }
            }
        }

        void _CheckNewAttributeRedundancies()
        {
            foreach (var newAttributeMember in SafeWeaveItemMembers.OfType<NewAttributeMember>())
            {
                if (newAttributeMember.WeaveItem.OnError)
                    continue;
                var newAttributeMembers = SafeWeaveItemMembers.OfType<NewAttributeMember>().Where(t => t.WeaveItem.Joinpoint == newAttributeMember.WeaveItem.Joinpoint && t.FullTargetMemberName == newAttributeMember.FullTargetMemberName);
                if (newAttributeMembers.Count() > 1)
                    newAttributeMembers.ToList().ForEach(t => t.WeaveItem.AddError(WeaverHelper.GetError(t, "AspectMemberAlreadyApplied", newAttributeMember.ClonedAttribute.AttributeType.FullName)));
            }
        }

        void _CheckNewCodeRedundancies()
        {
            foreach (var newCode in SafeWeaveItemMembers.OfType<NewCode>())
            {
                if (newCode.WeaveItem.OnError)
                    continue;
                if ((((ICodeAspectDefinition)newCode.WeaveItem.Aspect).ControlFlow & ControlFlows.body) == ControlFlows.body)
                {
                    var weaveItemMembers = WeaveItemMembers.OfType<NewCodeMember>().Where(t => t.TargetMethod.FullName == newCode.TargetMethod.FullName);
                    if (weaveItemMembers.Count() > 1)
                        weaveItemMembers.ToList().ForEach(t => t.AddError(WeaverHelper.GetError(t, "AspectMemberAlreadyApplied")));
                }
                else
                {
                    var weaveItemMembers = WeaveItemMembers.OfType<NewCodeMember>().Where(t => t != newCode && t.WeaveItem.Joinpoint == newCode.Joinpoint);
                    if (weaveItemMembers.Count() > 1)
                        weaveItemMembers.ToList().ForEach(t => t.AddError(WeaverHelper.GetError(t, "AspectMemberAlreadyApplied")));
                }
            }
        }

        void _CheckNewChangeValueRedundancies()
        {
            foreach (var newChangeValue in SafeWeaveItemMembers.OfType<NewChangeValue>())
            {
                if (newChangeValue.WeaveItem.OnError)
                    continue;
                var newChangeValues = SafeWeaveItemMembers.OfType<NewChangeValue>().Where(t => t != newChangeValue && t.Joinpoint == newChangeValue.Joinpoint);
                if (newChangeValues.Count() > 0)
                    newChangeValues.ToList().ForEach(t => t.WeaveItem.AddError(WeaverHelper.GetError(t, "AspectMemberAlreadyApplied", t.AdviceMember.Name)));
            }
        }

        void _ResolvePrototypeAndAdviceTypeMappings()
        {
            foreach (var weaveItem in WeaveItems.Where(t => t.HasPrototypeItemMappings || t.Aspect.AdviceDefinition.ReferencedPrototypeTypes.Any()))
            {
                foreach (var prototypeItemMappingDefinition in weaveItem.Aspect.PrototypeItemMappingDefinitions)
                {
                    switch (prototypeItemMappingDefinition.SourceKind)
                    {
                        case PrototypeItemMappingSourceKinds.GenericParameter:
                            switch (prototypeItemMappingDefinition.TargetKind)
                            {
                                case PrototypeItemMappingTargetKinds.MethodGenericParameter:
                                    MethodDefinition jointpointMethod = null;
                                    if (weaveItem.Joinpoint is IInstructionJoinpoint)
                                        jointpointMethod = ((IInstructionJoinpoint)weaveItem.Joinpoint).CallingMethod;
                                    else
                                        jointpointMethod = (MethodDefinition)weaveItem.Joinpoint.Member;
                                    var genericArgumentTarget = WeaverHelper.GetGenericArgument(prototypeItemMappingDefinition.TargetName, jointpointMethod);
                                    if (genericArgumentTarget != null)
                                        weaveItem.AddPrototypeItemMapping(prototypeItemMappingDefinition, genericArgumentTarget);
                                    else
                                        weaveItem.AddError(AspectDNErrorFactory.GetError("PrototypeTypeGenericArgumentMappingMissing", prototypeItemMappingDefinition.TargetName, prototypeItemMappingDefinition.PrototypeItemType.FullName, jointpointMethod.FullName));
                                    break;
                                case PrototypeItemMappingTargetKinds.TypeGenericParameter:
                                    TypeDefinition joinpointType = null;
                                    if (weaveItem.Joinpoint.Member is TypeDefinition)
                                        joinpointType = (TypeDefinition)weaveItem.Joinpoint.Member;
                                    else
                                        joinpointType = weaveItem.Joinpoint.Member.DeclaringType;
                                    genericArgumentTarget = WeaverHelper.GetGenericArgument(prototypeItemMappingDefinition.TargetName, joinpointType);
                                    if (genericArgumentTarget == null)
                                    {
                                        TypeDefinition declaringType = joinpointType.DeclaringType;
                                        while (joinpointType.DeclaringType != null)
                                        {
                                            genericArgumentTarget = WeaverHelper.GetGenericArgument(prototypeItemMappingDefinition.TargetName, declaringType);
                                            declaringType = declaringType.DeclaringType;
                                        }
                                    }

                                    if (genericArgumentTarget != null)
                                        weaveItem.AddPrototypeItemMapping(prototypeItemMappingDefinition, genericArgumentTarget);
                                    else
                                        weaveItem.AddError(AspectDNErrorFactory.GetError("PrototypeTypeGenericArgumentMappingMissing", prototypeItemMappingDefinition.TargetName, prototypeItemMappingDefinition.PrototypeItemType.FullName, joinpointType.FullName));
                                    break;
                                default:
                                    throw new NotSupportedException();
                            }

                            break;
                        case PrototypeItemMappingSourceKinds.Member:
                            break;
                        case PrototypeItemMappingSourceKinds.AdviceType:
                            if (prototypeItemMappingDefinition.TargetKind == PrototypeItemMappingTargetKinds.NamespaceOrClass)
                            {
                                WeaveItemMember newType = SafeWeaveItemMembers.OfType<NewAssemblyType>().Where(t => t.Namespace == prototypeItemMappingDefinition.TargetName && ((IMemberDefinition)t.AdviceMember.Member).Name == prototypeItemMappingDefinition.PrototypeItemType.Name).FirstOrDefault();
                                if (newType == null)
                                    newType = SafeWeaveItemMembers.OfType<NewNestedType>().Where(t => CecilHelper.IsTypenameEqual((TypeDefinition)t.WeaveItem.Joinpoint.Member, prototypeItemMappingDefinition.TargetName) && ((IMemberDefinition)t.AdviceMember.Member).Name == prototypeItemMappingDefinition.PrototypeItemType.Name).FirstOrDefault();
                                weaveItem.AddPrototypeItemMapping(prototypeItemMappingDefinition, newType);
                            }

                            if (prototypeItemMappingDefinition.TargetKind == PrototypeItemMappingTargetKinds.CompiledGeneratedMember)
                            {
                                WeaveItemMember newType = SafeWeaveItemMembers.OfType<NewNestedType>().Where(t => t.AdviceMember.IsCompilerGenerated && ((IMemberDefinition)t.AdviceMember.Member).FullName == prototypeItemMappingDefinition.PrototypeItemType.FullName).FirstOrDefault();
                                weaveItem.AddPrototypeItemMapping(prototypeItemMappingDefinition, newType);
                            }

                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }

                foreach (var referencedPrototypeType in weaveItem.Aspect.AdviceDefinition.ReferencedPrototypeTypes)
                {
                    if (_Weaver.PrototypeTypeMappings.ContainsKey(referencedPrototypeType.FullName))
                    {
                        var mappingTarget = _Weaver.PrototypeTypeMappings[referencedPrototypeType.FullName];
                        if (mappingTarget.OnError)
                            weaveItem.AddError(AspectDNErrorFactory.GetError("PrototypeTypeMappingError", referencedPrototypeType.FullName));
                        else
                        {
                            weaveItem.AddPrototypeTypeMapping(mappingTarget);
                        }
                    }
                    else
                        weaveItem.AddError(AspectDNErrorFactory.GetError("PrototypeTypeMappingMissing", referencedPrototypeType.FullName));
                }
            }

            foreach (var weaveItem in WeaveItems.Where(t => t.HasPrototypeItemMappings && t.PrototypeItemMappings.Any(v => v.Target is null)))
                weaveItem.AddError(WeaverHelper.GetError(weaveItem, "WeaveItemWWithPropertyItemWithoutTarget", weaveItem.Aspect.FullAspectDeclarationName));
        }

        void _CloneAdviceTypesAndTypeMembers()
        {
            foreach (var newType in SafeWeaveItemMembers.OfType<INewType>())
            {
                switch (newType)
                {
                    case NewAssemblyType newAssemblyType:
                        newType.ClonedType = WeaverHelper.Clone(newAssemblyType.SourceType, newAssemblyType.Namespace, newAssemblyType.SourceType.Name, newAssemblyType);
                        break;
                    case NewNestedType newNestedType:
                        newNestedType.ClonedType = WeaverHelper.Clone(newNestedType.AdviceTypeDefinition, newNestedType.JoinpointType, newNestedType.TargetName, newNestedType, false);
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }

            foreach (var newType in SafeWeaveItemMembers.OfType<INewType>())
                _CloneTypeGenericParamters((WeaveItemMember)newType, newType.SourceType, newType.ClonedType);
            foreach (var newType in SafeWeaveItemMembers.OfType<INewType>())
            {
                _CloneAddonType(newType.SourceType, newType.ClonedType, (WeaveItemMember)newType);
            }

            foreach (var newTypeMember in SafeWeaveItemMembers.OfType<NewFieldMember>())
                _CloneTypeMember(newTypeMember);
            foreach (var newTypeMember in SafeWeaveItemMembers.OfType<NewMethodMember>())
                _CloneTypeMember(newTypeMember);
            foreach (var newTypeMember in SafeWeaveItemMembers.OfType<NewPropertyMember>())
                _CloneTypeMember(newTypeMember);
            foreach (var newTypeMember in SafeWeaveItemMembers.OfType<NewEventMember>())
                _CloneTypeMember(newTypeMember);
            foreach (var newTypeMember in SafeWeaveItemMembers.OfType<NewConstructorMember>())
                _CloneTypeMember(newTypeMember);
            foreach (var newTypeMember in SafeWeaveItemMembers.OfType<NewOperatorMember>())
                _CloneTypeMember(newTypeMember);
            foreach (var newConstructor in SafeWeaveItemMembers.OfType<NewConstructorMember>())
            {
                foreach (var targetfield in newConstructor.JoinpointDeclaringType.Fields.Where(t => CecilHelper.GetFieldConstantInitILBlock(t) != null))
                {
                    var fieldInitILs = new List<Instruction>();
                    var fieldInitILCode = CecilHelper.GetFieldConstantInitILBlock(targetfield);
                    if (fieldInitILCode != null)
                    {
                        newConstructor.FieldInitILs.AddRange(fieldInitILCode.Instructions);
                    }
                }

                foreach (var newTargetField in SafeWeaveItemMembers.OfType<NewFieldMember>().Where(t => t.JoinpointDeclaringType.FullName == newConstructor.JoinpointDeclaringType.FullName && t.AdviceMember.AdviceDeclaration.FullName != newConstructor.AdviceMember.AdviceDeclaration.FullName))
                {
                    var fieldInitILCode = CecilHelper.GetFieldConstantInitILBlock(newTargetField.SourceField);
                    if (fieldInitILCode != null)
                    {
                        newConstructor.FieldInitILs.AddRange(fieldInitILCode.Instructions);
                    }

                    newConstructor.WeaveItem.AddPrototypeItemMapping(new GeneratedPrototypeItemMapping(PrototypeItemMappingSourceKinds.Member, newTargetField.SourceField, PrototypeItemMappingTargetKinds.Member, newTargetField.WeaveItem, newTargetField.ClonedField));
                }

                if (newConstructor.FieldInitILs.Any())
                {
                    var globalCompilerGeneratedTypeMvids = CecilHelper.GlobalCompilerGeneratedTypes(newConstructor.FieldInitILs).Where(t => t.Module.Mvid != newConstructor.JoinpointDeclaringType.Module.Mvid).Select(t => t.Module.Mvid).Distinct();
                    if (globalCompilerGeneratedTypeMvids.Any())
                    {
                        foreach (var globalCompilerGeneratedTypeMvid in globalCompilerGeneratedTypeMvids)
                        {
                            var clonedGlobalCompilerGeneratedType = _GlobalCompilerGeneratedTypes.First(t => t.Key.Module.Mvid == globalCompilerGeneratedTypeMvid);
                            newConstructor.WeaveItem.AddCompilerGeneratedTypeMapping(clonedGlobalCompilerGeneratedType.Key, clonedGlobalCompilerGeneratedType.Value);
                        }
                    }
                }
            }
        }

        void _CloneTypeGenericParamters(WeaveItemMember newType, TypeDefinition sourceType, TypeDefinition targetType)
        {
            if (!sourceType.HasGenericParameters)
                return;
            WeaverHelper.Clone(sourceType.GenericParameters, newType, targetType).ForEach(t => targetType.GenericParameters.Add(t));
            if (!sourceType.HasNestedTypes)
                return;
            foreach (var sourceNestedType in sourceType.NestedTypes)
                _CloneTypeGenericParamters(newType, sourceNestedType, targetType.NestedTypes[sourceType.NestedTypes.IndexOf(sourceNestedType)]);
        }

        void _CloneAddonType(TypeDefinition sourceType, TypeDefinition clonedType, WeaveItemMember weaveItemMember)
        {
            if (sourceType.HasCustomAttributes)
                WeaverHelper.Clone(sourceType.CustomAttributes, weaveItemMember).ForEach(t => clonedType.CustomAttributes.Add(t));
            if (sourceType.HasFields)
            {
                foreach (var sourceField in sourceType.Fields)
                {
                    var clonedField = WeaverHelper.Clone(sourceField, weaveItemMember);
                    WeaverHelper.Clone(sourceField.CustomAttributes, weaveItemMember).ForEach(t => clonedField.CustomAttributes.Add(t));
                    if (!clonedType.Fields.Any(t => t.FullName == clonedField.FullName))
                    {
                        clonedField.DeclaringType = null;
                        clonedType.Fields.Add(clonedField);
                    }
                }
            }

            if (sourceType.HasMethods)
            {
                foreach (var sourceMethod in sourceType.Methods)
                {
                    var clonedMethod = WeaverHelper.Clone(sourceMethod, weaveItemMember);
                    WeaverHelper.CloneMethodReturnAndParameterTypes(clonedMethod, sourceMethod, weaveItemMember);
                    WeaverHelper.Clone(sourceMethod.CustomAttributes, weaveItemMember).ForEach(t => clonedMethod.CustomAttributes.Add(t));
                    if (sourceMethod.HasParameters)
                    {
                        for (int i = 0; i < sourceMethod.Parameters.Count; i++)
                            WeaverHelper.Clone(sourceMethod.Parameters[i].CustomAttributes, weaveItemMember).ForEach(t => clonedMethod.Parameters[i].CustomAttributes.Add(t));
                    }

                    if (sourceMethod.MethodReturnType.HasCustomAttributes)
                        WeaverHelper.Clone(sourceMethod.MethodReturnType.CustomAttributes, weaveItemMember).ForEach(t => clonedMethod.MethodReturnType.CustomAttributes.Add(t));
                    if (sourceMethod.HasSecurityDeclarations)
                        WeaverHelper.Clone(sourceMethod.SecurityDeclarations, weaveItemMember).ForEach(t => clonedMethod.SecurityDeclarations.Add(t));
                    if (!clonedType.Methods.Any(t => t.FullName == clonedMethod.FullName))
                    {
                        clonedMethod.DeclaringType = null;
                        clonedType.Methods.Add(clonedMethod);
                    }
                }
            }

            if (sourceType.HasEvents)
            {
                foreach (var sourceEvent in sourceType.Events)
                {
                    var cloneEvent = WeaverHelper.Clone(sourceEvent, weaveItemMember);
                    WeaverHelper.Clone(sourceEvent.CustomAttributes, weaveItemMember).ForEach(t => cloneEvent.CustomAttributes.Add(t));
                    if (!clonedType.Events.Any(t => t.FullName == cloneEvent.FullName))
                    {
                        cloneEvent.DeclaringType = null;
                        clonedType.Events.Add(cloneEvent);
                    }
                }
            }

            if (sourceType.HasProperties)
            {
                foreach (var sourceProperty in sourceType.Properties)
                {
                    var clonedProperty = WeaverHelper.Clone(sourceProperty, weaveItemMember);
                    WeaverHelper.Clone(sourceProperty.CustomAttributes, weaveItemMember).ForEach(t => clonedProperty.CustomAttributes.Add(t));
                    if (sourceProperty.HasParameters)
                    {
                        for (int i = 0; i < sourceProperty.Parameters.Count; i++)
                            WeaverHelper.Clone(sourceProperty.Parameters[i].CustomAttributes, weaveItemMember).ForEach(t => clonedProperty.Parameters[i].CustomAttributes.Add(t));
                    }

                    if (!clonedType.Properties.Any(t => t.FullName == clonedProperty.FullName))
                    {
                        clonedProperty.DeclaringType = null;
                        clonedType.Properties.Add(clonedProperty);
                    }
                }
            }

            if (sourceType.BaseType != null)
                clonedType.BaseType = weaveItemMember.Resolve(sourceType.BaseType);
            if (sourceType.HasInterfaces)
            {
                foreach (var sourceInterface in sourceType.Interfaces)
                    clonedType.Interfaces.Add(WeaverHelper.Clone(sourceInterface, weaveItemMember));
            }

            if (sourceType.HasNestedTypes)
            {
                foreach (var sourceNestedType in sourceType.NestedTypes.ToList())
                {
                    var clonedNestedType = weaveItemMember.Resolve(sourceNestedType);
                    _CloneAddonType(sourceNestedType, clonedNestedType, weaveItemMember);
                }
            }
        }

        void _CloneTypeMember(NewTypeMember newTypeMember)
        {
            switch (newTypeMember)
            {
                case NewFieldMember field:
                    field.ClonedField = WeaverHelper.Clone(field.SourceField, newTypeMember);
                    break;
                case NewEventMember @event:
                    @event.ClonedEvent = WeaverHelper.Clone(@event.SourceEvent, newTypeMember);
                    break;
                case NewPropertyMember property:
                    property.ClonedProperty = WeaverHelper.Clone(property.SourceProperty, newTypeMember);
                    break;
                case NewAbstractMethodMember method:
                    method.ClonedMethod = WeaverHelper.Clone(method.SourceMethod, newTypeMember);
                    WeaverHelper.CloneMethodReturnAndParameterTypes(method.ClonedMethod, method.SourceMethod, newTypeMember);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        void _CheckNewAssemblyType()
        {
            foreach (var newType in SafeWeaveItemMembers.OfType<NewAssemblyType>())
            {
                if (newType.WeaveItem.OnError)
                    continue;
                if (newType.JoinpointAssembly.MainModule.GetTypes().Any(t => t.FullName == newType.ResolvedFullTypeName))
                    newType.AddError(WeaverHelper.GetError(newType, "TypeAlreadyExist", newType.ResolvedFullTypeName));
            }
        }

        void _CheckNewNestedTypes()
        {
            foreach (var newNestedType in SafeWeaveItems.OfType<NewNestedType>())
            {
                if (newNestedType.WeaveItem.OnError)
                    continue;
                foreach (var inherited in _GetInheritorTypesFromBaseType(newNestedType.JoinpointType))
                {
                    var flatTypeMembers = new FlatTypeMembers(inherited, null, SafeWeaveItemMembers).ResolveTypeMembers();
                    if (flatTypeMembers.OnError)
                        newNestedType.AddError(WeaverHelper.GetError(newNestedType, "TypeMemberConflict"));
                }
            }
        }

        void _CheckNewbaseType()
        {
            foreach (var newBaseType in SafeWeaveItemMembers.OfType<NewInheritedType>().Where(t => !t.IsInterface))
            {
                if (newBaseType.WeaveItem.OnError)
                    continue;
                if (newBaseType.JoinpointType.IsValueType)
                {
                    newBaseType.WeaveItem.AddError(WeaverHelper.GetError(newBaseType, "WrongBaseValueType", newBaseType.SourceBaseType.FullName, newBaseType.JoinpointType.FullName));
                    continue;
                }

                if (!CecilHelper.IsBaseTypeNullOrEmpty(newBaseType.JoinpointType))
                {
                    newBaseType.WeaveItem.AddError(WeaverHelper.GetError(newBaseType, "BaseTypeAlreadyExist", newBaseType.JoinpointType.FullName));
                    continue;
                }

                newBaseType.TargetBaseType = newBaseType.Resolve(newBaseType.SourceBaseType);
                if (newBaseType.TargetBaseType.HasGenericParameters)
                {
                    newBaseType.ResolvedGenericArguments = newBaseType.Resolve(((GenericInstanceType)newBaseType.SourceBaseType).GenericArguments);
                }

                var baseConstructorMethods = newBaseType.Resolve(newBaseType.SourceBaseType.GetElementType().Resolve()).Methods.Where(t => t.IsConstructor);
                var joinpointConstructors = newBaseType.Resolve(newBaseType.JoinpointType.GetElementType().Resolve()).Methods.Where(t => t.IsConstructor);
                var newWeaveItemConstructors = SafeWeaveItemMembers.OfType<NewTypeMember>().Select(t => t.AdviceMember.Member).OfType<MethodDefinition>().Where(t => t.IsConstructor);
                var defaultBaseTypeConstructor = baseConstructorMethods.FirstOrDefault(t => !t.Parameters.Any());
                IOverrideConstructorDefinition overrideConstructorDefinitionFound = null;
                foreach (var joinpointConstructor in joinpointConstructors)
                {
                    foreach (var overrideConstructorDefinition in newBaseType.OverrideConstructorDefinitions.Where(t => t.OverrideConstructorParameters.Count() == joinpointConstructor.Parameters.Count))
                    {
                        var resolvedOverrideConstructorParameters = newBaseType.Resolve(overrideConstructorDefinition.OverrideConstructorParameters.Select(p => p.ParameterType)).ToArray();
                        if (WeaverHelper.IsSame(resolvedOverrideConstructorParameters, joinpointConstructor.Parameters.Select(p => p.ParameterType).ToArray()))
                        {
                            overrideConstructorDefinitionFound = overrideConstructorDefinition;
                            break;
                        }
                    }

                    if (overrideConstructorDefinitionFound != null)
                    {
                        MethodDefinition baseConstructorMethodFound = null;
                        var resolvedBaseConstructorParameterValueTypes = newBaseType.Resolve(overrideConstructorDefinitionFound.BaseConstructorParameterValueTypes);
                        foreach (var baseConstructorMethod in baseConstructorMethods)
                        {
                            var baseConstructorMethodParameterTypes = baseConstructorMethod.Parameters.Select(t => t.ParameterType).ToArray();
                            for (int i = 0; i < baseConstructorMethodParameterTypes.Length; i++)
                            {
                                if (baseConstructorMethodParameterTypes[i].IsGenericParameter)
                                {
                                    var owner = ((GenericParameter)baseConstructorMethodParameterTypes[i]).Owner;
                                    baseConstructorMethodParameterTypes[i] = ((GenericInstanceType)newBaseType.TargetBaseType).GenericArguments[((GenericParameter)baseConstructorMethodParameterTypes[i]).Position];
                                }

                                if (WeaverHelper.IsSame(resolvedBaseConstructorParameterValueTypes, baseConstructorMethodParameterTypes))
                                {
                                    baseConstructorMethodFound = baseConstructorMethod;
                                    break;
                                }
                            }
                        }

                        if (baseConstructorMethodFound != null)
                            newBaseType.Add(joinpointConstructor, baseConstructorMethodFound, overrideConstructorDefinitionFound.BaseConstructeurParameterValues);
                        else
                            newBaseType.AddError(WeaverHelper.GetError(newBaseType, "OverloadedConstructorMismatch", joinpointConstructor.FullName));
                    }
                    else
                    {
                        if (defaultBaseTypeConstructor == null)
                            newBaseType.AddError(WeaverHelper.GetError(newBaseType, "OverloadedConstructorMismatch", joinpointConstructor.FullName));
                        else
                            newBaseType.Add(joinpointConstructor, defaultBaseTypeConstructor, null);
                    }
                }
            }
        }

        void _BuildILOverloadConstructors()
        {
            foreach (var newBaseType in SafeWeaveItemMembers.OfType<NewInheritedType>().Where(t => !t.IsInterface))
            {
                foreach (var overloadedConstructor in newBaseType.OverloadConstructors)
                {
                    var methodBodyCloner = MethodBodyCloner.Create(overloadedConstructor.JoinpointConstructor, newBaseType);
                    var clone = methodBodyCloner.CloneBody();
                    var ilObjectCtor = CecilHelper.GetIlObjectCtor(clone.Instructions, clone.Method.DeclaringType.BaseType.Resolve());
                    var ilObjectCtorIndex = clone.Instructions.IndexOf(ilObjectCtor);
                    if (overloadedConstructor.BaseArguments != null)
                    {
                        for (int i = 0; i < overloadedConstructor.BaseArguments.Length; i++)
                        {
                            var ils = overloadedConstructor.BaseArguments[i].ToList();
                            if (ils.First().OpCode == OpCodes.Nop)
                                ils.RemoveAt(0);
                            ils.RemoveAt(ils.IndexOf(ils.Last()));
                            foreach (var il in ils)
                            {
                                clone.Instructions.Insert(ilObjectCtorIndex, methodBodyCloner.CloneIL(il));
                                ilObjectCtorIndex = clone.Instructions.IndexOf(ilObjectCtor);
                            }
                        }
                    }

                    MethodReference methodReference = overloadedConstructor.BaseTypeConstructor;
                    if (methodReference.DeclaringType.HasGenericParameters)
                    {
                        methodReference = new MethodReference(methodReference.Name, methodReference.ReturnType, newBaseType.TargetBaseType);
                        methodReference.HasThis = true;
                        foreach (var parameter in overloadedConstructor.BaseTypeConstructor.Parameters)
                            methodReference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
                    }

                    methodReference = CecilHelper.ImportReference(newBaseType.TargetAssembly, methodReference);
                    var ctor = Instruction.Create(OpCodes.Call, methodReference);
                    clone.Instructions.Insert(ilObjectCtorIndex, ctor);
                    clone.Instructions.RemoveAt(clone.Instructions.IndexOf(ilObjectCtor));
                    overloadedConstructor.NewJoinpointConstructorBody = clone;
                }
            }
        }

        void _BuildILOverloadNewConstructors()
        {
            var newContructorMethods = SafeWeaveItemMembers.OfType<NewConstructorMember>().Where(t => t.JoinpointDeclaringType.BaseType.FullName != typeof(object).FullName).Select(t => t.ClonedMethod);
            foreach (var newConstructorMethod in newContructorMethods)
            {
                var ilObjectCtor = CecilHelper.GetIlObjectCtor(newConstructorMethod.Body.Instructions, newConstructorMethod.Module.ImportReference(typeof(object)).Resolve());
                var ilObjectCtorIndex = newConstructorMethod.Body.Instructions.IndexOf(ilObjectCtor);
                MethodReference methodReference = newConstructorMethod.DeclaringType.BaseType.Resolve().Methods.First(t => t.IsConstructor && t.Parameters.Count == 0);
                methodReference = newConstructorMethod.Module.ImportReference(methodReference);
                var ctor = Instruction.Create(OpCodes.Call, methodReference);
                newConstructorMethod.Body.Instructions.Insert(ilObjectCtorIndex, ctor);
                newConstructorMethod.Body.Instructions.RemoveAt(newConstructorMethod.Body.Instructions.IndexOf(ilObjectCtor));
            }
        }

        void _CheckTypeMembersIntegrity()
        {
            foreach (var newBaseType in SafeWeaveItemMembers.OfType<NewInheritedType>().Where(t => !t.IsInterface))
            {
                foreach (var inherited in _GetInheritorTypesFromBaseType(newBaseType.TargetBaseType))
                {
                    var rootFlatType = new FlatTypeMembers(inherited, newBaseType, SafeWeaveItemMembers);
                    if (rootFlatType.OnError)
                        newBaseType.AddError(WeaverHelper.GetError(newBaseType, "TypeMemberConflict", newBaseType.WeaveItem.Aspect.AspectDeclarationName, newBaseType.WeaveItem.Joinpoint.DeclaringType.FullName));
                }
            }

            foreach (var newTypeMember in SafeWeaveItemMembers.OfType<NewTypeMember>())
            {
                foreach (var inheritorType in _GetInheritorTypesFromBaseType((TypeDefinition)newTypeMember.WeaveItem.Joinpoint.Member))
                {
                    var rootFlatType = new FlatTypeMembers(inheritorType, null, SafeWeaveItemMembers).ResolveTypeMembers();
                    if (rootFlatType.OnError)
                        newTypeMember.AddError(WeaverHelper.GetError(newTypeMember, "TypeMemberConflict", newTypeMember.WeaveItem.Aspect.AspectDeclarationName, newTypeMember.WeaveItem.Joinpoint.DeclaringType.FullName));
                }
            }

            foreach (var newTypeMember in SafeWeaveItemMembers.OfType<NewMethodMember>().Where(t => t.IsNew || t.Override))
            {
                if (newTypeMember.Override)
                    newTypeMember.ClonedMethod.Attributes = newTypeMember.ClonedMethod.Attributes | MethodAttributes.Virtual;
                if (newTypeMember.IsNew)
                    newTypeMember.ClonedMethod.Attributes = newTypeMember.ClonedMethod.Attributes | MethodAttributes.NewSlot;
            }
        }

        void _CheckNewEnumMembers()
        {
            foreach (var newEnumMember in SafeWeaveItemMembers.OfType<NewEnumMember>())
            {
                if (((ITypeJoinpoint)newEnumMember.WeaveItem.Joinpoint).TypeDefinition.Fields.Any(t => t.Name == newEnumMember.SourceField.Name))
                    newEnumMember.AddError(WeaverHelper.GetError(newEnumMember, "EnumMemberAlreadyExist", newEnumMember.SourceField.Name));
                else
                    newEnumMember.ClonedField = WeaverHelper.Clone(newEnumMember.SourceField, newEnumMember);
            }
        }

        void _CheckBaseInterfaces()
        {
            foreach (var newBaseInterface in SafeWeaveItemMembers.OfType<NewInheritedType>().Where(t => t.IsInterface && !t.JoinpointType.IsInterface))
                _CheckMemberForNewBaseInterfaceToType(newBaseInterface);
            foreach (var newBaseInterface in SafeWeaveItemMembers.OfType<NewInheritedType>().Where(t => t.IsInterface && t.JoinpointType.IsInterface))
                _CheckMemberForNewBaseInterfaceToInterface(newBaseInterface);
            foreach (var newInterfaceMmember in SafeWeaveItemMembers.OfType<NewTypeMember>().Where(t => t.WeaveItem.Aspect.AspectKind == AspectKinds.InterfaceMembersAspect))
                _CheckNewInterfaceMember(newInterfaceMmember);
        }

        void _CheckMemberForNewBaseInterfaceToType(NewInheritedType newBaseInterface)
        {
            newBaseInterface.TargetBaseType = newBaseInterface.Resolve(newBaseInterface.SourceBaseType);
            newBaseInterface.ResolvedGenericArguments = newBaseInterface.TargetBaseType is GenericInstanceType ? ((GenericInstanceType)newBaseInterface.TargetBaseType).GenericArguments : null;
            var interfaceMembers = new FlatInterfaceMembers(newBaseInterface.TargetBaseType.GetElementType().Resolve(), SafeWeaveItemMembers, newBaseInterface).Check();
            if (interfaceMembers.OnError)
                newBaseInterface.AddError(WeaverHelper.GetError(newBaseInterface, "TypeMemberConflict"));
            var typeMembers = new FlatTypeMembers(newBaseInterface.JoinpointType, null, SafeWeaveItemMembers).ResolveTypeMembers();
            var errors = _CheckFlatInterfaceMembers(interfaceMembers, newBaseInterface.ResolvedGenericArguments, typeMembers);
            foreach (var error in errors)
                newBaseInterface.AddError(WeaverHelper.GetError(newBaseInterface, "TypeMemberConflict", newBaseInterface.FullAspectDeclarationName, newBaseInterface.JoinpointType.FullName));
        }

        IEnumerable<IError> _CheckFlatInterfaceMembers(FlatInterfaceMembers flatInterfaceMembers, IEnumerable<TypeReference> interfaceResolvedGenericArguements, FlatTypeMembers rootFlatType)
        {
            var errors = new List<IError>();
            var interfaceMembers = flatInterfaceMembers.GetInterfaceMembers(interfaceResolvedGenericArguements);
            foreach (var interfaceMember in interfaceMembers)
            {
                var allFlatTypeMembers = rootFlatType.Root.AllFlatTypeMembers;
                if (!allFlatTypeMembers.Any(t => _MemberNameEquals(t, interfaceMember.MemberDefinition) && t.GenericParametersCount == interfaceMember.GenericParametersCount && WeaverHelper.IsSame(t.ResolvedMemberType, interfaceMember.MemberType) && WeaverHelper.IsSame(t.ResolvedParameterTypes, interfaceMember.ParameterTypes)))
                    errors.Add(WeaverHelper.GetError(flatInterfaceMembers.WeaveItemMemberOrigin, "InterfaceMemberConflict", interfaceMember.MemberDefinition.FullName));
            }

            return errors;
        }

        bool _MemberNameEquals(FlatTypeMember flatTypeMember, IMemberDefinition interfaceMember)
        {
            var typeMemberName = flatTypeMember.MemberDefinition.Name;
            if (flatTypeMember.NewTypeMember != null)
                typeMemberName = flatTypeMember.NewTypeMember.ClonedMember.Name;
            var typeMemberNames = CecilHelper.GetMemberNames(typeMemberName);
            if (typeMemberNames.simplename == interfaceMember.Name)
            {
                if (string.IsNullOrEmpty(typeMemberNames.interfaceName))
                    return true;
                return (interfaceMember.DeclaringType.FullName == typeMemberNames.interfaceName || interfaceMember.DeclaringType.FullName.EndsWith($".{typeMemberNames.interfaceName}"));
            }

            return false;
        }

        void _CheckMemberForNewBaseInterfaceToInterface(NewInheritedType newBaseInterface)
        {
            var inheritorInterfaces = _GetInheritorInterfacesFromBaseInterface(newBaseInterface.JoinpointType, false);
            foreach (var inheritorInterface in inheritorInterfaces)
            {
                foreach (var inheritorType in _GetInheritorTypesFromBaseInterfaces(inheritorInterfaces))
                {
                    var flatTypeMembers = new FlatTypeMembers(inheritorType, null, SafeWeaveItemMembers);
                    foreach (var interfaceImplementation in inheritorType.Interfaces.Where(t => t.InterfaceType.GetElementType().Resolve().FullName == inheritorInterface.BaseType.GetElementType().Resolve().FullName))
                    {
                        var flatInterfaceMembers = new FlatInterfaceMembers(inheritorInterface, SafeWeaveItemMembers, newBaseInterface).Check();
                        var resolvedArguments = interfaceImplementation.InterfaceType is GenericInstanceType ? ((GenericInstanceType)interfaceImplementation.InterfaceType).GenericArguments : null;
                        foreach (var error in _CheckFlatInterfaceMembers(flatInterfaceMembers, resolvedArguments, flatTypeMembers))
                            newBaseInterface.AddError(error);
                    }
                }
            }
        }

        void _CheckNewInterfaceMember(NewTypeMember newInterfaceMember)
        {
            var inheritorInterfaces = _GetInheritorInterfacesFromBaseInterface(newInterfaceMember.JoinpointDeclaringType, false);
            foreach (var inheritorInterface in inheritorInterfaces)
            {
                var inheritorTypes = _GetInheritorTypesFromBaseInterface(inheritorInterface).Where(t => !t.IsInterface);
                foreach (var inheritorType in inheritorTypes)
                {
                    var rootFlatTypeMembers = new FlatTypeMembers(inheritorType, null, SafeWeaveItemMembers).ResolveTypeMembers();
                    foreach (var interfaceImplementation in inheritorType.Interfaces.Where(t => t.InterfaceType.GetElementType().Resolve().FullName == inheritorInterface.GetElementType().Resolve().FullName))
                    {
                        var flatInterfaceMembers = new FlatInterfaceMembers(inheritorInterface, SafeWeaveItemMembers, newInterfaceMember).Check();
                        var resolvedArguments = interfaceImplementation.InterfaceType is GenericInstanceType ? ((GenericInstanceType)interfaceImplementation.InterfaceType).GenericArguments : null;
                        foreach (var error in _CheckFlatInterfaceMembers(flatInterfaceMembers, resolvedArguments, rootFlatTypeMembers))
                            newInterfaceMember.AddError(error);
                    }
                }
            }
        }

        void _CheckNewAttributes()
        {
            foreach (var newAttibute in SafeWeaveItemMembers.OfType<NewAttributeMember>())
            {
                if (newAttibute.TargetMember.CustomAttributes.Any(t => t.AttributeType.FullName == newAttibute.SourceAttribute.AttributeType.FullName))
                    newAttibute.AddError(WeaverHelper.GetError(newAttibute, "MemberAlreadyExist"));
                else
                    newAttibute.ClonedAttribute = WeaverHelper.Clone(newAttibute.SourceAttribute, newAttibute);
            }
        }

        void _CheckNewCode()
        {
            foreach (var newCode in SafeWeaveItemMembers.OfType<NewCode>())
            {
                if (newCode.SourceMethod.ReturnType.FullName != typeof(void).FullName)
                {
                    var resolvedReturnType = newCode.Resolve(newCode.SourceMethod.ReturnType);
                    if (!WeaverHelper.IsTypeReferenceCompatible(resolvedReturnType, newCode.TargetMethod.ReturnType))
                    {
                        newCode.AddError(WeaverHelper.GetError(newCode, "CodeReturnTypeMismatch"));
                        continue;
                    }
                }
            }
        }

        void _ResolvePrototypeItemMappings()
        {
            foreach (var weaveItem in SafeWeaveItems.Where(t => t.HasPrototypeItemMappings && (!t.Aspect.AdviceDefinition.IsCompilerGenerated || t.Aspect.AspectKind == AspectKinds.TypeMembersApsect)))
            {
                foreach (var prototypeItemMappingDefinition in weaveItem.PrototypeItemMappingDefinitions.Where(t => t.SourceKind == PrototypeItemMappingSourceKinds.Member))
                {
                    _ResolvePrototypeItemMapping(weaveItem, prototypeItemMappingDefinition);
                }
            }
        }

        void _ResolvePrototypeItemMapping(WeaveItem weaveItem, IPrototypeItemMappingDefinition prototypeItemMappingDefinition)
        {
            object target = null;
            switch (prototypeItemMappingDefinition.PrototypeItem)
            {
                case FieldDefinition fieldDefinition:
                    target = _ResolvePrototypeField(fieldDefinition, prototypeItemMappingDefinition, weaveItem);
                    break;
                case PropertyDefinition property:
                    target = _ResolvePrototypeProperty(property, prototypeItemMappingDefinition, weaveItem);
                    break;
                case EventDefinition @event:
                    throw new NotImplementedException();
                case MethodDefinition method:
                    target = _ResolvePrototypeMethod(method, prototypeItemMappingDefinition, weaveItem);
                    break;
                default:
                    throw new NotImplementedException();
            }

            if (target == null)
                weaveItem.AddError(WeaverHelper.GetError(weaveItem, "PrototypeTargetItemNotFound", prototypeItemMappingDefinition.PrototypeItemMember.Name, prototypeItemMappingDefinition.TargetName));
            else
            {
                if (prototypeItemMappingDefinition.PrototypeItem is FieldReference && target is EventDefinition)
                {
                    var sourceEvent = ((FieldReference)prototypeItemMappingDefinition.PrototypeItem).Resolve().DeclaringType.Events.First(t => t.Name == ((FieldReference)prototypeItemMappingDefinition.PrototypeItem).Name);
                    if (sourceEvent.AddMethod != null)
                    {
                        object targetMethod = ((EventDefinition)target).AddMethod;
                        if (targetMethod is null)
                        {
                            targetMethod = AspectDNErrorFactory.GetWeaverError("PrototypeEventAddMethodMistake", weaveItem.Aspect.FullAspectDeclarationName, weaveItem.Aspect.AdviceDefinition.FullAdviceName, weaveItem.Aspect.Pointcut.FullDeclarationName, sourceEvent.FullName, ((EventDefinition)target).FullName);
                        }

                        var mapping = new GeneratedPrototypeItemMapping(PrototypeItemMappingSourceKinds.Member, sourceEvent.AddMethod, prototypeItemMappingDefinition.TargetKind, weaveItem, targetMethod);
                        weaveItem.AddPrototypeItemMapping(mapping);
                    }

                    if (sourceEvent.RemoveMethod != null)
                    {
                        object targetMethod = ((EventDefinition)target).RemoveMethod;
                        if (targetMethod is null)
                        {
                            targetMethod = AspectDNErrorFactory.GetWeaverError("PrototypeEventRemoveMethodMistake", weaveItem.Aspect.FullAspectDeclarationName, weaveItem.Aspect.AdviceDefinition.FullAdviceName, weaveItem.Aspect.Pointcut.FullDeclarationName, sourceEvent.FullName, ((EventDefinition)target).FullName);
                        }

                        var mapping = new GeneratedPrototypeItemMapping(PrototypeItemMappingSourceKinds.Member, sourceEvent.RemoveMethod, prototypeItemMappingDefinition.TargetKind, weaveItem, targetMethod);
                        weaveItem.AddPrototypeItemMapping(mapping);
                    }

                    if (sourceEvent.HasOtherMethods)
                    {
                        throw new NotImplementedException();
                    }

                    weaveItem.AddPrototypeItemMapping(prototypeItemMappingDefinition, AspectDNErrorFactory.GetWeaverError("PrototypeEventPropertyMistake", weaveItem.Aspect.FullAspectDeclarationName, weaveItem.Aspect.AdviceDefinition.FullAdviceName, weaveItem.Aspect.Pointcut.FullDeclarationName, sourceEvent.FullName, ((EventDefinition)target).FullName));
                }
                else
                {
                    if (prototypeItemMappingDefinition.PrototypeItem is FieldReference && CecilHelper.IsFieldEvent((FieldDefinition)prototypeItemMappingDefinition.PrototypeItem) && target is FieldDefinition && ((FieldDefinition)target).DeclaringType.FullName != weaveItem.Joinpoint.DeclaringType.FullName)
                    {
                        var sourceEvent = CecilHelper.GetEvent((FieldReference)prototypeItemMappingDefinition.PrototypeItem);
                        var targetEvent = CecilHelper.GetEvent((FieldReference)target);
                        if (targetEvent == null)
                            targetEvent = SafeWeaveItems.SelectMany(t => t.WeaveItemMembers).OfType<NewEventMember>().First(t => t.ClonedEvent.Name == ((FieldReference)target).Name).ClonedEvent;
                        if (sourceEvent.AddMethod != null)
                        {
                            object targetMethod = targetEvent.AddMethod;
                            if (targetMethod is null)
                            {
                                targetMethod = AspectDNErrorFactory.GetWeaverError("PrototypeEventAddMethodMistake", weaveItem.Aspect.FullAspectDeclarationName, weaveItem.Aspect.AdviceDefinition.FullAdviceName, weaveItem.Aspect.Pointcut.FullDeclarationName, sourceEvent.FullName, targetEvent.FullName);
                            }

                            var mapping = new GeneratedPrototypeItemMapping(PrototypeItemMappingSourceKinds.Member, sourceEvent.AddMethod, prototypeItemMappingDefinition.TargetKind, weaveItem, targetMethod);
                            weaveItem.AddPrototypeItemMapping(mapping);
                        }

                        if (sourceEvent.RemoveMethod != null)
                        {
                            object targetMethod = targetEvent.RemoveMethod;
                            if (targetMethod is null)
                            {
                                targetMethod = AspectDNErrorFactory.GetWeaverError("PrototypeEventRemoveMethodMistake", weaveItem.Aspect.FullAspectDeclarationName, weaveItem.Aspect.AdviceDefinition.FullAdviceName, weaveItem.Aspect.Pointcut.FullDeclarationName, sourceEvent.FullName, targetEvent.FullName);
                            }

                            var mapping = new GeneratedPrototypeItemMapping(PrototypeItemMappingSourceKinds.Member, sourceEvent.RemoveMethod, prototypeItemMappingDefinition.TargetKind, weaveItem, targetMethod);
                            weaveItem.AddPrototypeItemMapping(mapping);
                        }

                        if (sourceEvent.HasOtherMethods)
                        {
                            throw new NotImplementedException();
                        }

                        var error = AspectDNErrorFactory.GetWeaverError("PrototypeEventPropertyMistake", weaveItem.Aspect.FullAspectDeclarationName, weaveItem.Aspect.AdviceDefinition.FullAdviceName, weaveItem.Aspect.Pointcut.FullDeclarationName, sourceEvent.FullName, targetEvent.FullName);
                        weaveItem.AddPrototypeItemMapping(prototypeItemMappingDefinition, error);
                    }
                    else
                    {
                        if (target is PropertyDefinition && prototypeItemMappingDefinition.PrototypeItem is PropertyDefinition)
                        {
                            var sourceProperty = (PropertyDefinition)prototypeItemMappingDefinition.PrototypeItem;
                            var targetIndexer = (PropertyDefinition)target;
                            if (sourceProperty.SetMethod != null)
                            {
                                object targetMethod = targetIndexer.SetMethod;
                                if (targetMethod is null)
                                {
                                    targetMethod = AspectDNErrorFactory.GetWeaverError("PrototypePropertySetMethodMistake", weaveItem.Aspect.FullAspectDeclarationName, weaveItem.Aspect.AdviceDefinition.FullAdviceName, weaveItem.Aspect.Pointcut.FullDeclarationName, sourceProperty.FullName, weaveItem.Joinpoint.Member.FullName);
                                }

                                var mapping = new GeneratedPrototypeItemMapping(PrototypeItemMappingSourceKinds.Member, sourceProperty.SetMethod, prototypeItemMappingDefinition.TargetKind, weaveItem, targetMethod);
                                weaveItem.AddPrototypeItemMapping(mapping);
                            }

                            if (sourceProperty.GetMethod != null)
                            {
                                object targetMethod = targetIndexer.GetMethod;
                                if (targetMethod is null)
                                {
                                    targetMethod = AspectDNErrorFactory.GetWeaverError("PrototypePropertyGetMethodMistake", weaveItem.Aspect.FullAspectDeclarationName, weaveItem.Aspect.AdviceDefinition.FullAdviceName, weaveItem.Aspect.Pointcut.FullDeclarationName, sourceProperty.FullName, weaveItem.Joinpoint.Member.FullName);
                                }

                                var mapping = new GeneratedPrototypeItemMapping(PrototypeItemMappingSourceKinds.Member, sourceProperty.GetMethod, prototypeItemMappingDefinition.TargetKind, weaveItem, targetMethod);
                                weaveItem.AddPrototypeItemMapping(mapping);
                            }

                            if (sourceProperty.HasOtherMethods)
                            {
                                throw new NotImplementedException();
                            }
                        }
                        else
                            weaveItem.AddPrototypeItemMapping(prototypeItemMappingDefinition, target);
                    }
                }
            }

            if (prototypeItemMappingDefinition.PrototypeItem is FieldReference && CecilHelper.IsFieldEvent((FieldDefinition)prototypeItemMappingDefinition.PrototypeItem) && target is FieldDefinition && ((FieldDefinition)target).DeclaringType.FullName == weaveItem.Joinpoint.DeclaringType.FullName)
            {
                var sourceEvent = CecilHelper.GetEvent((FieldReference)prototypeItemMappingDefinition.PrototypeItem);
                var targetEvent = CecilHelper.GetEvent((FieldReference)target);
                if (targetEvent == null)
                    targetEvent = SafeWeaveItems.SelectMany(t => t.WeaveItemMembers).OfType<NewEventMember>().First(t => t.ClonedEvent.Name == ((FieldReference)target).Name).ClonedEvent;
                if (sourceEvent.AddMethod != null)
                {
                    object targetMethod = targetEvent.AddMethod;
                    if (targetMethod is null)
                    {
                        targetMethod = AspectDNErrorFactory.GetWeaverError("PrototypeEventAddMethodMistake", weaveItem.Aspect.FullAspectDeclarationName, weaveItem.Aspect.AdviceDefinition.FullAdviceName, weaveItem.Aspect.Pointcut.FullDeclarationName, sourceEvent.FullName, targetEvent.FullName);
                    }

                    var mapping = new GeneratedPrototypeItemMapping(PrototypeItemMappingSourceKinds.Member, sourceEvent.AddMethod, prototypeItemMappingDefinition.TargetKind, weaveItem, targetMethod);
                    weaveItem.AddPrototypeItemMapping(mapping);
                }

                if (sourceEvent.RemoveMethod != null)
                {
                    object targetMethod = targetEvent.RemoveMethod;
                    if (targetMethod is null)
                    {
                        targetMethod = AspectDNErrorFactory.GetWeaverError("PrototypeEventRemoveMethodMistake", weaveItem.Aspect.FullAspectDeclarationName, weaveItem.Aspect.AdviceDefinition.FullAdviceName, weaveItem.Aspect.Pointcut.FullDeclarationName, sourceEvent.FullName, targetEvent.FullName);
                    }

                    var mapping = new GeneratedPrototypeItemMapping(PrototypeItemMappingSourceKinds.Member, sourceEvent.RemoveMethod, prototypeItemMappingDefinition.TargetKind, weaveItem, targetMethod);
                    weaveItem.AddPrototypeItemMapping(mapping);
                }

                if (sourceEvent.HasOtherMethods)
                {
                    throw new NotImplementedException();
                }
            }
        }

        object _ResolvePrototypeField(FieldDefinition sourceField, IPrototypeItemMappingDefinition prototypeItemMappingDefinition, WeaveItem weaveItem)
        {
            TypeDefinition joinpointType = null;
            switch (weaveItem.Joinpoint)
            {
                case ITypeJoinpoint typeJoinpoint:
                    joinpointType = typeJoinpoint.TypeDefinition;
                    break;
                case IInstructionJoinpoint instructionJoinpoint:
                    joinpointType = instructionJoinpoint.CallingMethod.DeclaringType;
                    break;
                case IMemberJoinpoint memberJoinpoint:
                    joinpointType = memberJoinpoint.DeclaringType;
                    break;
                default:
                    throw new NotSupportedException();
            }

            var target = _ResolvePrototypeField(sourceField, prototypeItemMappingDefinition.TargetKind, prototypeItemMappingDefinition.TargetName, joinpointType, weaveItem);
            if (target != null)
                return target.TargetItem;
            MethodDefinition joinpointMethod = null;
            switch (weaveItem.Joinpoint)
            {
                case IInstructionJoinpoint instructionJoinpoint:
                    joinpointMethod = instructionJoinpoint.CallingMethod;
                    break;
                case IMemberJoinpoint memberJoinpoint:
                    if (memberJoinpoint.Member is MethodDefinition)
                        joinpointMethod = (MethodDefinition)memberJoinpoint.Member;
                    break;
            }

            if (joinpointMethod == null)
                return null;
            var joinpointMethodParameter = joinpointMethod.Parameters.Where(t => t.Name == prototypeItemMappingDefinition.TargetName).FirstOrDefault();
            if (joinpointMethodParameter != null)
            {
                target = new ResolvedPrototypeItem(joinpointMethodParameter, new DeclaringTypedType(null, joinpointMethod.DeclaringType, joinpointMethod.DeclaringType.GenericParameters.Cast<TypeReference>()));
                if (target != null || _IsReturnTypeCompatible(target, sourceField, weaveItem))
                    return target.TargetItem;
            }

            if (joinpointMethod.Body == null)
                return null;
            VariableDefinition variableTarget = null;
            int variableIndex = -1;
            if (!int.TryParse(prototypeItemMappingDefinition.TargetName, out variableIndex) && joinpointMethod.DebugInformation != null)
            {
                string variableName = null;
                variableIndex = -1;
                foreach (var variable in joinpointMethod.Body.Variables)
                {
                    variableName = variable.ToString();
                    var debugName = "";
                    if (joinpointMethod.DebugInformation.TryGetName(variable, out debugName))
                        variableName = debugName;
                    if (variableName == prototypeItemMappingDefinition.TargetName)
                    {
                        variableTarget = variable;
                        break;
                    }
                }
            }
            else
            {
                if (variableIndex != -1 && variableIndex < joinpointMethod.Body.Variables.Count)
                    variableTarget = joinpointMethod.Body.Variables[variableIndex];
            }

            if (variableTarget == null)
                return null;
            target = new ResolvedPrototypeItem(variableTarget, new DeclaringTypedType(null, joinpointMethod.DeclaringType, joinpointMethod.DeclaringType.GenericParameters.Cast<TypeReference>()));
            if (target != null || _IsReturnTypeCompatible(target, sourceField, weaveItem))
                return target.TargetItem;
            return null;
        }

        ResolvedPrototypeItem _ResolvePrototypeField(FieldDefinition sourceField, PrototypeItemMappingTargetKinds kind, string targetName, TypeDefinition fromType, WeaveItem weaveItem)
        {
            ResolvedPrototypeItem resolvedPrototypeItem = null;
            object target = null;
            var declaringTypedTypes = new TypedTypeVisitor().Visit(fromType, weaveItem.Weaver.SafeWeaveItemMembers);
            switch (kind)
            {
                case PrototypeItemMappingTargetKinds.ThisMember:
                    target = _ResolvePrototypeField(targetName, declaringTypedTypes.First());
                    resolvedPrototypeItem = new ResolvedPrototypeItem(target, declaringTypedTypes.First());
                    break;
                case PrototypeItemMappingTargetKinds.BaseMember:
                    foreach (var declaringTypedType in declaringTypedTypes.Where(t => t.ParentType != null))
                    {
                        target = _ResolvePrototypeField(targetName, declaringTypedType);
                        if (target != null)
                        {
                            resolvedPrototypeItem = new ResolvedPrototypeItem(target, declaringTypedType);
                            break;
                        }
                    }

                    break;
                case PrototypeItemMappingTargetKinds.Member:
                    foreach (var declaringTypedType in declaringTypedTypes)
                    {
                        target = _ResolvePrototypeField(targetName, declaringTypedType);
                        if (target != null)
                        {
                            resolvedPrototypeItem = new ResolvedPrototypeItem(target, declaringTypedTypes.First());
                            break;
                        }
                    }

                    break;
                default:
                    throw new NotImplementedException();
            }

            if (resolvedPrototypeItem == null || !_IsReturnTypeCompatible(resolvedPrototypeItem, sourceField, weaveItem))
                resolvedPrototypeItem = null;
            if (resolvedPrototypeItem == null || (resolvedPrototypeItem.TargetItem is PropertyDefinition && !_CheckGetSetIntegrity(sourceField.Name, weaveItem, resolvedPrototypeItem.TargetItem)))
                resolvedPrototypeItem = null;
            return resolvedPrototypeItem;
        }

        object _ResolvePrototypeField(string targetName, DeclaringTypedType fromType)
        {
            IMemberDefinition targetMember = fromType.Type.Fields.Where(t => t.Name == targetName).FirstOrDefault();
            if (targetMember == null)
            {
                targetMember = fromType.Type.Properties.Where(t => t.Name == targetName && !t.HasParameters).FirstOrDefault();
                if (targetMember == null)
                {
                    targetMember = fromType.Type.Events.FirstOrDefault(t => t.Name == targetName);
                    if (targetMember == null)
                    {
                        targetMember = SafeWeaveItemMembers.OfType<NewTypeMember>().Where(t => t.JoinpointDeclaringType.FullName == fromType.Type.FullName && (t.ClonedMember is FieldDefinition || t.ClonedMember is PropertyDefinition || t.ClonedMember is EventDefinition) && t.ClonedMember.Name == targetName).Select(t => t.ClonedMember).FirstOrDefault();
                    }
                }
            }

            return targetMember;
        }

        object _ResolvePrototypeMethod(MethodDefinition sourceMethod, IPrototypeItemMappingDefinition prototypeItemMappingDefinition, WeaveItem weaveItem)
        {
            object target = null;
            var targetName = prototypeItemMappingDefinition.TargetName;
            if (prototypeItemMappingDefinition.TargetKind == PrototypeItemMappingTargetKinds.CompiledGeneratedMember)
                targetName = prototypeItemMappingDefinition.PrototypeItemMember.DeclaringType.FullName + "." + ((MethodDefinition)prototypeItemMappingDefinition.PrototypeItem).Name;
            TypeDefinition joinpointType = null;
            switch (weaveItem.Joinpoint)
            {
                case ITypeJoinpoint typeJoinpoint:
                    joinpointType = typeJoinpoint.TypeDefinition;
                    break;
                case IInstructionJoinpoint instructionJoinpoint:
                    joinpointType = instructionJoinpoint.CallingMethod.DeclaringType;
                    break;
                case IMemberJoinpoint memberJoinpoint:
                    joinpointType = memberJoinpoint.DeclaringType;
                    break;
                default:
                    throw new NotSupportedException();
            }

            if (sourceMethod.IsConstructor)
                target = _ResolvePrototypeConstructor(sourceMethod, joinpointType, weaveItem);
            else
                target = _ResolvePrototypeMethod(sourceMethod, prototypeItemMappingDefinition.TargetKind, prototypeItemMappingDefinition.TargetName, joinpointType, weaveItem);
            return target;
        }

        object _ResolvePrototypeMethod(MethodDefinition sourceMethod, PrototypeItemMappingTargetKinds targetKind, string targetName, TypeDefinition fromType, WeaveItem weaveItem)
        {
            object target = null;
            var declaringTypedTypes = new TypedTypeVisitor().Visit(fromType, weaveItem.Weaver.SafeWeaveItemMembers);
            switch (targetKind)
            {
                case PrototypeItemMappingTargetKinds.ThisMember:
                    target = _ResolvePrototypeMethod(sourceMethod, targetName, declaringTypedTypes.First(), weaveItem);
                    if (target == null)
                    {
                        target = _ResolvePrototypeMethodDelegate(sourceMethod, targetName, declaringTypedTypes.First(), weaveItem);
                    }

                    break;
                case PrototypeItemMappingTargetKinds.BaseMember:
                    foreach (var declaringTypedType in declaringTypedTypes.Where(t => t.ParentType != null))
                    {
                        target = _ResolvePrototypeMethod(sourceMethod, targetName, declaringTypedType, weaveItem);
                        if (target != null)
                            break;
                    }

                    if (target == null)
                    {
                        foreach (var declaringTypedType in declaringTypedTypes.Where(t => t.ParentType != null))
                        {
                            target = _ResolvePrototypeMethodDelegate(sourceMethod, targetName, declaringTypedType, weaveItem);
                            if (target != null)
                                break;
                        }
                    }

                    break;
                case PrototypeItemMappingTargetKinds.Member:
                    foreach (var declaringTypedType in declaringTypedTypes)
                    {
                        target = _ResolvePrototypeMethod(sourceMethod, targetName, declaringTypedType, weaveItem);
                        if (target != null)
                            break;
                        ;
                    }

                    if (target == null)
                    {
                        foreach (var declaringTypedType in declaringTypedTypes)
                        {
                            target = _ResolvePrototypeMethodDelegate(sourceMethod, targetName, declaringTypedType, weaveItem);
                            if (target != null)
                                break;
                        }
                    }

                    break;
                case PrototypeItemMappingTargetKinds.CompiledGeneratedMember:
                    var resolvedDeclarationType = weaveItem.Resolve(sourceMethod.DeclaringType);
                    targetName = $"{sourceMethod.DeclaringType.FullName}.{sourceMethod.Name}";
                    var resolvedDeclaringTypedType = new DeclaringTypedType(null, resolvedDeclarationType, resolvedDeclarationType.GenericParameters.Cast<TypeReference>());
                    target = _ResolvePrototypeMethod(sourceMethod, targetName, resolvedDeclaringTypedType, weaveItem);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return target;
        }

        object _ResolvePrototypeMethod(MethodDefinition sourceMethod, string targetName, DeclaringTypedType declaringTypedType, WeaveItem weaveItem)
        {
            var targetMethods = declaringTypedType.Type.Methods.Where(t => t.Name == targetName && t.Parameters.Count == sourceMethod.Parameters.Count && t.GenericParameters.Count == sourceMethod.GenericParameters.Count);
            targetMethods = targetMethods.Union(SafeWeaveItems.SelectMany(m => m.WeaveItemMembers).OfType<NewMethodMember>().Where(t => t.ClonedMethod.Name == targetName && t.ClonedMethod.GenericParameters.Count == sourceMethod.GenericParameters.Count && t.ClonedMethod.Parameters.Count == sourceMethod.Parameters.Count).Select(t => t.ClonedMethod));
            object target = null;
            foreach (var targetMethod in targetMethods)
            {
                var resolvedPrototypeItem = new ResolvedPrototypeItem(targetMethod, declaringTypedType);
                var genericResolutionContext = new GenericResolutionContext(sourceMethod, targetMethod);
                var isReturnTypeCompatible = _IsReturnTypeCompatible(resolvedPrototypeItem, sourceMethod, weaveItem, genericResolutionContext);
                var areParameterTypesCompatible = false;
                if (isReturnTypeCompatible)
                    areParameterTypesCompatible = _AreParameterTypesCompatible(resolvedPrototypeItem, (IEnumerable<ParameterDefinition>)sourceMethod.Parameters, (IEnumerable<ParameterDefinition>)targetMethod.Parameters, weaveItem, genericResolutionContext);
                if (isReturnTypeCompatible && areParameterTypesCompatible)
                {
                    target = targetMethod;
                    break;
                }
            }

            if (target != null)
            {
                var isNewSlotVirtual = ((MethodDefinition)target).IsNewSlot && ((MethodDefinition)target).IsVirtual;
                var isInstance = !((MethodDefinition)target).IsNewSlot && !((MethodDefinition)target).IsVirtual;
                var isOverride = !((MethodDefinition)target).IsNewSlot && ((MethodDefinition)target).IsVirtual;
                if (!isNewSlotVirtual && !isInstance && !isOverride)
                    target = null;
            }

            return target;
        }

        object _ResolvePrototypeConstructor(MethodDefinition sourceConstructor, TypeDefinition fromType, WeaveItem weaveItem)
        {
            object target = null;
            DeclaringTypedType declaringTypedType = new DeclaringTypedType(null, fromType, fromType.GenericParameters.Cast<TypeReference>());
            var targetConstructors = fromType.Methods.Where(t => t.IsConstructor && t.Parameters.Count == sourceConstructor.Parameters.Count);
            targetConstructors = targetConstructors.Union(SafeWeaveItems.SelectMany(m => m.WeaveItemMembers).OfType<NewConstructorMember>().Where(t => t.ClonedMethod.Parameters.Count == sourceConstructor.Parameters.Count).Select(t => t.ClonedMethod));
            foreach (var targetConstructor in targetConstructors)
            {
                var resolvedPrototypeItem = new ResolvedPrototypeItem(targetConstructor, declaringTypedType);
                if (_IsReturnTypeCompatible(resolvedPrototypeItem, sourceConstructor, weaveItem) && _AreParameterTypesCompatible(resolvedPrototypeItem, (IEnumerable<ParameterDefinition>)sourceConstructor.Parameters, (IEnumerable<ParameterDefinition>)targetConstructor.Parameters, weaveItem, null))
                {
                    target = targetConstructor;
                    break;
                }
            }

            return target;
        }

        object _ResolvePrototypeMethodDelegate(MethodDefinition sourceMethod, string targetName, DeclaringTypedType declaringTypedType, WeaveItem weaveItem)
        {
            object target = null;
            var resolvedFieldDelegateTargets = declaringTypedType.Type.Fields.Where(t => t.Name == targetName && CecilHelper.IsDelegate(t));
            resolvedFieldDelegateTargets = resolvedFieldDelegateTargets.Union(SafeWeaveItems.SelectMany(m => m.WeaveItemMembers).OfType<NewFieldMember>().Where(t => t.ClonedField.DeclaringType.FullName == declaringTypedType.Type.FullName && t.ClonedField.Name == targetName && CecilHelper.IsDelegate(t.ClonedField)).Select(t => t.ClonedField));
            foreach (var resolvedFieldDelegateTarget in resolvedFieldDelegateTargets)
            {
                var resolvedFieldReturnType = WeaverHelper.ResolveDefinition(resolvedFieldDelegateTarget, weaveItem);
                MethodDefinition targetMethod = resolvedFieldDelegateTarget.FieldType.Resolve().Methods.Where(t => t.Name == "Invoke").First();
                var resolvedPrototypeItem = new ResolvedPrototypeItem(targetMethod, declaringTypedType);
                var genericResolutionContext = new GenericResolutionContext(sourceMethod, targetMethod);
                if (_IsReturnTypeCompatible(resolvedPrototypeItem, sourceMethod, weaveItem) && _AreParameterTypesCompatible(resolvedPrototypeItem, (IEnumerable<ParameterDefinition>)sourceMethod.Parameters, (IEnumerable<ParameterDefinition>)targetMethod.Parameters, weaveItem, genericResolutionContext))
                {
                    target = resolvedFieldDelegateTarget;
                    break;
                }
            }

            return target;
        }

        object _ResolvePrototypeProperty(PropertyDefinition sourceProperty, IPrototypeItemMappingDefinition prototypeItemMappingDefinition, WeaveItem weaveItem)
        {
            TypeDefinition joinpointType = null;
            switch (weaveItem.Joinpoint)
            {
                case ITypeJoinpoint typeJoinpoint:
                    joinpointType = typeJoinpoint.TypeDefinition;
                    break;
                case IInstructionJoinpoint instructionJoinpoint:
                    joinpointType = instructionJoinpoint.CallingMethod.DeclaringType;
                    break;
                case IMemberJoinpoint memberJoinpoint:
                    joinpointType = memberJoinpoint.DeclaringType;
                    break;
                default:
                    throw new NotSupportedException();
            }

            var target = _ResolvePrototypeProperty(sourceProperty, prototypeItemMappingDefinition.TargetKind, sourceProperty.Name, joinpointType, weaveItem);
            return target;
        }

        object _ResolvePrototypeProperty(PropertyDefinition sourceProperty, PrototypeItemMappingTargetKinds kind, string targetName, TypeDefinition fromType, WeaveItem weaveItem)
        {
            object target = null;
            var declaringTypedTypes = new TypedTypeVisitor().Visit(fromType, weaveItem.Weaver.SafeWeaveItemMembers);
            switch (kind)
            {
                case PrototypeItemMappingTargetKinds.ThisMember:
                    target = _ResolvePrototypeProperty(sourceProperty, targetName, declaringTypedTypes.First(), weaveItem);
                    break;
                case PrototypeItemMappingTargetKinds.BaseMember:
                    foreach (var declaringTypedType in declaringTypedTypes.Where(t => t.ParentType != null))
                    {
                        target = _ResolvePrototypeProperty(sourceProperty, targetName, declaringTypedType, weaveItem);
                        if (target != null)
                            break;
                    }

                    break;
                case PrototypeItemMappingTargetKinds.Member:
                    foreach (var declaringTypedType in declaringTypedTypes)
                    {
                        target = _ResolvePrototypeProperty(sourceProperty, targetName, declaringTypedType, weaveItem);
                        if (target != null)
                            break;
                        ;
                    }

                    break;
                default:
                    throw new NotImplementedException();
            }

            return target;
        }

        object _ResolvePrototypeProperty(PropertyDefinition sourceProperty, string targetName, DeclaringTypedType declaringTypedType, WeaveItem weaveItem)
        {
            var targetproperties = declaringTypedType.Type.Properties.Where(t => t.Name == targetName && t.Parameters.Count == sourceProperty.Parameters.Count);
            targetproperties = targetproperties.Union(SafeWeaveItems.SelectMany(m => m.WeaveItemMembers).OfType<NewPropertyMember>().Where(t => t.ClonedProperty.Name == targetName && t.ClonedProperty.Parameters.Count == sourceProperty.Parameters.Count).Select(t => t.ClonedProperty));
            object target = null;
            foreach (var targetProperty in targetproperties)
            {
                var resolvedPrototypeItem = new ResolvedPrototypeItem(targetProperty, declaringTypedType);
                if (_IsReturnTypeCompatible(resolvedPrototypeItem, sourceProperty, weaveItem) && _AreParameterTypesCompatible(resolvedPrototypeItem, (IEnumerable<ParameterDefinition>)sourceProperty.Parameters, (IEnumerable<ParameterDefinition>)targetProperty.Parameters, weaveItem, null))
                {
                    target = targetProperty;
                    break;
                }
            }

            return target;
        }

        bool _IsReturnTypeCompatible(ResolvedPrototypeItem resolvePrototypeItem, IMemberDefinition sourcePrototypeMember, WeaveItem weaveItem, GenericResolutionContext genericResolutionContext)
        {
            TypeReference targetReturnTypeReference = null;
            switch (resolvePrototypeItem.TargetItem)
            {
                case FieldDefinition fieldDefinition:
                    targetReturnTypeReference = fieldDefinition.FieldType;
                    break;
                case PropertyDefinition propertyDefinition:
                    targetReturnTypeReference = propertyDefinition.PropertyType;
                    break;
                case EventDefinition eventDefinition:
                    targetReturnTypeReference = eventDefinition.EventType;
                    break;
                case ParameterDefinition parameterDefinition:
                    targetReturnTypeReference = parameterDefinition.ParameterType;
                    break;
                case VariableDefinition variableDefinition:
                    targetReturnTypeReference = variableDefinition.VariableType;
                    break;
                case MethodDefinition methodDefinition:
                    targetReturnTypeReference = methodDefinition.ReturnType;
                    break;
                default:
                    throw new NotSupportedException();
            }

            TypeReference sourceReturnTypeReference = null;
            switch (sourcePrototypeMember)
            {
                case FieldDefinition fieldDefinition:
                    sourceReturnTypeReference = fieldDefinition.FieldType;
                    break;
                case PropertyDefinition propertyDefinition:
                    sourceReturnTypeReference = propertyDefinition.PropertyType;
                    break;
                case EventDefinition eventDefinition:
                    sourceReturnTypeReference = eventDefinition.EventType;
                    break;
                case MethodDefinition methodDefinition:
                    sourceReturnTypeReference = methodDefinition.ReturnType;
                    break;
                default:
                    throw new NotSupportedException();
            }

            if ((targetReturnTypeReference is GenericParameter && !(sourceReturnTypeReference is GenericParameter)) || (sourceReturnTypeReference is GenericParameter && !(targetReturnTypeReference is GenericParameter)))
                return false;
            return _IsTypeReferenceCompatible(resolvePrototypeItem, sourceReturnTypeReference, targetReturnTypeReference, weaveItem, genericResolutionContext);
        }

        bool _IsTypeReferenceCompatible(ResolvedPrototypeItem resolvePrototypeItem, TypeReference sourceTypeReference, TypeReference targetTypeReference, WeaveItem weaveItem, GenericResolutionContext genericResolutionContext)
        {
            if ((targetTypeReference is GenericParameter && !(sourceTypeReference is GenericParameter)) || (sourceTypeReference is GenericParameter && !(targetTypeReference is GenericParameter)))
                return false;
            if (sourceTypeReference is GenericParameter)
            {
                if (((GenericParameter)sourceTypeReference).Owner is MethodDefinition)
                {
                    if (((GenericParameter)targetTypeReference).Owner is MethodDefinition)
                    {
                        return ((GenericParameter)sourceTypeReference).Position == ((GenericParameter)targetTypeReference).Position;
                    }
                    else
                        return false;
                }
                else
                {
                    if (((GenericParameter)targetTypeReference).Owner is TypeDefinition)
                    {
                        var targetGenericParameter = resolvePrototypeItem.DeclaringTypedType.GenericArguments.ToArray()[((GenericParameter)targetTypeReference).Position];
                        var sourcenGenericParameter = (GenericParameter)weaveItem.Resolve((TypeReference)sourceTypeReference);
                        return targetGenericParameter.FullName == sourcenGenericParameter.FullName;
                    }
                    else
                        return false;
                }
            }
            else
            {
                var sourceReturnTypeReference = weaveItem.Resolve(sourceTypeReference, genericResolutionContext);
                var targetReturnTypeReference = targetTypeReference;
                if (sourceReturnTypeReference.FullName == targetReturnTypeReference.FullName)
                    return true;
                return CheckBoxing(sourceReturnTypeReference, targetReturnTypeReference, weaveItem.Weaver.SafeWeaveItemMembers);
            }
        }

        internal bool CheckBoxing(TypeReference boxingType, TypeReference typeToBox, IEnumerable<WeaveItemMember> safeWeaveItemMembers)
        {
            var baseTypeList = new TypedTypeVisitor().Visit(typeToBox, safeWeaveItemMembers, true);
            var boxingGenericArguments = CecilHelper.GetGenericArguments(boxingType);
            foreach (var baseType in baseTypeList)
            {
                if (baseType.Type.FullName != boxingType.GetElementType().Resolve().FullName)
                    continue;
                if (baseType.GenericArguments.Count() != boxingGenericArguments.Count())
                    continue;
                if (baseType.GenericArguments.Count() > 0)
                {
                    var a = baseType.GenericArguments.ToArray();
                    var b = boxingGenericArguments.ToArray();
                    bool isEqual = true;
                    for (int i = 0; i < a.Length; i++)
                    {
                        isEqual = a[i].FullName == b[i].FullName;
                        if (!isEqual)
                            break;
                    }

                    if (!isEqual)
                        continue;
                }

                return true;
            }

            return false;
        }

        bool _AreParameterTypesCompatible(ResolvedPrototypeItem resolvePrototypeItem, IEnumerable<ParameterDefinition> sourceParameters, IEnumerable<ParameterDefinition> targetParameters, WeaveItem weaveItem, GenericResolutionContext genericResolutionContext)
        {
            if (sourceParameters.Count() != targetParameters.Count())
                return false;
            for (int i = 0; i < sourceParameters.Count(); i++)
            {
                if (!_IsTypeReferenceCompatible(resolvePrototypeItem, sourceParameters.ElementAt(i).ParameterType, targetParameters.ElementAt(i).ParameterType, weaveItem, genericResolutionContext))
                    return false;
            }

            return true;
        }

        object _ResolvePrototypeMethodMappings(MethodDefinition sourceMethod, IPrototypeItemMappingDefinition prototypeItemMappingDefinition, WeaveItem weaveItem)
        {
            object target = null;
            var targetName = prototypeItemMappingDefinition.TargetName;
            if (prototypeItemMappingDefinition.TargetKind == PrototypeItemMappingTargetKinds.CompiledGeneratedMember)
                targetName = prototypeItemMappingDefinition.PrototypeItemMember.DeclaringType.FullName + "." + ((MethodDefinition)prototypeItemMappingDefinition.PrototypeItem).Name;
            var resolvedParameterTypes = new TypeReference[sourceMethod.Parameters.Count];
            int i = 0;
            foreach (var parameterTypeReference in sourceMethod.Parameters.Select(t => t.ParameterType))
            {
                if (parameterTypeReference.IsGenericParameter && ((GenericParameter)parameterTypeReference).Owner is MethodDefinition && ((MethodDefinition)((GenericParameter)parameterTypeReference).Owner).FullName == sourceMethod.FullName)
                    resolvedParameterTypes[i++] = parameterTypeReference;
                else
                    resolvedParameterTypes[i++] = weaveItem.Resolve(parameterTypeReference);
            }

            var genericParametersCount = sourceMethod.GenericParameters.Count();
            TypeDefinition joinpointType = null;
            switch (weaveItem.Joinpoint)
            {
                case ITypeJoinpoint typeJoinpoint:
                    joinpointType = typeJoinpoint.TypeDefinition;
                    break;
                case IInstructionJoinpoint instructionJoinpoint:
                    joinpointType = instructionJoinpoint.CallingMethod.DeclaringType;
                    break;
                case IMemberJoinpoint memberJoinpoint:
                    joinpointType = memberJoinpoint.DeclaringType;
                    break;
                default:
                    throw new NotSupportedException();
            }

            if (sourceMethod.IsConstructor)
                target = _ResolvePrototypeConstructorMappings(sourceMethod, prototypeItemMappingDefinition.TargetKind, genericParametersCount, resolvedParameterTypes, joinpointType, weaveItem);
            else
                target = _ResolvePrototypeMethodMappings(sourceMethod, prototypeItemMappingDefinition.TargetKind, prototypeItemMappingDefinition.TargetName, genericParametersCount, resolvedParameterTypes, joinpointType, weaveItem);
            return target;
        }

        object _ResolvePrototypeMethodMappings(MethodDefinition sourceMethod, PrototypeItemMappingTargetKinds targetKind, string targetName, int genericParametersCount, IEnumerable<TypeReference> parameterTypes, TypeDefinition fromType, WeaveItem weaveItem)
        {
            object target = null;
            switch (targetKind)
            {
                case PrototypeItemMappingTargetKinds.ThisMember:
                    target = _ResolvePrototypeMethodMappings(sourceMethod, targetName, genericParametersCount, parameterTypes, fromType, weaveItem);
                    if (target == null)
                    {
                        target = _ResolvePrototypeMethodDelegateMappings(sourceMethod, targetName, genericParametersCount, parameterTypes, fromType, weaveItem);
                    }

                    break;
                case PrototypeItemMappingTargetKinds.BaseMember:
                    foreach (var baseType in WeaverHelper.GetBaseTypes(fromType, SafeWeaveItemMembers))
                    {
                        target = _ResolvePrototypeMethodMappings(sourceMethod, targetName, genericParametersCount, parameterTypes, baseType, weaveItem);
                        if (target != null)
                            break;
                    }

                    if (target == null)
                    {
                        foreach (var baseType in WeaverHelper.GetBaseTypes(fromType, SafeWeaveItemMembers))
                        {
                            target = _ResolvePrototypeMethodDelegateMappings(sourceMethod, targetName, genericParametersCount, parameterTypes, baseType, weaveItem);
                            if (target != null)
                                break;
                        }
                    }

                    break;
                case PrototypeItemMappingTargetKinds.Member:
                    target = _ResolvePrototypeMethodMappings(sourceMethod, targetName, genericParametersCount, parameterTypes, fromType, weaveItem);
                    if (target == null)
                    {
                        foreach (var baseType in WeaverHelper.GetBaseTypes(fromType, SafeWeaveItemMembers))
                        {
                            target = _ResolvePrototypeMethodMappings(sourceMethod, targetName, genericParametersCount, parameterTypes, baseType, weaveItem);
                            if (target != null)
                                break;
                        }
                    }

                    if (target == null)
                    {
                        target = _ResolvePrototypeMethodDelegateMappings(sourceMethod, targetName, genericParametersCount, parameterTypes, fromType, weaveItem);
                        if (target == null)
                        {
                            foreach (var baseType in WeaverHelper.GetBaseTypes(fromType, SafeWeaveItemMembers))
                            {
                                target = _ResolvePrototypeMethodDelegateMappings(sourceMethod, targetName, genericParametersCount, parameterTypes, baseType, weaveItem);
                                if (target != null)
                                    break;
                            }
                        }
                    }

                    break;
                case PrototypeItemMappingTargetKinds.CompiledGeneratedMember:
                    var resolvedDeclarationType = weaveItem.Resolve(sourceMethod.DeclaringType);
                    targetName = $"{sourceMethod.DeclaringType.FullName}.{sourceMethod.Name}";
                    target = _ResolvePrototypeMethodMappings(sourceMethod, targetName, genericParametersCount, parameterTypes, resolvedDeclarationType, weaveItem);
                    break;
                default:
                    throw new NotImplementedException();
            }

            return target;
        }

        object _ResolvePrototypeMethodMappings(MethodDefinition sourceMethod, string targetName, int sourceGenericParametersCount, IEnumerable<TypeReference> sourceParameterTypes, TypeDefinition fromType, WeaveItem weaveItem)
        {
            object target = fromType.Methods.FirstOrDefault(t => t.Name == targetName && _AreParameterTypeSame(sourceMethod, sourceParameterTypes.ToArray(), t, t.Parameters.Select(p => p.ParameterType).ToArray()) && t.GenericParameters.Count == sourceGenericParametersCount && _AreReturnTypesSame(sourceMethod, t, weaveItem));
            if (target != null)
            {
                var isNewSlotVirtual = ((MethodDefinition)target).IsNewSlot && ((MethodDefinition)target).IsVirtual;
                var isInstance = !((MethodDefinition)target).IsNewSlot && !((MethodDefinition)target).IsVirtual;
                var isOverride = !((MethodDefinition)target).IsNewSlot && ((MethodDefinition)target).IsVirtual;
                if (!isNewSlotVirtual && !isInstance && !isOverride)
                    target = null;
            }

            if (target == null)
            {
                target = SafeWeaveItems.SelectMany(m => m.WeaveItemMembers).OfType<NewMethodMember>().Where(t => t.NewMemberName == targetName && _AreParameterTypeSame(sourceMethod, sourceParameterTypes.ToArray(), t.ClonedMethod, t.ClonedMethod.Parameters.Select(p => p.ParameterType).ToArray()) && t.ClonedMethod.GenericParameters.Count == sourceGenericParametersCount && _AreReturnTypesSame(sourceMethod, t.ClonedMethod, weaveItem)).Select(m => m.ClonedMethod).FirstOrDefault();
            }

            return target;
        }

        object _ResolvePrototypeMethodDelegateMappings(MethodDefinition sourceMethod, string targetName, int sourceGenericParametersCount, IEnumerable<TypeReference> sourceParameterTypes, TypeDefinition fromType, WeaveItem weaveItem)
        {
            object target = null;
            if (target == null)
            {
                var delegateTargets = fromType.Fields.Where(t => t.Name == targetName && CecilHelper.IsDelegate(t.FieldType.Resolve()));
                foreach (var delegateTarget in delegateTargets)
                {
                    MethodDefinition targetMethod = delegateTarget.FieldType.Resolve().Methods.Where(t => t.Name == "Invoke").First();
                    if (_AreParameterTypeSame(sourceMethod, sourceParameterTypes.ToArray(), targetMethod, targetMethod.Parameters.Select(p => p.ParameterType).ToArray()) && targetMethod.GenericParameters.Count == sourceGenericParametersCount && _AreReturnTypesSame(sourceMethod, targetMethod, weaveItem))
                    {
                        target = delegateTarget;
                        break;
                    }
                }
            }

            if (target == null)
            {
                var newFieldDelegates = SafeWeaveItems.SelectMany(m => m.WeaveItemMembers).OfType<NewFieldMember>().Where(t => t.ClonedField.DeclaringType.FullName == fromType.FullName && t.ClonedField.Name == targetName && CecilHelper.IsDelegate(t.ClonedField)).Select(t => t.ClonedField);
                foreach (var newFieldDelegate in newFieldDelegates)
                {
                    MethodDefinition targetMethod = newFieldDelegate.FieldType.Resolve().Methods.Where(t => t.Name == "Invoke").First();
                    if (_AreParameterTypeSame(sourceMethod, sourceParameterTypes.ToArray(), targetMethod, targetMethod.Parameters.Select(p => p.ParameterType).ToArray()) && targetMethod.GenericParameters.Count == sourceGenericParametersCount && _AreReturnTypesSame(sourceMethod, targetMethod, weaveItem))
                    {
                        target = newFieldDelegate;
                        break;
                    }
                }
            }

            return target;
        }

        object _ResolvePrototypeConstructorMappings(MethodDefinition sourceConstructor, PrototypeItemMappingTargetKinds targetKind, int genericParametersCount, IEnumerable<TypeReference> sourceParameterTypes, TypeDefinition fromType, WeaveItem weaveItem)
        {
            object target = null;
            target = fromType.Methods.FirstOrDefault(t => t.Name == sourceConstructor.Name && _AreParameterTypeSame(sourceConstructor, sourceParameterTypes.ToArray(), t, t.Parameters.Select(p => p.ParameterType).ToArray()) && t.GenericParameters.Count == genericParametersCount && _AreReturnTypesSame(sourceConstructor, t, weaveItem));
            if (target == null)
            {
                target = SafeWeaveItems.SelectMany(m => m.WeaveItemMembers).OfType<NewConstructorMember>().Where(t => _AreParameterTypeSame(sourceConstructor, sourceParameterTypes.ToArray(), t.ClonedMethod, t.ClonedMethod.Parameters.Select(p => p.ParameterType).ToArray()) && t.ClonedMethod.GenericParameters.Count == genericParametersCount && _AreReturnTypesSame(sourceConstructor, t.ClonedMethod, weaveItem)).Select(m => m.ClonedMethod).FirstOrDefault();
            }

            return target;
        }

        object _ResolvePrototypePropertyMappings(PropertyDefinition sourceProperty, IPrototypeItemMappingDefinition prototypeItemMappingDefinition, WeaveItem weaveItem)
        {
            TypeDefinition joinpointType = null;
            switch (weaveItem.Joinpoint)
            {
                case ITypeJoinpoint typeJoinpoint:
                    joinpointType = typeJoinpoint.TypeDefinition;
                    break;
                case IInstructionJoinpoint instructionJoinpoint:
                    joinpointType = instructionJoinpoint.CallingMethod.DeclaringType;
                    break;
                case IMemberJoinpoint memberJoinpoint:
                    joinpointType = memberJoinpoint.DeclaringType;
                    break;
                default:
                    throw new NotSupportedException();
            }

            var target = _ResolvePrototypePropertyMapping(sourceProperty, prototypeItemMappingDefinition.TargetKind, sourceProperty.Name, joinpointType, weaveItem);
            return target;
        }

        object _ResolvePrototypePropertyMapping(PropertyDefinition sourceProperty, PrototypeItemMappingTargetKinds kind, string targetName, TypeDefinition fromType, WeaveItem weaveItem)
        {
            object target = null;
            switch (kind)
            {
                case PrototypeItemMappingTargetKinds.ThisMember:
                    target = _ResolvePrototypePropertyMappings(sourceProperty, targetName, sourceProperty.Parameters.Select(t => t.ParameterType), fromType, weaveItem);
                    break;
                case PrototypeItemMappingTargetKinds.BaseMember:
                    foreach (var baseType in WeaverHelper.GetBaseTypes(fromType, SafeWeaveItemMembers))
                    {
                        target = _ResolvePrototypePropertyMappings(sourceProperty, targetName, sourceProperty.Parameters.Select(t => t.ParameterType), fromType, weaveItem);
                        if (target != null)
                            break;
                    }

                    break;
                case PrototypeItemMappingTargetKinds.Member:
                    target = _ResolvePrototypePropertyMappings(sourceProperty, targetName, sourceProperty.Parameters.Select(t => t.ParameterType), fromType, weaveItem);
                    if (target == null)
                    {
                        foreach (var baseType in WeaverHelper.GetBaseTypes(fromType, SafeWeaveItemMembers))
                        {
                            target = _ResolvePrototypePropertyMappings(sourceProperty, targetName, sourceProperty.Parameters.Select(t => t.ParameterType), baseType, weaveItem);
                            if (target != null)
                                break;
                        }
                    }

                    break;
                default:
                    throw new NotImplementedException();
            }

            return target;
        }

        object _ResolvePrototypePropertyMappings(PropertyDefinition sourceProperty, string targetName, IEnumerable<TypeReference> sourceParameterTypes, TypeDefinition fromType, WeaveItem weaveItem)
        {
            object target = fromType.Properties.FirstOrDefault(t => t.Name == targetName && _AreParameterTypeSame(sourceProperty.FullName, sourceParameterTypes.ToArray(), t.FullName, t.Parameters.Select(p => p.ParameterType).ToArray()) && _IsReturnTypeCompatible(t, sourceProperty, weaveItem));
            if (target == null)
            {
                target = SafeWeaveItems.SelectMany(m => m.WeaveItemMembers).OfType<NewPropertyMember>().Where(t => t.ClonedProperty.Name == targetName && _AreParameterTypeSame(sourceProperty.FullName, sourceParameterTypes.ToArray(), t.ClonedProperty.FullName, t.ClonedProperty.Parameters.Select(p => p.ParameterType).ToArray()) && _IsReturnTypeCompatible(t.ClonedProperty, sourceProperty, weaveItem)).Select(m => m.ClonedProperty).FirstOrDefault();
            }

            return target;
        }

        bool _AreReturnTypesSame(MethodDefinition sourceMethod, object target, WeaveItem weaveItem)
        {
            MethodDefinition targetMethod = null;
            if (target is MethodDefinition)
                targetMethod = (MethodDefinition)target;
            else
                targetMethod = ((FieldDefinition)target).FieldType.Resolve().Methods.Where(t => t.Name == "Invoke").First();
            if (sourceMethod.ReturnType.IsGenericParameter && ((GenericParameter)sourceMethod.ReturnType).Owner is MethodDefinition && ((MethodDefinition)((GenericParameter)sourceMethod.ReturnType).Owner).FullName == sourceMethod.FullName)
            {
                if (targetMethod.ReturnType.IsGenericParameter && ((GenericParameter)targetMethod.ReturnType).Owner is MethodDefinition && ((MethodDefinition)((GenericParameter)targetMethod.ReturnType).Owner).FullName == targetMethod.FullName)
                {
                    return ((GenericParameter)sourceMethod.ReturnType).Position == ((GenericParameter)targetMethod.ReturnType).Position;
                }
                else
                    return false;
            }
            else
            {
                return _IsReturnTypeCompatible(targetMethod, sourceMethod, weaveItem);
            }
        }

        bool _AreParameterTypeSame(MethodDefinition sourceMethod, TypeReference[] sourceParameterTypes, MethodDefinition targetMethod, TypeReference[] targetParameterTypes)
        {
            return _AreParameterTypeSame(sourceMethod.FullName, sourceParameterTypes, targetMethod.FullName, targetParameterTypes);
        }

        bool _AreParameterTypeSame(string sourceMemberFullName, TypeReference[] sourceParameterTypes, string targetMemberFullName, TypeReference[] targetParameterTypes)
        {
            if (sourceParameterTypes.Count() != targetParameterTypes.Count())
                return false;
            for (int i = 0; i < sourceParameterTypes.Count(); i++)
            {
                if (sourceParameterTypes[i].IsGenericParameter && ((GenericParameter)sourceParameterTypes[i]).Owner is MethodDefinition && ((MethodDefinition)((GenericParameter)sourceParameterTypes[i]).Owner).FullName == sourceMemberFullName)
                {
                    if (targetParameterTypes[i].IsGenericParameter && ((GenericParameter)targetParameterTypes[i]).Owner is MethodDefinition && ((MethodDefinition)((GenericParameter)targetParameterTypes[i]).Owner).FullName == targetMemberFullName)
                    {
                        if (((GenericParameter)sourceParameterTypes[i]).Position != ((GenericParameter)targetParameterTypes[i]).Position)
                            return false;
                    }
                    else
                        return false;
                }
                else
                {
                    if (!WeaverHelper.IsSame(sourceParameterTypes[i], targetParameterTypes[i]))
                        return false;
                }
            }

            return true;
        }

        TypeReference[] _ResolvePrototypeMethodParameterMappings(MonoCollection.Collection<ParameterDefinition> parameters, WeaveItem weaveItem)
        {
            var resolvedPropertyParameters = new TypeReference[parameters.Count];
            var prototypeParameters = parameters.Select(p => p.ParameterType).ToArray();
            for (int i = 0; i < prototypeParameters.Length; i++)
            {
                if (prototypeParameters[i].IsGenericParameter)
                    resolvedPropertyParameters[i] = prototypeParameters[i];
                else
                    resolvedPropertyParameters[i] = weaveItem.Resolve(prototypeParameters[i]);
            }

            return resolvedPropertyParameters;
        }

        TypeReference _ResolvePrototypeGenericParamterMappings(IPrototypeItemMappingDefinition prototypeItemMappingDefinition, WeaveItem weaveItem)
        {
            TypeDefinition joinpointType = ((ITypeJoinpoint)weaveItem.Joinpoint).TypeDefinition;
            var targetGenericParameter = joinpointType.GenericParameters.Where(t => t.Name == prototypeItemMappingDefinition.TargetName).FirstOrDefault();
            return targetGenericParameter;
        }

        void _CloneMethodBodies()
        {
            foreach (var newType in SafeWeaveItemMembers.OfType<INewType>())
                _CloneNewTypeMethodBody(newType.SourceType, (WeaveItemMember)newType);
            foreach (var newAbstractMethodMember in SafeWeaveItemMembers.OfType<NewAbstractMethodMember>().Where(t => t.SourceMethod.HasBody))
                _CloneNewTypeMemberAbstractMethodBody(newAbstractMethodMember);
            foreach (var newConstructor in SafeWeaveItemMembers.OfType<NewConstructorMember>().Where(t => t.FieldInitILs.Any()))
                _CloneNewFieldInitToNewContsructor(newConstructor);
        }

        void _CloneNewTypeMethodBody(INewType newType)
        {
            foreach (var sourceMethod in newType.AdviceTypeDefinition.Methods)
            {
                var targetMethod = (MethodDefinition)newType.WeaveItem.Resolve(sourceMethod);
                if (sourceMethod.HasBody)
                {
                    var clonedBody = MethodBodyCloner.Create(sourceMethod, (WeaveItemMember)newType).CloneBody();
                    if (clonedBody != null)
                        targetMethod.Body = clonedBody;
                }

                if (sourceMethod.HasOverrides)
                {
                    foreach (var sourceOverrideMethod in sourceMethod.Overrides)
                    {
                        var targetOverrideMethod = (MethodDefinition)newType.WeaveItem.Resolve(sourceOverrideMethod);
                        var sourceMethodDefinition = sourceOverrideMethod.DeclaringType.Resolve();
                        if (sourceMethod.Name == WeaverHelper.GetOverrrideInterfaceMemberName(sourceOverrideMethod.DeclaringType.FullName, sourceOverrideMethod.Name))
                            targetMethod.Name = WeaverHelper.GetOverrrideInterfaceMemberName(targetOverrideMethod.DeclaringType.FullName, targetOverrideMethod.Name);
                        targetMethod.Overrides.Add(targetOverrideMethod);
                    }
                }
            }
        }

        void _CloneNewTypeMethodBody(TypeDefinition sourceType, WeaveItemMember weaveItemMember)
        {
            foreach (var sourceMethod in sourceType.Methods)
            {
                var targetMethod = (MethodDefinition)weaveItemMember.Resolve(sourceMethod);
                if (sourceMethod.HasBody)
                {
                    var clonedBody = MethodBodyCloner.Create(sourceMethod, weaveItemMember).CloneBody();
                    if (clonedBody != null)
                        targetMethod.Body = clonedBody;
                }

                if (sourceMethod.HasOverrides)
                {
                    foreach (var sourceOverrideMethod in sourceMethod.Overrides)
                    {
                        var targetOverrideMethod = (MethodReference)weaveItemMember.Resolve(sourceOverrideMethod);
                        var sourceMethodDefinition = sourceOverrideMethod.DeclaringType.Resolve();
                        if (sourceMethod.Name == WeaverHelper.GetOverrrideInterfaceMemberName(sourceOverrideMethod.DeclaringType.FullName, sourceOverrideMethod.Name))
                            targetMethod.Name = WeaverHelper.GetOverrrideInterfaceMemberName(targetOverrideMethod.DeclaringType.FullName, targetOverrideMethod.Name);
                        targetMethod.Overrides.Add(targetOverrideMethod);
                    }
                }
            }

            if (sourceType.HasNestedTypes)
            {
                foreach (var sourceNestedType in sourceType.NestedTypes.ToList())
                    _CloneNewTypeMethodBody(sourceNestedType, weaveItemMember);
            }
        }

        void _CloneNewTypeMemberAbstractMethodBody(NewAbstractMethodMember newAbstractMethodBody)
        {
            var clonedBody = MethodBodyCloner.Create(newAbstractMethodBody.SourceMethod, newAbstractMethodBody).CloneBody();
            newAbstractMethodBody.ClonedMethod.Body = clonedBody;
            if (newAbstractMethodBody.SourceMethod.HasOverrides)
            {
                foreach (var ovveride in newAbstractMethodBody.SourceMethod.Overrides)
                    newAbstractMethodBody.ClonedMethod.Overrides.Add((MethodReference)newAbstractMethodBody.Resolve(ovveride));
            }
        }

        void _CloneNewFieldInitToNewContsructor(NewConstructorMember newConstructor)
        {
            var clonedBody = newConstructor.ClonedMethod.Body;
            var bodyBuilder = new ILBodyMerger(newConstructor, clonedBody);
            bodyBuilder.InsertBeforeBody(newConstructor.FieldInitILs);
            clonedBody = bodyBuilder.Merge();
            newConstructor.ClonedMethod.Body = clonedBody;
        }

        void _MergeExtendedCode()
        {
            var newCodeMemberGroups = SafeWeaveItemMembers.OfType<NewCodeMember>().GroupBy(t => t.TargetMethod);
            foreach (var newCodeMemberGroup in newCodeMemberGroups)
            {
                new CodeMethodExtender(newCodeMemberGroup, newCodeMemberGroup.Key).Merge();
            }
        }

        void _SetOverloadedTypeMember()
        {
            foreach (var newBaseInterface in SafeWeaveItemMembers.OfType<NewInheritedType>().Where(t => t.IsInterface && !t.JoinpointType.IsInterface))
                _SetOverloadedTypeMemberFromNewBaseInterfaceToType(newBaseInterface);
            foreach (var newBaseInterface in SafeWeaveItemMembers.OfType<NewInheritedType>().Where(t => t.IsInterface && t.JoinpointType.IsInterface))
                _SetOverloadedTypeMemberFromNewBaseInterfaceToInterface(newBaseInterface);
            foreach (var newInterfaceMmember in SafeWeaveItemMembers.OfType<NewTypeMember>().Where(t => t.WeaveItem.Aspect.AspectKind == AspectKinds.InterfaceMembersAspect))
                _SetOverloadedTypeMemberFromNewInterfaceMember(newInterfaceMmember);
        }

        void _SetOverloadedTypeMemberFromNewBaseInterfaceToType(NewInheritedType newBaseInterface)
        {
            var interfaceMembers = new FlatInterfaceMembers(newBaseInterface.TargetBaseType.GetElementType().Resolve(), SafeWeaveItemMembers, newBaseInterface).Check();
            var typeMembers = new FlatTypeMembers(newBaseInterface.JoinpointType, null, SafeWeaveItemMembers).ResolveTypeMembers();
            foreach (var interfaceMember in interfaceMembers.GetInterfaceMembers(newBaseInterface.ResolvedGenericArguments))
            {
                var typeMember = typeMembers.Root.AllFlatTypeMembers.FirstOrDefault(t => t.MemberDefinition.GetType() == interfaceMember.MemberDefinition.GetType() && t.MemberDefinition.Name == interfaceMember.MemberDefinition.Name && t.GenericParametersCount == interfaceMember.GenericParametersCount && WeaverHelper.IsSame(t.MemberType, interfaceMember.MemberType) && WeaverHelper.IsSame(t.ParameterTypes, interfaceMember.ParameterTypes));
                newBaseInterface.AddOverloadedFlatTypeMember(typeMember);
            }
        }

        void _SetOverloadedTypeMemberFromNewBaseInterfaceToInterface(NewInheritedType newBaseInterface)
        {
            var interfaceMembers = new FlatInterfaceMembers(newBaseInterface.TargetBaseType.GetElementType().Resolve(), SafeWeaveItemMembers, newBaseInterface).Check();
            var inheritorInterfaces = _GetInheritorInterfacesFromBaseInterface(newBaseInterface.JoinpointType, false);
            foreach (var inheritorInterface in inheritorInterfaces)
            {
                foreach (var inheritorType in _GetInheritorTypesFromBaseInterfaces(inheritorInterfaces))
                {
                    var typeMembers = new FlatTypeMembers(inheritorType, null, SafeWeaveItemMembers);
                    foreach (var interfaceMember in interfaceMembers.GetInterfaceMembers(newBaseInterface.ResolvedGenericArguments))
                    {
                        var typeMember = typeMembers.Root.FlatTypeMembers.FirstOrDefault(t => t.MemberDefinition.GetType() == interfaceMember.MemberDefinition.GetType() && t.MemberDefinition.Name == interfaceMember.MemberDefinition.Name && t.GenericParametersCount == interfaceMember.GenericParametersCount && WeaverHelper.IsSame(t.MemberType, interfaceMember.MemberType) && WeaverHelper.IsSame(t.ParameterTypes, interfaceMember.ParameterTypes));
                        newBaseInterface.AddOverloadedFlatTypeMember(typeMember);
                    }
                }
            }
        }

        void _SetOverloadedTypeMemberFromNewInterfaceMember(NewTypeMember newInterfaceMember)
        {
            var inheritorInterfaces = _GetInheritorInterfacesFromBaseInterface(newInterfaceMember.JoinpointDeclaringType, false);
            foreach (var inheritorInterface in inheritorInterfaces)
            {
                var inheritorTypes = _GetInheritorTypesFromBaseInterface(inheritorInterface);
                foreach (var inheritorType in inheritorTypes)
                {
                    var typeMembers = new FlatTypeMembers(inheritorType, null, SafeWeaveItemMembers).ResolveTypeMembers();
                    foreach (var interfaceImplementation in inheritorType.Interfaces.Where(t => t.InterfaceType.GetElementType().Resolve().FullName == inheritorInterface.GetElementType().Resolve().FullName))
                    {
                        var flatInterfaceMembers = new FlatInterfaceMembers(inheritorInterface, SafeWeaveItemMembers, newInterfaceMember).Check();
                        var resolvedArguments = interfaceImplementation.InterfaceType is GenericInstanceType ? ((GenericInstanceType)interfaceImplementation.InterfaceType).GenericArguments : null;
                        var interfaceMembers = flatInterfaceMembers.GetInterfaceMembers(resolvedArguments);
                        var interfaceMember = interfaceMembers.First(t => t.MemberDefinition.FullName == newInterfaceMember.ClonedMember.FullName);
                        var typeMember = typeMembers.Root.FlatTypeMembers.FirstOrDefault(t => t.MemberDefinition.Name == interfaceMember.MemberDefinition.Name && t.GenericParametersCount == interfaceMember.GenericParametersCount && WeaverHelper.IsSame(t.ResolvedMemberType, interfaceMember.MemberType) && WeaverHelper.IsSame(t.ResolvedParameterTypes, interfaceMember.ParameterTypes));
                        if (typeMember != null)
                        {
                            var overloadedTypeMember = typeMember.MemberDefinition;
                            if (typeMember.NewTypeMember != null)
                                overloadedTypeMember = typeMember.NewTypeMember.ClonedMember;
                            newInterfaceMember.AddOverloadedTypeMember(overloadedTypeMember);
                        }
                    }
                }
            }
        }

        bool _CheckGetSetIntegrity(string prototypeFieldName, WeaveItem weaveItem, object target)
        {
            if (target is FieldDefinition)
                return true;
            foreach (var newMethodMember in weaveItem.WeaveItemMembers.OfType<NewMethodMember>())
            {
                if (newMethodMember.SourceMethod.Body == null)
                    continue;
                var get = newMethodMember.SourceMethod.Body.Instructions.Any(t => t.OpCode == OpCodes.Ldfld && t.Operand is FieldReference && ((FieldReference)t.Operand).Resolve().FullName == prototypeFieldName);
                var set = newMethodMember.SourceMethod.Body.Instructions.Any(t => t.OpCode == OpCodes.Stfld && t.Operand is FieldReference && ((FieldReference)t.Operand).Resolve().FullName == prototypeFieldName);
                if (!get && !set)
                    continue;
                switch (target)
                {
                    case PropertyDefinition propertyDefinition:
                        if (get && propertyDefinition.GetMethod == null)
                        {
                            weaveItem.AddError(WeaverHelper.GetError(weaveItem, "PropertSetGetMisMatch", prototypeFieldName, propertyDefinition.FullName));
                            return false;
                        }

                        if (set && propertyDefinition.SetMethod == null)
                        {
                            weaveItem.AddError(WeaverHelper.GetError(weaveItem, "PropertSetGetMisMatch", prototypeFieldName, propertyDefinition.FullName));
                            return false;
                        }

                        break;
                }
            }

            return true;
        }

        bool _CheckAddRemoveIntegrity(EventDefinition prototypeEvent, WeaveItem weaveItem, EventDefinition target)
        {
            return prototypeEvent.AddMethod != null == (target.AddMethod != null) && (prototypeEvent.RemoveMethod != null == (target.RemoveMethod != null));
        }

        bool _IsReturnTypeCompatible(object targetMember, IMemberDefinition sourcePrototypeMember, WeaveItem weaveItem)
        {
            TypeReference targetReturnTypeReference = null;
            switch (targetMember)
            {
                case FieldDefinition fieldDefinition:
                    targetReturnTypeReference = fieldDefinition.FieldType;
                    break;
                case PropertyDefinition propertyDefinition:
                    targetReturnTypeReference = propertyDefinition.PropertyType;
                    break;
                case EventDefinition eventDefinition:
                    targetReturnTypeReference = eventDefinition.EventType;
                    break;
                case ParameterDefinition parameterDefinition:
                    targetReturnTypeReference = parameterDefinition.ParameterType;
                    break;
                case VariableDefinition variableDefinition:
                    targetReturnTypeReference = variableDefinition.VariableType;
                    break;
                case MethodDefinition methodDefinition:
                    targetReturnTypeReference = methodDefinition.ReturnType;
                    break;
                default:
                    throw new NotSupportedException();
            }

            TypeReference sourceReturnTypeReference = null;
            switch (sourcePrototypeMember)
            {
                case FieldDefinition fieldDefinition:
                    sourceReturnTypeReference = fieldDefinition.FieldType;
                    break;
                case PropertyDefinition propertyDefinition:
                    sourceReturnTypeReference = propertyDefinition.PropertyType;
                    break;
                case EventDefinition eventDefinition:
                    sourceReturnTypeReference = eventDefinition.EventType;
                    break;
                case MethodDefinition methodDefinition:
                    sourceReturnTypeReference = methodDefinition.ReturnType;
                    break;
                default:
                    throw new NotSupportedException();
            }

            if ((targetReturnTypeReference is GenericParameter && !(sourceReturnTypeReference is GenericParameter)) || (sourceReturnTypeReference is GenericParameter && !(targetReturnTypeReference is GenericParameter)))
                return false;
            if (targetReturnTypeReference is GenericParameter)
            {
                var targetReturnGenericParameter = (GenericParameter)targetReturnTypeReference;
                var sourceReturnGenericParameter = (GenericParameter)weaveItem.Resolve((TypeReference)sourceReturnTypeReference);
                return (targetReturnGenericParameter.Position == sourceReturnGenericParameter.Position && targetReturnGenericParameter.Type == sourceReturnGenericParameter.Type && targetReturnGenericParameter.FullName == sourceReturnGenericParameter.FullName);
            }
            else
            {
                TypeDefinition sourceReturnType = weaveItem.Resolve(CecilHelper.Resolve(sourceReturnTypeReference));
                TypeDefinition targetReturnType = CecilHelper.Resolve(targetReturnTypeReference);
                return sourceReturnType.FullName == targetReturnType.FullName;
            }
        }

        bool _IsReturnTypeCompatible(ResolvedPrototypeItem resolvedTargetItem, IMemberDefinition sourcePrototypeMember, WeaveItem weaveItem)
        {
            TypeReference targetReturnTypeReference = null;
            switch (resolvedTargetItem.TargetItem)
            {
                case FieldDefinition fieldDefinition:
                    targetReturnTypeReference = fieldDefinition.FieldType;
                    break;
                case PropertyDefinition propertyDefinition:
                    targetReturnTypeReference = propertyDefinition.PropertyType;
                    break;
                case EventDefinition eventDefinition:
                    targetReturnTypeReference = eventDefinition.EventType;
                    break;
                case ParameterDefinition parameterDefinition:
                    targetReturnTypeReference = parameterDefinition.ParameterType;
                    break;
                case VariableDefinition variableDefinition:
                    targetReturnTypeReference = variableDefinition.VariableType;
                    break;
                case MethodDefinition methodDefinition:
                    targetReturnTypeReference = methodDefinition.ReturnType;
                    break;
                case string target:
                    if (target == "This")
                        targetReturnTypeReference = resolvedTargetItem.DeclaringTypedType.Type;
                    else
                        throw new NotSupportedException();
                    break;
                default:
                    throw new NotSupportedException();
            }

            TypeReference sourceReturnTypeReference = null;
            switch (sourcePrototypeMember)
            {
                case FieldDefinition fieldDefinition:
                    sourceReturnTypeReference = fieldDefinition.FieldType;
                    break;
                case PropertyDefinition propertyDefinition:
                    sourceReturnTypeReference = propertyDefinition.PropertyType;
                    break;
                case EventDefinition eventDefinition:
                    sourceReturnTypeReference = eventDefinition.EventType;
                    break;
                case MethodDefinition methodDefinition:
                    sourceReturnTypeReference = methodDefinition.ReturnType;
                    break;
                default:
                    throw new NotSupportedException();
            }

            if ((targetReturnTypeReference is GenericParameter && !(sourceReturnTypeReference is GenericParameter)) || (sourceReturnTypeReference is GenericParameter && !(targetReturnTypeReference is GenericParameter)))
                return false;
            if (targetReturnTypeReference is GenericParameter)
            {
                var targetReturnGenericParameter = (GenericParameter)resolvedTargetItem.DeclaringTypedType.ResolveGenericTypeReference(targetReturnTypeReference);
                var sourceReturnGenericParameter = (GenericParameter)weaveItem.Resolve((TypeReference)sourceReturnTypeReference);
                return (targetReturnGenericParameter.Position == sourceReturnGenericParameter.Position && targetReturnGenericParameter.Type == sourceReturnGenericParameter.Type && targetReturnGenericParameter.FullName == sourceReturnGenericParameter.FullName);
            }
            else
            {
                TypeDefinition sourceReturnType = weaveItem.Resolve(CecilHelper.Resolve(sourceReturnTypeReference));
                TypeDefinition targetReturnType = CecilHelper.Resolve(targetReturnTypeReference);
                var isCompatible = _IsTypeReferenceCompatible(resolvedTargetItem, sourceReturnTypeReference, targetReturnTypeReference, weaveItem, null);
                return isCompatible;
            }
        }

        IEnumerable<TypeDefinition> _GetInheritorTypesFromBaseType(TypeReference fromBaseTypeReference)
        {
            var fromBaseType = fromBaseTypeReference.GetElementType().Resolve();
            var inheritedRootTypes = new List<(TypeDefinition inheritingType, bool? isRoot)>(new (TypeDefinition inheritingType, bool? isRoot)[]{(fromBaseType, null)});
            var inheritingTypes = _GetInheritingTypesFromBaseType(inheritedRootTypes);
            return inheritingTypes.Where(t => t.isRoot.Value).Select(t => t.inheritorType);
        }

        List<(TypeDefinition inheritorType, bool? isRoot)> _GetInheritingTypesFromBaseType(List<(TypeDefinition baseType, bool? isRoot)> inheritorTypes)
        {
            foreach (var inheritorType in inheritorTypes.Where(t => t.isRoot is null).ToList())
            {
                var newInheritorTypes = new List<TypeDefinition>();
                foreach (var assembly in _Weaver.JointpointsContainer.AssemblyTargets)
                {
                    var assemblyInheritors = assembly.MainModule.Types.Where(t => t.BaseType != null && t.BaseType.FullName != typeof(Object).FullName && t.BaseType.GetElementType().Resolve().FullName == inheritorType.baseType.GetElementType().Resolve().FullName);
                    newInheritorTypes.AddRange(assemblyInheritors);
                }

                var newInheritedMembers = SafeWeaveItemMembers.OfType<INewType>().Where(t => t.AdviceTypeDefinition.BaseType != null && t.AdviceTypeDefinition.BaseType.Resolve().FullName == inheritorType.baseType.FullName).Select(t => t.ClonedType);
                newInheritorTypes.AddRange(newInheritedMembers);
                newInheritedMembers = SafeWeaveItemMembers.OfType<NewInheritedType>().Where(t => t.JoinpointType.BaseType.Resolve().FullName == inheritorType.baseType.FullName).Select(t => t.JoinpointType);
                newInheritorTypes.AddRange(newInheritedMembers);
                newInheritorTypes = newInheritorTypes.Where(t => !inheritorTypes.Any(b => b.baseType.FullName == t.Resolve().FullName)).ToList();
                if (newInheritorTypes.Count == 0)
                {
                    inheritorTypes.Remove(inheritorType);
                    inheritorTypes.Add((inheritorType.baseType, true));
                }
                else
                {
                    inheritorTypes.Remove(inheritorType);
                    inheritorTypes.Add((inheritorType.baseType, false));
                    newInheritorTypes.ForEach(t => inheritorTypes.Add((t, null)));
                }
            }

            if (inheritorTypes.Any(t => t.isRoot is null))
                inheritorTypes = _GetInheritingTypesFromBaseType(inheritorTypes).ToList();
            return inheritorTypes;
        }

        IEnumerable<TypeDefinition> _GetInheritorInterfacesFromBaseInterface(TypeReference fromResolvedBaseInterfaceReference, bool onlyRoot)
        {
            var fromBaseInterfaceType = fromResolvedBaseInterfaceReference.GetElementType().Resolve();
            var inheritorInterfaces = new List<(TypeDefinition inheritorInterface, bool? isRoot)>(new (TypeDefinition inheritorInterface, bool? isRoot)[]{(fromBaseInterfaceType, null)});
            if (onlyRoot)
                inheritorInterfaces = _GetInheritorInterfacesFromBaseInterface(inheritorInterfaces).Where(t => t.isRoot.Value).ToList();
            return inheritorInterfaces.Select(t => t.inheritorInterface).Distinct();
        }

        List<(TypeDefinition inheritorType, bool? isRoot)> _GetInheritorInterfacesFromBaseInterface(List<(TypeDefinition baseType, bool? isRoot)> inheritorInterfaces)
        {
            foreach (var inheritorInterface in inheritorInterfaces.Where(t => t.isRoot is null))
            {
                var newInheritorInterfaces = new List<TypeDefinition>();
                foreach (var assembly in _Weaver.JointpointsContainer.AssemblyTargets)
                {
                    var assemblyInheritors = assembly.MainModule.Types.Where(t => t.IsInterface && t.GetElementType().Resolve().Interfaces.Any(i => i.InterfaceType.GetElementType().Resolve().FullName == inheritorInterface.baseType.GetElementType().Resolve().FullName));
                    newInheritorInterfaces.AddRange(assemblyInheritors);
                }

                var newTypes = SafeWeaveItemMembers.OfType<INewType>().Where(t => t.AdviceTypeDefinition.BaseType.Resolve().FullName == inheritorInterface.baseType.FullName).Select(t => t.ClonedType);
                newInheritorInterfaces.AddRange(newTypes);
                newTypes = SafeWeaveItemMembers.OfType<NewInheritedType>().Where(t => t.JoinpointType.BaseType.Resolve().FullName == inheritorInterface.baseType.FullName).Select(t => t.JoinpointType);
                newInheritorInterfaces.AddRange(newTypes);
                newInheritorInterfaces = newInheritorInterfaces.Where(t => !inheritorInterfaces.Any(b => b.baseType.FullName == t.Resolve().FullName)).ToList();
                if (newInheritorInterfaces.Count == 0)
                {
                    inheritorInterfaces.Remove(inheritorInterface);
                    inheritorInterfaces.Add((inheritorInterface.baseType, true));
                }
                else
                {
                    inheritorInterfaces.Remove(inheritorInterface);
                    inheritorInterfaces.Add((inheritorInterface.baseType, false));
                    newInheritorInterfaces.ForEach(t => inheritorInterfaces.Add((t, null)));
                }
            }

            if (inheritorInterfaces.Any(t => t.isRoot is null))
                inheritorInterfaces = _GetInheritorInterfacesFromBaseInterface(inheritorInterfaces).ToList();
            return inheritorInterfaces;
        }

        IEnumerable<TypeDefinition> _GetInheritorTypesFromBaseInterfaces(IEnumerable<TypeDefinition> fromBaseInterfaces)
        {
            var inheritorTypes = new List<TypeDefinition>();
            foreach (var fromBaseInterface in fromBaseInterfaces)
            {
                foreach (var assembly in _Weaver.JointpointsContainer.AssemblyTargets)
                {
                    var newInheritorTypes = assembly.MainModule.Types.Where(t => t.Interfaces.Any(i => i.InterfaceType.GetElementType().Resolve().FullName == fromBaseInterface.GetElementType().Resolve().FullName));
                    inheritorTypes.AddRange(newInheritorTypes);
                }

                var newInheritedMembers = SafeWeaveItemMembers.OfType<INewType>().Where(t => t.ClonedType.Interfaces.Any(i => i.InterfaceType.GetElementType().Resolve().FullName == fromBaseInterface.GetElementType().Resolve().FullName)).Select(t => t.ClonedType);
                inheritorTypes.AddRange(newInheritedMembers);
            }

            return inheritorTypes;
        }

        IEnumerable<TypeDefinition> _GetInheritorTypesFromBaseInterface(TypeDefinition fromBaseInterface)
        {
            var inheritorTypes = new List<TypeDefinition>();
            foreach (var assembly in _Weaver.JointpointsContainer.AssemblyTargets)
            {
                var newInheritorTypes = assembly.MainModule.Types.Where(t => t.Interfaces.Any(i => i.InterfaceType.GetElementType().Resolve().FullName == fromBaseInterface.GetElementType().Resolve().FullName));
                inheritorTypes.AddRange(newInheritorTypes);
            }

            var newInheritedMembers = SafeWeaveItemMembers.OfType<INewType>().Where(t => t.ClonedType.Interfaces.Any(i => i.InterfaceType.GetElementType().Resolve().FullName == fromBaseInterface.GetElementType().Resolve().FullName)).Select(t => t.ClonedType);
            inheritorTypes.AddRange(newInheritedMembers);
            return inheritorTypes;
        }
    }
}