// Author:
//
// T. Freyburger (t.freyburger@gmail.com)
//
// Copyright (c)  Thierry Freyburger
//
// Licensed under the GPLV3 license.
////
using System.IO;
using System.Collections.Generic;
using Mono.Cecil;
using AspectDN.Common;
using Foundation.Common.Error;

namespace AspectDN.Aspect.Weaving.IConcerns
{
    public interface IAspectsContainer
    {
        IEnumerable<IAspectDefinition> Aspects { get; }

        IEnumerable<IPrototypeTypeMappingDefinition> PrototypeTypeMappingDefinitions { get; }

        IEnumerable<IError> Errors { get; }

        ReaderParameters ReaderParameters { get; }
    }
}