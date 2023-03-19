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
using AspectDN.Aspect.Weaving.IConcerns;

namespace AspectDN.Aspect.Concerns
{
    internal class ConsumedPrototypeTypesVisitor
    {
        TypeDefinition _PrototypeType;
        List<TypeDefinition> _ConsumedTypes;
        internal ConsumedPrototypeTypesVisitor(TypeDefinition prototypeType)
        {
            _PrototypeType = prototypeType;
        }

        internal IEnumerable<TypeDefinition> Get()
        {
            _ConsumedTypes = new List<TypeDefinition>();
            _Get(_PrototypeType);
            return _ConsumedTypes;
        }

        void _Add(TypeDefinition type)
        {
            if (CecilHelper.HasCustomAttributesOfType(type, typeof(PrototypeTypeDeclarationAttribute)) && !_ConsumedTypes.Exists(t => t.FullName == type.FullName))
                _ConsumedTypes.Add(type);
        }

        void _Get(TypeDefinition type)
        {
            if (type.BaseType != null && type.BaseType.FullName != typeof(object).FullName)
                _Get(type.BaseType.GetElementType().Resolve());
            if (type.HasInterfaces)
                type.Interfaces.ToList().ForEach(t => _Get(t.InterfaceType.GetElementType().Resolve()));
            var members = CecilHelper.GetTypeMembers(type);
            _Get(members);
            var deductedTypeAttributes = members.Where(t => CecilHelper.HasCustomAttributesOfType(t, typeof(AdviceMemberOrign))).Select(t => (TypeDefinition)CecilHelper.GetCustomAttributeOfType(t, typeof(AdviceMemberOrign)).ConstructorArguments[0].Value).SelectMany(t => CecilHelper.GetCustomAttributesOfType(t, typeof(ReferencedAdviceTypesAttribute)));
            if (deductedTypeAttributes.Any())
            {
                var deductedTypes = deductedTypeAttributes.SelectMany(t => (CustomAttributeArgument[])t.ConstructorArguments.First().Value).Select(t => (TypeDefinition)t.Value);
                deductedTypes.ToList().ForEach(t => _Add(t));
            }
        }

        void _Get(IEnumerable<IMemberDefinition> members)
        {
            foreach (var member in members)
            {
                switch (member)
                {
                    case FieldDefinition field:
                        _Get(field);
                        break;
                    case PropertyDefinition property:
                        _Get(property);
                        break;
                    case MethodDefinition method:
                        _Get(method);
                        break;
                    case EventDefinition @event:
                        _Get(@event);
                        break;
                    case TypeDefinition type:
                        _Add(type);
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        void _Get(FieldDefinition field)
        {
            if (!field.FieldType.IsGenericParameter)
                _Add(field.FieldType.GetElementType().Resolve());
        }

        void _Get(PropertyDefinition property)
        {
            if (!property.PropertyType.IsGenericParameter)
                _Add(property.PropertyType.GetElementType().Resolve());
            property.Parameters.Where(t => !t.ParameterType.IsGenericParameter && !t.ParameterType.GetElementType().IsGenericParameter).Select(t => t.ParameterType.GetElementType().Resolve()).ToList().ForEach(t => _Add(t));
        }

        void _Get(MethodDefinition method)
        {
            if (!method.ReturnType.IsGenericParameter)
                _Add(method.ReturnType.GetElementType().Resolve());
            method.Parameters.Where(t => !t.ParameterType.IsGenericParameter && !t.ParameterType.GetElementType().IsGenericParameter).Select(t => t.ParameterType.GetElementType().Resolve()).ToList().ForEach(t => _Add(t));
        }

        void _Get(EventDefinition @event)
        {
            _Add(@event.EventType.GetElementType().Resolve());
        }
    }
}