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
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using AspectDN.Common;
using System.IO;

namespace AspectDN.Aspect.Compilation.Foundation
{
    internal abstract class Workspace
    {
        protected List<MetadataReference> _ReferencedAssemblies;
        protected List<MetadataReference> ReferencedAssemblies { get => _ReferencedAssemblies; }

        internal Workspace()
        {
            _ReferencedAssemblies = new List<MetadataReference>();
        }

        internal abstract void AddReferencedAssembly(params string[] referencedAssemblyFilenames);
    }
}