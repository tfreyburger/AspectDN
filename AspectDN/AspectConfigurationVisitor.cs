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
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace AspectDN
{
    internal class AspectConfigurationVisitor : IAspectConfigurationVisitor
    {
        void _Visit(AspectSolutionConfiguration aspectSolutionConfiguration, XDocument xAspectSolutionConfiguration)
        {
            aspectSolutionConfiguration.Name = _GetSolutionName(xAspectSolutionConfiguration);
            aspectSolutionConfiguration.SourceTargetPath = _GetOriginalTargetPath(xAspectSolutionConfiguration);
            aspectSolutionConfiguration.OutputTargetPath = _GetOutputTargetPath(xAspectSolutionConfiguration);
            aspectSolutionConfiguration.Add(_GetAspectFileNames(xAspectSolutionConfiguration));
            aspectSolutionConfiguration.Add(_GetAspectProjectDeclarations(xAspectSolutionConfiguration));
        }

#region #AspectSolutionConfiguration
        string _GetSolutionName(XDocument xAspectSolutionConfiguration)
        {
            return xAspectSolutionConfiguration.Root.Descendants("Solution").First().Attributes("name").First().Value;
        }

        string _GetLogPath(XDocument xAspectSolutionConfiguration)
        {
            return xAspectSolutionConfiguration.Root.Descendants("Solution").First().Attributes("logPath").First().Value;
        }

        string _GetOriginalTargetPath(XDocument xAspectSolutionConfiguration)
        {
            return xAspectSolutionConfiguration.Root.Descendants("Solution").First().Attributes("originalTargetPath").First().Value;
        }

        string _GetOutputTargetPath(XDocument xAspectSolutionConfiguration)
        {
            return xAspectSolutionConfiguration.Root.Descendants("Solution").First().Attributes("outputTargetPath").First().Value;
        }

        IEnumerable<string> _GetAspectFileNames(XDocument xAspectSolutionConfiguration)
        {
            return xAspectSolutionConfiguration.Root.Descendants("Solution").First().Descendants("AspectFileName").Select(t => t.Attributes("filename").First().Value);
        }

        IEnumerable<AspectProjectConfiguration> _GetAspectProjectDeclarations(XDocument xAspectSolutionConfiguration)
        {
            var aspectProjects = new List<AspectProjectConfiguration>();
            var xProjectConfigurations = xAspectSolutionConfiguration.Root.Descendants("Solution").First().Descendants("AspectProjectDeclaration").Select(t => t.Attributes("filename").First().Value).ToArray();
            foreach (var projectConfigurationFileName in xProjectConfigurations)
            {
                var sr = File.ReadAllText(projectConfigurationFileName);
                var xDocument = XDocument.Parse(sr);
            }

            return aspectProjects;
        }

#endregion
#region AspectProjectConfiguration
        AspectProjectConfiguration _GetAspectProjectConfiguration(XDocument xDocument)
        {
            var aspectProjectConfiguration = new AspectProjectConfiguration();
            aspectProjectConfiguration.Name = _GetProjectName(xDocument);
            aspectProjectConfiguration.Language = _GetLanguage(xDocument);
            aspectProjectConfiguration.DirectoryPath = _GetDirectoryPath(xDocument);
            aspectProjectConfiguration.CompilDirectoryPath = _GetCompilDirectoryPath(xDocument);
            return aspectProjectConfiguration;
        }

        string _GetProjectName(XDocument xDocument)
        {
            return xDocument.Root.Descendants("Project").First().Attributes("name").First().Value;
        }

        string _GetLanguage(XDocument xDocument)
        {
            return xDocument.Root.Descendants("Project").First().Attributes("language").First().Value;
        }

        string _GetDirectoryPath(XDocument xDocument)
        {
            return xDocument.Root.Descendants("Project").First().Attributes("directoryPath").First().Value;
        }

        string _GetCompilDirectoryPath(XDocument xDocument)
        {
            return xDocument.Root.Descendants("Project").First().Attributes("compilDirectoryPath").First().Value;
        }

        IEnumerable<string> _GetAspectReferenceFileNames(XDocument xDocument)
        {
            return xDocument.Root.Descendants("Aspect").First().Descendants("AspectFileReference").Select(t => t.Attributes("filename").First().Value);
        }

        IEnumerable<string> _GetAssemblyReferenceFileNames(XDocument xDocument)
        {
            return xDocument.Root.Descendants("Aspect").First().Descendants("AssemblyReference").Select(t => t.Attributes("filename").First().Value);
        }

        IEnumerable<string> _GetSourceAspectFileNames(XDocument xDocument)
        {
            return xDocument.Root.Descendants("Aspect").First().Descendants("AspectSourceFile").Select(t => t.Attributes("filename").First().Value);
        }

#endregion
#region IApsectConfigurationVisitor
        void IAspectConfigurationVisitor.Visit(AspectSolutionConfiguration aspectSolutionConfiguration, XDocument xDocument) => _Visit(aspectSolutionConfiguration, xDocument);
#endregion
    }
}