// Author:
//
// T. Freyburger (t.freyburger@gmail.com)
//
// Copyright (c)  Thierry Freyburger
//
// Licensed under the GPLV3 license.
////
using System;
using System.IO;
using AspectDN.Common;
using AspectDN.Aspect.Compilation.Foundation;
using System.Collections.Generic;
using System.Linq;
using AspectDN.Aspect.Weaving;
using AspectDN.Aspect.Weaving.IConcerns;
using AspectDN.Aspect.Weaving.IJoinpoints;
using System.Text;
using System.Threading.Tasks;

namespace AspectDN
{
    internal class AspectSolutionWeaver
    {
        AspectSolutionConfiguration _AspectSolutionConfiguration;
        internal AspectSolutionWeaver(AspectSolutionConfiguration aspectSolutionConfiguration)
        {
            _AspectSolutionConfiguration = aspectSolutionConfiguration;
        }

        internal void Weave()
        {
            _Weave();
        }

        IAspectsContainer _GetAspectsContainer()
        {
            throw new NotImplementedException();
        }

        void _Weave()
        {
        }
    }
}