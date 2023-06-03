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
using System.Configuration;
using System.Reflection;
using Foundation.Common.Error;
using TokenizerDN.Common.SourceAnalysis;
using TokenizerDN.Common;
using Foundation.Common;

namespace AspectDN.Aspect.Compilation.Foundation
{
    internal class ConfigurationCompilerAspect : ConfigurationElement
    {
        Type _SourceAnalyserType;
        [ConfigurationProperty("Language", IsRequired = true, IsKey = true)]
        internal string Language
        {
            get
            {
                return (string)this["Language"];
            }

            set
            {
                this["Language"] = value;
            }
        }

        [ConfigurationProperty("CompilerAspectAssemblyFilename", IsRequired = true, IsKey = false)]
        internal string CompilerAspectAssemblyFilename
        {
            get
            {
                return (string)this["CompilerAspectAssemblyFilename"];
            }

            set
            {
                this["CompilerAspectAssemblyFilename"] = value;
            }
        }

        [ConfigurationProperty("SyntaxDefinitionAssemblyFilename", IsRequired = true, IsKey = false)]
        public string SyntaxDefinitionAssemblyFilename
        {
            get
            {
                return (string)this["SyntaxDefinitionAssemblyFilename"];
            }

            set
            {
                this["SyntaxDefinitionAssemblyFilename"] = value;
            }
        }

        [ConfigurationProperty("TokenVisitorAssemblyFilename", IsRequired = true, IsKey = false)]
        public string TokenVisitorAssemblyFilename
        {
            get
            {
                return (string)this["TokenVisitorAssemblyFilename"];
            }

            set
            {
                this["TokenVisitorAssemblyFilename"] = value;
            }
        }

        [ConfigurationProperty("SyntaxName", IsRequired = true, IsKey = false)]
        internal string SyntaxName
        {
            get
            {
                return (string)this["SyntaxName"];
            }

            set
            {
                this["SyntaxName"] = value;
            }
        }

        [ConfigurationProperty("LogFilename", IsRequired = true, IsKey = false)]
        internal string LogFilename
        {
            get
            {
                return (string)this["LogFilename"];
            }

            set
            {
                this["LogFilename"] = value;
            }
        }

        [ConfigurationProperty("IsDefault", IsRequired = false, IsKey = false)]
        internal bool IsDefault
        {
            get
            {
                return (bool)this["IsDefault"];
            }

            set
            {
                this["IsDefault"] = value;
            }
        }

        internal ConfigurationCompilerAspect()
        {
        }

        internal ConfigurationCompilerAspect(string language, string syntaxName, string loggerName, string logFilename)
        {
            Language = language;
            SyntaxName = syntaxName;
            LogFilename = logFilename;
        }

        internal AspectCompiler CreateAspectCompiler()
        {
            return CreateAspectCompiler(LogFilename, LogFilename);
        }

        internal AspectCompiler CreateAspectCompiler(string loggerName, string logFilename)
        {
            Assembly assembly = null;
            try
            {
                assembly = Assembly.LoadFrom(Helper.GetFullPath(CompilerAspectAssemblyFilename));
            }
            catch (Exception ex)
            {
                throw ErrorFactory.GetException("UnknownCompilerAssemblyFilename", CompilerAspectAssemblyFilename, ex.ToString());
            }

            Type type = assembly.GetTypes().Where(t => AspectCompilerHelper.IsTypeOf<AspectCompiler>(t)).FirstOrDefault();
            var ctor = type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).First();
            var aspectCompiler = (AspectCompiler)ctor.Invoke(new object[]{SyntaxName, loggerName, logFilename});
            return aspectCompiler;
        }

        internal ITokenVisitor CreateTokenVisitor(ITree tree, List<ICompilerError> errors)
        {
            ITokenVisitor tokenVisitor = null;
            Assembly assembly = Assembly.LoadFile(Helper.GetFullPath(TokenVisitorAssemblyFilename));
            Type tokenVisistorType = assembly.GetTypes().FirstOrDefault(t => Helper.IsInheritedFrom(t, typeof(ITokenVisitor)));
            if (tokenVisistorType != null)
            {
                BindingFlags flags = BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance;
                Binder binder = null;
                var ctor = tokenVisistorType.GetConstructor(flags, binder, new[]{tree.GetType(), errors.GetType()}, null);
                tokenVisitor = (ITokenVisitor)ctor.Invoke(new object[]{tree, errors});
            }

            return tokenVisitor;
        }

        internal ISourceAnalyzer CreateSourceAnalyzer()
        {
            if (_SourceAnalyserType == null)
            {
                Assembly assembly = Assembly.LoadFile(Helper.GetFullPath(SyntaxDefinitionAssemblyFilename));
                _SourceAnalyserType = assembly.GetTypes().FirstOrDefault(t => Helper.IsInheritedFrom(t, typeof(ISourceAnalyzer)));
            }

            var sourceAnalyser = (ISourceAnalyzer)Activator.CreateInstance(_SourceAnalyserType);
            return sourceAnalyser;
        }
    }
}