// Author:
//
// T. Freyburger (t.freyburger@gmail.com)
//
// Copyright (c)  Thierry Freyburger
//
// Licensed under the GPLV3 license.
////
using Foundation.Common.Error;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TokenizerDN.Common;

namespace AspectDN.Common
{
    public class AspectDNErrorFactory
    {
        public static IError GetError(string errorId, params string[] parameters)
        {
            return ErrorFactory.GetError(errorId, parameters);
        }

        public static ErrorException GetException(string errorId, params string[] parameters)
        {
            return ErrorFactory.GetException(errorId, parameters);
        }

        public static ICompilerError GetCompilerError(string errorId, SourceLocation sourceLocation, params string[] parameters)
        {
            return CompilerErrorFactory.GetCompilerError(errorId, sourceLocation, parameters);
        }

        internal static ICompilerError GetCSCompilerError(string code, int level, string description, SourceLocation sourceLocation)
        {
            return new CSCompilerError(code, level, description, sourceLocation);
        }

        internal static IError GetWeaverError(string errorId, string aspectName, string adviceName, string joinpointName, params string[] parameters)
        {
            var callinErrorgMethod = new StackFrame(1).GetMethod();
            return ErrorFactory.GetErrorDefinition(errorId).GetError(Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName, callinErrorgMethod, aspectName, adviceName, joinpointName, ErrorFactory.GetErrorDefinition(errorId).GetDescription(Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName, parameters));
        }
    }
}