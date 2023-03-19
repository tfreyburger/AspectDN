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

namespace AspectDN.Aspect.Weaving.IConcerns
{
    public interface IPrototypeTypeMappingDefinition
    {
        TypeDefinition PrototypeType { get; }

        string TargetTypename { get; }

        IEnumerable<TypeDefinition> InternalReferencedPrototypeTypes { get; }
    }

    public interface IPrototypeItemMappingDefinition
    {
        IAspectDefinition ParentAspectDefinition { get; }

        PrototypeItemMappingSourceKinds SourceKind { get; }

        object PrototypeItem { get; }

        IMemberDefinition PrototypeItemMember { get; }

        TypeReference PrototypeItemType { get; }

        string FullPrototypeItemName { get; }

        PrototypeItemMappingTargetKinds TargetKind { get; }

        string TargetName { get; }
    }
}