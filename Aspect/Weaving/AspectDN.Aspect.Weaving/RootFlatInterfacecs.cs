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
    internal class FlatInterfaceMembers
    {
        int _Id;
        IEnumerable<WeaveItemMember> _SafeWeaveItemMembers;
        TypeDefinition _ResolvedInterface;
        List<InterfaceMember> _AllInterfaceMembers;
        List<(TypeReference resolvedInterface, IEnumerable<TypeReference> genericArguments, IEnumerable<InterfaceMember> interfaceMembers, NewInheritedType newInheritedType)> _InterfaceStack;
        bool _OnError = false;
        internal WeaveItemMember WeaveItemMemberOrigin { get; }

        internal bool OnError => _OnError;
        internal FlatInterfaceMembers(TypeDefinition resolvedInterface, IEnumerable<WeaveItemMember> safeWeaveItemMembers, WeaveItemMember weaveItemMemberOrigin = null)
        {
            _SafeWeaveItemMembers = safeWeaveItemMembers;
            _ResolvedInterface = resolvedInterface;
            WeaveItemMemberOrigin = weaveItemMemberOrigin;
        }

        internal FlatInterfaceMembers Check()
        {
            _AllInterfaceMembers = new List<InterfaceMember>();
            _InterfaceStack = new List<(TypeReference resolvedInterface, IEnumerable<TypeReference> genericArguments, IEnumerable<InterfaceMember> interfaceMembers, NewInheritedType newInheritedType)>();
            _Check(_ResolvedInterface, null);
            return this;
        }

        internal IEnumerable<InterfaceMember> GetInterfaceMembers(IEnumerable<TypeReference> interfaceResolvedGenericArguments)
        {
            if (_ResolvedInterface.HasGenericParameters)
            {
                var interfaceMembers = new List<InterfaceMember>(_AllInterfaceMembers.Count);
                foreach (var interfaceMember in _AllInterfaceMembers)
                {
                    var memberType = _ChangeTypeReferenceElementType(interfaceMember.MemberType, interfaceResolvedGenericArguments);
                    var parameterTypes = new List<TypeReference>(interfaceMember.ParameterTypes);
                    for (int i = 0; i < parameterTypes.Count; i++)
                        parameterTypes[i] = _ChangeTypeReferenceElementType(parameterTypes[i], interfaceResolvedGenericArguments);
                    interfaceMembers.Add(new InterfaceMember(this, interfaceMember.MemberDefinition, memberType, parameterTypes, interfaceMember.WeaveItemMember));
                }

                return interfaceMembers;
            }
            else
                return _AllInterfaceMembers;
        }

        internal TypeReference _ChangeTypeReferenceElementType(TypeReference typeReference, IEnumerable<TypeReference> interfaceResolvedGenericArguments)
        {
            var newTypeReference = typeReference;
            var genericParameter = _ResolvedInterface.GenericParameters.FirstOrDefault(t => t.GetElementType().FullName == newTypeReference.GetElementType().FullName);
            if (genericParameter != null)
            {
                newTypeReference = interfaceResolvedGenericArguments.ToList()[genericParameter.Position];
            }

            return newTypeReference;
        }

        void _Check(TypeReference @interface, NewInheritedType newInheritedInterface)
        {
            TypeReference parentInterface = null;
            IEnumerable<TypeReference> interfaceGenericArguments = null;
            List<InterfaceMember> interfaceMembers = null;
            var interfaceType = @interface.Resolve();
            if (_InterfaceStack.Any())
            {
                var parent = _InterfaceStack.Last();
                parentInterface = parent.resolvedInterface;
                if (@interface is GenericInstanceType && parent.genericArguments != null)
                    interfaceGenericArguments = WeaverHelper.GetTypeGenericArguments(parentInterface.Resolve(), parent.genericArguments, ((GenericInstanceType)@interface).GenericArguments);
            }

            interfaceMembers = _ResolveInterfaceMembers(interfaceGenericArguments, interfaceType, newInheritedInterface);
            _InterfaceStack.Add((interfaceType, interfaceGenericArguments, interfaceMembers, newInheritedInterface));
            var newBaseInterfaces = _SafeWeaveItemMembers.OfType<NewInheritedType>().Where(t => t.IsInterface && t.JoinpointType.FullName == interfaceType.FullName);
            foreach (var newBaseInterface in newBaseInterfaces)
            {
                if (_InterfaceStack.Any(t => t.resolvedInterface.FullName == newBaseInterface.TargetBaseType.FullName))
                {
                    newBaseInterface.AddError(WeaverHelper.GetError(newInheritedInterface, "NewIhneritedInterfaceInterfaceHirarchyLoop"));
                    _OnError = true;
                }
                else
                {
                    _Check(newBaseInterface.SourceBaseType, newBaseInterface);
                }
            }

            if (interfaceType.HasInterfaces)
            {
                foreach (var baseInterface in interfaceType.Interfaces.Select(t => t.InterfaceType))
                {
                    if (_InterfaceStack.Any(t => t.resolvedInterface.Resolve().FullName == baseInterface.Resolve().FullName))
                    {
                        var stack = _InterfaceStack.Where(t => t.resolvedInterface.Resolve().FullName == baseInterface.Resolve().FullName).First().newInheritedType;
                        stack.AddError(WeaverHelper.GetError(newInheritedInterface, "NewIhneritedInterfaceAlreadyExist"));
                        _OnError = true;
                    }
                    else
                        _Check(baseInterface, null);
                }
            }

            foreach (var interfaceMember in interfaceMembers.Where(t => !_AllInterfaceMembers.Any(o => o.Id == t.Id)))
                _AllInterfaceMembers.Add(interfaceMember);
            _InterfaceStack.Remove(_InterfaceStack.Last());
        }

        List<InterfaceMember> _ResolveInterfaceMembers(IEnumerable<TypeReference> interfaceGenericArguments, TypeReference @interface, NewInheritedType newInheritedType)
        {
            var interfaceMembers = new List<InterfaceMember>();
            var newMembers = CecilHelper.GetTypeMembers(@interface.Resolve(), false);
            foreach (var interfaceMember in newMembers)
            {
                InterfaceMember newMember = null;
                switch (interfaceMember)
                {
                    case PropertyDefinition property:
                        newMember = _Create(interfaceGenericArguments, property, property.PropertyType, property.Parameters.Select(p => p.ParameterType));
                        break;
                    case EventDefinition @event:
                        newMember = _Create(interfaceGenericArguments, @event, @event.EventType, null);
                        break;
                    case MethodDefinition method:
                        newMember = _Create(interfaceGenericArguments, method, method.MethodReturnType.ReturnType, method.Parameters.Select(p => p.ParameterType));
                        break;
                    default:
                        throw new NotSupportedException();
                }

                if (newMember != null)
                    interfaceMembers.Add(newMember);
            }

            var newInterfaceMembers = _SafeWeaveItemMembers.OfType<NewTypeMember>().Where(t => t.JoinpointDeclaringType.FullName == @interface.Resolve().FullName);
            foreach (var newInterfaceMember in newInterfaceMembers)
            {
                InterfaceMember newMember = null;
                switch (newInterfaceMember.ClonedMember)
                {
                    case PropertyDefinition property:
                        newMember = _Create(interfaceGenericArguments, property, property.PropertyType, property.Parameters.Select(p => p.ParameterType));
                        break;
                    case EventDefinition @event:
                        newMember = _Create(interfaceGenericArguments, @event, @event.EventType, null);
                        break;
                    case MethodDefinition method:
                        newMember = _Create(interfaceGenericArguments, method, method.MethodReturnType.ReturnType, method.Parameters.Select(p => p.ParameterType));
                        break;
                    default:
                        throw new NotSupportedException();
                }

                if (newMember != null)
                    interfaceMembers.Add(newMember);
            }

            return interfaceMembers;
        }

        void _Create(List<InterfaceMember> interfaceMembers, IEnumerable<TypeReference> declaringInterfaceGenericArguments, IMemberDefinition memberDefinition, WeaveItemMember weaveItemMember)
        {
            InterfaceMember newMember = null;
            switch (memberDefinition)
            {
                case PropertyDefinition property:
                    newMember = _Create(declaringInterfaceGenericArguments, property, property.PropertyType, property.Parameters.Select(p => p.ParameterType));
                    if (property.HasParameters)
                        _AddNewInterfaceIndexer(interfaceMembers, newMember);
                    else
                        _AddNewInterfacePropertyOrEvent(interfaceMembers, newMember);
                    break;
                case EventDefinition @event:
                    newMember = _Create(declaringInterfaceGenericArguments, @event, @event.EventType, null);
                    _AddNewInterfacePropertyOrEvent(interfaceMembers, newMember);
                    break;
                case MethodDefinition method:
                    newMember = _Create(declaringInterfaceGenericArguments, method, method.MethodReturnType.ReturnType, method.Parameters.Select(p => p.ParameterType));
                    _AddNewInterfaceMethod(interfaceMembers, newMember);
                    break;
                default:
                    throw new NotSupportedException();
            }

            if (!_AllInterfaceMembers.Any(t => t.MemberDefinition.Name == newMember.MemberDefinition.Name && t.GenericParametersCount == newMember.GenericParametersCount && WeaverHelper.IsSame(t.MemberType, newMember.MemberType) && WeaverHelper.IsSame(t.ParameterTypes, newMember.ParameterTypes)))
                _AllInterfaceMembers.Add(newMember);
        }

        InterfaceMember _Create(IEnumerable<TypeReference> declaringInterfaceGenericArguments, IMemberDefinition memberDefinition, TypeReference memberType, IEnumerable<TypeReference> parameterTypes, WeaveItemMember weaveItemMember = null)
        {
            var flatMemberTypeReference = memberType;
            if (memberType is GenericParameter)
                flatMemberTypeReference = WeaverHelper.GetTypeReference((GenericParameter)memberType, declaringInterfaceGenericArguments);
            var flatGenericArguments = declaringInterfaceGenericArguments;
            if (memberDefinition is MethodDefinition && weaveItemMember != null)
                flatGenericArguments = weaveItemMember.Resolve(WeaverHelper.GetMethodGenericArguments((MethodDefinition)memberDefinition, declaringInterfaceGenericArguments));
            var flatParameterTypes = WeaverHelper.GetTypeReferences(parameterTypes ?? Array.Empty<TypeReference>(), flatGenericArguments);
            return new InterfaceMember(this, memberDefinition, flatMemberTypeReference, flatParameterTypes, weaveItemMember);
        }

        void _AddNewInterfacePropertyOrEvent(List<InterfaceMember> interfaceMembers, InterfaceMember newInterfaceMember)
        {
            foreach (var existingMember in interfaceMembers.Where(t => t.MemberDefinition.Name == newInterfaceMember.MemberDefinition.Name))
            {
                if (WeaverHelper.IsSame(existingMember.MemberType, newInterfaceMember.MemberType) && existingMember.MemberDefinition.GetType().FullName == existingMember.MemberDefinition.GetType().FullName)
                    continue;
                if (existingMember.IsNew)
                    continue;
                if (existingMember.WeaveItemMember != null)
                {
                    existingMember.WeaveItemMember.AddError(WeaverHelper.GetError(existingMember.WeaveItemMember, "NewInterfaceMemberAlreadyExist"));
                    interfaceMembers.Remove(existingMember);
                }

                if (newInterfaceMember.WeaveItemMember != null)
                    newInterfaceMember.WeaveItemMember.AddError(WeaverHelper.GetError(newInterfaceMember.WeaveItemMember, "NewInterfaceMemberAlreadyExist"));
            }

            if (newInterfaceMember.WeaveItemMember == null || !newInterfaceMember.WeaveItemMember.WeaveItem.OnError)
                interfaceMembers.Add(newInterfaceMember);
        }

        void _AddNewInterfaceIndexer(List<InterfaceMember> interfaceMembers, InterfaceMember newInterfaceMember)
        {
            foreach (var existingMember in interfaceMembers.Where(t => t.MemberDefinition.Name == newInterfaceMember.MemberDefinition.Name))
            {
                if (WeaverHelper.IsSame(existingMember.MemberType, newInterfaceMember.MemberType) && WeaverHelper.IsSame(existingMember.ParameterTypes, newInterfaceMember.ParameterTypes) && existingMember.MemberDefinition.GetType().FullName == existingMember.MemberDefinition.GetType().FullName)
                    continue;
                if (existingMember.IsNew)
                    continue;
                if (existingMember.WeaveItemMember != null)
                {
                    existingMember.WeaveItemMember.AddError(WeaverHelper.GetError(existingMember.WeaveItemMember, "NewInterfaceMemberAlreadyExist"));
                    interfaceMembers.Remove(existingMember);
                }

                if (newInterfaceMember.WeaveItemMember != null)
                    newInterfaceMember.WeaveItemMember.AddError(WeaverHelper.GetError(newInterfaceMember.WeaveItemMember, "NewInterfaceMemberAlreadyExist"));
            }

            if (newInterfaceMember.WeaveItemMember == null || !newInterfaceMember.WeaveItemMember.WeaveItem.OnError)
                interfaceMembers.Add(newInterfaceMember);
        }

        void _AddNewInterfaceMethod(List<InterfaceMember> interfaceMembers, InterfaceMember newInterfaceMember)
        {
            foreach (var existingMember in interfaceMembers.Where(t => t.MemberDefinition.Name == newInterfaceMember.MemberDefinition.Name))
            {
                if (existingMember.IsNew || existingMember.MemberDefinition is MethodDefinition)
                    continue;
                if (existingMember.WeaveItemMember != null)
                {
                    existingMember.WeaveItemMember.AddError(WeaverHelper.GetError(existingMember.WeaveItemMember, "NewTypeemberAlreadyExist"));
                    interfaceMembers.Remove(existingMember);
                }

                if (newInterfaceMember.WeaveItemMember != null)
                    newInterfaceMember.WeaveItemMember.AddError(WeaverHelper.GetError(newInterfaceMember.WeaveItemMember, "NewTypeemberAlreadyExist"));
            }

            if (newInterfaceMember.WeaveItemMember == null || !newInterfaceMember.WeaveItemMember.WeaveItem.OnError)
                interfaceMembers.Add(newInterfaceMember);
        }

        internal class InterfaceMember
        {
            internal int Id { get; }

            internal TypeDefinition DeclaringType => MemberDefinition.DeclaringType;
            internal IMemberDefinition MemberDefinition { get; }

            internal TypeReference MemberType { get; }

            internal int GenericParametersCount => MemberDefinition is MethodDefinition ? ((MethodDefinition)MemberDefinition).GenericParameters.Count : -1;
            internal IEnumerable<TypeReference> ParameterTypes { get; }

            internal WeaveItemMember WeaveItemMember { get; }

            internal bool IsNew
            {
                get
                {
                    if (WeaveItemMember != null && WeaveItemMember is NewTypeMember)
                        return (((NewTypeMember)WeaveItemMember).MemberModifiers & IConcerns.AspectMemberModifiers.@new) == IConcerns.AspectMemberModifiers.@new;
                    return false;
                }
            }

            internal InterfaceMember(FlatInterfaceMembers parent, IMemberDefinition memberDefinition, TypeReference memberType, IEnumerable<TypeReference> parameterTypes, WeaveItemMember weaveItemMember)
            {
                Id = ++parent._Id;
                MemberDefinition = memberDefinition;
                ParameterTypes = parameterTypes ?? Array.Empty<TypeReference>();
                MemberType = memberType;
                WeaveItemMember = weaveItemMember;
            }
        }
    }
}