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
    internal class GenericResolutionContext
    {
        internal MethodReference SourceMethodReference { get; }

        internal MethodReference TargetMethodReference { get; }

        internal GenericResolutionContext(MethodReference sourceMethodReference, MethodReference targetMethodReference)
        {
            SourceMethodReference = sourceMethodReference;
            TargetMethodReference = targetMethodReference;
        }
    }
}