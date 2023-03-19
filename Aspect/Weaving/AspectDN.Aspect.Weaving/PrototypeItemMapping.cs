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
using AspectDN.Aspect.Weaving.IConcerns;
using Mono.Cecil;

namespace AspectDN.Aspect.Weaving
{
    internal class DefinedPrototypeItemMapping : PrototypeItemMapping
    {
        internal override IAspectDefinition ParentAspectDefinition => PrototypeItemMappingDefinition.ParentAspectDefinition;
        internal override PrototypeItemMappingSourceKinds SourceKind => PrototypeItemMappingDefinition.SourceKind;
        internal override object PrototypeItem => PrototypeItemMappingDefinition.PrototypeItem;
        internal override IMemberDefinition PrototypeItemMember => PrototypeItemMappingDefinition.PrototypeItemMember;
        internal override TypeReference PrototypeItemType => PrototypeItemMappingDefinition.PrototypeItemType;
        internal override string FullPrototypeItemName => PrototypeItemMappingDefinition.FullPrototypeItemName;
        internal override PrototypeItemMappingTargetKinds TargetKind => PrototypeItemMappingDefinition.TargetKind;
        internal override string TargetName => throw new NotImplementedException();
        internal IPrototypeItemMappingDefinition PrototypeItemMappingDefinition { get; }

        internal DefinedPrototypeItemMapping(IPrototypeItemMappingDefinition prototypeItemMappingDefinition, WeaveItem weaveItem, object target) : base(weaveItem, target)
        {
            PrototypeItemMappingDefinition = prototypeItemMappingDefinition;
        }
    }

    internal abstract class PrototypeItemMapping
    {
        internal abstract IAspectDefinition ParentAspectDefinition { get; }

        internal abstract PrototypeItemMappingSourceKinds SourceKind { get; }

        internal abstract object PrototypeItem { get; }

        internal abstract IMemberDefinition PrototypeItemMember { get; }

        internal abstract TypeReference PrototypeItemType { get; }

        internal abstract string FullPrototypeItemName { get; }

        internal abstract PrototypeItemMappingTargetKinds TargetKind { get; }

        internal abstract string TargetName { get; }

        internal object Target { get; }

        internal WeaveItem WeaveItem { get; }

        internal PrototypeItemMapping(WeaveItem weaveItem, object target)
        {
            Target = target;
            WeaveItem = weaveItem;
        }
    }

    internal class GeneratedPrototypeItemMapping : PrototypeItemMapping
    {
        PrototypeItemMappingSourceKinds _SourceKind;
        IMemberDefinition _PrototypeItem;
        PrototypeItemMappingTargetKinds _TargetKind;
        internal override IAspectDefinition ParentAspectDefinition => WeaveItem.Aspect;
        internal override PrototypeItemMappingSourceKinds SourceKind => _SourceKind;
        internal override object PrototypeItem => _PrototypeItem;
        internal override IMemberDefinition PrototypeItemMember => _PrototypeItem;
        internal override TypeReference PrototypeItemType => null;
        internal override string FullPrototypeItemName => _PrototypeItem.FullName;
        internal override PrototypeItemMappingTargetKinds TargetKind => _TargetKind;
        internal override string TargetName => ((IMemberDefinition)Target).Name;
        internal GeneratedPrototypeItemMapping(PrototypeItemMappingSourceKinds sourceKind, IMemberDefinition prototypeItem, PrototypeItemMappingTargetKinds targetKind, WeaveItem weaveItem, object target) : base(weaveItem, target)
        {
            _SourceKind = sourceKind;
            _PrototypeItem = prototypeItem;
            _TargetKind = targetKind;
        }
    }
}