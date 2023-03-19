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
using AspectDN.Aspect.Weaving.IConcerns;
using AspectDN.Aspect.Weaving.IJoinpoints;
using AspectDN.Common;
using AspectDN.Aspect.Weaving;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AspectDN
{
    internal class AspectProjectConfiguration
    {
        string _Name;
        string _Language;
        List<string> _AspectFileNameReferences;
        List<string> _AspectSourceFileNames;
        List<string> _AssemblyReferences;
        string _DirectoryPath;
        string _CompilDirectoryPath;
        internal string Name { get => _Name; set => _Name = value; }

        internal string Language { get => _Language; set => _Language = value; }

        internal List<string> AspectFileNameReferences { get => _AspectFileNameReferences; }

        internal List<string> AspectSourceFileNames { get => AspectSourceFileNames; }

        internal List<string> AssemblyReferences { get => _AssemblyReferences; }

        internal string DirectoryPath { get => _DirectoryPath; set => _DirectoryPath = value; }

        internal string CompilDirectoryPath { get => _CompilDirectoryPath; set => _CompilDirectoryPath = value; }

        internal AspectProjectConfiguration()
        {
            _AspectFileNameReferences = new List<string>();
            _AspectSourceFileNames = new List<string>();
            _AssemblyReferences = new List<string>();
        }

        internal void AddAspectFileNameReference(string aspectFileNameReference)
        {
            if (_AspectFileNameReferences.Contains(aspectFileNameReference))
                return;
            if (!File.Exists(aspectFileNameReference))
                throw AspectDNErrorFactory.GetException("NotExistingAspectFile", aspectFileNameReference);
            _AspectFileNameReferences.Add(aspectFileNameReference);
        }

        internal void AddAspectSourceFileName(string aspectSourceFileName)
        {
            if (_AspectSourceFileNames.Contains(aspectSourceFileName))
                return;
            if (!File.Exists(aspectSourceFileName))
                throw AspectDNErrorFactory.GetException("NotExistingAspectSourceFile", aspectSourceFileName);
            _AspectSourceFileNames.Add(aspectSourceFileName);
        }

        internal void AddAssemblyReference(string assemblyReference)
        {
            if (_AssemblyReferences.Contains(assemblyReference))
                return;
            if (!File.Exists(assemblyReference))
                throw AspectDNErrorFactory.GetException("NotExistingAssemblyReference", assemblyReference);
            _AssemblyReferences.Add(assemblyReference);
        }
    }
}