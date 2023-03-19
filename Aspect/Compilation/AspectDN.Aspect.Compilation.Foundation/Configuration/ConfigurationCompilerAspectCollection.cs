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
using System.Configuration;

namespace AspectDN.Aspect.Compilation.Foundation
{
    internal class ConfigurationCompilerAspectCollection : ConfigurationElementCollection
    {
        internal new ConfigurationCompilerAspect this[string syntaxName]
        {
            get
            {
                return (ConfigurationCompilerAspect)base.BaseGet(syntaxName);
            }
        }

        internal ConfigurationCompilerAspect GetConfigurationDefaultCompiler()
        {
            ConfigurationCompilerAspect defaultConfigurationSyntax = null;
            foreach (ConfigurationCompilerAspect syntax in this)
            {
                if (syntax.IsDefault)
                {
                    defaultConfigurationSyntax = syntax;
                    break;
                }
            }

            return defaultConfigurationSyntax;
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ConfigurationCompilerAspect();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ConfigurationCompilerAspect)element).Language;
        }
    }
}