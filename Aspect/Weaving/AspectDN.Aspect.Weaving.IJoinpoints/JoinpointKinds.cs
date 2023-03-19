// Author:
//
// T. Freyburger (t.freyburger@gmail.com)
//
// Copyright (c)  Thierry Freyburger
//
// Licensed under the GPLV3 license.
////
using System;

namespace AspectDN.Aspect.Weaving.IJoinpoints
{
    [Flags]
    public enum JoinpointKinds : ulong
    {
        none = 0,
        assemblies = 1,
        classes = 2,
        interfaces = 4,
        enums = 16,
        structs = 32,
        fields = 64,
        properties = 128,
        methods = 256,
        exceptions = 512,
        constructors = 1024,
        events = 2048,
        type_delegates = 4096,
        get = 8192,
        set = 16384,
        body = 32768,
        call = 65536,
        @throw = 131072,
        add = 262144,
        remove = 524288,
        declaration = 1048576,
        field_delegates = 2097152
    }
}