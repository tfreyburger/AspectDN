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
using AspectDN.Aspect.Weaving.IJoinpoints;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AspectDN.Aspect.Joinpoints
{
    internal abstract class Joinpoint : IJoinpoint
    {
        protected bool _HasChanged = false;
        internal bool HasChanged { get => _HasChanged; }

        internal JoinpointKinds JoinpointKind { get; }

        internal abstract AssemblyDefinition Assembly { get; }

        internal abstract string FullName { get; }

        internal abstract TypeDefinition DeclaringType { get; }

        internal Joinpoint(JoinpointKinds joinpointKind)
        {
            JoinpointKind = joinpointKind;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Joinpoint))
                return false;
            var joinpoint = (Joinpoint)obj;
            return (joinpoint.FullName == FullName && joinpoint.JoinpointKind == JoinpointKind);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

#region IJoinPoint
        AssemblyDefinition IJoinpoint.Assembly => Assembly;
        JoinpointKinds IJoinpoint.JoinpointKind => JoinpointKind;
        bool IJoinpoint.HasChanged => HasChanged;
        TypeDefinition IJoinpoint.DeclaringType => DeclaringType;
        IMemberDefinition IJoinpoint.Member => null;
#endregion
    }

    internal class AssemblyJoinpoint : Joinpoint, IAssemblyJoinpoint
    {
        List<AssemblyNameReference> InitialAssemblyReferences { get; }

        internal override AssemblyDefinition Assembly
        {
            get
            {
                return ModuleDefinition.Assembly;
            }
        }

        internal override string FullName
        {
            get
            {
                return Assembly.FullName;
            }
        }

        internal override TypeDefinition DeclaringType => null;
        internal ModuleDefinition ModuleDefinition { get; }

        internal AssemblyJoinpoint(ModuleDefinition moduleDefinition) : base(JoinpointKinds.assemblies | JoinpointKinds.declaration)
        {
            ModuleDefinition = moduleDefinition;
        }

        internal void SetAsChanged()
        {
            _HasChanged = true;
        }

        public override string ToString()
        {
            return $"{JoinpointKind.ToString()} : {Assembly.ToString()}";
        }

#region IJoinPoint
        IMemberDefinition IJoinpoint.Member => null;
#endregion
#region IAssemblyJoinpoint
        ModuleDefinition IAssemblyJoinpoint.ModuleDefinition => ModuleDefinition;
        void IAssemblyJoinpoint.SetAsChanged() => SetAsChanged();
#endregion
    }

    internal class InstructionJoinpoint : Joinpoint, IInstructionJoinpoint
    {
        internal override AssemblyDefinition Assembly
        {
            get
            {
                return CallingMethod.DeclaringType.Module.Assembly;
            }
        }

        internal Instruction Instruction { get; }

        internal IMemberDefinition InvokedMemberDefinition { get; }

        internal override string FullName => ToString();
        internal MethodDefinition CallingMethod { get; }

        internal override TypeDefinition DeclaringType => CallingMethod.DeclaringType;
        internal InstructionJoinpoint(MethodDefinition callingMethod, IMemberDefinition invokedMemberDefinition, Instruction instruction, JoinpointKinds joinpointType) : this(instruction, joinpointType)
        {
            InvokedMemberDefinition = invokedMemberDefinition;
            CallingMethod = callingMethod;
        }

        internal InstructionJoinpoint(Instruction instruction, JoinpointKinds joinpointKind) : base(joinpointKind)
        {
            Instruction = instruction;
        }

        public override string ToString()
        {
            return $"{JoinpointKind.ToString()} : {CallingMethod.FullName} : {Instruction.ToString()}";
        }

#region IJoinPoint
        IMemberDefinition IJoinpoint.Member => InvokedMemberDefinition;
#endregion
#region IInstructionJoinpoint
        string IInstructionJoinpoint.FullName => FullName;
        IMemberDefinition IInstructionJoinpoint.InvokedMemberDefinition => InvokedMemberDefinition;
        Instruction IInstructionJoinpoint.Instruction => Instruction;
        MethodDefinition IInstructionJoinpoint.CallingMethod => CallingMethod;
#endregion
    }

    internal abstract class MemberJoinpoint : Joinpoint, IMemberJoinpoint
    {
        internal abstract IMemberDefinition MemberDefinition { get; }

        internal MemberJoinpoint(JoinpointKinds joinpointKind) : base(joinpointKind)
        {
        }

        public override string ToString()
        {
            return $"{JoinpointKind.ToString()} : {MemberDefinition.FullName}";
        }

#region IJoinPoint
        IMemberDefinition IJoinpoint.Member => MemberDefinition;
#endregion
#region IMemberJoinpoint
        string IMemberJoinpoint.FullName => FullName;
        IMemberDefinition IMemberJoinpoint.MemberDefinition => MemberDefinition;
#endregion
    }

    internal class MethodJoinpoint : MemberJoinpoint
    {
        internal override string FullName => MethodDefinition.FullName;
        internal override AssemblyDefinition Assembly
        {
            get
            {
                return MethodDefinition.Module.Assembly;
            }
        }

        internal override TypeDefinition DeclaringType
        {
            get
            {
                return MethodDefinition.DeclaringType;
            }
        }

        internal IMemberDefinition ParentMember { get; }

        internal MethodDefinition MethodDefinition { get; }

        internal override IMemberDefinition MemberDefinition { get => MethodDefinition; }

        internal MethodJoinpoint(IMemberDefinition parentMember, MethodDefinition methodDefinition, JoinpointKinds joinpointType) : this(methodDefinition, joinpointType)
        {
            ParentMember = parentMember;
        }

        internal MethodJoinpoint(MethodDefinition methodDefinition, JoinpointKinds joinpointType) : base(joinpointType)
        {
            MethodDefinition = methodDefinition;
        }
    }

    internal class PropertyJoinpoint : MemberJoinpoint
    {
        internal override string FullName => PropertyDefinition.FullName;
        internal override AssemblyDefinition Assembly
        {
            get
            {
                return PropertyDefinition.Module.Assembly;
            }
        }

        internal override TypeDefinition DeclaringType
        {
            get
            {
                return PropertyDefinition.DeclaringType;
            }
        }

        internal PropertyDefinition PropertyDefinition { get; }

        internal override IMemberDefinition MemberDefinition { get => PropertyDefinition; }

        internal PropertyJoinpoint(PropertyDefinition propertyDefinition, JoinpointKinds joinpointType) : base(joinpointType)
        {
            PropertyDefinition = propertyDefinition;
        }
    }

    internal class EventJointpoint : MemberJoinpoint
    {
        internal override string FullName => EventDefinition.FullName;
        internal override AssemblyDefinition Assembly
        {
            get
            {
                return EventDefinition.Module.Assembly;
            }
        }

        internal override TypeDefinition DeclaringType
        {
            get
            {
                return EventDefinition.DeclaringType;
            }
        }

        internal EventDefinition EventDefinition { get; }

        internal override IMemberDefinition MemberDefinition { get => EventDefinition; }

        internal EventJointpoint(EventDefinition eventDefinition, JoinpointKinds joinpointType) : base(joinpointType)
        {
            EventDefinition = eventDefinition;
        }
    }

    internal class FieldJoinpoint : MemberJoinpoint
    {
        internal override string FullName => FieldDefinition.FullName;
        internal override AssemblyDefinition Assembly
        {
            get
            {
                return FieldDefinition.Module.Assembly;
            }
        }

        internal override TypeDefinition DeclaringType
        {
            get
            {
                return FieldDefinition.DeclaringType;
            }
        }

        internal FieldDefinition FieldDefinition { get; }

        internal override IMemberDefinition MemberDefinition { get => FieldDefinition; }

        internal FieldJoinpoint(FieldDefinition fieldDefinition, JoinpointKinds joinpointType) : base(joinpointType)
        {
            FieldDefinition = fieldDefinition;
        }
    }

    internal class TypeJointpoint : MemberJoinpoint, ITypeJoinpoint
    {
        internal override AssemblyDefinition Assembly
        {
            get
            {
                return TypeDefinition.Module.Assembly;
            }
        }

        internal TypeDefinition TypeDefinition { get; }

        internal override TypeDefinition DeclaringType => TypeDefinition;
        internal override string FullName
        {
            get
            {
                return TypeDefinition.FullName;
            }
        }

        internal TypeJointpoint(TypeDefinition typeDefinition, JoinpointKinds joinpointKind) : base(joinpointKind)
        {
            TypeDefinition = typeDefinition;
        }

        public override string ToString()
        {
            return $"{JoinpointKind.ToString()} : {TypeDefinition.FullName}";
        }

#region IMemberDefinition
        internal override IMemberDefinition MemberDefinition => TypeDefinition;
#endregion
#region IJoinPoint
        IMemberDefinition IJoinpoint.Member => TypeDefinition;
#endregion
#region ITypeJoinpoint
        string ITypeJoinpoint.FullName => FullName;
        TypeDefinition ITypeJoinpoint.TypeDefinition => TypeDefinition;
#endregion
    }
}