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
using System.IO;

namespace AspectDN.Common
{
    public static class Helper
    {
        public static bool IsInheritedFrom(Type t, Type inheritedType)
        {
            var baseType = t;
            while ((baseType = baseType.BaseType) != null)
            {
                if (baseType.FullName == inheritedType.FullName)
                    return true;
            }

            return false;
        }

        public static string GetFullPath(string filename)
        {
            if (filename.IndexOf(@"..\") == 0)
            {
                filename = filename.Replace("..", Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            }

            return filename;
        }
    }
}