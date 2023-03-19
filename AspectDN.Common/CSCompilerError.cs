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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TokenizerDN.Common;

namespace AspectDN.Common
{
    internal class CSCompilerError : ICompilerError
    {
        internal string Code { get; }

        internal int Level { get; }

        internal string Description { get; }

        internal SourceLocation SourceLocation { get; }

        internal CSCompilerError(string code, int level, string description, SourceLocation sourceLocation)
        {
            this.Code = code;
            Level = level;
            Description = description;
            SourceLocation = sourceLocation;
        }

        public override string ToString()
        {
            return $"{SourceLocation.ToString()} {Code} {Level.ToString()} {Description}";
        }

#region ICompiler
        string ICompilerError.FileName => SourceLocation.Filename;
        int ICompilerError.Line => SourceLocation.LineNo;
        int ICompilerError.Column => SourceLocation.ColumnNo;
        string IError.Code => Code;
        int IError.Level => Level;
        string IError.Description => Description;
        MethodBase IError.CallingMethod => null;
#endregion
    }
}