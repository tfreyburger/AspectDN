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
using System.Reflection;
using System.IO;
using TokenizerDN.Common.SourceAnalysis;
using Microsoft.CodeAnalysis;

namespace AspectDN.Aspect.Compilation.Foundation
{
    internal abstract class AspectFileFactory
    {
        protected AspectCoreCompilation _CoreCompilation;
        protected List<Assembly> _ReferencedAssemblies;
        internal AspectFileFactory(AspectCoreCompilation coreCompilation)
        {
            _CoreCompilation = coreCompilation;
            _ReferencedAssemblies = new List<Assembly>();
        }

        internal void AddReferencedAssembly(params string[] assemblyFilenames)
        {
            foreach (string usedAssemblyFilename in assemblyFilenames)
            {
                Assembly assembly = Assembly.LoadFile(usedAssemblyFilename);
                if (!_ReferencedAssemblies.Contains(assembly))
                    _ReferencedAssemblies.Add(assembly);
            }
        }

        internal bool CreateAssembly(string assemblyName, MemoryStream mStream, MemoryStream pdbStream)
        {
            var isOnError = false;
            _CoreCompilation.Compile();
            if (!_CoreCompilation.GetErrors().Any())
                _CreateAssembly(assemblyName, mStream, pdbStream);
            if (_CoreCompilation.GetErrors().Any())
            {
                foreach (var error in _CoreCompilation.GetErrors())
                    _CoreCompilation.LogWriter.LogInfo(error.ToString());
                isOnError = true;
            }

            return isOnError;
        }

        internal void CreateAssembly(string location, string assemblyName, string pdbFileName)
        {
            MemoryStream pdbStream = null;
            if (!string.IsNullOrEmpty(pdbFileName))
                pdbStream = new MemoryStream();
            using (MemoryStream mStream = new MemoryStream())
            {
                CreateAssembly(assemblyName, mStream, pdbStream);
                if (mStream.Length != 0)
                {
                    var path = Path.Combine(location, $"{assemblyName}.aspdn");
                    using (FileStream fStream = new FileStream(path, FileMode.Create))
                    {
                        mStream.Position = 0;
                        mStream.CopyTo(fStream);
                    }
                }
            }

            if (pdbStream != null)
            {
                if (pdbStream.Length != 0)
                {
                    var path = Path.Combine(location, pdbFileName);
                    using (FileStream fStream = new FileStream(path, FileMode.Create))
                    {
                        pdbStream.Position = 0;
                        pdbStream.CopyTo(fStream);
                    }

                    pdbStream.Dispose();
                }
            }
        }

        internal abstract void _CreateAssembly(string assemblyName, MemoryStream mStream, MemoryStream pdbStream);
    }
}