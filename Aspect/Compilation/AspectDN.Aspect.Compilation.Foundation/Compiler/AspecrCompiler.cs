// Author:
//
// T. Freyburger (t.freyburger@gmail.com)
//
// Copyright (c)  Thierry Freyburger
//
// Licensed under the GPLV3 license.
////
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using AspectDN.Common;
using TokenizerDN.Common;
using TokenizerDN.Common.SourceAnalysis;

namespace AspectDN.Aspect.Compilation.Foundation
{
    internal abstract class AspectCompiler
    {
        protected AspectCoreCompilation _AspectCoreCompilation;
        protected AspectFileFactory _AspectFileFactory;
        protected string _Language;
        protected Workspace _Workspace;
        internal AspectCompiler(string language)
        {
            _Language = language;
        }

        internal AspectCompiler AddSources(string[] sources)
        {
            _AspectCoreCompilation.AddSources(sources, _Language);
            return this;
        }

        internal AspectCompiler AddSourceFilenames(string[] filenames)
        {
            _AspectCoreCompilation.AddSourceFilenames(filenames, _Language);
            return this;
        }

        internal virtual IEnumerable<ICompilerError> Compile()
        {
            _AspectCoreCompilation.Compile();
            return _AspectCoreCompilation.GetErrors();
        }

        internal byte[] GetAspectBytes(string aspectFilename)
        {
            byte[] bytes = null;
            using (MemoryStream mStream = new MemoryStream())
            {
                _AspectFileFactory.CreateAssembly(aspectFilename, mStream, null);
                bytes = new byte[mStream.Length];
                mStream.Position = 0;
                mStream.Read(bytes, 0, (int)mStream.Length);
            }

            return bytes;
        }

        internal virtual AspectCompiler AddReferencedAssembly(params string[] referencedAssemblyFilenames)
        {
            _Workspace.AddReferencedAssembly(referencedAssemblyFilenames);
            return this;
        }

        internal void CreateAspectFileAndSave(string location, string assemblyName)
        {
            _AspectFileFactory.CreateAssembly(location, assemblyName, null);
        }

        internal static AspectCompiler Create(string language, string loggerName, string logFileName)
        {
            return CompilerAspectRepository.GetCompilerAspect(language, loggerName, logFileName);
        }

        internal static AspectCompiler Create(string language)
        {
            return CompilerAspectRepository.GetCompilerAspect(language);
        }
    }
}