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
using AspectDN.Aspect.Compilation.Foundation;
using Foundation.Common.Error;

namespace AspectDN
{
    internal class SingleAspectProjectConfiguration
    {
        string _Name;
        string _Language;
        string _ProjectDirectoryPath;
        string _LogPath;
        string _SourceTargetPath;
        string _OutputTargetPath;
        List<string> _FileNameReferences;
        List<string> _AspectSourceFileNames;
        IEnumerable<string> _ExcludedSourceTargetFiles;
        IEnumerable<string> _ExcludedSourceTargetDirectories;
        internal string Name { get => _Name; }

        internal string Language { get => _Language; }

        internal string ProjectDirectoryPath { get => _ProjectDirectoryPath; }

        internal string SourceTargetPath { get => _SourceTargetPath; }

        internal string OutputTargetPath { get => _OutputTargetPath; }

        internal string LogPath { get => _LogPath; }

        internal string LogFilename { get => $"{LogPath}{Name}.AspectDNLog"; }

        internal List<string> FileNameReferences { get => _FileNameReferences; }

        internal List<string> AspectSourceFileNames { get => AspectSourceFileNames; }

        internal SingleAspectProjectConfiguration()
        {
            _AspectSourceFileNames = new List<string>();
            _FileNameReferences = new List<string>();
        }

        internal SingleAspectProjectConfiguration Setup(XDocument xDocument)
        {
            _Name = xDocument.Root.Descendants("Project").First().Attributes("name").First().Value;
            _Language = xDocument.Root.Descendants("Project").First().Attributes("language").First().Value;
            _ProjectDirectoryPath = xDocument.Root.Descendants("Project").First().Attributes("projectDirectoryPath").First().Value;
            _LogPath = xDocument.Root.Descendants("Project").First().Attributes("logPath").First().Value;
            if (_LogPath.IndexOf(@"..\") == 0)
            {
                _LogPath = _LogPath.Replace("..", _ProjectDirectoryPath);
            }

            _SourceTargetPath = xDocument.Root.Descendants("Project").First().Attributes("sourceTargetPath").First().Value;
            if (xDocument.Root.Descendants("SourceTargetExclusion").SelectMany(t => t.Descendants("File")).Any())
            {
                var excludedSourceFiles = xDocument.Root.Descendants("SourceTargetExclusion").SelectMany(t => t.Descendants("File")).Select(t => t.Attributes("filename").First()).Select(t => t.Value).ToArray();
                for (int i = 0; i < excludedSourceFiles.Length; i++)
                    excludedSourceFiles[i] = excludedSourceFiles[i].Replace("..", _SourceTargetPath);
                _ExcludedSourceTargetFiles = excludedSourceFiles;
            }

            _OutputTargetPath = xDocument.Root.Descendants("Project").First().Attributes("outputTargetPath").First().Value;
            if (_OutputTargetPath.IndexOf(@"..\") == 0)
            {
                _OutputTargetPath = _OutputTargetPath.Replace("..", _ProjectDirectoryPath);
            }

            var filenameReferences = xDocument.Root.Descendants("FileReference").Select(t => t.Attributes("filename").First().Value).ToArray();
            for (int i = 0; i < filenameReferences.Length; i++)
                filenameReferences[i] = filenameReferences[i].Replace("..", _ProjectDirectoryPath);
            _FileNameReferences.AddRange(filenameReferences);
            var aspectSourceFiles = xDocument.Root.Descendants("AspectSourceFile").Select(t => t.Attributes("filename").First().Value);
            _AspectSourceFileNames.AddRange(aspectSourceFiles);
            return this;
        }

        internal void CreateAssembly()
        {
            if (File.Exists(LogFilename))
                File.Delete(LogFilename);
            var logWriter = new LogWriter(Name, LogFilename);
            var fileReferences = new List<string>(_FileNameReferences);
            fileReferences.Add(typeof(object).Assembly.Location);
            fileReferences.Add(typeof(Func<>).Assembly.Location);
            fileReferences.Add(typeof(IQueryable).Assembly.Location);
            AspectCompiler compiler = null;
            try
            {
                compiler = CompilerAspectRepository.GetCompilerAspect(Language, Name, LogFilename);
            }
            catch (Exception ex)
            {
                logWriter.LogInfo(ex.ToString());
                logWriter.ShutDown();
                System.Windows.Forms.MessageBox.Show(AspectDNErrorFactory.GetError("WeavingError").Description);
            }

            compiler.AddReferencedAssembly(fileReferences.ToArray());
            var filenames = new List<string>(_AspectSourceFileNames.Count());
            foreach (var filename in _AspectSourceFileNames)
                filenames.Add(Path.Combine(ProjectDirectoryPath, filename));
            compiler.AddSourceFilenames(filenames.ToArray());
            var aspectFile = compiler.GetAspectBytes(Name);
            if (aspectFile == null || aspectFile.Length == 0)
                throw AspectDNErrorFactory.GetException("CompilationError");
            var joinpointsContainer = _GetJoinpointsContainer();
            var aspectContainer = _GetAspectsContainer(aspectFile);
            var weaver = new Weaver(joinpointsContainer, aspectContainer, null, OutputTargetPath);
            weaver.Weave();
            if (!Directory.Exists(_OutputTargetPath))
                Directory.CreateDirectory(_OutputTargetPath);
            foreach (var fileReference in _FileNameReferences)
            {
                if (File.Exists(fileReference) && !File.Exists(Path.Combine(_OutputTargetPath, Path.GetFileName(fileReference))))
                    File.Copy(fileReference, Path.Combine(_OutputTargetPath, Path.GetFileName(fileReference)));
            }

            if (weaver.Errors.Any())
            {
                foreach (var error in weaver.Errors)
                    logWriter.LogInfo(error.ToString());
                logWriter.ShutDown();
                System.Windows.Forms.MessageBox.Show(AspectDNErrorFactory.GetError("WeavingError").Description);
            }
            else
            {
                logWriter.ShutDown();
                if (File.Exists(LogFilename))
                    File.Delete(LogFilename);
                System.Windows.Forms.MessageBox.Show(AspectDNErrorFactory.GetError("WeavingOk").Description);
            }
        }

        IJoinpointsContainer _GetJoinpointsContainer()
        {
            var sourceFileOptions = new SourceFileOptions(SourceTargetPath, _ExcludedSourceTargetFiles, _ExcludedSourceTargetDirectories, new string[]{"*.dll", "*.exe"});
            var assemblyFiles = CecilHelper.GetAssemblies(sourceFileOptions);
            return JoinpointsContainerFactory.Get(@"..\AspectDN.Aspect.Joinpoints.dll", assemblyFiles);
        }

        IAspectsContainer _GetAspectsContainer(byte[] aspectFile)
        {
            var searchAspectRepositoryDirectoryNames = _FileNameReferences.Select(t => Path.GetDirectoryName(t)).Distinct().ToArray();
            return AspectsContainerFactory.Get(new List<byte[]>(new byte[][]{aspectFile}), searchAspectRepositoryDirectoryNames);
        }
    }
}