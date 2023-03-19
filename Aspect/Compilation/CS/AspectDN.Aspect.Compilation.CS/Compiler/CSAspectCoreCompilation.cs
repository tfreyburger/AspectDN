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
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using AspectDN.Aspect.Compilation.Foundation;
using AspectDN.Aspect.Weaving.IConcerns;
using Mono.Cecil;
using AspectDN.Common;

namespace AspectDN.Aspect.Compilation.CS
{
    internal abstract class CSAspectCoreCompilation : AspectCoreCompilation
    {
        internal List<CSAspectTree> CSAspectTrees
        {
            get
            {
                return _Trees.Cast<CSAspectTree>().ToList();
            }
        }

        internal CSWorkspace CSWorkspace { get; }

        internal CSAspectCoreCompilation(CSWorkspace workspace, string loggerName, string logFilename) : base(loggerName, logFilename)
        {
            CSWorkspace = workspace;
            workspace.AddReferencedAssembly(typeof(object).Assembly.Location, typeof(IPointcutDefinition).Assembly.Location, typeof(IQueryable).Assembly.Location, typeof(AssemblyDefinition).Assembly.Location);
        }

        internal override void AddSourceFilenames(string[] filenames, string syntaxName = null)
        {
            CSAspectTree[] AspectTrees = new CSAspectTree[filenames.Length];
            for (int i = 0; i < AspectTrees.Length; i++)
            {
                if (!File.Exists(filenames[i]))
                    throw AspectDNErrorFactory.GetException("NotExistingAspectSourceFile", filenames[i]);
                using (StreamReader sr = new StreamReader(filenames[i]))
                {
                    AspectTrees[i] = new CSAspectTree(CSWorkspace, syntaxName ?? "", sr.ReadToEnd(), filenames[i]);
                }
            }

            AddTrees(AspectTrees);
        }

        internal override void AddSources(string[] sources, string syntaxName = null)
        {
            CSAspectTree[] AspectTrees = new CSAspectTree[sources.Length];
            for (int i = 0; i < AspectTrees.Length; i++)
                AspectTrees[i] = new CSAspectTree(CSWorkspace, syntaxName ?? "", sources[i], "");
            AddTrees(AspectTrees);
        }
    }
}