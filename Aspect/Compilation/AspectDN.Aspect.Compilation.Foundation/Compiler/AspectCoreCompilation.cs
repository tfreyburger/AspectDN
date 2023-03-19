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
using TokenizerDN.Common.SourceAnalysis;
using AspectDN.Common;
using TokenizerDN.Common;

namespace AspectDN.Aspect.Compilation.Foundation
{
    internal abstract class AspectCoreCompilation
    {
        protected List<AspectTree> _Trees;
        protected List<ICompilerError> _Errors;
        internal LogWriter LogWriter { get; }

        internal List<ICompilerError> Errors
        {
            get
            {
                return _Errors;
            }
        }

        internal AspectCoreCompilation(string loggerName, string logFilename)
        {
            _Trees = new List<AspectTree>();
            LogWriter = new LogWriter(loggerName, logFilename);
        }

        internal virtual void Compile()
        {
            _Errors = new List<ICompilerError>();
            if (_Trees.Count(t => t.SourceChanged) != 0)
            {
                foreach (var tree in _Trees.Where(t => t.SourceChanged))
                    tree.Parse();
                foreach (var tree in _Trees.Where(t => t.Errors.Count() != 0))
                    _Errors.AddRange(tree.Errors);
                _Compile();
            }
        }

        internal abstract void AddSources(string[] sources, string syntaxName);
        internal abstract void AddSourceFilenames(string[] filenames, string syntaxName = null);
        internal void AddTrees(params AspectTree[] trees)
        {
            _Trees.AddRange(trees);
        }

        internal IEnumerable<ICompilerError> GetErrors()
        {
            return _Errors;
        }

        protected abstract void _Compile();
    }
}