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
using System.IO;
using Mono.Cecil;

namespace AspectDN.Aspect.Weaving.IJoinpoints
{
    public interface IJoinpointsContainer
    {
        IEnumerable<AssemblyDefinition> AssemblyTargets { get; }

        IEnumerable<string> Namespaces { get; }

        void SetAsChanged(IJoinpoint joinpoint);
        void Add(IJoinpoint joinPoint);
        IEnumerable<ITypeJoinpoint> TypeJoinpoints { get; }

        IEnumerable<IJoinpoint> GetAssemblies(Func<ModuleDefinition, MethodDefinition, bool> expression = null);
        IEnumerable<IJoinpoint> GetTypes(JoinpointKinds joinpointKind, Func<TypeDefinition, MethodDefinition, bool> expression);
        IEnumerable<IJoinpoint> GetFields(JoinpointKinds joinpointType, Func<FieldDefinition, MethodDefinition, bool> expression);
        IEnumerable<IJoinpoint> GetMethods(JoinpointKinds joinpointType, Func<MethodDefinition, MethodDefinition, bool> expression);
        IEnumerable<IJoinpoint> GetProperties(JoinpointKinds joinpointType, Func<PropertyDefinition, MethodDefinition, bool> expression);
        IEnumerable<IJoinpoint> GetEvents(JoinpointKinds joinpointType, Func<EventDefinition, MethodDefinition, bool> expression);
        IEnumerable<IJoinpoint> GetDelegates(JoinpointKinds joinpointType, Func<FieldDefinition, MethodDefinition, bool> expression);
        IEnumerable<IJoinpoint> GetConstructors(JoinpointKinds joinpointType, Func<MethodDefinition, MethodDefinition, bool> expression);
        IEnumerable<IJoinpoint> GetExceptions(JoinpointKinds joinpointType, Func<TypeDefinition, MethodDefinition, bool> expression);
        IEnumerable<ITypeJoinpoint> GetInheritedTypes(IEnumerable<ITypeJoinpoint> baseTypes);
        IEnumerable<IMemberDefinition> GetMembers(ITypeJoinpoint typeJoinpoint, string memberName);
    }
}