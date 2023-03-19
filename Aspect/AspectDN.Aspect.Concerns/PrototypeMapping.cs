// Author:
//
// T. Freyburger (t.freyburger@gmail.com)
//
// Copyright (c)  Thierry Freyburger
//
// Licensed under the GPLV3 license.
////
using System;
using AspectDN.Aspect.Weaving.IConcerns;
using Mono.Cecil;
using System.Collections.Generic;

namespace AspectDN.Aspect.Concerns
{
    internal class PrototypeTypeMappingDefinition : IPrototypeTypeMappingDefinition
    {
        PrototypeTypeMappingDeclaration PrototypeTypeMappingDeclaration { get; }

        internal TypeDefinition PrototypeType { get; }

        internal string TargetTypename => PrototypeTypeMappingDeclaration.TargetTypename;
        internal PrototypeTypeKinds PrototypeTypeKind { get; }

        internal PrototypeTypeMappingDefinition(TypeDefinition virtualType, PrototypeTypeMappingDeclaration prototypeTypeMappingDeclaration, PrototypeTypeKinds prototypeTypeKind = PrototypeTypeKinds.AspectDN)
        {
            PrototypeType = virtualType;
            PrototypeTypeMappingDeclaration = prototypeTypeMappingDeclaration;
            PrototypeTypeKind = prototypeTypeKind;
        }

#region IPrototypeMapTypeMember
        TypeDefinition IPrototypeTypeMappingDefinition.PrototypeType => PrototypeType;
        string IPrototypeTypeMappingDefinition.TargetTypename => TargetTypename;
        IEnumerable<TypeDefinition> IPrototypeTypeMappingDefinition.InternalReferencedPrototypeTypes => PrototypeTypeMappingDeclaration.InternalReferencedPrototypeTypes;
#endregion
    }

    internal class PrototypeItemMappingDefinition : IPrototypeItemMappingDefinition
    {
        internal AspectDefinition ParentAspectDefinition { get; set; }

        internal PrototypeItemMappingSourceKinds SourceKind { get; }

        internal object PrototypeItem { get; }

        internal PrototypeItemMappingTargetKinds TargetKind { get; }

        internal string TargetName { get; }

        internal IMemberDefinition Target { get; set; }

        internal string FullPrototypeItemName
        {
            get
            {
                switch (PrototypeItem)
                {
                    case string stringValue:
                        return stringValue;
                    case IMemberDefinition memberDefinition:
                        return memberDefinition.FullName;
                    case TypeReference typeReference:
                        return typeReference.FullName;
                    default:
                        return "";
                        PrototypeItem.ToString();
                }
            }
        }

        internal PrototypeItemMappingDefinition(PrototypeItemMappingSourceKinds sourceType, object prototypeItem, PrototypeItemMappingTargetKinds targetType, string targetName)
        {
            SourceKind = sourceType;
            PrototypeItem = prototypeItem;
            TargetName = targetName;
            TargetKind = targetType;
        }

#region IPrototypeMappingItemDefinition
        PrototypeItemMappingSourceKinds IPrototypeItemMappingDefinition.SourceKind => SourceKind;
        object IPrototypeItemMappingDefinition.PrototypeItem => PrototypeItem;
        string IPrototypeItemMappingDefinition.TargetName => TargetName;
        PrototypeItemMappingTargetKinds IPrototypeItemMappingDefinition.TargetKind => TargetKind;
        IAspectDefinition IPrototypeItemMappingDefinition.ParentAspectDefinition => ParentAspectDefinition;
        IMemberDefinition IPrototypeItemMappingDefinition.PrototypeItemMember => PrototypeItem is IMemberDefinition ? (IMemberDefinition)PrototypeItem : null;
        TypeReference IPrototypeItemMappingDefinition.PrototypeItemType => PrototypeItem is TypeReference ? (TypeReference)PrototypeItem : null;
        string IPrototypeItemMappingDefinition.FullPrototypeItemName => FullPrototypeItemName;
#endregion
    }

    internal enum PrototypeSourceTypes : uint
    {
        Member = 1,
        Indexer = 2,
        Event = 4,
        Delegate = 16,
        Method = 32,
        Constructor = 64,
        Type = 128
    }

    internal enum PrototypeTargetTypes : uint
    {
        None = 0,
        Field = 1,
        Property = 2,
        Indexer = 4,
        Event = 8,
        Delegate = 16,
        Method = 32,
        MethodParameter = 64,
        Variable = 128,
        Type = 256
    }

    internal enum PrototypeTypeKinds
    {
        AspectDN,
        CompilerGeneratedType
    }
}