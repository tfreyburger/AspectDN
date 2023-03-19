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
using TokenizerDN.Common.SourceAnalysis;
using AspectDN.Aspect.Compilation.Foundation;
using Microsoft.CodeAnalysis;

namespace AspectDN.Aspect.Compilation.CS
{
    internal class KeywordAspect : CSAspectNode
    {
        internal Keywords Keyword { get; }

        internal KeywordAspect(ISynToken token, Keywords keyword) : base(token)
        {
            Keyword = keyword;
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.Keyword(this);
        }
    }

    internal enum Keywords
    {
        ABSTRACT,
        AS,
        BASE,
        BOOL,
        BREAK,
        BYTE,
        CASE,
        CATCH,
        CHAR,
        CHECKED,
        CLASS,
        CONST,
        CONTINUE,
        DECIMAL,
        DEFAULT,
        DELEGATE,
        DO,
        DOUBLE,
        DYNAMIC,
        ELSE,
        ENUM,
        EVENT,
        EXPLICIT,
        EXTERN,
        FINALLY,
        FIXED,
        FLOAT,
        FOR,
        FOREACH,
        GOTO,
        IF,
        IMPLICIT,
        IN,
        INT,
        INTERFACE,
        IS,
        LOCK,
        LONG,
        NAMESPACE,
        NEW,
        OBJECT,
        OPERATOR,
        OUT,
        OVERRIDE,
        PARAMS,
        PRIVATE,
        PROTECTED,
        PUBLIC,
        READONLY,
        REF,
        RETURN,
        SBYTE,
        SEALED,
        SHORT,
        SIZEOF,
        STACKALLOC,
        STATIC,
        STRING,
        STRUCT,
        SWITCH,
        THIS,
        THROW,
        TRY,
        TYPEOF,
        UINT,
        ULONG,
        UNCHECKED,
        UNSAFE,
        USHORT,
        USING,
        VIRTUAL,
        VOID,
        VOLATILE,
        WHILE
    }
}