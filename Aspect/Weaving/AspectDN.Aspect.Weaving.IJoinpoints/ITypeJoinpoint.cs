// Author:
//
// T. Freyburger (t.freyburger@gmail.com)
//
// Copyright (c)  Thierry Freyburger
//
// Licensed under the GPLV3 license.
////
using Mono.Cecil;

namespace AspectDN.Aspect.Weaving.IJoinpoints
{
    public interface ITypeJoinpoint : IJoinpoint
    {
        string FullName { get; }

        TypeDefinition TypeDefinition { get; }
    }
}