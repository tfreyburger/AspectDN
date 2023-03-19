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
using System.IO;
using AspectDN.Common;
using System.Threading.Tasks;

namespace AspectDN
{
    internal class AspectSolutionConfiguration
    {
        string _Name;
        string _SourceTargetPath;
        string _OutputTargetPath;
        string _LogPath;
        List<string> _AspectFileNames;
        List<AspectProjectConfiguration> _AspectProjectConfigurations;
        internal string Name { get => _Name; set => _Name = value; }

        internal string SourceTargetPath { get => _SourceTargetPath; set => _SourceTargetPath = value; }

        internal string OutputTargetPath { get => _OutputTargetPath; set => _OutputTargetPath = value; }

        internal string LogPath { get => _LogPath; set => _LogPath = value; }

        public IEnumerable<string> AspectFileNames { get => _AspectFileNames; }

        internal IEnumerable<AspectProjectConfiguration> AspectProjectConfigurations { get => _AspectProjectConfigurations; }

        internal AspectSolutionConfiguration Accept(IAspectConfigurationVisitor visitor, XDocument xDocument)
        {
            visitor.Visit(this, xDocument);
            return this;
        }

        internal void Add(AspectProjectConfiguration projectConfiguration) => _AspectProjectConfigurations.Add(projectConfiguration);
        internal void Add(IEnumerable<AspectProjectConfiguration> projectConfigurations) => _AspectProjectConfigurations.AddRange(projectConfigurations);
        internal void Add(IEnumerable<string> aspectFilenames)
        {
            foreach (var aspectFilename in aspectFilenames)
                Add(aspectFilename);
        }

        internal void Add(string aspectFilename)
        {
            if (_AspectFileNames.Contains(aspectFilename))
                return;
            if (!File.Exists(aspectFilename))
                throw AspectDNErrorFactory.GetException("NotExistingAspectFile", aspectFilename);
            _AspectFileNames.Add(aspectFilename);
        }

        internal string GetAspectFileDirectory()
        {
            throw new NotImplementedException();
        }
    }
}