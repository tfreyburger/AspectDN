// Author:
//
// T. Freyburger (t.freyburger@gmail.com)
//
// Copyright (c)  Thierry Freyburger
//
// Licensed under the GPLV3 license.
////
using System;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspectDN
{
    internal interface IAspectConfigurationVisitor
    {
        void Visit(AspectSolutionConfiguration aspectSolution, XDocument xDocument);
    }
}