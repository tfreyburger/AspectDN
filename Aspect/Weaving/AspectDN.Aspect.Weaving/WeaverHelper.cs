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
using AspectDN.Aspect.Weaving.Marker;
using AspectDN.Common;
using Foundation.Common.Error;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AspectDN.Aspect.Weaving
{
    internal static class WeaverHelper
    {
        internal static IEnumerable<object> GetTargets(IAspectDefinition aspect, IJoinpointsContainer joinpoints)
        {
            switch (aspect.Pointcut.PointcutType)
            {
                case PointcutTypes.assemblies:
                    return joinpoints.GetAssemblies(((IPointcutAssemblyDefinition)aspect.Pointcut).Expression);
                case PointcutTypes.classes:
                    return joinpoints.GetTypes(JoinpointKinds.classes | JoinpointKinds.declaration, ((IPointcutTypeDefinition)aspect.Pointcut).Expression);
                case PointcutTypes.interfaces:
                    return joinpoints.GetTypes(JoinpointKinds.interfaces | JoinpointKinds.declaration, ((IPointcutTypeDefinition)aspect.Pointcut).Expression);
                case PointcutTypes.methods:
                    var jointpointType = _GetJoinpoint(aspect, JoinpointKinds.methods);
                    return joinpoints.GetMethods(jointpointType, ((IPointcutMethodDefinition)aspect.Pointcut).Expression);
                case PointcutTypes.fields:
                    jointpointType = _GetJoinpoint(aspect, JoinpointKinds.fields);
                    return joinpoints.GetFields(jointpointType, ((IPointcutFieldDefinition)aspect.Pointcut).Expression);
                case PointcutTypes.properties:
                    jointpointType = _GetJoinpoint(aspect, JoinpointKinds.properties);
                    return joinpoints.GetProperties(jointpointType, ((IPointcutPropertyDefinition)aspect.Pointcut).Expression);
                case PointcutTypes.events:
                    jointpointType = _GetJoinpoint(aspect, JoinpointKinds.events);
                    return joinpoints.GetEvents(jointpointType, ((IPointcutEventDefinition)aspect.Pointcut).Expression);
                case PointcutTypes.delegates:
                    return joinpoints.GetTypes(JoinpointKinds.type_delegates, ((IPointcutTypeDefinition)aspect.Pointcut).Expression);
                case PointcutTypes.structs:
                    return joinpoints.GetTypes(JoinpointKinds.structs, ((IPointcutTypeDefinition)aspect.Pointcut).Expression);
                case PointcutTypes.exceptions:
                    return joinpoints.GetExceptions(JoinpointKinds.exceptions, ((IPointcutTypeDefinition)aspect.Pointcut).Expression);
                case PointcutTypes.constructors:
                    jointpointType = _GetJoinpoint(aspect, JoinpointKinds.constructors);
                    return joinpoints.GetConstructors(jointpointType, ((IPointcutMethodDefinition)aspect.Pointcut).Expression);
                case PointcutTypes.enums:
                    return joinpoints.GetTypes(JoinpointKinds.enums, ((IPointcutTypeDefinition)aspect.Pointcut).Expression);
                default:
                    throw ErrorFactory.GetException("UndefinedPointcut", Enum.GetName(typeof(PointcutTypes), aspect.Pointcut.PointcutType), aspect.FullAspectDeclarationName);
            }
        }

        internal static bool AreILLocalisationEqual(ILLocalisations iLLocalisation, ILLocalisations with)
        {
            return (iLLocalisation & with) == with;
        }

        static JoinpointKinds _GetJoinpoint(IAspectDefinition aspect, JoinpointKinds joinpointType)
        {
            ControlFlows controlFlow = ControlFlows.none;
            switch (aspect)
            {
                case ICodeAspectDefinition aspectCode:
                    controlFlow = aspectCode.ControlFlow;
                    break;
                case IChangeValueAspectDefinition changeValueAspect:
                    controlFlow = changeValueAspect.ControlFlow;
                    break;
            }

            switch (controlFlow)
            {
                case ControlFlows.set:
                    joinpointType |= JoinpointKinds.set;
                    break;
                case ControlFlows.get:
                    joinpointType |= JoinpointKinds.get;
                    break;
                case ControlFlows.set | ControlFlows.body:
                    joinpointType |= JoinpointKinds.set | JoinpointKinds.body;
                    break;
                case ControlFlows.get | ControlFlows.body:
                    joinpointType |= JoinpointKinds.get | JoinpointKinds.body;
                    break;
                case ControlFlows.call:
                    joinpointType |= JoinpointKinds.call;
                    break;
                case ControlFlows.body:
                    joinpointType |= JoinpointKinds.body;
                    break;
                case ControlFlows.@throw:
                    joinpointType |= JoinpointKinds.@throw;
                    break;
                case ControlFlows.add:
                    joinpointType |= JoinpointKinds.add;
                    break;
                case ControlFlows.remove:
                    joinpointType |= JoinpointKinds.remove;
                    break;
                case ControlFlows.add | ControlFlows.body:
                    joinpointType |= JoinpointKinds.add | JoinpointKinds.body;
                    break;
                case ControlFlows.remove | ControlFlows.body:
                    joinpointType |= JoinpointKinds.remove | JoinpointKinds.body;
                    break;
                case ControlFlows.none:
                    joinpointType |= joinpointType | JoinpointKinds.declaration;
                    break;
                default:
                    throw ErrorFactory.GetException("UndefinedControlFlow", aspect.FullAspectDeclarationName, (Enum.GetName(typeof(ControlFlows), controlFlow)));
            }

            return joinpointType;
        }

        internal static TypeReference GetGenericArgument(string genericTargetNameOrIndex, TypeDefinition jointpointType)
        {
            TypeReference targetGenericParameter = null;
            if (int.TryParse(genericTargetNameOrIndex, out int index))
            {
                if (jointpointType.GenericParameters.Any() && jointpointType.GenericParameters.Count > index)
                    targetGenericParameter = jointpointType.GenericParameters[index];
            }
            else
            {
                targetGenericParameter = jointpointType.GenericParameters.Where(t => t.Name == genericTargetNameOrIndex).FirstOrDefault();
            }

            return targetGenericParameter;
        }

        internal static TypeReference GetGenericArgument(string genericTargetNameOrIndex, MethodDefinition jointpointMethod)
        {
            TypeReference targetGenericParameter = null;
            if (int.TryParse(genericTargetNameOrIndex, out int index))
            {
                if (jointpointMethod.GenericParameters.Any() && jointpointMethod.GenericParameters.Count > index)
                    targetGenericParameter = jointpointMethod.GenericParameters[index];
            }
            else
            {
                targetGenericParameter = jointpointMethod.GenericParameters.Where(t => t.Name == genericTargetNameOrIndex).FirstOrDefault();
            }

            return targetGenericParameter;
        }

        internal static CustomAttribute CreateAspectDNMarker(ModuleDefinition targetAssemblyModule, string adviceMemberName, string aspectRepositoryName, DateTime updateDate)
        {
            var markerType = targetAssemblyModule.ImportReference(typeof(AspectDNMarkerAttribute));
            var constructor = targetAssemblyModule.ImportReference(markerType.Resolve().Methods.Where(t => t.IsConstructor).First());
            var stringType = targetAssemblyModule.ImportReference(typeof(string));
            var marker = new CustomAttribute(constructor);
            var argument = new CustomAttributeNamedArgument(nameof(AspectDNMarkerAttribute.AdviceName), new CustomAttributeArgument(stringType, adviceMemberName));
            marker.Properties.Add(argument);
            argument = new CustomAttributeNamedArgument(nameof(AspectDNMarkerAttribute.AspectRepositoryName), new CustomAttributeArgument(stringType, aspectRepositoryName));
            marker.Properties.Add(argument);
            argument = new CustomAttributeNamedArgument(nameof(AspectDNMarkerAttribute.Update), new CustomAttributeArgument(stringType, updateDate.ToString("g")));
            marker.Properties.Add(argument);
            return marker;
        }

        internal static bool IsAspectAssembly(AssemblyDefinition asseembly)
        {
            return asseembly.CustomAttributes.Any(t => t.AttributeType.FullName == typeof(AspectDNAssemblyAttribute).FullName);
        }

        internal static TypeDefinition GetBaseType(TypeDefinition type, IEnumerable<WeaveItemMember> safeWeaveItemMembers)
        {
            if (type.BaseType == null)
                return null;
            var baseType = type.BaseType.GetElementType().Resolve();
            if (baseType.FullName == typeof(System.Object).FullName || baseType.FullName == typeof(System.ValueType).FullName)
            {
                baseType = null;
                var newBaseType = safeWeaveItemMembers.OfType<NewInheritedType>().FirstOrDefault(t => t.JoinpointType.FullName == type.FullName);
                if (newBaseType != null)
                    baseType = newBaseType.TargetBaseType.GetElementType().Resolve();
            }

            return baseType;
        }

        internal static IEnumerable<TypeDefinition> GetBaseTypes(TypeDefinition fromType, IEnumerable<WeaveItemMember> safeWeaveItemMembers)
        {
            var baseTypes = new List<TypeDefinition>();
            var baseType = GetBaseType(fromType.GetElementType().Resolve(), safeWeaveItemMembers);
            while (baseType != null)
            {
                baseTypes.Add(baseType);
                baseType = GetBaseType(baseType.GetElementType().Resolve(), safeWeaveItemMembers);
                ;
            }

            return baseTypes;
        }

        internal static IEnumerable<TypeReference> GetTypeGenericArguments(TypeDefinition declaringType, IEnumerable<TypeReference> declaringtypeGenericArguments, IEnumerable<TypeReference> typeGenericArguments)
        {
            var baseTypeGenericArgumentList = typeGenericArguments.ToList();
            for (int i = 0; i < baseTypeGenericArgumentList.Count(); i++)
            {
                if (baseTypeGenericArgumentList[i].IsGenericParameter)
                {
                    var genericParameter = baseTypeGenericArgumentList[i] as GenericParameter;
                    if (((TypeReference)genericParameter.Owner).FullName == declaringType.FullName)
                        baseTypeGenericArgumentList[i] = declaringtypeGenericArguments.ToArray()[genericParameter.Position];
                }
            }

            return baseTypeGenericArgumentList;
        }

        internal static IEnumerable<TypeReference> GetMethodGenericArguments(MethodDefinition method, IEnumerable<TypeReference> declaringTypeGenericArguments)
        {
            List<TypeReference> genericArguments = method.GenericParameters.Cast<TypeReference>().ToList();
            for (int i = 0; i < method.GenericParameters.Count(); i++)
            {
                if (method.GenericParameters[i].Owner is TypeReference)
                    genericArguments[i] = declaringTypeGenericArguments.ToList()[method.GenericParameters[i].Position];
            }

            return genericArguments;
        }

        internal static TypeReference GetTypeReference(GenericParameter genericParameter, IEnumerable<TypeReference> newDeclaringTypeGenericArguments)
        {
            TypeReference flatGenericParameter = genericParameter;
            if (newDeclaringTypeGenericArguments != null)
                flatGenericParameter = newDeclaringTypeGenericArguments.ToArray()[genericParameter.Position];
            return flatGenericParameter;
        }

        internal static IError GetError(WeaveItemMember weaveItemMember, string errorId, params string[] parameters)
        {
            return AspectDNErrorFactory.GetWeaverError(errorId, weaveItemMember.WeaveItem.Aspect.FullAspectDeclarationName, weaveItemMember.AdviceMember.FullAdviceName, weaveItemMember.WeaveItem.Joinpoint.Member.FullName, parameters);
        }

        internal static IError GetError(string errorId, params string[] parameters)
        {
            return ErrorFactory.GetError(errorId, parameters);
        }

        internal static IError GetError(WeaveItem weaveItem, string errorId, params string[] parameters)
        {
            return AspectDNErrorFactory.GetWeaverError(errorId, weaveItem.Aspect.FullAspectDeclarationName, weaveItem.Aspect.AdviceDefinition.FullAdviceName, weaveItem.Joinpoint.Member.FullName, parameters);
        }

        internal static bool IsMemberModifierCompatible(WeaveItem weaveItem, IEnumerable<TypeDefinition> baseTargetTypes, IMemberDefinition member)
        {
            var modifierKind = ModifierKinds.None;
            switch (member)
            {
                case FieldDefinition field:
                    var fieldModifier = (FieldAttributes)CecilHelper.GetMaskedAttributes((ushort)field.Attributes, (ushort)FieldAttributes.FieldAccessMask);
                    switch (fieldModifier)
                    {
                        case FieldAttributes.Private:
                            modifierKind = ModifierKinds.Private;
                            break;
                        case FieldAttributes.Family:
                            modifierKind = ModifierKinds.Protected;
                            break;
                        case FieldAttributes.Public:
                            modifierKind = ModifierKinds.Public;
                            break;
                        case FieldAttributes.Assembly:
                            modifierKind = ModifierKinds.Internal;
                            break;
                        case FieldAttributes.FamORAssem:
                            modifierKind = ModifierKinds.ProtectedInternal;
                            break;
                        case FieldAttributes.FamANDAssem:
                            modifierKind = ModifierKinds.PrivateProtected;
                            break;
                    }

                    break;
                case MethodDefinition method:
                    var methodModifier = (MethodAttributes)CecilHelper.GetMaskedAttributes((ushort)method.Attributes, (ushort)MethodAttributes.MemberAccessMask);
                    switch (methodModifier)
                    {
                        case MethodAttributes.Private:
                            modifierKind = ModifierKinds.Private;
                            break;
                        case MethodAttributes.Family:
                            modifierKind = ModifierKinds.Protected;
                            break;
                        case MethodAttributes.Public:
                            modifierKind = ModifierKinds.Public;
                            break;
                        case MethodAttributes.Assembly:
                            modifierKind = ModifierKinds.Internal;
                            break;
                        case MethodAttributes.FamORAssem:
                            modifierKind = ModifierKinds.ProtectedInternal;
                            break;
                        case MethodAttributes.FamANDAssem:
                            modifierKind = ModifierKinds.PrivateProtected;
                            break;
                    }

                    break;
                case PropertyDefinition property:
                    return true;
                case EventDefinition @event:
                default:
                    throw new NotSupportedException();
            }

            var value = IsMemberModifierCompatible(weaveItem.Joinpoint.DeclaringType, baseTargetTypes, member.DeclaringType, modifierKind);
            return value;
        }

        internal static bool IsMemberModifierCompatible(TypeDefinition targetType, IEnumerable<TypeDefinition> baseTargetTypes, TypeDefinition declaringType, ModifierKinds targetMemberModifierKind)
        {
            switch (targetMemberModifierKind)
            {
                case ModifierKinds.Private:
                    return targetType.FullName == declaringType.FullName;
                case ModifierKinds.Public:
                    return true;
                case ModifierKinds.Protected:
                    var type = targetType;
                    if (type.FullName == declaringType.FullName && type.Module.Assembly.FullName == targetType.Module.Assembly.FullName)
                        return true;
                    foreach (var baseTargetType in baseTargetTypes)
                    {
                        if (baseTargetType.FullName == declaringType.FullName && baseTargetType.Module.Assembly.FullName == targetType.Module.Assembly.FullName)
                            return true;
                    }

                    break;
                case ModifierKinds.Internal:
                    if (targetType.Module.Assembly.FullName == declaringType.Module.Assembly.FullName)
                        return true;
                    var internalsVisibleAttributes = CecilHelper.GetCustomAttributesOfType(declaringType.Module.Assembly, typeof(InternalsVisibleToAttribute));
                    foreach (var attribute in internalsVisibleAttributes)
                    {
                        var assemblyName = (string)attribute.Properties.First().Argument.Value;
                        if (targetType.Module.Assembly.FullName == assemblyName)
                            return true;
                    }

                    break;
                case ModifierKinds.ProtectedInternal:
                    if (targetType.FullName == declaringType.FullName && targetType.Module.Assembly.FullName == targetType.Module.Assembly.FullName)
                        return true;
                    foreach (var baseTargetType in baseTargetTypes)
                    {
                        if (baseTargetType.FullName == declaringType.FullName)
                        {
                            if (baseTargetType.Module.Assembly.FullName == targetType.Module.Assembly.FullName)
                                return true;
                            else
                            {
                                internalsVisibleAttributes = CecilHelper.GetCustomAttributesOfType(declaringType.Module.Assembly, typeof(InternalsVisibleToAttribute));
                                foreach (var attribute in internalsVisibleAttributes)
                                {
                                    var assemblyName = (string)attribute.Properties.First().Argument.Value;
                                    if (targetType.Module.Assembly.FullName == assemblyName)
                                        return true;
                                }
                            }
                        }
                    }

                    break;
                case ModifierKinds.PrivateProtected:
                case ModifierKinds.None:
                default:
                    throw new NotSupportedException();
            }

            return false;
        }

        internal static IEnumerable<TypeReference> GetTypeReferences(IEnumerable<TypeReference> typeReferences, IEnumerable<TypeReference> newDeclaringTypeGenericArguments)
        {
            foreach (var typeReference in typeReferences)
            {
                if (typeReference is GenericParameter)
                    yield return GetTypeReference((GenericParameter)typeReference, newDeclaringTypeGenericArguments);
                else
                    yield return typeReference;
            }
        }

        internal static string GetOverrrideInterfaceMemberName(string declaringFullTypeName, string memberName)
        {
            var startIndex = declaringFullTypeName.IndexOf("`");
            if (startIndex >= 0)
            {
                var endIndex = declaringFullTypeName.IndexOf("<");
                declaringFullTypeName = $"{declaringFullTypeName.Substring(0, startIndex)}{declaringFullTypeName.Substring(endIndex, declaringFullTypeName.Length - endIndex)}";
            }

            return $"{declaringFullTypeName}.{memberName}";
        }

        internal static TypeReference ReplaceElementType(TypeReference typeReference, TypeReference newElementType, AssemblyDefinition targetAssembly)
        {
            var newTypeReference = new TypeReference((string)newElementType.Namespace, (string)newElementType.Name, targetAssembly.MainModule, (IMetadataScope)newElementType.Scope, newElementType.IsValueType);
            newTypeReference.DeclaringType = newElementType.DeclaringType;
            return newTypeReference;
        }

        internal static IEnumerable<TypeDefinition> GetAdviceDefinition(TypeDefinition declaringType)
        {
            List<TypeDefinition> declaringTypes = new List<TypeDefinition>();
            if (CecilHelper.HasCustomAttributesOfType(declaringType, typeof(ITypesAdviceDeclaration)) || CecilHelper.HasCustomAttributesOfType(declaringType, typeof(ITypeMembersAdviceDeclaration)) || CecilHelper.HasCustomAttributesOfType(declaringType, typeof(IInterfaceMembersAdviceDeclaration)))
            {
                declaringTypes.Add(declaringType);
                return declaringTypes;
            }

            while (declaringType.DeclaringType != null)
            {
                if (CecilHelper.HasCustomAttributesOfType(declaringType, typeof(ITypesAdviceDeclaration)) || CecilHelper.HasCustomAttributesOfType(declaringType, typeof(ITypeMembersAdviceDeclaration)) || CecilHelper.HasCustomAttributesOfType(declaringType, typeof(IInterfaceMembersAdviceDeclaration)))
                {
                    declaringTypes.Add(declaringType.DeclaringType);
                    return declaringTypes;
                }

                declaringType = declaringType.DeclaringType;
            }

            return null;
        }

#region GetConsumnedPrototypeTypes
        internal static IEnumerable<TypeDefinition> GetConsumnedPrototypeTypes(TypeDefinition type)
        {
            if (CecilHelper.HasCustomAttributesOfType(type, typeof(PrototypeTypeDeclarationAttribute)))
            {
                foreach (var genericParameter in type.GenericParameters)
                {
                    foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(genericParameter))
                        yield return consumnedPrototypeType;
                }

                foreach (var customAttribute in type.CustomAttributes)
                {
                    foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(customAttribute))
                        yield return consumnedPrototypeType;
                }

                foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(type.BaseType))
                    yield return consumnedPrototypeType;
                foreach (var @interface in type.Interfaces)
                {
                    foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(@interface.InterfaceType))
                        yield return consumnedPrototypeType;
                    foreach (var customAttribute in @interface.CustomAttributes)
                    {
                        foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(customAttribute))
                            yield return consumnedPrototypeType;
                    }
                }

                foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(type.BaseType))
                    yield return consumnedPrototypeType;
                foreach (var prototypeMember in CecilHelper.GetTypeMembers(type))
                {
                    switch (prototypeMember)
                    {
                        case FieldDefinition field:
                            foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(field))
                                yield return consumnedPrototypeType;
                            break;
                        case PropertyDefinition property:
                            foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(property))
                                yield return consumnedPrototypeType;
                            break;
                        case MethodDefinition method:
                            foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(method))
                                yield return consumnedPrototypeType;
                            break;
                        case EventDefinition @event:
                            foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(@event))
                                yield return consumnedPrototypeType;
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
        }

        internal static IEnumerable<TypeDefinition> GetConsumnedPrototypeTypes(TypeReference typeReference)
        {
            switch (CecilHelper.GetTypeReferenceKind(typeReference))
            {
                case TypeReferenceKinds.GenericInstance:
                    foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(typeReference.GetElementType().Resolve()))
                        yield return consumnedPrototypeType;
                    foreach (var genericArgument in ((GenericInstanceType)typeReference).GenericArguments)
                    {
                        foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(typeReference.GetElementType().Resolve()))
                            yield return consumnedPrototypeType;
                    }

                    break;
                case TypeReferenceKinds.SimpleTypeReference:
                    foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(typeReference.GetElementType().Resolve()))
                        yield return consumnedPrototypeType;
                    break;
                case TypeReferenceKinds.GenericParameter:
                    foreach (var typeReferenceConstraint in ((GenericParameter)typeReference).Constraints)
                    {
                        foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(typeReferenceConstraint))
                            yield return consumnedPrototypeType;
                    }

                    break;
                default:
                    break;
            }
        }

        internal static IEnumerable<TypeDefinition> GetConsumnedPrototypeTypes(CustomAttribute customAttribute)
        {
            foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(customAttribute.AttributeType))
                yield return consumnedPrototypeType;
            foreach (var propertyValue in customAttribute.Properties.Select(t => t.Argument.Value).OfType<TypeReference>())
            {
                foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(propertyValue))
                    yield return consumnedPrototypeType;
            }

            foreach (var propertyValue in customAttribute.ConstructorArguments.Select(t => t.Value).OfType<TypeReference>())
            {
                foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(propertyValue))
                    yield return consumnedPrototypeType;
            }
        }

        internal static IEnumerable<TypeDefinition> GetConsumnedPrototypeTypes(FieldDefinition field)
        {
            foreach (var customAttribute in field.CustomAttributes)
            {
                foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(customAttribute))
                    yield return consumnedPrototypeType;
            }

            foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(field.FieldType))
                yield return consumnedPrototypeType;
        }

        internal static IEnumerable<TypeDefinition> GetConsumnedPrototypeTypes(PropertyDefinition property)
        {
            foreach (var customAttribute in property.CustomAttributes)
            {
                foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(customAttribute))
                    yield return consumnedPrototypeType;
            }

            foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(property.PropertyType))
                yield return consumnedPrototypeType;
            foreach (var parameter in property.Parameters)
            {
                foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(parameter))
                    yield return consumnedPrototypeType;
            }
        }

        internal static IEnumerable<TypeDefinition> GetConsumnedPrototypeTypes(ParameterDefinition parameter)
        {
            foreach (var customAttribute in parameter.CustomAttributes)
            {
                foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(customAttribute))
                    yield return consumnedPrototypeType;
            }

            foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(parameter.ParameterType))
                yield return consumnedPrototypeType;
            if (parameter.HasConstant && parameter.Constant is TypeReference)
            {
                foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes((TypeReference)parameter.Constant))
                    yield return consumnedPrototypeType;
            }
        }

        internal static IEnumerable<TypeDefinition> GetConsumnedPrototypeTypes(MethodDefinition methodDefinition)
        {
            foreach (var customAttribute in methodDefinition.CustomAttributes)
            {
                foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(customAttribute))
                    yield return consumnedPrototypeType;
            }

            foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(methodDefinition.MethodReturnType.ReturnType))
                yield return consumnedPrototypeType;
            foreach (var parameter in methodDefinition.Parameters)
            {
                foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(parameter))
                    yield return consumnedPrototypeType;
            }

            foreach (var customAttribute in methodDefinition.MethodReturnType.CustomAttributes)
            {
                foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(customAttribute))
                    yield return consumnedPrototypeType;
            }
        }

        internal static IEnumerable<TypeDefinition> GetConsumnedPrototypeTypes(EventDefinition eventDefinition)
        {
            foreach (var customAttribute in eventDefinition.CustomAttributes)
            {
                foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(customAttribute))
                    yield return consumnedPrototypeType;
            }

            foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(eventDefinition.EventType))
                yield return consumnedPrototypeType;
        }

        internal static IEnumerable<TypeDefinition> GetConsumnedPrototypeTypes(GenericParameterConstraint genericConstraint)
        {
            foreach (var customAttribute in genericConstraint.CustomAttributes)
            {
                foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(customAttribute))
                    yield return consumnedPrototypeType;
            }

            foreach (var consumnedPrototypeType in GetConsumnedPrototypeTypes(genericConstraint.ConstraintType))
                yield return consumnedPrototypeType;
        }

#endregion
#region _IsTargetAndPrototypeTypeCompatible
        internal static IEnumerable<IError> IsTargetAndPrototypeTypeCompatible(PrototypeTypeMapping prototypeTypeMapping, IEnumerable<PrototypeTypeMapping> prototypeTypeMappings, IEnumerable<WeaveItemMember> safeWeaveItemMembers)
        {
            var prototypeType = prototypeTypeMapping.PrototypeType;
            var targetType = prototypeTypeMapping.TargetType;
            var errors = new List<IError>();
            if (prototypeType.GenericParameters.Count != targetType.GenericParameters.Count)
            {
                errors.Add(WeaverHelper.GetError("PrototypeTypeMappingMemberMatchingError", prototypeType.FullName));
            }

            if (prototypeType.IsAbstract != targetType.IsAbstract)
            {
                errors.Add(WeaverHelper.GetError("PrototypeTypeMappingModifierMatchingError", prototypeType.FullName));
            }

            if (prototypeType.IsSealed != targetType.IsSealed)
            {
                errors.Add(WeaverHelper.GetError("PrototypeTypeMappingModifierMatchingError", prototypeType.FullName));
            }

            if (CecilHelper.GetTypeKinds(prototypeType) != CecilHelper.GetTypeKinds(targetType))
            {
                errors.Add(WeaverHelper.GetError("PrototypeTypeMappingKindMatchingError", prototypeType.FullName, Enum.GetName(typeof(TypeDefinitionKinds), CecilHelper.GetTypeKinds(prototypeType))));
            }

            if (prototypeType.BaseType != null && prototypeType.BaseType.FullName != typeof(object).FullName)
            {
                var resolvedPrototypeType = _ResolvedPrototypeType(prototypeType.BaseType, prototypeTypeMappings, null);
                if (resolvedPrototypeType == null || !IsTypeReferenceCompatible(resolvedPrototypeType, targetType.BaseType))
                {
                    errors.Add(WeaverHelper.GetError("PrototypeTypeMappingKindMatchingError", prototypeType.FullName, prototypeType.BaseType.FullName));
                }
            }

            foreach (var @interface in prototypeType.Interfaces)
            {
                var resolvedPrototypeType = _ResolvedPrototypeType(@interface.InterfaceType, prototypeTypeMappings, null);
                if (resolvedPrototypeType == null || !targetType.Interfaces.Any(t => IsTypeReferenceCompatible(resolvedPrototypeType, t.InterfaceType)))
                {
                    errors.Add(WeaverHelper.GetError("PrototypeTypeMappingInterfaceMatchingError", prototypeType.FullName, @interface.InterfaceType.FullName));
                }
            }

            foreach (var prototypeFieldDefinition in prototypeType.Fields)
            {
                var resolvedPrototypeType = _ResolvedPrototypeType(prototypeFieldDefinition.FieldType, prototypeTypeMappings, null);
                if (resolvedPrototypeType == null)
                {
                    errors.Add(WeaverHelper.GetError("PrototypeTypeMappingMemberMatchingError", prototypeType.FullName, prototypeFieldDefinition.FullName));
                }

                var targetField = targetType.Fields.Where(t => t.Name == prototypeFieldDefinition.Name).FirstOrDefault();
                if (targetField == null)
                {
                    targetField = safeWeaveItemMembers.OfType<NewFieldMember>().Where(t => t.JoinpointDeclaringType.FullName == targetType.FullName && t.ClonedField.Name == prototypeFieldDefinition.Name).Select(t => t.ClonedField).FirstOrDefault();
                }

                if (targetField == null || !IsTypeReferenceCompatible(resolvedPrototypeType, targetField.FieldType))
                {
                    errors.Add(WeaverHelper.GetError("PrototypeTypeMappingMemberMatchingError", prototypeType.FullName, prototypeFieldDefinition.FullName));
                }
            }

            foreach (var prototypePropertyDefinition in prototypeType.Properties)
            {
                var prototypePropertyName = WeaverHelper.ResolvePrototypeMemberName(prototypePropertyDefinition.Name, prototypeTypeMappings);
                var targetProperties = targetType.Properties.Where(t => t.Name == prototypePropertyName);
                targetProperties = targetProperties.Union(safeWeaveItemMembers.OfType<NewPropertyMember>().Where(t => t.JoinpointDeclaringType.FullName == targetType.FullName && t.ClonedProperty.Name == prototypePropertyName).Select(t => t.ClonedProperty));
                if (!targetProperties.Any())
                {
                    errors.Add(WeaverHelper.GetError("PrototypeTypeMappingMemberMatchingError", prototypeType.FullName, prototypePropertyDefinition.FullName));
                }

                var targetPropertyFound = false;
                foreach (var targetProperty in targetProperties)
                {
                    var resolvedPrototypeType = _ResolvedPrototypeType(prototypePropertyDefinition.PropertyType, prototypeTypeMappings, null);
                    if (resolvedPrototypeType == null || !IsTypeReferenceCompatible(resolvedPrototypeType, targetProperty.PropertyType))
                        continue;
                    if (prototypePropertyDefinition.GetMethod != null && targetProperty.GetMethod == null)
                        continue;
                    if (prototypePropertyDefinition.SetMethod != null && targetProperty.SetMethod == null)
                        continue;
                    if (prototypePropertyDefinition.OtherMethods.Count > targetProperty.OtherMethods.Count)
                        continue;
                    if (!_IsMethodParametersTypeCompatible(prototypePropertyDefinition.Parameters.ToArray(), targetProperty.Parameters.ToArray(), prototypeTypeMappings))
                        continue;
                    targetPropertyFound = true;
                }

                if (!targetPropertyFound)
                {
                    errors.Add(WeaverHelper.GetError("PrototypeTypeMappingMemberMatchingError", prototypeType.FullName, prototypePropertyDefinition.FullName));
                }
            }

            foreach (var prototypeMethodDefinition in prototypeType.Methods.Where(t => !t.IsConstructor || (t.IsConstructor && !t.IsRuntimeSpecialName)))
            {
                if (!HasMethodCompatible(prototypeMethodDefinition, targetType, prototypeTypeMappings, safeWeaveItemMembers))
                {
                    errors.Add(WeaverHelper.GetError("PrototypeTypeMappingMemberMatchingError", prototypeType.FullName, prototypeMethodDefinition.FullName));
                }
            }

            foreach (var prototypeEventDefinition in prototypeType.Events)
            {
                var resolvedPrototypeType = _ResolvedPrototypeType(prototypeEventDefinition.EventType, prototypeTypeMappings, null);
                if (resolvedPrototypeType == null)
                {
                    errors.Add(WeaverHelper.GetError("PrototypeTypeMappingMemberMatchingError", prototypeType.FullName, prototypeEventDefinition.FullName));
                }

                var prototypeEventName = WeaverHelper.ResolvePrototypeMemberName(prototypeEventDefinition.Name, prototypeTypeMappings);
                var targetEvent = targetType.Events.Where(t => t.Name == prototypeEventName).FirstOrDefault();
                if (targetEvent == null)
                {
                    targetEvent = safeWeaveItemMembers.OfType<NewEventMember>().Where(t => t.JoinpointDeclaringType.FullName == targetType.FullName && t.ClonedEvent.Name == prototypeEventName).Select(t => t.ClonedEvent).FirstOrDefault();
                }

                if (targetEvent == null || !IsTypeReferenceCompatible(resolvedPrototypeType, targetEvent.EventType))
                {
                    errors.Add(WeaverHelper.GetError("PrototypeTypeMappingMemberMatchingError", prototypeType.FullName, prototypeEventDefinition.FullName));
                }

                if ((prototypeEventDefinition.AddMethod != null) != (targetEvent.AddMethod != null) || (prototypeEventDefinition.RemoveMethod != null) != (targetEvent.RemoveMethod != null) || prototypeEventDefinition.OtherMethods.Count != targetEvent.OtherMethods.Count)
                {
                    errors.Add(WeaverHelper.GetError("PrototypeTypeMappingMemberMatchingError", prototypeType.FullName, prototypeEventDefinition.FullName));
                }
            }

            return errors;
        }

        internal static bool HasMethodCompatible(MethodDefinition prototypeMethodDefinition, TypeDefinition targetType, IEnumerable<PrototypeTypeMapping> prototypeTypeTargets, IEnumerable<WeaveItemMember> safeWeaveItemMembers)
        {
            return GetMethodCompatible(prototypeMethodDefinition, targetType, prototypeTypeTargets, safeWeaveItemMembers) != null;
        }

        internal static MethodDefinition GetMethodCompatible(MethodDefinition prototypeMethodDefinition, TypeDefinition targetType, IEnumerable<PrototypeTypeMapping> prototypeTypeTargets, IEnumerable<WeaveItemMember> safeWeaveItemMembers)
        {
            var prototypeMethodName = WeaverHelper.ResolvePrototypeMemberName(prototypeMethodDefinition.Name, prototypeTypeTargets);
            var targetMethods = targetType.Methods.Where(t => t.Name == prototypeMethodName);
            targetMethods = targetMethods.Union(targetMethods = safeWeaveItemMembers.OfType<NewMethodMember>().Where(t => t.ClonedMethod.Name == prototypeMethodName && t.JoinpointDeclaringType.FullName == targetType.FullName).Select(t => t.ClonedMethod)).ToList();
            if (!targetMethods.Any())
                return null;
            var resolvedPrototypeType = _ResolvedPrototypeType(prototypeMethodDefinition.MethodReturnType.ReturnType, prototypeTypeTargets, null);
            if (resolvedPrototypeType == null)
                return null;
            targetMethods = targetMethods.Where(t => IsTypeReferenceCompatible(resolvedPrototypeType, t.MethodReturnType.ReturnType)).ToList();
            if (!targetMethods.Any())
                return null;
            targetMethods = targetMethods.Where(t => _IsMethodParametersTypeCompatible(prototypeMethodDefinition.Parameters.ToArray(), t.Parameters.ToArray(), prototypeTypeTargets)).ToList();
            if (targetMethods.Count() != 1)
                return null;
            return targetMethods.First();
        }

        internal static bool HasPropertyCompatible(PropertyDefinition prototypePropertyDefinition, TypeDefinition targetType, IEnumerable<PrototypeTypeMapping> prototypeTypeTargets)
        {
            return GetPropertyCompatible(prototypePropertyDefinition, targetType, prototypeTypeTargets) != null;
        }

        internal static PropertyDefinition GetPropertyCompatible(PropertyDefinition prototypePropertyDefinition, TypeDefinition targetType, IEnumerable<PrototypeTypeMapping> prototypeTypeTargets)
        {
            var targetProperty = targetType.Properties.Where(t => t.Name == prototypePropertyDefinition.Name).FirstOrDefault();
            if (targetProperty == null)
                return null;
            var resolvedPrototypeType = _ResolvedPrototypeType(prototypePropertyDefinition.PropertyType, prototypeTypeTargets, null);
            if (resolvedPrototypeType == null || IsTypeReferenceCompatible(resolvedPrototypeType, targetProperty.PropertyType))
                return null;
            if ((prototypePropertyDefinition.GetMethod != null) != (targetProperty.GetMethod != null) || (prototypePropertyDefinition.SetMethod != null) != (targetProperty.SetMethod != null) || prototypePropertyDefinition.OtherMethods.Count != targetProperty.OtherMethods.Count)
                return null;
            if (!_IsMethodParametersTypeCompatible(prototypePropertyDefinition.Parameters.ToArray(), targetProperty.Parameters.ToArray(), prototypeTypeTargets))
                return null;
            return targetProperty;
        }

        static bool _IsMethodParametersTypeCompatible(ParameterDefinition[] sourceMethodParameters, ParameterDefinition[] targetMethodParameters, IEnumerable<PrototypeTypeMapping> prototypeTypeTargets)
        {
            if (sourceMethodParameters.Length != targetMethodParameters.Length)
                return false;
            if (sourceMethodParameters.Length == 0)
                return true;
            GenericResolutionContext genericResolutionContext = null;
            var sourceMethodReference = (MethodReference)sourceMethodParameters.First().Method;
            var targetMethodReference = (MethodReference)targetMethodParameters.First().Method;
            if (sourceMethodReference.HasGenericParameters && targetMethodReference.HasGenericParameters)
                genericResolutionContext = new GenericResolutionContext(sourceMethodReference, targetMethodReference);
            for (int i = 0; i < sourceMethodParameters.Length; i++)
            {
                if (CecilHelper.GetTypeReferenceKind(sourceMethodParameters[i].ParameterType) != CecilHelper.GetTypeReferenceKind(targetMethodParameters[i].ParameterType))
                    return false;
                if (sourceMethodParameters[i].ParameterType is GenericParameter && targetMethodParameters[i].ParameterType is TypeReference)
                {
                    if (((GenericParameter)sourceMethodParameters[i].ParameterType).Owner is MethodReference)
                    {
                        if (!(((GenericParameter)targetMethodParameters[i].ParameterType).Owner is MethodReference))
                            return false;
                        if (((GenericParameter)sourceMethodParameters[i].ParameterType).Position != ((GenericParameter)targetMethodParameters[i].ParameterType).Position)
                            return false;
                    }
                    else
                    {
                        if (!(((GenericParameter)targetMethodParameters[i].ParameterType).Owner is TypeReference))
                            return false;
                        if (((GenericParameter)sourceMethodParameters[i].ParameterType).Position != ((GenericParameter)targetMethodParameters[i].ParameterType).Position)
                            return false;
                    }
                }
                else
                {
                    var resolvedParameterType = _ResolvedPrototypeType(sourceMethodParameters[i].ParameterType, prototypeTypeTargets, genericResolutionContext);
                    if (resolvedParameterType == null || !IsTypeReferenceCompatible(resolvedParameterType, targetMethodParameters[i].ParameterType))
                        return false;
                }
            }

            return true;
        }

        internal static bool IsTypeReferenceCompatible(TypeReference resolvedPrototypeTypeReference, TypeReference targetTypeReference)
        {
            if (resolvedPrototypeTypeReference is TypeDefinition)
            {
                if (targetTypeReference is TypeDefinition)
                    return _IsTypeDefinnitionCompatible((TypeDefinition)resolvedPrototypeTypeReference, (TypeDefinition)targetTypeReference);
                else
                    return false;
            }
            else
            {
                if (targetTypeReference is TypeDefinition)
                {
                    resolvedPrototypeTypeReference = CecilHelper.Resolve(resolvedPrototypeTypeReference);
                    if (resolvedPrototypeTypeReference != null)
                        return _IsTypeDefinnitionCompatible((TypeDefinition)resolvedPrototypeTypeReference, (TypeDefinition)targetTypeReference);
                    return false;
                }
            }

            if (CecilHelper.GetTypeReferenceKind(resolvedPrototypeTypeReference) != CecilHelper.GetTypeReferenceKind(targetTypeReference))
                return false;
            switch (CecilHelper.GetTypeReferenceKind(resolvedPrototypeTypeReference))
            {
                case TypeReferenceKinds.GenericInstance:
                    if (!IsTypeReferenceCompatible(resolvedPrototypeTypeReference.GetElementType(), targetTypeReference.GetElementType()))
                        return false;
                    var prototypeGenericInstance = (GenericInstanceType)resolvedPrototypeTypeReference;
                    var targetGenericInstance = (GenericInstanceType)targetTypeReference;
                    if (prototypeGenericInstance.GenericArguments.Count != targetGenericInstance.GenericArguments.Count)
                        return false;
                    for (int i = 0; i < prototypeGenericInstance.GenericArguments.Count; i++)
                    {
                        if (!IsTypeReferenceCompatible(prototypeGenericInstance.GenericArguments[i], targetGenericInstance.GenericArguments[i]))
                            return false;
                    }

                    break;
                case TypeReferenceKinds.GenericParameter:
                    break;
                case TypeReferenceKinds.SimpleTypeReference:
                    if (!_IsTypeDefinnitionCompatible(resolvedPrototypeTypeReference.GetElementType().Resolve(), targetTypeReference.GetElementType().Resolve()))
                        return false;
                    break;
                default:
                    throw new NotImplementedException();
            }

            return true;
        }

        static bool _IsTypeDefinnitionCompatible(TypeDefinition resolvedPrototypeTypeDefinition, TypeDefinition targetTypeDefinition)
        {
            if (resolvedPrototypeTypeDefinition.Name == targetTypeDefinition.Name || CecilHelper.IsTypeDefinionOf(resolvedPrototypeTypeDefinition, targetTypeDefinition))
            {
                return (resolvedPrototypeTypeDefinition.GenericParameters.Count == targetTypeDefinition.GenericParameters.Count);
            }

            return false;
        }

        static TypeReference _ResolvedPrototypeType(TypeReference prototypeTypeReference, IEnumerable<PrototypeTypeMapping> prototypeTypeTargets, GenericResolutionContext genericResolutionContext)
        {
            switch (CecilHelper.GetTypeReferenceKind(prototypeTypeReference))
            {
                case TypeReferenceKinds.GenericInstance:
                    var targetElementType = _ResolvedPrototypeType(prototypeTypeReference.GetElementType(), prototypeTypeTargets, genericResolutionContext);
                    if (targetElementType == null)
                        return null;
                    var targetReferenceType = new GenericInstanceType(targetElementType);
                    foreach (var genericArgument in ((GenericInstanceType)prototypeTypeReference).GenericArguments)
                        targetReferenceType.GenericArguments.Add(_ResolvedPrototypeType(genericArgument, prototypeTypeTargets, genericResolutionContext));
                    if (targetReferenceType.GenericArguments.Any(t => t == null))
                        targetReferenceType = null;
                    return targetReferenceType;
                case TypeReferenceKinds.GenericParameter:
                    IGenericParameterProvider targetOwner = null;
                    switch (((GenericParameter)prototypeTypeReference).Owner)
                    {
                        case MethodReference methodReferenceOwner:
                            if (genericResolutionContext != null && ((MethodReference)((GenericParameter)prototypeTypeReference).Owner).FullName == genericResolutionContext.SourceMethodReference.FullName)
                                targetOwner = genericResolutionContext.TargetMethodReference;
                            else
                                targetOwner = _ResolvePrototypeTypeMethod((MethodReference)((GenericParameter)prototypeTypeReference).Owner, prototypeTypeTargets);
                            if (targetOwner == null)
                                return null;
                            break;
                        case TypeReference typeReferenceOwner:
                            targetOwner = _ResolvedPrototypeType((TypeReference)((GenericParameter)prototypeTypeReference).Owner, prototypeTypeTargets, genericResolutionContext);
                            break;
                    }

                    var targetGenericParameter = new GenericParameter(((GenericParameter)prototypeTypeReference).Name, targetOwner);
                    return targetGenericParameter;
                case TypeReferenceKinds.SimpleTypeReference:
                    targetElementType = _ResolvedPrototypeType(prototypeTypeReference.GetElementType().Resolve(), prototypeTypeTargets);
                    if (targetElementType == null)
                        return null;
                    var targetTypeReference = targetElementType;
                    if (!prototypeTypeReference.IsDefinition)
                        targetTypeReference = new TypeReference((string)targetElementType.Namespace, (string)targetElementType.Name, (ModuleDefinition)targetElementType.Module, (IMetadataScope)targetElementType.Scope);
                    return targetTypeReference;
                default:
                    throw new NotImplementedException();
            }
        }

        static MethodReference _ResolvePrototypeTypeMethod(MethodReference sourceMethodReference, IEnumerable<PrototypeTypeMapping> prototypeTypeTargets)
        {
            var targetDeclaringType = _ResolvedPrototypeType(sourceMethodReference.DeclaringType.Resolve(), prototypeTypeTargets);
            var targetMethods = targetDeclaringType.Methods.Where(t => t.Name == sourceMethodReference.Name);
            targetMethods.Where(t => t.GenericParameters.Count == sourceMethodReference.GenericParameters.Count);
            var targetMethod = targetMethods.FirstOrDefault(t => _IsMethodParametersTypeCompatible(sourceMethodReference.Parameters.ToArray(), t.Parameters.ToArray(), prototypeTypeTargets));
            return targetMethod;
        }

        static TypeDefinition _ResolvedPrototypeType(TypeDefinition prototypeTypeDefinition, IEnumerable<PrototypeTypeMapping> prototypeTypeTargets)
        {
            if (!prototypeTypeDefinition.CustomAttributes.Any(t => t.AttributeType.FullName == typeof(PrototypeTypeDeclarationAttribute).FullName))
                return prototypeTypeDefinition;
            var prototypeTypeTarget = prototypeTypeTargets.Where(t => t.PrototypeType.FullName == prototypeTypeDefinition.FullName).FirstOrDefault();
            if (prototypeTypeTarget != null)
                prototypeTypeDefinition = prototypeTypeTarget.TargetType;
            return prototypeTypeDefinition;
        }

        internal static string ResolvePrototypeMemberName(string memberName, IEnumerable<PrototypeTypeMapping> prototypeTypeMappings)
        {
            var memberNames = CecilHelper.GetMemberNames(memberName);
            if (!string.IsNullOrEmpty(memberNames.interfaceName))
            {
                var prototypeTypeMapping = prototypeTypeMappings.FirstOrDefault(t => t.PrototypeType.FullName.EndsWith(memberNames.interfaceName) && !t.OnError);
                if (prototypeTypeMapping != null)
                {
                    var newInterfaceName = prototypeTypeMapping.TargetType.FullName;
                    memberName = $"{newInterfaceName}.{memberNames.simplename}";
                }
            }

            return memberName;
        }

#endregion
#region Compararison
        internal static bool IsSame(Mono.Collections.Generic.Collection<ParameterDefinition> aParameters, Mono.Collections.Generic.Collection<ParameterDefinition> bParameters)
        {
            if (aParameters.Count() != bParameters.Count())
                return false;
            for (int i = 0; i < aParameters.Count(); i++)
            {
                if (!IsSame(aParameters[i].ParameterType, bParameters[i].ParameterType))
                    return false;
            }

            return true;
        }

        internal static bool IsSame(TypeReference aTypeReference, TypeReference bTypeReference)
        {
            var aTypeRefernceKind = CecilHelper.GetTypeReferenceKind(aTypeReference);
            if (aTypeRefernceKind != CecilHelper.GetTypeReferenceKind(bTypeReference))
                return false;
            switch (aTypeRefernceKind)
            {
                case TypeReferenceKinds.GenericInstance:
                    if (!IsSame(aTypeReference.GetElementType(), bTypeReference.GetElementType()))
                        return false;
                    if (!IsSame(((GenericInstanceType)aTypeReference).GenericArguments, ((GenericInstanceType)bTypeReference).GenericArguments))
                        return false;
                    break;
                case TypeReferenceKinds.SimpleTypeReference:
                    if (aTypeReference.FullName != bTypeReference.FullName)
                        return false;
                    break;
                case TypeReferenceKinds.GenericParameter:
                    if (((GenericParameter)aTypeReference).Owner.GetType().FullName != ((GenericParameter)bTypeReference).Owner.GetType().FullName)
                        return false;
                    if (((GenericParameter)aTypeReference).Position != ((GenericParameter)bTypeReference).Position)
                        return false;
                    return true;
                default:
                    throw new NotSupportedException();
            }

            return true;
        }

        internal static bool IsSame(Mono.Collections.Generic.Collection<TypeReference> aTypeReferences, Mono.Collections.Generic.Collection<TypeReference> bTypeReferences)
        {
            return IsSame(aTypeReferences.ToArray(), bTypeReferences.ToArray());
        }

        internal static bool IsSame(IEnumerable<TypeReference> aTypeReferences, IEnumerable<TypeReference> bTypeReferences)
        {
            return IsSame(aTypeReferences.ToArray(), bTypeReferences.ToArray());
        }

        internal static bool IsSame(TypeReference[] aTypeReferences, TypeReference[] bTypeReferences)
        {
            var a = aTypeReferences;
            var b = bTypeReferences;
            if (aTypeReferences.Length != bTypeReferences.Length)
                return false;
            for (int i = 0; i < aTypeReferences.Length; i++)
            {
                if (!IsSame(a[i], b[i]))
                    return false;
            }

            return true;
        }

        internal static bool IsSame(TypeDefinition aType, TypeDefinition bType)
        {
            if (aType.FullName != bType.FullName)
                return false;
            if (aType.GenericParameters.Count != bType.GenericParameters.Count)
                return false;
            return true;
        }

#endregion
#region PrototypeResolution
#endregion
#region Clone
        internal static TypeDefinition Clone(TypeDefinition sourceType, WeaveItemMember weaveItemMember)
        {
            TypeDefinition targetType = new TypeDefinition(sourceType.Namespace, sourceType.Name, sourceType.Attributes);
            targetType.DeclaringType = sourceType.DeclaringType;
            targetType.ClassSize = sourceType.ClassSize;
            targetType.MetadataToken = sourceType.MetadataToken;
            targetType.PackingSize = sourceType.PackingSize;
            targetType.Scope = weaveItemMember.TargetAssembly.Name;
            targetType.IsNotPublic = sourceType.IsNotPublic;
            targetType.IsNestedAssembly = sourceType.IsNestedAssembly;
            targetType.IsNestedFamily = sourceType.IsNestedFamily;
            targetType.IsNestedFamilyAndAssembly = sourceType.IsNestedFamilyAndAssembly;
            targetType.IsNestedFamilyOrAssembly = sourceType.IsNestedFamilyOrAssembly;
            targetType.IsNestedPrivate = !sourceType.IsNestedPrivate;
            targetType.IsPublic = sourceType.IsNestedPublic;
            targetType.IsNotPublic = sourceType.IsNestedPrivate;
            if (sourceType.HasNestedTypes)
                Clone(sourceType.NestedTypes, targetType, weaveItemMember, true);
            return targetType;
        }

        internal static TypeDefinition Clone(TypeDefinition sourceType, string @namespace, string targetName, WeaveItemMember weaveItemMember)
        {
            TypeDefinition targetType = new TypeDefinition(@namespace, targetName, sourceType.Attributes);
            targetType.ClassSize = sourceType.ClassSize;
            targetType.MetadataToken = sourceType.MetadataToken;
            targetType.PackingSize = sourceType.PackingSize;
            targetType.Scope = weaveItemMember.TargetAssembly.Name;
            targetType.IsNestedAssembly = false;
            targetType.IsNestedFamily = false;
            targetType.IsNestedFamilyAndAssembly = false;
            targetType.IsNestedFamilyOrAssembly = false;
            targetType.IsNestedPrivate = false;
            targetType.IsNestedPublic = false;
            targetType.IsPublic = sourceType.IsNestedPublic;
            targetType.IsNotPublic = sourceType.IsNestedPrivate;
            if (sourceType.HasNestedTypes)
                Clone(sourceType.NestedTypes, targetType, weaveItemMember, true);
            return targetType;
        }

        static void Clone(IEnumerable<TypeDefinition> sourceNestedTypes, TypeDefinition declaringType, WeaveItemMember weaveItemMember, bool addNestedTypeToDeclaringType)
        {
            foreach (var sourceNestedType in sourceNestedTypes)
            {
                var targetNestedType = Clone(sourceNestedType, declaringType, sourceNestedType.Name, weaveItemMember, addNestedTypeToDeclaringType);
                if (addNestedTypeToDeclaringType)
                    declaringType.NestedTypes.Add(targetNestedType);
                if (sourceNestedType.HasNestedTypes)
                {
                    Clone(sourceNestedType.NestedTypes, targetNestedType, weaveItemMember, addNestedTypeToDeclaringType);
                }
            }
        }

        internal static TypeDefinition Clone(TypeDefinition sourceNestedType, TypeDefinition declaringType, string targetName, WeaveItemMember weaveItemMember, bool addNestedTypeToDeclaringType)
        {
            TypeDefinition targetNestedType = new TypeDefinition(null, targetName, sourceNestedType.Attributes);
            targetNestedType.DeclaringType = declaringType;
            targetNestedType.ClassSize = sourceNestedType.ClassSize;
            targetNestedType.MetadataToken = sourceNestedType.MetadataToken;
            targetNestedType.PackingSize = sourceNestedType.PackingSize;
            if (!addNestedTypeToDeclaringType)
            {
                targetNestedType.Scope = declaringType.Module.Assembly.Name;
                if (sourceNestedType.HasNestedTypes)
                {
                    Clone(sourceNestedType.NestedTypes, targetNestedType, weaveItemMember, true);
                }
            }
            else
                targetNestedType.Scope = weaveItemMember.TargetAssembly.Name;
            return targetNestedType;
        }

        internal static List<GenericParameter> Clone(IEnumerable<GenericParameter> sourceGenericParamters, WeaveItemMember weaveItemMember, IMemberDefinition owner)
        {
            var targetGenericParameters = new List<GenericParameter>();
            foreach (var sourceGenericParamter in sourceGenericParamters)
            {
                var targetGenericParameter = new GenericParameter((string)sourceGenericParamter.Name, (IGenericParameterProvider)owner);
                targetGenericParameter.IsValueType = sourceGenericParamter.IsValueType;
                targetGenericParameter.Attributes = sourceGenericParamter.Attributes;
                if (sourceGenericParamter.HasGenericParameters)
                    throw new NotSupportedException();
                if (sourceGenericParamter.HasConstraints)
                    sourceGenericParamter.Constraints.ToList().ForEach(t => targetGenericParameter.Constraints.Add(Clone(t, weaveItemMember)));
                targetGenericParameters.Add(targetGenericParameter);
            }

            return targetGenericParameters;
        }

        internal static GenericParameterConstraint Clone(GenericParameterConstraint genericParameterConstraint, WeaveItemMember weaveItemMember)
        {
            var clone = new GenericParameterConstraint(weaveItemMember.Resolve(genericParameterConstraint.ConstraintType));
            clone.MetadataToken = genericParameterConstraint.MetadataToken;
            foreach (var cloneCustomerAttribute in Clone(genericParameterConstraint.CustomAttributes, weaveItemMember))
                clone.CustomAttributes.Add(cloneCustomerAttribute);
            return clone;
        }

        internal static List<ParameterDefinition> Clone(IEnumerable<ParameterDefinition> sourceParameters, WeaveItemMember weaveItemMember)
        {
            var targetParameters = new List<ParameterDefinition>();
            foreach (var sourceParameter in sourceParameters)
            {
                var targetParameter = new ParameterDefinition(sourceParameter.Name, sourceParameter.Attributes, weaveItemMember.Resolve(sourceParameter.ParameterType));
                targetParameter.Constant = sourceParameter.Constant;
                targetParameter.MarshalInfo = sourceParameter.MarshalInfo;
                targetParameters.Add(targetParameter);
            }

            return targetParameters;
        }

        internal static FieldDefinition Clone(FieldDefinition sourceField, WeaveItemMember weaveItemMember)
        {
            switch (weaveItemMember)
            {
                case NewEnumMember newEnumMember:
                    return _CloneEnumMember(sourceField, newEnumMember);
                default:
                    return _CloneField(sourceField, weaveItemMember);
            }
        }

        static FieldDefinition _CloneField(FieldDefinition sourceField, WeaveItemMember weaveItemMember)
        {
            var targetField = new FieldDefinition(sourceField.Name, sourceField.Attributes, weaveItemMember.Resolve(sourceField.FieldType));
            if (weaveItemMember is NewTypeMember && !weaveItemMember.WeaveItem.Aspect.AdviceDefinition.CompilerGeneratedTypes.Any(t => sourceField.DeclaringType == t))
                targetField.DeclaringType = ((NewTypeMember)weaveItemMember).JoinpointDeclaringType;
            else
                targetField.DeclaringType = weaveItemMember.Resolve(sourceField.DeclaringType);
            targetField.MetadataToken = sourceField.MetadataToken;
            targetField.InitialValue = sourceField.InitialValue;
            targetField.MarshalInfo = sourceField.MarshalInfo;
            if (sourceField.HasConstant)
                targetField.Constant = weaveItemMember.Resolve(sourceField.Constant);
            if (sourceField.HasCustomAttributes)
            {
                foreach (var targetCustomAttribute in Clone(sourceField.CustomAttributes, weaveItemMember))
                    targetField.CustomAttributes.Add(targetCustomAttribute);
            }

            return targetField;
        }

        static FieldDefinition _CloneEnumMember(FieldDefinition sourceField, NewEnumMember weaveItemMember)
        {
            var targetFiledType = weaveItemMember.Resolve(sourceField.FieldType);
            var targetField = new FieldDefinition(sourceField.Name, sourceField.Attributes, targetFiledType);
            targetField.DeclaringType = weaveItemMember.Resolve(sourceField.DeclaringType);
            targetField.MetadataToken = sourceField.MetadataToken;
            targetField.InitialValue = sourceField.InitialValue;
            targetField.MarshalInfo = sourceField.MarshalInfo;
            if (sourceField.HasConstant)
                targetField.Constant = weaveItemMember.Resolve(sourceField.Constant);
            if (sourceField.HasCustomAttributes)
            {
                foreach (var targetCustomAttribute in Clone(sourceField.CustomAttributes, weaveItemMember))
                    targetField.CustomAttributes.Add(targetCustomAttribute);
            }

            object index = 0;
            if (targetField.DeclaringType.Fields.First().FieldType.FullName != typeof(uint).FullName)
            {
                var resolvedEnumMembers = weaveItemMember.WeaveItem.WeaveItemMembers.OfType<NewEnumMember>().Where(t => t.ClonedField != null);
                if (resolvedEnumMembers.Any())
                    index = weaveItemMember.WeaveItem.WeaveItemMembers.OfType<NewEnumMember>().Where(t => t.ClonedField != null).Max(t => (int)t.ClonedField.Constant) + 1;
                else
                {
                    var enumMembers = targetField.DeclaringType.Fields.Where(t => t.Constant != null);
                    if (enumMembers.Any())
                        index = enumMembers.Max(t => (int)t.Constant) + 1;
                }
            }
            else
            {
                index = uint.Parse(weaveItemMember.SourceField.Constant.ToString());
            }

            targetField.Constant = index;
            return targetField;
        }

        internal static MethodDefinition Clone(MethodDefinition sourceMethod, WeaveItemMember weaveItemMember)
        {
            var resolvedDeclaringType = weaveItemMember.Resolve(sourceMethod.DeclaringType);
            string targetMethodName = sourceMethod.Name;
            if (weaveItemMember.AdviceMember.IsCompilerGenerated && weaveItemMember is NewMethodMember)
                targetMethodName = ((NewMethodMember)weaveItemMember).NewMemberName;
            else
            {
                targetMethodName = weaveItemMember.ResolveMemberName(targetMethodName);
            }

            var targetMethod = new MethodDefinition(targetMethodName, sourceMethod.Attributes, weaveItemMember.TargetAssembly.MainModule.ImportReference(typeof(void)));
            weaveItemMember.MappedMethods.Add((sourceMethod, targetMethod));
            if (weaveItemMember is NewTypeMember && !weaveItemMember.WeaveItem.Aspect.AdviceDefinition.CompilerGeneratedTypes.Any(t => sourceMethod.DeclaringType == t))
                targetMethod.DeclaringType = ((NewTypeMember)weaveItemMember).JoinpointDeclaringType;
            else
                targetMethod.DeclaringType = resolvedDeclaringType;
            targetMethod.ImplAttributes = sourceMethod.ImplAttributes;
            targetMethod.SemanticsAttributes = sourceMethod.SemanticsAttributes;
            targetMethod.CallingConvention = sourceMethod.CallingConvention;
            targetMethod.ExplicitThis = sourceMethod.ExplicitThis;
            targetMethod.HasThis = sourceMethod.HasThis;
            targetMethod.IsUnmanagedExport = sourceMethod.IsUnmanagedExport;
            targetMethod.MetadataToken = sourceMethod.MetadataToken;
            if (sourceMethod.ContainsGenericParameter)
                Clone(sourceMethod.GenericParameters, weaveItemMember, targetMethod).ForEach(t => targetMethod.GenericParameters.Add(t));
            if (sourceMethod.HasCustomAttributes)
            {
                foreach (var targetCustomAttribute in Clone(sourceMethod.CustomAttributes, weaveItemMember))
                    targetMethod.CustomAttributes.Add(targetCustomAttribute);
            }

            return targetMethod;
        }

        internal static void CloneMethodReturnAndParameterTypes(MethodDefinition targetMethod, MethodDefinition sourceMethod, WeaveItemMember weaveItemMember)
        {
            targetMethod.ReturnType = weaveItemMember.Resolve(sourceMethod.ReturnType);
            if (sourceMethod.HasParameters)
                Clone(sourceMethod.Parameters, weaveItemMember).ForEach(t => targetMethod.Parameters.Add(t));
            targetMethod.MethodReturnType.MarshalInfo = sourceMethod.MethodReturnType.MarshalInfo;
            targetMethod.MethodReturnType.MetadataToken = sourceMethod.MethodReturnType.MetadataToken;
        }

        internal static EventDefinition Clone(EventDefinition sourceEvent, WeaveItemMember weaveItemMember)
        {
            var targetEventName = sourceEvent.Name;
            targetEventName = weaveItemMember.ResolveMemberName(targetEventName);
            var targetEvent = new EventDefinition(targetEventName, sourceEvent.Attributes, weaveItemMember.Resolve(sourceEvent.EventType));
            targetEvent.IsSpecialName = sourceEvent.IsSpecialName;
            if (weaveItemMember is NewTypeMember)
                targetEvent.DeclaringType = ((NewTypeMember)weaveItemMember).JoinpointDeclaringType;
            else
                targetEvent.DeclaringType = weaveItemMember.Resolve(sourceEvent.DeclaringType);
            if (sourceEvent.InvokeMethod != null)
                targetEvent.InvokeMethod = (MethodDefinition)weaveItemMember.Resolve(sourceEvent.InvokeMethod);
            if (sourceEvent.AddMethod != null)
                targetEvent.AddMethod = (MethodDefinition)weaveItemMember.Resolve(sourceEvent.AddMethod);
            if (sourceEvent.RemoveMethod != null)
                targetEvent.RemoveMethod = (MethodDefinition)weaveItemMember.Resolve(sourceEvent.RemoveMethod);
            if (sourceEvent.HasOtherMethods)
            {
                foreach (var sourceOtherMethod in sourceEvent.OtherMethods)
                    targetEvent.OtherMethods.Add((MethodDefinition)weaveItemMember.Resolve(sourceOtherMethod));
            }

            return targetEvent;
        }

        internal static PropertyDefinition Clone(PropertyDefinition sourceProperty, WeaveItemMember weaveItemMember)
        {
            var targetPropertyName = sourceProperty.Name;
            targetPropertyName = weaveItemMember.ResolveMemberName(targetPropertyName);
            var targetProperty = new PropertyDefinition(targetPropertyName, sourceProperty.Attributes, weaveItemMember.Resolve(sourceProperty.PropertyType));
            if (weaveItemMember is NewTypeMember)
                targetProperty.DeclaringType = ((NewTypeMember)weaveItemMember).JoinpointDeclaringType;
            else
                targetProperty.DeclaringType = weaveItemMember.Resolve(sourceProperty.DeclaringType);
            targetProperty.MetadataToken = sourceProperty.MetadataToken;
            targetProperty.IsSpecialName = sourceProperty.IsSpecialName;
            targetProperty.HasThis = sourceProperty.HasThis;
            if (sourceProperty.HasParameters)
                Clone(sourceProperty.Parameters, weaveItemMember).ForEach(t => targetProperty.Parameters.Add(t));
            if (sourceProperty.GetMethod != null)
                targetProperty.GetMethod = (MethodDefinition)weaveItemMember.Resolve(sourceProperty.GetMethod);
            if (sourceProperty.SetMethod != null)
                targetProperty.SetMethod = (MethodDefinition)weaveItemMember.Resolve(sourceProperty.SetMethod);
            if (sourceProperty.HasOtherMethods)
            {
                foreach (var sourceOtherMethod in sourceProperty.OtherMethods)
                    targetProperty.OtherMethods.Add((MethodDefinition)weaveItemMember.Resolve(sourceOtherMethod));
            }

            return targetProperty;
        }

        internal static List<CustomAttribute> Clone(IEnumerable<CustomAttribute> sourceCustomAttributes, WeaveItemMember weaveItemMember)
        {
            var targetCustomAttributes = new List<CustomAttribute>();
            foreach (var sourceCustomAttribute in sourceCustomAttributes)
            {
                var targetCustomAttributeConstructor = (MethodReference)weaveItemMember.Resolve(sourceCustomAttribute.Constructor);
                var targetCustomAttribute = new CustomAttribute(targetCustomAttributeConstructor, sourceCustomAttribute.GetBlob());
                if (sourceCustomAttribute.HasConstructorArguments)
                {
                    foreach (var sourceConstructorArgument in sourceCustomAttribute.ConstructorArguments)
                    {
                        targetCustomAttribute.ConstructorArguments.Add(new CustomAttributeArgument(weaveItemMember.Resolve(sourceConstructorArgument.Type), weaveItemMember.Resolve(sourceConstructorArgument.Value)));
                    }
                }

                if (sourceCustomAttribute.HasProperties)
                {
                    foreach (var sourceCustomAttributeProperty in sourceCustomAttribute.Properties)
                    {
                        var targetCustomAttributeArgument = new CustomAttributeArgument(weaveItemMember.Resolve(sourceCustomAttributeProperty.Argument.Type), weaveItemMember.Resolve(sourceCustomAttributeProperty.Argument.Value));
                        targetCustomAttribute.Properties.Add(new CustomAttributeNamedArgument(sourceCustomAttributeProperty.Name, targetCustomAttributeArgument));
                    }
                }

                if (sourceCustomAttribute.HasFields)
                {
                    foreach (var sourceCustomAttributeField in sourceCustomAttribute.Fields)
                    {
                        var targetCustomAttributeArgument = new CustomAttributeArgument(weaveItemMember.Resolve(sourceCustomAttributeField.Argument.Type), weaveItemMember.Resolve(sourceCustomAttributeField.Argument.Value));
                        targetCustomAttribute.Fields.Add(new CustomAttributeNamedArgument(sourceCustomAttributeField.Name, targetCustomAttributeArgument));
                    }
                }

                targetCustomAttributes.Add(targetCustomAttribute);
            }

            return targetCustomAttributes;
        }

        internal static List<SecurityDeclaration> Clone(IEnumerable<SecurityDeclaration> sourceSecurityDeclarations, WeaveItemMember weaveItemMember)
        {
            var targetSecurityDeclarations = new List<SecurityDeclaration>();
            foreach (var sourceSecurityDeclaration in sourceSecurityDeclarations)
            {
                var targetSecurityDeclaration = new SecurityDeclaration(sourceSecurityDeclaration.Action);
                if (sourceSecurityDeclaration.HasSecurityAttributes)
                {
                    foreach (var sourceSecurityAttribute in sourceSecurityDeclaration.SecurityAttributes)
                    {
                        var targetSecurityAttribute = new SecurityAttribute(weaveItemMember.Resolve(sourceSecurityAttribute.AttributeType));
                        if (sourceSecurityAttribute.HasFields)
                        {
                            foreach (var sourceSecurityAttributeField in sourceSecurityAttribute.Fields)
                            {
                                var targetSecurityAttributeField = new CustomAttributeArgument(weaveItemMember.Resolve(sourceSecurityAttributeField.Argument.Type), weaveItemMember.Resolve(sourceSecurityAttributeField.Argument.Value));
                                targetSecurityAttribute.Fields.Add(new CustomAttributeNamedArgument(sourceSecurityAttributeField.Name, targetSecurityAttributeField));
                            }
                        }

                        if (sourceSecurityAttribute.HasProperties)
                        {
                            foreach (var sourceSecurityAttributeProperty in sourceSecurityAttribute.Properties)
                            {
                                var targetSecurityAttributeProperty = new CustomAttributeArgument(weaveItemMember.Resolve(sourceSecurityAttributeProperty.Argument.Type), weaveItemMember.Resolve(sourceSecurityAttributeProperty.Argument.Value));
                                targetSecurityAttribute.Properties.Add(new CustomAttributeNamedArgument(sourceSecurityAttributeProperty.Name, targetSecurityAttributeProperty));
                            }
                        }

                        targetSecurityDeclaration.SecurityAttributes.Add(targetSecurityAttribute);
                    }
                }

                targetSecurityDeclarations.Add(targetSecurityDeclaration);
            }

            return targetSecurityDeclarations;
        }

        internal static InterfaceImplementation Clone(InterfaceImplementation interfaceImplementation, WeaveItemMember weaveItemMember)
        {
            var newInterfaceImplementation = new InterfaceImplementation(weaveItemMember.Resolve(interfaceImplementation.InterfaceType));
            if (interfaceImplementation.HasCustomAttributes)
                WeaverHelper.Clone(interfaceImplementation.CustomAttributes, weaveItemMember).ForEach(t => newInterfaceImplementation.CustomAttributes.Add(t));
            newInterfaceImplementation.MetadataToken = interfaceImplementation.MetadataToken;
            return newInterfaceImplementation;
        }

        internal static CustomAttribute Clone(CustomAttribute sourceCustomAttribute, WeaveItemMember weaveItemMember)
        {
            var targetConstructor = (MethodReference)weaveItemMember.Resolve(sourceCustomAttribute.Constructor);
            var targetCustomAttribute = new CustomAttribute(targetConstructor);
            if (sourceCustomAttribute.HasConstructorArguments)
            {
                foreach (var constructorArgument in sourceCustomAttribute.ConstructorArguments)
                    targetCustomAttribute.ConstructorArguments.Add(CloneCustomAttributeArgument(constructorArgument, weaveItemMember));
            }

            if (sourceCustomAttribute.HasFields)
            {
                foreach (var fieldArgument in sourceCustomAttribute.Fields)
                    targetCustomAttribute.Fields.Add(CloneCustomAttributeNamedArgument(fieldArgument, weaveItemMember));
            }

            if (sourceCustomAttribute.HasProperties)
            {
                foreach (var propertyArgument in sourceCustomAttribute.Properties)
                    targetCustomAttribute.Properties.Add(CloneCustomAttributeNamedArgument(propertyArgument, weaveItemMember));
            }

            return targetCustomAttribute;
        }

        internal static CustomAttributeNamedArgument CloneCustomAttributeNamedArgument(CustomAttributeNamedArgument sourceArgument, WeaveItemMember weaveItemMember)
        {
            return new CustomAttributeNamedArgument(sourceArgument.Name, CloneCustomAttributeArgument(sourceArgument.Argument, weaveItemMember));
        }

        internal static CustomAttributeArgument CloneCustomAttributeArgument(CustomAttributeArgument sourceCustomAttributeArgument, WeaveItemMember weaveItemMember)
        {
            return new CustomAttributeArgument(weaveItemMember.Resolve(sourceCustomAttributeArgument.Type), weaveItemMember.Resolve(sourceCustomAttributeArgument.Value));
        }

#endregion
#region DefinitionResolution
        internal static FieldDefinition ResolveDefinition(FieldReference fieldReference, WeaveItem weaveItem)
        {
            FieldDefinition fieldDefinition = null;
            if (fieldReference.Module != null)
                fieldDefinition = fieldReference.Resolve();
            if (fieldDefinition == null)
            {
                var declaringTypeDefinition = ResolveDefinition(fieldReference.DeclaringType, weaveItem);
                var flatTypeMembers = new FlatTypeMembers(declaringTypeDefinition, null, weaveItem.Weaver.SafeWeaveItemMembers).ResolveTypeMembers();
                fieldDefinition = (FieldDefinition)flatTypeMembers.First(t => t.MemberDefinition is FieldDefinition && t.MemberDefinition.Name == fieldReference.Name).MemberDefinition;
                if (fieldDefinition == null)
                    throw new NotSupportedException($"{fieldReference.FullName} has no definition");
            }

            return fieldDefinition;
        }

        internal static TypeDefinition ResolveDefinition(TypeReference typeReference, WeaveItem weaveItem)
        {
            TypeDefinition typeDefinition = null;
            if (typeReference.Module != null)
                typeDefinition = typeReference.Resolve();
            if (typeDefinition == null)
            {
                typeDefinition = weaveItem.Weaver.SafeWeaveItemMembers.OfType<INewType>().First(t => t.ClonedType.FullName == typeReference.GetElementType().FullName).ClonedType;
                if (typeDefinition == null)
                    throw new NotSupportedException($"{typeReference.FullName} has no definition");
            }

            return typeDefinition;
        }
#endregion
    }
}