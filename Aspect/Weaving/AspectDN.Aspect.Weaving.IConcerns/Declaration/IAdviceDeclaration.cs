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
    public interface IAdviceDeclaration : IConcernDeclaration
    {
    }

    public interface ICodeAdviceDeclaration : IAdviceDeclaration
    {
    }

    public interface IChangeValueAdviceDeclaration : IAdviceDeclaration
    {
    }

    public interface IInterfaceMembersAdviceDeclaration : IAdviceDeclaration
    {
    }

    public interface ITypeMembersAdviceDeclaration : IAdviceDeclaration
    {
    }

    public interface IEnumMembersAdviceDeclaration : IAdviceDeclaration
    {
    }

    public interface IInheritedTypesAdviceDeclaration : IAdviceDeclaration
    {
    }

    public interface ITypesAdviceDeclaration : IAdviceDeclaration
    {
    }

    public interface IAttributesAdviceDeclaration : IAdviceDeclaration
    {
    }
}