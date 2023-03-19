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
using System.Collections.Generic;
using Mono.Cecil;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspectDN.Common
{
    public abstract class AssemblyFile
    {
        protected AssemblyDefinition _AssemblyDefinition;
        internal IEnumerable<string> AssemblyNameReferences;
        internal string FullFileName { get; }

        internal int LevelNo { get; set; }

        internal string AssemblyName { get; }

        internal List<AssemblyFile> Children { get; }

        internal ReaderParameters ReaderParameters { get; }

        internal AssemblyFile(string fullFileName, string assemblyName, IEnumerable<string> assemblyNameReferences, ReaderParameters readerParameters = null)
        {
            AssemblyNameReferences = assemblyNameReferences;
            FullFileName = fullFileName;
            AssemblyName = assemblyName;
            Children = new List<AssemblyFile>();
            ReaderParameters = readerParameters;
        }

        internal abstract AssemblyDefinition GetAssemblyDefinition();
    }

    internal class AssemblyDefinitionFile : AssemblyFile
    {
        internal AssemblyDefinitionFile(string fullFileName, string assemblyName, IEnumerable<string> assemblyNameReferences, ReaderParameters readerParameters) : base(fullFileName, assemblyName, assemblyNameReferences, readerParameters)
        {
        }

        internal override AssemblyDefinition GetAssemblyDefinition()
        {
            if (_AssemblyDefinition == null)
                _AssemblyDefinition = CecilHelper.GetAssembly(FullFileName, false, ReaderParameters);
            return _AssemblyDefinition;
        }
    }

    internal class AssemblySourceFile : AssemblyFile
    {
        internal AssemblySourceFile(string fullFileName, AssemblyDefinition assemblyDefinition) : base(fullFileName, assemblyDefinition.FullName, assemblyDefinition.MainModule.AssemblyReferences.Select(t => t.FullName), null)
        {
            _AssemblyDefinition = assemblyDefinition;
        }

        internal override AssemblyDefinition GetAssemblyDefinition() => _AssemblyDefinition;
    }
}