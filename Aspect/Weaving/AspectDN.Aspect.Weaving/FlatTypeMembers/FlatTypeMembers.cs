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
using Foundation.Common.Error;

namespace AspectDN.Aspect.Weaving
{
    internal class FlatTypeMembers : IEnumerable<FlatTypeMember>
    {
        IEnumerable<WeaveItemMember> _SafeWeaveItemMembers;
        List<FlatType> _FlatTypesStack;
        List<FlatTypeMember> _FlatTypeMembers;
        bool _OnError = false;
        internal FlatType Root => _FlatTypesStack.LastOrDefault();
        internal bool OnError => _OnError;
        internal FlatTypeMembers(TypeDefinition rootType, NewInheritedType newInheritedType, IEnumerable<WeaveItemMember> safeWeaveItemMembers)
        {
            _SafeWeaveItemMembers = safeWeaveItemMembers;
            _FlatTypesStack = new List<FlatType>();
            _FlatTypeMembers = new List<FlatTypeMember>();
            _BuildFlatTypesStack(null, rootType, newInheritedType, null);
        }

        internal FlatTypeMembers ResolveTypeMembers()
        {
            foreach (var flatType in _FlatTypesStack)
                _ResolveTypeMember(flatType);
            foreach (var flatType in _FlatTypesStack)
                _ResolveNewTypeMember(flatType);
            return this;
        }

        void _BuildFlatTypesStack(FlatType parentFlatType, TypeDefinition typeToInspect, NewInheritedType newInheritedType, IEnumerable<TypeReference> typeGenericArguments)
        {
            TypeReference baseType = null;
            NewInheritedType baseNewInheritedType = null;
            IEnumerable<TypeReference> baseTypeGenericArguments = null;
            var flatType = new FlatType(this, parentFlatType, typeToInspect, typeGenericArguments, newInheritedType);
            _FlatTypesStack.Insert(0, flatType);
            if (typeGenericArguments == null)
                typeGenericArguments = typeToInspect.GenericParameters;
            if (typeToInspect.BaseType != null && CecilHelper.IsObjectOrValueTypeObject(typeToInspect.BaseType))
            {
                if (newInheritedType == null)
                    newInheritedType = _SafeWeaveItemMembers.OfType<NewInheritedType>().FirstOrDefault(t => !t.IsInterface && t.JoinpointType.FullName == typeToInspect.FullName);
                if (newInheritedType != null)
                {
                    baseNewInheritedType = _SafeWeaveItemMembers.OfType<NewInheritedType>().Where(t => t.JoinpointType.FullName == typeToInspect.FullName && !t.IsInterface).FirstOrDefault();
                    if (baseNewInheritedType == null)
                        return;
                    if (baseNewInheritedType.SourceBaseType is GenericInstanceType)
                    {
                        if (baseNewInheritedType.ResolvedGenericArguments == null)
                        {
                            var initialBaseTypeGenericArguments = ((GenericInstanceType)baseNewInheritedType.SourceBaseType).GenericArguments.ToList();
                            for (int i = 0; i < initialBaseTypeGenericArguments.Count; i++)
                            {
                                var baseTypeGenericArgument = newInheritedType.Resolve(initialBaseTypeGenericArguments[i]);
                                initialBaseTypeGenericArguments[i] = baseTypeGenericArgument;
                            }

                            newInheritedType.ResolvedGenericArguments = WeaverHelper.GetTypeGenericArguments(typeToInspect, typeGenericArguments, initialBaseTypeGenericArguments);
                        }

                        baseTypeGenericArguments = newInheritedType.ResolvedGenericArguments;
                    }

                    baseType = newInheritedType.TargetBaseType;
                }
                else
                {
                    baseType = typeToInspect.BaseType;
                }
            }
            else
            {
                baseType = typeToInspect.BaseType;
                if (baseType is GenericInstanceType)
                    baseTypeGenericArguments = WeaverHelper.GetTypeGenericArguments(typeToInspect, typeGenericArguments, ((GenericInstanceType)baseType).GenericArguments);
            }

            if (baseType != null)
            {
                _BuildFlatTypesStack(flatType, baseType.GetElementType().Resolve(), baseNewInheritedType, baseTypeGenericArguments);
            }
            else
                return;
        }

        void _ResolveTypeMember(FlatType flatType)
        {
            var baseMembers = CecilHelper.GetTypeMembers(flatType.Type);
            foreach (var baseMember in baseMembers)
            {
                FlatTypeMember oldMember = null;
                FlatTypeMember newMember = null;
                switch (baseMember)
                {
                    case FieldDefinition field:
                        newMember = _CreateFlatMember(flatType, field, field.FieldType, null, flatType.GenericArguments);
                        oldMember = _FlatTypeMembers.Where(t => t.MemberDefinition.Name == field.Name).FirstOrDefault();
                        break;
                    case PropertyDefinition property:
                        newMember = _CreateFlatMember(flatType, property, property.PropertyType, property.Parameters.Select(p => p.ParameterType), flatType.GenericArguments);
                        oldMember = _FlatTypeMembers.Where(t => t.MemberDefinition.Name == property.Name && t.ParameterTypes != null && WeaverHelper.IsSame(t.ParameterTypes.ToArray(), property.Parameters.Select(p => p.ParameterType).ToArray())).FirstOrDefault();
                        break;
                    case EventDefinition @event:
                        newMember = _CreateFlatMember(flatType, @event, @event.EventType, null, flatType.GenericArguments);
                        oldMember = _FlatTypeMembers.Where(t => t.MemberDefinition.Name == @event.Name).FirstOrDefault();
                        break;
                    case MethodDefinition method:
                        newMember = _CreateFlatMember(flatType, method, method.MethodReturnType.ReturnType, method.Parameters.Select(p => p.ParameterType), flatType.GenericArguments);
                        oldMember = _FlatTypeMembers.Where(t => t.MemberDefinition.Name == method.Name && t.ParameterTypes != null && WeaverHelper.IsSame(t.ParameterTypes.ToArray(), newMember.ParameterTypes.ToArray()) && t.GenericParametersCount == newMember.GenericParametersCount).FirstOrDefault();
                        break;
                    case TypeDefinition type:
                        newMember = _CreateFlatMember(flatType, type, null, null, flatType.GenericArguments);
                        oldMember = _FlatTypeMembers.Where(t => t.MemberDefinition.Name == type.Name).FirstOrDefault();
                        break;
                    default:
                        throw new NotSupportedException();
                }

                if (oldMember == null)
                    flatType.AddMember(newMember);
                else
                {
                    if (newMember.NewTypeMember != null)
                    {
                        newMember.NewTypeMember.AddError(AspectDNErrorFactory.GetError("MemberAlreadyExist", oldMember.MemberDefinition.FullName));
                        _OnError = true;
                    }

                    flatType.ReplaceMember(oldMember, newMember);
                }
            }
        }

        void _ResolveNewTypeMember(FlatType flatType)
        {
            var newTypeMembers = _SafeWeaveItemMembers.OfType<NewTypeMember>().Where(t => flatType.Type.FullName == t.FullJoinpointDeclaringTypeName);
            foreach (var newTypeMember in newTypeMembers)
            {
                FlatTypeMember newMember = null;
                switch (newTypeMember.Member)
                {
                    case FieldDefinition field:
                        newMember = _CreateFlatMember(flatType, field, field.FieldType, null, flatType.GenericArguments, newTypeMember);
                        break;
                    case PropertyDefinition property:
                        newMember = _CreateFlatMember(flatType, property, property.PropertyType, property.Parameters.Select(p => p.ParameterType), flatType.GenericArguments, newTypeMember);
                        break;
                    case EventDefinition @event:
                        newMember = _CreateFlatMember(flatType, @event, @event.EventType, null, flatType.GenericArguments, newTypeMember);
                        break;
                    case MethodDefinition method:
                        newMember = _CreateFlatMember(flatType, method, method.MethodReturnType.ReturnType, method.Parameters.Select(p => p.ParameterType), flatType.GenericArguments, newTypeMember);
                        break;
                    default:
                        throw new NotSupportedException();
                }

                switch (newTypeMember.Member)
                {
                    case TypeDefinition type:
                    case FieldDefinition field:
                    case EventDefinition @event:
                        var oldMembers = _FlatTypeMembers.Where(t => t.MemberDefinition.Name == newMember.MemberDefinition.Name);
                        if (oldMembers.Count() > 1)
                        {
                            newTypeMember.AddError(AspectDNErrorFactory.GetError("MemberAlreadyExist", oldMembers.First().MemberDefinition.FullName));
                            foreach (var existing in oldMembers.Where(t => t.IsWeaveItemMemberOrigin))
                            {
                                existing.WeaveItemMemberOrigin.AddError(AspectDNErrorFactory.GetError("MemberAlreadyExist", existing.MemberDefinition.FullName));
                                _OnError = true;
                            }
                        }
                        else
                            flatType.AddMember(newMember);
                        break;
                    case PropertyDefinition property:
                        if (property.HasParameters)
                        {
                            oldMembers = _FlatTypeMembers.Where(t => t.MemberDefinition.Name == property.Name && t.ParameterTypes != null && WeaverHelper.IsSame(t.ParameterTypes.ToArray(), property.Parameters.Select(p => p.ParameterType).ToArray()) && !t.IsWeaveItemMemberOrigin);
                            if (oldMembers.Count() > 1)
                            {
                                newTypeMember.AddError(AspectDNErrorFactory.GetError("MemberAlreadyExist", oldMembers.First().MemberDefinition.FullName));
                                _OnError = true;
                                foreach (var existing in oldMembers.Where(t => t.IsWeaveItemMemberOrigin))
                                    existing.WeaveItemMemberOrigin.AddError(AspectDNErrorFactory.GetError("MemberAlreadyExist", existing.MemberDefinition.FullName));
                            }
                            else
                                flatType.AddMember(newMember);
                        }
                        else
                        {
                            oldMembers = _FlatTypeMembers.Where(t => t.MemberDefinition.Name == newMember.MemberDefinition.Name);
                            if (oldMembers.Count() > 1)
                            {
                                newTypeMember.AddError(AspectDNErrorFactory.GetError("MemberAlreadyExist", oldMembers.First().MemberDefinition.FullName));
                                _OnError = true;
                                foreach (var existing in oldMembers.Where(t => t.IsWeaveItemMemberOrigin))
                                    newTypeMember.AddError(AspectDNErrorFactory.GetError("MemberAlreadyExist", existing.MemberDefinition.FullName));
                            }
                            else
                                flatType.AddMember(newMember);
                        }

                        break;
                    case MethodDefinition method:
                        var oldMember = _FlatTypeMembers.Where(t => t.MemberDefinition.Name == method.Name && t.ParameterTypes != null && WeaverHelper.IsSame(t.ParameterTypes.ToArray(), newMember.ParameterTypes.ToArray()) && t.GenericParametersCount == newMember.GenericParametersCount).FirstOrDefault();
                        if (oldMember != null)
                        {
                            if (oldMember.ParentFlatType.FLatTypeLevel == newMember.ParentFlatType.FLatTypeLevel)
                            {
                                newTypeMember.AddError(AspectDNErrorFactory.GetError("MemberAlreadyExist", oldMember.MemberDefinition.FullName));
                                _OnError = true;
                                if (oldMember.IsWeaveItemMemberOrigin)
                                    newTypeMember.AddError(AspectDNErrorFactory.GetError("MemberAlreadyExist", oldMember.MemberDefinition.FullName));
                            }
                            else
                            {
                                if (oldMember.ParentFlatType.FLatTypeLevel < newMember.ParentFlatType.FLatTypeLevel)
                                {
                                    if (oldMember.IsOverriden && newMember.MemberType.FullName == oldMember.MemberType.FullName)
                                    {
                                        if (newMember.IsNew)
                                        {
                                            flatType.ReplaceMember(oldMember, newMember);
                                            newMember.NewTypeMember.SetNew();
                                        }
                                        else
                                        {
                                            if (newMember.IsOverriden)
                                            {
                                                flatType.ReplaceMember(oldMember, newMember);
                                                newMember.NewTypeMember.SetOverride();
                                            }
                                            else
                                            {
                                                newTypeMember.AddError(AspectDNErrorFactory.GetError("MemberAlreadyExist", oldMember.MemberDefinition.FullName));
                                                _OnError = true;
                                                if (oldMember.IsWeaveItemMemberOrigin)
                                                    newTypeMember.AddError(AspectDNErrorFactory.GetError("MemberAlreadyExist", oldMember.MemberDefinition.FullName));
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (newMember.IsNew)
                                        {
                                            flatType.ReplaceMember(oldMember, newMember);
                                            newMember.NewTypeMember.SetNew();
                                        }
                                        else
                                        {
                                            newTypeMember.AddError(AspectDNErrorFactory.GetError("MemberAlreadyExist", oldMember.MemberDefinition.FullName));
                                            _OnError = true;
                                            if (oldMember.IsWeaveItemMemberOrigin)
                                                newTypeMember.AddError(AspectDNErrorFactory.GetError("MemberAlreadyExist", oldMember.MemberDefinition.FullName));
                                        }
                                    }
                                }
                                else
                                {
                                    newTypeMember.AddError(AspectDNErrorFactory.GetError("MemberAlreadyExist", oldMember.MemberDefinition.FullName));
                                    _OnError = true;
                                    if (oldMember.IsWeaveItemMemberOrigin)
                                        newTypeMember.AddError(AspectDNErrorFactory.GetError("MemberAlreadyExist", oldMember.MemberDefinition.FullName));
                                }
                            }
                        }
                        else
                            flatType.AddMember(newMember);
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        FlatTypeMember _CreateFlatMember(FlatType parentFlatType, IMemberDefinition memberDefinition, TypeReference memberType, IEnumerable<TypeReference> parameterTypes, IEnumerable<TypeReference> declaringTypeGenericArguments, NewTypeMember newTypeMember = null)
        {
            var flatMemberType = memberType;
            if (memberType is GenericParameter)
                flatMemberType = WeaverHelper.GetTypeReference((GenericParameter)memberType, declaringTypeGenericArguments);
            IEnumerable<TypeReference> flatGenericArguments = declaringTypeGenericArguments;
            if (memberDefinition is MethodDefinition && newTypeMember != null)
                flatGenericArguments = newTypeMember.Resolve(WeaverHelper.GetMethodGenericArguments((MethodDefinition)memberDefinition, declaringTypeGenericArguments));
            var flatParameterTypes = WeaverHelper.GetTypeReferences(parameterTypes ?? Array.Empty<TypeReference>(), declaringTypeGenericArguments);
            return new FlatTypeMember(parentFlatType, memberDefinition, flatMemberType, flatParameterTypes, newTypeMember);
        }

        IEnumerator<FlatTypeMember> IEnumerable<FlatTypeMember>.GetEnumerator()
        {
            return _FlatTypeMembers.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((System.Collections.IEnumerable)_FlatTypeMembers).GetEnumerator();
        }

        internal class FlatType
        {
            FlatTypeMembers RootFlatType { get; }

            internal FlatType ParentFlatType { get; }

            internal int FLatTypeLevel => RootFlatType._FlatTypesStack.IndexOf(this);
            internal TypeDefinition Type { get; }

            internal IEnumerable<TypeReference> GenericArguments { get; }

            internal IEnumerable<TypeReference> ParentGenericArguments => ParentFlatType != null ? ParentFlatType.GenericArguments : null;
            internal NewInheritedType NewInheritedType { get; }

            internal IEnumerable<FlatTypeMember> FlatTypeMembers => RootFlatType._FlatTypeMembers.Where(t => t.ParentFlatType == this);
            internal IEnumerable<FlatTypeMember> AllFlatTypeMembers
            {
                get
                {
                    return RootFlatType._FlatTypesStack.SelectMany(t => t.FlatTypeMembers);
                }
            }

            internal FlatType(FlatTypeMembers flatTypeMembers, FlatType parentType, TypeDefinition type, IEnumerable<TypeReference> genericArguments, NewInheritedType newInheritedType)
            {
                ParentFlatType = parentType;
                Type = type;
                GenericArguments = genericArguments;
                NewInheritedType = newInheritedType;
                RootFlatType = flatTypeMembers;
            }

            internal void AddMember(FlatTypeMember flatTypeMember)
            {
                RootFlatType._FlatTypeMembers.Add(flatTypeMember);
            }

            internal void ReplaceMember(FlatTypeMember oldFlatTypeMember, FlatTypeMember newFlatTypeMember)
            {
                RootFlatType._FlatTypeMembers.Remove(oldFlatTypeMember);
                AddMember(newFlatTypeMember);
            }
        }
    }
}