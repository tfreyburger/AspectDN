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
using System.Linq;
using AspectDN.Aspect.Weaving.IJoinpoints;
using Mono.Cecil;
using AspectDN.Common;
using Foundation.Common.Error;

namespace AspectDN.Aspect.Joinpoints
{
    public class JoinpointsContainer : IJoinpointsContainer
    {
        public static IJoinpointsContainer Create(IEnumerable<AssemblyFile> assemblyFiles)
        {
            return new JoinpointVisitor().Visit(new JoinpointsContainer(assemblyFiles));
        }

        List<IJoinpoint> _Joinpoints;
        internal IEnumerable<AssemblyFile> AssemblyFiles { get; }

        internal List<AssemblyDefinition> AssemblyTargets { get; }

        internal DirectoryInfo AssembliesDirectory { get; }

        internal IEnumerable<ITypeJoinpoint> TypeJoinpoints => _Joinpoints.OfType<ITypeJoinpoint>();
        internal IEnumerable<string> Namespaces => TypeJoinpoints.Select(t => t.TypeDefinition.Namespace).Distinct();
        JoinpointsContainer(IEnumerable<AssemblyFile> assemblyFiles)
        {
            _Joinpoints = new List<IJoinpoint>();
            AssemblyFiles = assemblyFiles;
            AssemblyTargets = new List<AssemblyDefinition>();
        }

        IEnumerable<IMemberDefinition> GetMembers(TypeDefinition typeJoinpoint, string memberName)
        {
            return _Joinpoints.OfType<MemberJoinpoint>().Where(j => j.DeclaringType.FullName == typeJoinpoint.GetElementType().Resolve().FullName).Select(t => t.MemberDefinition);
        }

        internal void SetAsChanged(IJoinpoint joinpoint)
        {
            var module = _Joinpoints.Where(t => t.Assembly.FullName == joinpoint.Assembly.FullName).FirstOrDefault();
            if (module == null)
                throw ErrorFactory.GetException("UndefinedTargetAssembly", joinpoint.Assembly.FullName);
            ((IAssemblyJoinpoint)module).SetAsChanged();
        }

        internal void Add(IJoinpoint joinpoint)
        {
            if (!_Joinpoints.Any(t => t == joinpoint))
                _Joinpoints.Add(joinpoint);
            if (joinpoint is IAssemblyJoinpoint)
                AssemblyTargets.Add(((IAssemblyJoinpoint)joinpoint).Assembly);
        }

        internal IEnumerable<IJoinpoint> GetJointpoints(JoinpointKinds joinpointKind)
        {
            var types = _Joinpoints.Where(j => joinpointKind == (j.JoinpointKind & joinpointKind));
            return types;
        }

        internal IEnumerable<IJoinpoint> GetJointpointMembers(JoinpointKinds joinpointKind, Func<IMemberDefinition, MethodDefinition, bool> expression = null)
        {
            var jointpoints = _Joinpoints.Where(j => j is IMemberJoinpoint && joinpointKind == (j.JoinpointKind & joinpointKind));
            jointpoints = jointpoints.Where(t => expression(t.Member, null));
            return jointpoints;
        }

        internal IEnumerable<IJoinpoint> GetAssemblies(Func<ModuleDefinition, MethodDefinition, bool> expression = null)
        {
            var modules = _Joinpoints.OfType<IAssemblyJoinpoint>();
            if (expression != null)
                modules = modules.Where(t => expression(t.ModuleDefinition, null));
            return modules;
        }

        internal IEnumerable<IJoinpoint> GetTypes(JoinpointKinds joinpointKind, Func<TypeDefinition, MethodDefinition, bool> expression)
        {
            var types = _Joinpoints.Where(j => joinpointKind == (j.JoinpointKind & joinpointKind) && j is ITypeJoinpoint).Cast<ITypeJoinpoint>();
            types = types.Where(t => expression(t.TypeDefinition, null));
            return types;
        }

        internal IEnumerable<IJoinpoint> GetFields(JoinpointKinds joinpointType, Func<FieldDefinition, MethodDefinition, bool> expression)
        {
            if (JoinpointKinds.declaration == (joinpointType & JoinpointKinds.declaration))
            {
                var fields = _Joinpoints.Where(j => joinpointType == (j.JoinpointKind & joinpointType)).OfType<MemberJoinpoint>();
                fields = fields.Where(t => expression((FieldDefinition)t.MemberDefinition, null));
                return fields;
            }
            else
            {
                var fields = _Joinpoints.Where(j => joinpointType == (j.JoinpointKind & joinpointType)).OfType<InstructionJoinpoint>();
                fields = fields.Where(t => expression((FieldDefinition)t.InvokedMemberDefinition, t.CallingMethod));
                return fields;
            }
        }

        internal IEnumerable<IJoinpoint> GetMethods(JoinpointKinds joinpointType, Func<MethodDefinition, MethodDefinition, bool> expression)
        {
            var methods = _GetMethods(joinpointType, expression);
            return methods.Where(t => !_IsConstructor(t) && !_IsPropertyMethod(t));
        }

        internal IEnumerable<IJoinpoint> GetProperties(JoinpointKinds jointpoinKind, Func<PropertyDefinition, MethodDefinition, bool> expression)
        {
            if (JoinpointKinds.declaration == (jointpoinKind & JoinpointKinds.declaration))
            {
                var properties = _Joinpoints.Where(j => jointpoinKind == (j.JoinpointKind & jointpoinKind)).OfType<MemberJoinpoint>();
                properties = properties.Where(t => expression((PropertyDefinition)t.MemberDefinition, null));
                return properties;
            }
            else
            {
                if (JoinpointKinds.body == (jointpoinKind & JoinpointKinds.body))
                {
                    var properties = _Joinpoints.Where(j => jointpoinKind == (j.JoinpointKind & jointpoinKind)).OfType<MemberJoinpoint>();
                    properties = properties.Where(t => expression(CecilHelper.GetPropertyFromGetOrSetMethod((MethodDefinition)t.MemberDefinition), null));
                    return properties;
                }
                else
                {
                    var instructionJoinpoints = _Joinpoints.Where(j => jointpoinKind == (j.JoinpointKind & jointpoinKind)).OfType<InstructionJoinpoint>();
                    instructionJoinpoints = instructionJoinpoints.Where(t => expression((PropertyDefinition)t.InvokedMemberDefinition, t.CallingMethod));
                    return instructionJoinpoints;
                }
            }
        }

        internal IEnumerable<IJoinpoint> GetEvents(JoinpointKinds joinpointType, Func<EventDefinition, MethodDefinition, bool> expression)
        {
            if (JoinpointKinds.declaration == (joinpointType & JoinpointKinds.declaration))
            {
                var events = _Joinpoints.Where(j => joinpointType == (j.JoinpointKind & joinpointType)).OfType<IMemberJoinpoint>();
                events = events.Where(t => expression((EventDefinition)t.MemberDefinition, null));
                return events;
            }
            else
            {
                if (JoinpointKinds.body == (joinpointType & JoinpointKinds.body))
                {
                    var events = _Joinpoints.Where(j => joinpointType == (j.JoinpointKind & joinpointType)).OfType<MemberJoinpoint>();
                    events = events.Where(t => expression(CecilHelper.GetEventFromAddorRemoveMethod((MethodDefinition)t.MemberDefinition), null));
                    return events;
                }
                else
                {
                    var events = _Joinpoints.Where(j => joinpointType == (j.JoinpointKind & joinpointType)).OfType<InstructionJoinpoint>();
                    events = events.Where(t => expression((EventDefinition)t.InvokedMemberDefinition, t.CallingMethod));
                    return events;
                }
            }
        }

        internal IEnumerable<IJoinpoint> GetDelegates(JoinpointKinds joinpointType, Func<FieldDefinition, MethodDefinition, bool> expression)
        {
            if (JoinpointKinds.declaration == (joinpointType & JoinpointKinds.declaration))
            {
                var delegates = _Joinpoints.Where(j => joinpointType == (j.JoinpointKind & joinpointType)).OfType<IMemberJoinpoint>();
                delegates = delegates.Where(t => expression((FieldDefinition)t.MemberDefinition, null));
                return delegates;
            }
            else
            {
                var delegates = _Joinpoints.Where(j => joinpointType == (j.JoinpointKind & joinpointType)).OfType<InstructionJoinpoint>();
                delegates = delegates.Where(t => expression((FieldDefinition)t.InvokedMemberDefinition, t.CallingMethod));
                return delegates;
            }
        }

        internal IEnumerable<IJoinpoint> GetExceptions(JoinpointKinds joinpointType, Func<TypeDefinition, MethodDefinition, bool> expression)
        {
            var types = _Joinpoints.Where(j => joinpointType == (j.JoinpointKind & joinpointType)).OfType<InstructionJoinpoint>();
            types = types.Where(t => expression((TypeDefinition)t.InvokedMemberDefinition, t.CallingMethod));
            return types;
        }

        internal IEnumerable<ITypeJoinpoint> GetInheritedTypeJoinpoints(IEnumerable<ITypeJoinpoint> baseTypes)
        {
            var inheritedTypeJoinpoints = new List<ITypeJoinpoint>();
            foreach (var baseType in baseTypes)
                inheritedTypeJoinpoints.AddRange(_Joinpoints.OfType<ITypeJoinpoint>().Where(t => t.TypeDefinition.BaseType != null && t.TypeDefinition.BaseType.FullName == baseType.FullName));
            return inheritedTypeJoinpoints;
        }

        internal IEnumerable<IJoinpoint> GetConstructors(JoinpointKinds joinpointKind, Func<MethodDefinition, MethodDefinition, bool> expression)
        {
            return _GetMethods(joinpointKind, expression).Where(t => _IsConstructor(t));
        }

        IEnumerable<IJoinpoint> _GetMethods(JoinpointKinds joinpointType, Func<MethodDefinition, MethodDefinition, bool> expression)
        {
            if (JoinpointKinds.declaration == (joinpointType & JoinpointKinds.declaration) || JoinpointKinds.body == (joinpointType & JoinpointKinds.body))
            {
                var methods = _Joinpoints.Where(j => joinpointType == (j.JoinpointKind & joinpointType)).OfType<MemberJoinpoint>();
                methods = methods.Where(t => expression((MethodDefinition)t.MemberDefinition, null));
                return methods;
            }
            else
            {
                var methods = _Joinpoints.Where(j => joinpointType == (j.JoinpointKind & joinpointType)).OfType<InstructionJoinpoint>();
                methods = methods.Where(t => expression((MethodDefinition)t.InvokedMemberDefinition, t.CallingMethod));
                return methods;
            }
        }

        bool _IsConstructor(IJoinpoint joinpoint)
        {
            MethodDefinition methodDefinition = null;
            switch (joinpoint)
            {
                case IMemberJoinpoint member:
                    methodDefinition = (MethodDefinition)member.Member;
                    break;
                case IInstructionJoinpoint instruction:
                    methodDefinition = (MethodDefinition)instruction.InvokedMemberDefinition;
                    break;
            }

            return methodDefinition != null && methodDefinition.IsConstructor;
        }

        bool _IsPropertyMethod(IJoinpoint joinpoint)
        {
            MethodDefinition methodDefinition = null;
            switch (joinpoint)
            {
                case IMemberJoinpoint member:
                    methodDefinition = (MethodDefinition)member.Member;
                    break;
                case IInstructionJoinpoint instruction:
                    methodDefinition = (MethodDefinition)instruction.InvokedMemberDefinition;
                    break;
            }

            return CecilHelper.GetPropertyFromGetOrSetMethod(methodDefinition) != null;
        }

#region IJoinpoints
        IEnumerable<AssemblyDefinition> IJoinpointsContainer.AssemblyTargets => AssemblyTargets;
        void IJoinpointsContainer.SetAsChanged(IJoinpoint joinpoint) => SetAsChanged(joinpoint);
        void IJoinpointsContainer.Add(IJoinpoint joinPoint) => Add(joinPoint);
        IEnumerable<IJoinpoint> IJoinpointsContainer.GetAssemblies(Func<ModuleDefinition, MethodDefinition, bool> expression) => GetAssemblies(expression);
        IEnumerable<IJoinpoint> IJoinpointsContainer.GetTypes(JoinpointKinds joinpointKind, Func<TypeDefinition, MethodDefinition, bool> expression) => GetTypes(joinpointKind, expression);
        IEnumerable<IJoinpoint> IJoinpointsContainer.GetFields(JoinpointKinds joinpointType, Func<FieldDefinition, MethodDefinition, bool> expression) => GetFields(joinpointType, expression);
        IEnumerable<IJoinpoint> IJoinpointsContainer.GetMethods(JoinpointKinds joinpointType, Func<MethodDefinition, MethodDefinition, bool> expression) => GetMethods(joinpointType, expression);
        IEnumerable<IJoinpoint> IJoinpointsContainer.GetProperties(JoinpointKinds joinpointType, Func<PropertyDefinition, MethodDefinition, bool> expression) => GetProperties(joinpointType, expression);
        IEnumerable<IJoinpoint> IJoinpointsContainer.GetEvents(JoinpointKinds joinpointType, Func<EventDefinition, MethodDefinition, bool> expression) => GetEvents(joinpointType, expression);
        IEnumerable<IJoinpoint> IJoinpointsContainer.GetDelegates(JoinpointKinds joinpointType, Func<FieldDefinition, MethodDefinition, bool> expression) => GetDelegates(joinpointType, expression);
        IEnumerable<IJoinpoint> IJoinpointsContainer.GetExceptions(JoinpointKinds joinpointType, Func<TypeDefinition, MethodDefinition, bool> expression) => GetExceptions(joinpointType, expression);
        IEnumerable<ITypeJoinpoint> IJoinpointsContainer.GetInheritedTypes(IEnumerable<ITypeJoinpoint> baseTypes) => GetInheritedTypeJoinpoints(baseTypes);
        IEnumerable<IMemberDefinition> IJoinpointsContainer.GetMembers(ITypeJoinpoint typeJoinpoint, string memberName) => GetMembers(typeJoinpoint.TypeDefinition, memberName);
        IEnumerable<IJoinpoint> IJoinpointsContainer.GetConstructors(JoinpointKinds joinpointType, Func<MethodDefinition, MethodDefinition, bool> expression) => GetConstructors(joinpointType, expression);
        IEnumerable<ITypeJoinpoint> IJoinpointsContainer.TypeJoinpoints => TypeJoinpoints;
        IEnumerable<string> IJoinpointsContainer.Namespaces => Namespaces;
#endregion
    }
}