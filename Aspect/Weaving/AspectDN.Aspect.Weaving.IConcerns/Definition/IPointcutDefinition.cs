// Author:
//
// T. Freyburger (t.freyburger@gmail.com)
//
// Copyright (c)  Thierry Freyburger
//
// Licensed under the GPLV3 license.
////
using System;
using Mono.Cecil;

namespace AspectDN.Aspect.Weaving.IConcerns
{
    public interface IPointcutDefinition
    {
        string FullDeclarationName { get; }

        PointcutTypes PointcutType { get; }
    }

    public interface IPointcutAssemblyDefinition
    {
        Func<Mono.Cecil.ModuleDefinition, MethodDefinition, bool> Expression { get; }
    }

    public interface IPointcutTypeDefinition
    {
        Func<Mono.Cecil.TypeDefinition, MethodDefinition, bool> Expression { get; }
    }

    public interface IPointcutFieldDefinition
    {
        Func<Mono.Cecil.FieldDefinition, MethodDefinition, bool> Expression { get; }
    }

    public interface IPointcutPropertyDefinition
    {
        Func<Mono.Cecil.PropertyDefinition, MethodDefinition, bool> Expression { get; }
    }

    public interface IPointcutEventDefinition
    {
        Func<Mono.Cecil.EventDefinition, MethodDefinition, bool> Expression { get; }
    }

    public interface IPointcutMethodDefinition
    {
        Func<Mono.Cecil.MethodDefinition, MethodDefinition, bool> Expression { get; }
    }
}