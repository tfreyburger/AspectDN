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
using Microsoft.CodeAnalysis;
using TokenizerDN.Common.SourceAnalysis;
using System.Reflection;
using Foundation.Common.Error;
using TokenizerDN.Common;

namespace AspectDN.Aspect.Compilation.Foundation
{
    internal class TokenErrorAspect : AspectNode, ICompilerError
    {
        ICompilerError _CompilerError;
        internal TokenErrorAspect(ISynToken token, ICompilerError compilerError) : base(token)
        {
            _CompilerError = compilerError;
            if (compilerError == null)
                throw new NotSupportedException();
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            throw ErrorFactory.GetException("NotImplementedException", "");
        }

        public override string ToString()
        {
            return $"{SynToken.GetSourceLocation()} : {_CompilerError}";
        }

#region IError
        string IError.Code => _CompilerError.Code;
        int IError.Level => _CompilerError.Level;
        string IError.Description => _CompilerError.Description;
        MethodBase IError.CallingMethod => _CompilerError.CallingMethod;
#endregion
#region ICompilerError
        string ICompilerError.FileName => _CompilerError.FileName;
        int ICompilerError.Line => _CompilerError.Line;
        int ICompilerError.Column => _CompilerError.Column;
#endregion
    }
}