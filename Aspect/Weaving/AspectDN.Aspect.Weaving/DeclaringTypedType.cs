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
using AspectDN.Common;

namespace AspectDN.Aspect.Weaving
{
    internal class DeclaringTypedType
    {
        internal DeclaringTypedType ParentType { get; }

        internal TypeDefinition Type { get; }

        internal IEnumerable<TypeReference> GenericArguments { get; }

        internal DeclaringTypedType(DeclaringTypedType parentType, TypeDefinition type, IEnumerable<TypeReference> genericArguments)
        {
            ParentType = parentType;
            Type = type;
            GenericArguments = genericArguments;
        }

        internal TypeReference ResolveGenericTypeReference(TypeReference typeReference)
        {
            if (typeReference.IsGenericParameter && GenericArguments.Any())
                typeReference = GenericArguments.ToArray()[((GenericParameter)typeReference).Position];
            return typeReference;
        }

        internal IEnumerable<TypeReference> ResolveGenericTypeReferences(IEnumerable<TypeReference> typeReferences)
        {
            var newTypeReferences = new List<TypeReference>();
            foreach (var typeReference in typeReferences)
                newTypeReferences.Add(ResolveGenericTypeReference(typeReference));
            return newTypeReferences;
        }
    }

    internal class TypedTypeVisitor
    {
        IEnumerable<WeaveItemMember> _WeaveItemMembers;
        List<DeclaringTypedType> _DeclaringTypeTypes;
        internal IEnumerable<DeclaringTypedType> Visit(TypeDefinition rootType, IEnumerable<WeaveItemMember> safeWeaveItemMembers, bool withObject = false)
        {
            _DeclaringTypeTypes = new List<DeclaringTypedType>();
            _WeaveItemMembers = safeWeaveItemMembers;
            var root = new DeclaringTypedType(null, rootType, rootType.GenericParameters.Cast<TypeReference>());
            _DeclaringTypeTypes.Add(root);
            _VisitTypedBaseType(root, withObject);
            return _DeclaringTypeTypes;
        }

        internal IEnumerable<DeclaringTypedType> Visit(TypeReference rootTypeReference, IEnumerable<WeaveItemMember> safeWeaveItemMembers, bool withObject = false)
        {
            _DeclaringTypeTypes = new List<DeclaringTypedType>();
            _WeaveItemMembers = safeWeaveItemMembers;
            var root = new DeclaringTypedType(null, CecilHelper.Resolve(rootTypeReference), CecilHelper.GetGenericArguments(rootTypeReference));
            _DeclaringTypeTypes.Add(root);
            _VisitTypedBaseType(root, withObject);
            return _DeclaringTypeTypes;
        }

        void _VisitTypedBaseType(DeclaringTypedType parentTypedType, bool withObject)
        {
            var parentTypeDefinition = parentTypedType.Type;
            TypeDefinition baseTypeDefinition = null;
            IEnumerable<TypeReference> baseGenericArguments = new TypeReference[0];
            foreach (var newInterface in _WeaveItemMembers.OfType<NewInheritedType>().Where(t => !t.OnError && t.JoinpointType.FullName == parentTypeDefinition.FullName && t.IsInterface))
            {
                baseTypeDefinition = newInterface.TargetBaseType.GetElementType().Resolve();
                baseGenericArguments = new TypeReference[0];
                if (newInterface.TargetBaseType is GenericInstanceType)
                    baseGenericArguments = ((GenericInstanceType)newInterface.TargetBaseType).GenericArguments;
            }

            foreach (var @interface in parentTypeDefinition.Interfaces)
            {
                baseTypeDefinition = @interface.InterfaceType.GetElementType().Resolve();
                baseGenericArguments = new TypeReference[0];
                if (@interface.InterfaceType is GenericInstanceType)
                {
                    var baseGenericArgumentArray = ((GenericInstanceType)@interface.InterfaceType).GenericArguments.ToArray();
                    for (int i = 0; i < baseGenericArgumentArray.Length; i++)
                    {
                        if (baseGenericArgumentArray[i] is GenericParameter && baseGenericArgumentArray[i].DeclaringType.FullName == parentTypeDefinition.FullName)
                            baseGenericArgumentArray[i] = parentTypedType.GenericArguments.ToArray()[((GenericParameter)baseGenericArgumentArray[i]).Position];
                    }

                    baseGenericArguments = baseGenericArgumentArray;
                }

                _DeclaringTypeTypes.Add(new DeclaringTypedType(parentTypedType, baseTypeDefinition, baseGenericArguments));
            }

            if (parentTypeDefinition.BaseType == null)
                return;
            baseTypeDefinition = parentTypeDefinition.BaseType.GetElementType().Resolve();
            baseGenericArguments = new TypeReference[0];
            if (baseTypeDefinition.FullName == typeof(System.Object).FullName || baseTypeDefinition.FullName == typeof(System.ValueType).FullName)
            {
                baseTypeDefinition = null;
                var newinheritWeaveItem = _WeaveItemMembers.OfType<NewInheritedType>().FirstOrDefault(t => !t.OnError && t.JoinpointType.FullName == parentTypeDefinition.FullName && !t.IsInterface);
                if (newinheritWeaveItem != null)
                {
                    baseTypeDefinition = newinheritWeaveItem.TargetBaseType.GetElementType().Resolve();
                    if (newinheritWeaveItem.TargetBaseType is GenericInstanceType)
                        baseGenericArguments = ((GenericInstanceType)newinheritWeaveItem.TargetBaseType).GenericArguments.ToArray();
                }
                else
                {
                    if (withObject)
                    {
                        baseTypeDefinition = parentTypeDefinition.BaseType.GetElementType().Resolve();
                    }
                }
            }
            else
            {
                if (parentTypeDefinition.BaseType is GenericInstanceType)
                    baseGenericArguments = ((GenericInstanceType)parentTypeDefinition.BaseType).GenericArguments;
            }

            if (baseTypeDefinition != null)
            {
                var typedBaseType = new DeclaringTypedType(parentTypedType, baseTypeDefinition, baseGenericArguments);
                _DeclaringTypeTypes.Add(typedBaseType);
                _VisitTypedBaseType(typedBaseType, withObject);
            }
        }
    }
}