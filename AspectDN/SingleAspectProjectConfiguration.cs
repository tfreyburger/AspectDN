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
using System.Windows.Forms;
using System.Runtime.CompilerServices;
using System.Xml;
using Foundation.Common;

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
        List<string> _ExcludedSourceTargetFiles;
        List<string> _ExcludedSourceTargetDirectories;
        bool _Error;
        internal string Name { get => _Name; }

        internal string Language { get => _Language; }

        internal string ProjectDirectoryPath { get => _ProjectDirectoryPath; }

        internal string SourceTargetPath { get => _SourceTargetPath; }

        internal string OutputTargetPath { get => _OutputTargetPath; }

        internal string LogPath { get => _LogPath; }

        internal string LogFilename { get => $"{LogPath}{Name}.AspectDNLog"; }

        internal List<string> FileNameReferences { get => _FileNameReferences; }

        internal List<string> AspectSourceFileNames { get => AspectSourceFileNames; }

        internal bool Error { get => _Error; }

        internal SingleAspectProjectConfiguration()
        {
            _AspectSourceFileNames = new List<string>();
            _FileNameReferences = new List<string>();
            _ExcludedSourceTargetDirectories = new List<string>();
            _ExcludedSourceTargetFiles = new List<string>();
            _Error = false;
        }

        internal SingleAspectProjectConfiguration Setup(XDocument xDocument)
        {
            TaskEventLogger.Log(this, new TaskEvent()
            {Message = "Loading file configuration"});
            var xProject = xDocument.Root.Descendants("Project").FirstOrDefault();
            if (xProject == null)
            {
                _ShowError("BadProjectXmlElement", null);
            }
            else
            {
                _Name = _GetProjectName(xProject.Attributes("name").FirstOrDefault());
                _Language = _GetLanguage(xProject.Attributes("language").FirstOrDefault());
                _ProjectDirectoryPath = _GetProjectDirectoryPath(xProject.Attributes("projectDirectoryPath").FirstOrDefault());
                _LogPath = _GetLogPath(xProject.Attributes("logPath").FirstOrDefault());
                _OutputTargetPath = _GetOutputTargetPath(xProject.Attributes("outputTargetPath").FirstOrDefault());
                _SourceTargetPath = _GetSourceTargetPath(xProject.Attributes("sourceTargetPath").FirstOrDefault());
            }

            _ExcludedSourceTargetFiles.AddRange(_GetExcludedSourceTargetFiles(xDocument.Root.Descendants("SourceTargetExclusion")));
            _FileNameReferences.AddRange(_GetFilenameReferences(xDocument.Root.Descendants("FileReference")));
            _AspectSourceFileNames.AddRange(_GetAspectSourceFiles(xDocument.Root.Descendants("AspectSourceFile")));
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
            TaskEventLogger.Log(this, new TaskEvent()
            {Message = "Loading the aspect compiler"});
            try
            {
                compiler = CompilerAspectRepository.GetCompilerAspect(Language, Name, LogFilename);
            }
            catch (Exception ex)
            {
                logWriter.LogInfo(ex.ToString());
                logWriter.ShutDown();
                _ShowError("WeavingError");
                return;
            }

            compiler.AddReferencedAssembly(fileReferences.ToArray());
            var filenames = new List<string>(_AspectSourceFileNames.Count());
            foreach (var filename in _AspectSourceFileNames)
                filenames.Add(Path.Combine(ProjectDirectoryPath, filename));
            compiler.AddSourceFilenames(filenames.ToArray());
            TaskEventLogger.Log(this, new TaskEvent()
            {Message = "Check Aspect Syntax"});
            var aspectFile = compiler.GetAspectBytes(Name);
            if (aspectFile == null || aspectFile.Length == 0)
            {
                _ShowError("CompilationError");
                return;
            }

            var joinpointsContainer = _GetJoinpointsContainer();
            var aspectContainer = _GetAspectsContainer(aspectFile);
            var weaver = new Weaver(joinpointsContainer, aspectContainer, null, OutputTargetPath);
            TaskEventLogger.Log(this, new TaskEvent()
            {Message = "Start Weaving"});
            weaver.Weave();
            if (!weaver.Errors.Any())
            {
                if (!Directory.Exists(_OutputTargetPath))
                    Directory.CreateDirectory(_OutputTargetPath);
                foreach (var fileReference in _FileNameReferences)
                {
                    if (File.Exists(fileReference) && !File.Exists(Path.Combine(_OutputTargetPath, Path.GetFileName(fileReference))))
                        File.Copy(fileReference, Path.Combine(_OutputTargetPath, Path.GetFileName(fileReference)));
                }

                logWriter.ShutDown();
                if (File.Exists(LogFilename))
                    File.Delete(LogFilename);
                _ShowError("WeavingOk");
            }
            else
            {
                foreach (var error in weaver.Errors)
                    logWriter.LogInfo(error.ToString());
                logWriter.ShutDown();
                _ShowError("WeavingError");
            }
        }

        string _GetProjectName(XAttribute xAttribute)
        {
            string name = null;
            if (xAttribute != null)
                name = xAttribute.Value;
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show(ErrorFactory.GetError("NoProjectName", null).ToString());
            }

            return name;
        }

        string _GetLanguage(XAttribute xAttribute)
        {
            string language = null;
            if (xAttribute != null)
                language = xAttribute.Value;
            if (string.IsNullOrEmpty(language))
            {
                MessageBox.Show(ErrorFactory.GetError("NoLanguage", null).ToString());
            }

            return language;
        }

        string _GetProjectDirectoryPath(XAttribute xAttribute)
        {
            string projectDirectoryPath = null;
            if (xAttribute != null)
                projectDirectoryPath = xAttribute.Value;
            if (string.IsNullOrEmpty(projectDirectoryPath))
            {
                MessageBox.Show(ErrorFactory.GetError("NoProjectDirectoryPath").ToString());
                return projectDirectoryPath;
            }

            if (!Directory.Exists(projectDirectoryPath))
            {
                MessageBox.Show(ErrorFactory.GetError("ProjectDirectoryInvalid", projectDirectoryPath).ToString());
                return projectDirectoryPath;
            }

            return projectDirectoryPath;
        }

        string _GetLogPath(XAttribute xAttribute)
        {
            string logPath = null;
            if (xAttribute != null)
            {
                logPath = xAttribute.Value;
            }

            if (string.IsNullOrEmpty(logPath))
            {
                MessageBox.Show(ErrorFactory.GetError("NoLogPath", null).ToString());
                return logPath;
            }

            if (logPath.IndexOf(@"..\") == 0)
                logPath = logPath.Replace("..", _ProjectDirectoryPath);
            else
            {
                if (!Directory.Exists(_LogPath))
                    MessageBox.Show(ErrorFactory.GetError("LogPathInvalid", logPath).ToString());
            }

            return logPath;
        }

        string _GetSourceTargetPath(XAttribute xAttribute)
        {
            string sourceTargetPath = null;
            if (xAttribute != null)
            {
                sourceTargetPath = xAttribute.Value;
                if (sourceTargetPath.IndexOf(@"..\") == 0)
                {
                    sourceTargetPath = sourceTargetPath.Replace("..", _ProjectDirectoryPath);
                }
            }

            if (string.IsNullOrEmpty(sourceTargetPath))
            {
                MessageBox.Show(ErrorFactory.GetError("NoSourceTargetPath", null).ToString());
                return sourceTargetPath;
            }

            if (!Directory.Exists(sourceTargetPath))
            {
                MessageBox.Show(ErrorFactory.GetError("SourceTargetPathInvalid", sourceTargetPath).ToString());
            }

            return sourceTargetPath;
        }

        IEnumerable<string> _GetExcludedSourceTargetFiles(IEnumerable<XElement> xElements)
        {
            if (xElements == null || xElements.Select(t => t.Descendants("File")).Any())
                return new string[0];
            var excludedSourceTargetFiles = xElements.Select(t => t.Descendants("File")).Select(t => t.Attributes("filename").First()).Select(t => t.Value).ToList();
            for (int i = 0; i < excludedSourceTargetFiles.Count; i++)
            {
                if (string.IsNullOrEmpty(excludedSourceTargetFiles[i]))
                {
                    MessageBox.Show(ErrorFactory.GetError("NoSourceTargetExclusion", null).ToString());
                }

                excludedSourceTargetFiles[i] = excludedSourceTargetFiles[i].Replace("..", _SourceTargetPath);
                if (!Directory.Exists(excludedSourceTargetFiles[i]))
                {
                    MessageBox.Show(ErrorFactory.GetError("SourceTargetExclusionInvalid", excludedSourceTargetFiles[i]).ToString());
                }
            }

            return excludedSourceTargetFiles;
        }

        string _GetOutputTargetPath(XAttribute xAttribute)
        {
            string outputTargetPath = null;
            if (xAttribute != null)
            {
                outputTargetPath = xAttribute.Value;
                if (outputTargetPath.IndexOf(@"..\") == 0)
                {
                    outputTargetPath = outputTargetPath.Replace("..", _ProjectDirectoryPath);
                }
            }

            if (string.IsNullOrEmpty(outputTargetPath))
            {
                MessageBox.Show(ErrorFactory.GetError("NoOutputTargetPath", null).ToString());
                return outputTargetPath;
            }

            return outputTargetPath;
        }

        IEnumerable<string> _GetFilenameReferences(IEnumerable<XElement> xElements)
        {
            if (xElements == null || xElements.Select(t => t.Descendants("FileReference")).Any())
                return new string[0];
            var filenameReferences = xElements.Select(t => t.Descendants("FileReference")).Select(t => t.Attributes("filename").First()).Select(t => t.Value).ToArray();
            for (int i = 0; i < filenameReferences.Length; i++)
            {
                if (string.IsNullOrEmpty(filenameReferences[i]))
                {
                    MessageBox.Show(ErrorFactory.GetError("NoFileReference", null).ToString());
                }

                filenameReferences[i] = filenameReferences[i].Replace("..", _SourceTargetPath);
                if (!Directory.Exists(filenameReferences[i]))
                {
                    MessageBox.Show(ErrorFactory.GetError("FileReferenceInvalid", filenameReferences[i]).ToString());
                }
            }

            return filenameReferences;
        }

        IEnumerable<string> _GetAspectSourceFiles(IEnumerable<XElement> xElements)
        {
            if (xElements == null || xElements.Select(t => t.Descendants("AspectSourceFile")).Any())
                return new string[0];
            var aspectSourceFiles = xElements.Select(t => t.Descendants("AspectSourceFile")).Select(t => t.Attributes("filename").First()).Select(t => t.Value).ToList();
            for (int i = 0; i < aspectSourceFiles.Count; i++)
            {
                if (string.IsNullOrEmpty(aspectSourceFiles[i]))
                {
                    MessageBox.Show(ErrorFactory.GetError("NoAspectSourceFile", null).ToString());
                }

                aspectSourceFiles[i] = aspectSourceFiles[i].Replace("..", _SourceTargetPath);
                if (!Directory.Exists(aspectSourceFiles[i]))
                {
                    MessageBox.Show(ErrorFactory.GetError("AspectSourceFileInvalid", aspectSourceFiles[i]).ToString());
                }
            }

            return aspectSourceFiles;
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

        void _ShowError(string errorId, params string[] args)
        {
            var message = ErrorFactory.GetError(errorId, args).ToString();
            if (!TaskEventLogger.EventEnabled)
            {
                MessageBox.Show(message);
            }
            else
            {
                TaskEventLogger.Log(this, new TaskEvent()
                {Message = message});
            }

            _Error = true;
        }
    }
}