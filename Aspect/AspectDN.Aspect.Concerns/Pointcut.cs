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
using AspectDN.Common;
using Mono.Cecil;
using AspectDN.Aspect.Weaving.IConcerns;
using AspectDN.Aspect.Weaving.IJoinpoints;

namespace AspectDN.Aspect.Concerns
{
    internal abstract class PointcutDefinition : IPointcutDefinition
    {
        internal TypeDefinition _PointcutDeclaration;
        internal string FullDeclarationName => _PointcutDeclaration.FullName;
        internal PointcutTypes PointcutType { get; }

        internal PointcutDefinition(TypeDefinition pointcutDeclaration, PointcutTypes pointcutType)
        {
            PointcutType = pointcutType;
            _PointcutDeclaration = pointcutDeclaration;
        }

#region IPointcutDefinition
        string IPointcutDefinition.FullDeclarationName => FullDeclarationName;
        PointcutTypes IPointcutDefinition.PointcutType => PointcutType;
#endregion
    }

    internal class PointcutAssemblyDefinition : PointcutDefinition, IPointcutAssemblyDefinition
    {
        internal Func<ModuleDefinition, MethodDefinition, bool> Expression { get; }

        internal PointcutAssemblyDefinition(TypeDefinition pointcutDeclaration, PointcutTypes pointcutType, Func<ModuleDefinition, MethodDefinition, bool> expression) : base(pointcutDeclaration, pointcutType)
        {
            Expression = expression;
        }

#region
        Func<ModuleDefinition, MethodDefinition, bool> IPointcutAssemblyDefinition.Expression => this.Expression;
#endregion
    }

    internal class PointcutTypeDefinition : PointcutDefinition, IPointcutTypeDefinition
    {
        internal Func<TypeDefinition, MethodDefinition, bool> Expression { get; }

        internal PointcutTypeDefinition(TypeDefinition pointcutDeclaration, PointcutTypes pointcutType, Func<TypeDefinition, MethodDefinition, bool> expression) : base(pointcutDeclaration, pointcutType)
        {
            Expression = expression;
        }

#region IPointcutTypeDefinition
        Func<TypeDefinition, MethodDefinition, bool> IPointcutTypeDefinition.Expression => this.Expression;
#endregion
    }

    internal class PointcutPropertyDefinition : PointcutDefinition, IPointcutPropertyDefinition
    {
        internal Func<PropertyDefinition, MethodDefinition, bool> Expression { get; }

        internal PointcutPropertyDefinition(TypeDefinition pointcutDeclaration, PointcutTypes pointcutType, Func<PropertyDefinition, MethodDefinition, bool> expression) : base(pointcutDeclaration, pointcutType)
        {
            Expression = expression;
        }

#region IPointcutTypeDefinition
        Func<PropertyDefinition, MethodDefinition, bool> IPointcutPropertyDefinition.Expression => this.Expression;
#endregion
    }

    internal class PointcutFieldDefinition : PointcutDefinition, IPointcutFieldDefinition
    {
        internal Func<FieldDefinition, MethodDefinition, bool> Expression { get; }

        internal PointcutFieldDefinition(TypeDefinition pointcutDeclaration, PointcutTypes pointcutType, Func<FieldDefinition, MethodDefinition, bool> expression) : base(pointcutDeclaration, pointcutType)
        {
            Expression = expression;
        }

#region IPointcutFieldDefinition
        Func<FieldDefinition, MethodDefinition, bool> IPointcutFieldDefinition.Expression => this.Expression;
#endregion
    }

    internal class PointcutMethodDefinition : PointcutDefinition, IPointcutMethodDefinition
    {
        internal Func<MethodDefinition, MethodDefinition, bool> Expression { get; }

        internal PointcutMethodDefinition(TypeDefinition pointcutDeclaration, PointcutTypes pointcutType, Func<MethodDefinition, MethodDefinition, bool> expression) : base(pointcutDeclaration, pointcutType)
        {
            Expression = expression;
        }

#region IPointcutMethodDefinition
        Func<MethodDefinition, MethodDefinition, bool> IPointcutMethodDefinition.Expression => this.Expression;
#endregion
    }

    internal class PointcutEventDefinition : PointcutDefinition, IPointcutEventDefinition
    {
        internal Func<EventDefinition, MethodDefinition, bool> Expression { get; }

        internal PointcutEventDefinition(TypeDefinition pointcutDeclaration, PointcutTypes pointcutType, Func<EventDefinition, MethodDefinition, bool> expression) : base(pointcutDeclaration, pointcutType)
        {
            Expression = expression;
        }

#region IPointcutEventDefinition
        Func<EventDefinition, MethodDefinition, bool> IPointcutEventDefinition.Expression => this.Expression;
#endregion
    }
}