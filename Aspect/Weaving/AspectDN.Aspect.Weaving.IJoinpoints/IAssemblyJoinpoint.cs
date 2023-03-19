// Author:
//
// T. Freyburger (t.freyburger@gmail.com)
//
// Copyright (c)  Thierry Freyburger
//
// Licensed under the GPLV3 license.
////
using Mono.Cecil;
using System.Collections.Generic;

namespace AspectDN.Aspect.Weaving.IJoinpoints
{
    public interface IAssemblyJoinpoint : IJoinpoint
    {
        ModuleDefinition ModuleDefinition { get; }

        void SetAsChanged();
    }
}