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
    internal class FlatTypeMember
    {
        internal FlatTypeMembers.FlatType ParentFlatType { get; }

        internal IMemberDefinition MemberDefinition { get; }

        internal TypeReference MemberType { get; }

        internal TypeReference ResolvedMemberType => NewTypeMember == null ? MemberType : NewTypeMember.Resolve(MemberType);
        internal IEnumerable<TypeReference> ResolvedParameterTypes => NewTypeMember == null ? ParameterTypes : NewTypeMember.Resolve(ParameterTypes);
        internal int GenericParametersCount => MemberDefinition is MethodDefinition ? ((MethodDefinition)MemberDefinition).GenericParameters.Count : -1;
        internal IEnumerable<TypeReference> ParameterTypes { get; }

        internal NewTypeMember NewTypeMember { get; }

        internal bool IsWeaveItemMemberOrigin => ParentFlatType.NewInheritedType != null || NewTypeMember != null;
        internal WeaveItemMember WeaveItemMemberOrigin
        {
            get
            {
                if (!IsWeaveItemMemberOrigin)
                    return null;
                return (ParentFlatType.NewInheritedType != null) ? (WeaveItemMember)ParentFlatType.NewInheritedType : (WeaveItemMember)NewTypeMember;
            }
        }

        internal bool IsNew
        {
            get
            {
                if (ParentFlatType.NewInheritedType != null)
                    return false;
                if (NewTypeMember != null)
                    return (NewTypeMember.MemberModifiers & IConcerns.AspectMemberModifiers.@new) == IConcerns.AspectMemberModifiers.@new;
                return false;
            }
        }

        internal bool IsOverriden
        {
            get
            {
                if (ParentFlatType.NewInheritedType != null)
                    return false;
                if (NewTypeMember != null)
                    return (NewTypeMember.MemberModifiers & IConcerns.AspectMemberModifiers.@override) == IConcerns.AspectMemberModifiers.@override;
                if (MemberDefinition is MethodDefinition)
                    return ((MethodDefinition)MemberDefinition).IsVirtual;
                return false;
            }
        }

        internal FlatTypeMember(FlatTypeMembers.FlatType parentFlatType, IMemberDefinition memberDefinition, TypeReference memberType, IEnumerable<TypeReference> parameterTypes, NewTypeMember newTypeMemeber)
        {
            MemberDefinition = memberDefinition;
            ParameterTypes = parameterTypes ?? Array.Empty<TypeReference>();
            MemberType = memberType;
            ParentFlatType = parentFlatType;
            NewTypeMember = newTypeMemeber;
        }

        public override string ToString() => MemberDefinition.FullName;
    }
}