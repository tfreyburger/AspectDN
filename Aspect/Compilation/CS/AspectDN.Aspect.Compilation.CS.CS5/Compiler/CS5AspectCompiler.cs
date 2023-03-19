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
using System.Reflection;
using AspectDN.Common;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using TokenizerDN.Common;
using Foundation.Common.Error;
using TokenizerDN.Common.SourceAnalysis;

namespace AspectDN.Aspect.Compilation.CS.CS5
{
    internal class CS5AspectCompiler : AspectCompiler
    {
        public IEnumerable<ICompilerError> Errors => _AspectCoreCompilation.GetErrors() ?? new ICompilerError[]{};
        internal CS5AspectCompiler(string language, string loggerName, string logFilename) : base(language)
        {
            _Workspace = null;
            try
            {
                _Workspace = new CSWorkspace();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            _AspectCoreCompilation = new CS5AspectCoreCompilation((CSWorkspace)_Workspace, loggerName, logFilename);
            _AspectFileFactory = new CSAspectAssemblyFactory((AspectCoreCompilation)_AspectCoreCompilation);
            _Language = language;
        }

        internal string AssemblyName { get => ((CSWorkspace)_Workspace).AssemblyName; set => ((CSWorkspace)_Workspace).AssemblyName = value; }

        internal IEnumerable<ICompilerError> Compile(string assemblyName)
        {
            Compile();
            ((CSWorkspace)_Workspace).AssemblyName = assemblyName;
            ((CSAspectAssemblyFactory)_AspectFileFactory).SetDiagnostics(((CSWorkspace)_Workspace).GetDiagnostics());
            return _AspectCoreCompilation.GetErrors();
        }

        internal IEnumerable<ICompilerError> GetErrors(string dllName)
        {
            ((CSWorkspace)_Workspace).AssemblyName = dllName;
            ((CSAspectAssemblyFactory)_AspectFileFactory).CreateAssembly(dllName, new System.IO.MemoryStream(), null);
            return Errors;
        }

        internal new CS5AspectCompiler AddReferencedAssembly(params string[] referencedAssemblyFilenames)
        {
            ((CSWorkspace)_Workspace).AddReferencedAssembly(referencedAssemblyFilenames);
            return this;
        }

        internal string GetSource()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var tree in ((CSWorkspace)_Workspace).SyntaxTrees)
                sb.Append(tree.GetRoot().NormalizeWhitespace().ToFullString());
            return sb.ToString();
        }
    }
}