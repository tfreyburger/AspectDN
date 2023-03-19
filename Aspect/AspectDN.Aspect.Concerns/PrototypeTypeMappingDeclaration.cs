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

namespace AspectDN.Aspect.Concerns
{
    internal class PrototypeTypeMappingDeclaration
    {
        internal string FullPrototypeName { get; }

        internal string TargetTypename { get; }

        internal List<TypeDefinition> InternalReferencedPrototypeTypes { get; }

        internal PrototypeTypeMappingDeclaration(CustomAttribute prototypeTypeMappingDeclaration)
        {
            FullPrototypeName = ((TypeReference)prototypeTypeMappingDeclaration.ConstructorArguments[0].Value).FullName;
            TargetTypename = (string)prototypeTypeMappingDeclaration.ConstructorArguments[1].Value;
            InternalReferencedPrototypeTypes = new List<TypeDefinition>();
        }
    }
}