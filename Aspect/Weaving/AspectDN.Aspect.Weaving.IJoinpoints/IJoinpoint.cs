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
    public interface IJoinpoint
    {
        AssemblyDefinition Assembly { get; }

        JoinpointKinds JoinpointKind { get; }

        bool HasChanged { get; }

        TypeDefinition DeclaringType { get; }

        IMemberDefinition Member { get; }
    }
}