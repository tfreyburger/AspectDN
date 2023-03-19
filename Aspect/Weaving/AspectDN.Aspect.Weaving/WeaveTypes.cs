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
    internal enum WeaveTypes
    {
        AddTypeInAssembly,
        AddTypeInType,
        AddFieldInType,
        AddPropertyInType,
        AddEventInType,
        AddMethodInType,
        AddConstructorInType,
        AddOperatorInType,
        CodeToBodyMethod,
        CodeToInstruction,
        ChangeStackValue,
        AddInterfaceToType,
        AddBaseTypeToType,
        AddEnumMembersInType,
        AddAttributesToType,
        AddAttributesToMember
    }
}