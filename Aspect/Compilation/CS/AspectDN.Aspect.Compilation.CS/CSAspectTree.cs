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
using AspectDN.Aspect.Compilation.Foundation;
using Microsoft.CodeAnalysis;
using TokenizerDN.Common.SourceAnalysis;

namespace AspectDN.Aspect.Compilation.CS
{
    internal class CSAspectTree : AspectTree
    {
        internal static CSAspectTree Create(CSWorkspace workspace, string syntaxName = null, string source = null, string sourceName = null)
        {
            return new CSAspectTree(workspace, syntaxName, source, sourceName);
        }

        internal CSWorkspace CSWorkspace { get; }

        internal SyntaxTree SyntaxTree { get; set; }

        internal CSAspectTree(CSWorkspace workspace, string syntaxName = null, string source = null, string sourceName = null) : base(syntaxName, source, sourceName)
        {
            CSWorkspace = workspace;
        }

        internal CSAspectNode AspectRoot
        {
            get
            {
                return (CSAspectNode)_Root;
            }
        }

        internal override AspectNode CreateRoot(ISynToken token = null)
        {
            _Root = AspectNodeFactory.CompilationUnit(this, token);
            return (CompilationUnitAspect)_Root;
        }
    }
}