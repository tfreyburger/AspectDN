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
using AspectDN.Aspect.Weaving.IConcerns;

namespace AspectDN.Aspect.Weaving
{
    internal class PrototypeTypeMapping
    {
        IPrototypeTypeMappingDefinition _PrototypeTypeMappingDefinition;
        internal TypeDefinition PrototypeType => _PrototypeTypeMappingDefinition.PrototypeType;
        internal TypeDefinition TargetType { get; set; }

        internal IEnumerable<TypeDefinition> InternalReferencedPrototypeTypes => _PrototypeTypeMappingDefinition.InternalReferencedPrototypeTypes;
        internal bool OnError { get; set; }

        public PrototypeTypeMapping(IPrototypeTypeMappingDefinition prototypeTypeMappingDefinition)
        {
            _PrototypeTypeMappingDefinition = prototypeTypeMappingDefinition;
        }
    }
}