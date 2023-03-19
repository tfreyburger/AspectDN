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

namespace AspectDN.Aspect.Weaving
{
    internal class ResolvedPrototypeItem
    {
        internal object TargetItem { get; }

        internal DeclaringTypedType DeclaringTypedType { get; }

        internal ResolvedPrototypeItem(object targetItem, DeclaringTypedType declaringTypedType)
        {
            TargetItem = targetItem;
            DeclaringTypedType = declaringTypedType;
        }
    }
}