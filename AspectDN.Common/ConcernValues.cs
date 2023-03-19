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

namespace AspectDN.Common
{
    public static class ConcernValues
    {
        public static string CodeAdviceAnnotation => "CodeAdvice";
        public static string StackAdviceAnnotation => "StackAdvice";
        public static string AroundStatement => "_AroundStatement";
        public static string AdviceNameUnderscore => "_";
        public static string VarStackName => "value";
        public static string ArgStackName => "stackValue";
    }
}