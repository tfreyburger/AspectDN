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
using TokenizerDN.Common.SourceAnalysis;

namespace AspectDN.Aspect.Compilation.CS
{
    internal static class AspectNodeFactory
    {
        internal static CompilationUnitAspect CompilationUnit(CSAspectTree tree, ISynToken token)
        {
            return new CompilationUnitAspect(tree, token);
        }
    }
}