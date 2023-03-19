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

namespace AspectDN.Aspect.Weaving
{
    internal class ResolvedTypeMember
    {
        internal TypeReference DeclaringType { get; }

        internal TypeReference MemberType { get; }

        internal string Name { get; }

        internal IEnumerable<TypeReference> Parameters { get; }

        internal ResolvedTypeMember(TypeReference declaringType, TypeReference memberType, string name, IEnumerable<TypeReference> parameters)
        {
            DeclaringType = declaringType;
            MemberType = memberType;
            Name = name;
            Parameters = parameters;
        }
    }

    internal class ResolvedNewTypeMember : ResolvedTypeMember
    {
        internal NewTypeMember NewTypeMember { get; }

        internal ResolvedNewTypeMember(NewTypeMember newTypeMember, TypeReference declaringType, TypeReference memberType, string name, IEnumerable<TypeReference> parameters) : base(declaringType, memberType, name, parameters)
        {
            NewTypeMember = newTypeMember;
        }
    }

    internal class ResolvedExistingTypeMember : ResolvedTypeMember
    {
        internal IMemberDefinition MemberDefinition { get; }

        internal ResolvedExistingTypeMember(IMemberDefinition memberDefinition, TypeReference declaringType, TypeReference memberType, string name, IEnumerable<TypeReference> parameters) : base(declaringType, memberType, name, parameters)
        {
            MemberDefinition = memberDefinition;
        }
    }
}