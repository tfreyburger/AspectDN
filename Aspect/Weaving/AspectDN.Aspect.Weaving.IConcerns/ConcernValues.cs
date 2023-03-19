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

namespace AspectDN.Aspect.Weaving.IConcerns
{
    public static class ConcernConstantValues
    {
        public static string CodeAdviceAnnotation => "CodeAdvice";
        public static string PrototypeTypeNameDeclaration => "PrototypeTypeNameDeclaration";
        public static string PrototypeTypeNameReferenceAnnotation => "PrototypeTypeNameReference";
        public static string AdviceChangeValueAnnotation => "StackAdvice";
        public static string DummyReturn => "_DummyReturn";
        public static string AroundStatement => "_AroundStatement";
        public static string AdviceNameUnderscore => "_";
        public static string VarStackName => "value";
        public static string ArgStackName => "stackValue";
        public static string PrototypeItemsDeclarations = "PrototypeTypesDeclarations";
        public static string PrototypeMappingTag = "PrototypeMappingTag";
        public static string PrototypeTypeMapping = "PrototypeMappingType";
        public static string PrototypeMemberMapping = "PrototypeMappingMember";
        public static string PrototypeBaseConstructorMapping = "PrototypeBaseConstructorMapping";
        public static string MarkerDllFileName = "AspectDN.Aspect.Weaving.Marker.dll";
        public static string AdviceChar = "#";
    }
}