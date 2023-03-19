// Author:
//
// T. Freyburger (t.freyburger@gmail.com)
//
// Copyright (c)  Thierry Freyburger
//
// Licensed under the GPLV3 license.
////
using System.Configuration;
using System.Xml;
using Foundation.Common.Error;
using System;

namespace AspectDN.Aspect.Compilation.Foundation
{
    internal class ConfigurationCompilerAspectSection : ConfigurationSection
    {
        internal static ConfigurationCompilerAspectSection GetConfig()
        {
            if (System.Configuration.ConfigurationManager.GetSection("ConfigurationCompilerAspectSection") == null)
                throw new Exception("faux");
            return (ConfigurationCompilerAspectSection)System.Configuration.ConfigurationManager.GetSection("ConfigurationCompilerAspectSection") ?? new ConfigurationCompilerAspectSection();
        }

        internal object Create(object parent, object configContext, XmlNode section)
        {
            throw ErrorFactory.GetException("NotImplementedException");
        }

        [System.Configuration.ConfigurationProperty("ConfigurationCompilerAspectCollection")]
        [ConfigurationCollection(typeof(ConfigurationCompilerAspectCollection), AddItemName = "Add")]
        internal ConfigurationCompilerAspectCollection ConfigurationCompilerAspectCollection
        {
            get
            {
                object o = this["ConfigurationCompilerAspectCollection"];
                return o as ConfigurationCompilerAspectCollection;
            }
        }
    }
}