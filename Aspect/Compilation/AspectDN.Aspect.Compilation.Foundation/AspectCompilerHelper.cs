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

namespace AspectDN.Aspect.Compilation.Foundation
{
    internal static class AspectCompilerHelper
    {
        internal static bool IsTypeOf<T>(AspectNode aspectNode)
        {
            var isTypeof = false;
            var type = aspectNode.GetType();
            while (type != null)
            {
                isTypeof = typeof(T).ToString() == type.ToString();
                if (isTypeof)
                    break;
                type = type.BaseType;
            }

            return isTypeof;
        }

        internal static bool IsTypeOf<T>(Type type)
        {
            var isTypeof = false;
            while (type != null)
            {
                isTypeof = typeof(T).ToString() == type.ToString();
                if (isTypeof)
                    break;
                type = type.BaseType;
            }

            return isTypeof;
        }
    }
}