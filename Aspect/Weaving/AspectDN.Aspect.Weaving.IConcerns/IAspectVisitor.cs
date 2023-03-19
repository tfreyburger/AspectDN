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

namespace AspectDN.Aspect.Weaving.IConcerns
{
    public interface IAspectVisitor
    {
        IAspectsContainer Visit(IEnumerable<AssemblyDefinition> assemblyDefinitions);
    }
}