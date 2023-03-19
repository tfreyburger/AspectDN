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
using TokenizerDN.Common.SourceAnalysis;
using Foundation.Common.Error;
using TokenizerDN.Common;

namespace AspectDN.Aspect.Compilation.Foundation
{
    internal static class CompilerAspectRepository
    {
        static ConfigurationCompilerAspectCollection _ConfigurationCompilerAspectCollection;
        internal static ConfigurationCompilerAspect GetConfigurationCompiler(string language)
        {
            ConfigurationCompilerAspect configurationCompilerAspect = null;
            if (_ConfigurationCompilerAspectCollection == null)
                _ConfigurationCompilerAspectCollection = ConfigurationCompilerAspectSection.GetConfig().ConfigurationCompilerAspectCollection;
            if (!string.IsNullOrEmpty(language))
                configurationCompilerAspect = _ConfigurationCompilerAspectCollection[language];
            else
                configurationCompilerAspect = _ConfigurationCompilerAspectCollection.GetConfigurationDefaultCompiler();
            if (configurationCompilerAspect == null)
                throw new Exception($"The language \"{language}\" is not defined in your application setting");
            return configurationCompilerAspect;
        }

        internal static AspectCompiler GetCompilerAspect(string language, string loggerName, string logFilename)
        {
            return GetConfigurationCompiler(language).CreateAspectCompiler(loggerName, logFilename);
        }

        internal static AspectCompiler GetCompilerAspect(string language)
        {
            return GetConfigurationCompiler(language).CreateAspectCompiler();
        }

        internal static ITokenVisitor GetTokenVisitor(string language, ITree tree, List<ICompilerError> errors)
        {
            return GetConfigurationCompiler(language).CreateTokenVisitor(tree, errors);
        }

        internal static ISourceAnalyzer GetSourceAnalyzer(string language)
        {
            return GetConfigurationCompiler(language).CreateSourceAnalyzer();
        }
    }
}