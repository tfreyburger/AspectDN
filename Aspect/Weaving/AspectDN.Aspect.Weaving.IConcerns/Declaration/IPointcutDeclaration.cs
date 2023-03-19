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
using Mono.Cecil;

namespace AspectDN.Aspect.Weaving.IConcerns
{
    public interface IPointcutDeclaration : IConcernDeclaration
    {
    }

    public interface IPointcutAsssemblyDeclaration : IPointcutDeclaration
    {
        Func<ModuleDefinition, MethodDefinition, bool> GetDefinition();
    }

    public interface IPointcutTypeDeclaration : IPointcutDeclaration
    {
        Func<TypeDefinition, MethodDefinition, bool> GetDefinition();
    }

    public interface IPointcutPropertyDeclaration : IPointcutDeclaration
    {
        Func<PropertyDefinition, MethodDefinition, bool> GetDefinition();
    }

    public interface IPointcutFieldDeclaration : IPointcutDeclaration
    {
        Func<FieldDefinition, MethodDefinition, bool> GetDefinition();
    }

    public interface IPointcutMethodDeclaration : IPointcutDeclaration
    {
        Func<MethodDefinition, MethodDefinition, bool> GetDefinition();
    }

    public interface IPointcutEventDeclaration : IPointcutDeclaration
    {
        Func<EventDefinition, MethodDefinition, bool> GetDefinition();
    }

    public enum PointcutTypes
    {
        assemblies,
        classes,
        interfaces,
        methods,
        fields,
        properties,
        events,
        delegates,
        structs,
        exceptions,
        constructors,
        enums
    }

    public enum AspectMemberModifiers : uint
    {
        none = 0,
        @new = 1,
        @override = 2
    }
}