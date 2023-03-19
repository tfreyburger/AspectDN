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
    public interface IAspectDeclaration : IConcernDeclaration
    {
    }

    public interface IAspectControlFlowDeclaration : IConcernDeclaration
    {
    }

    public interface ICodeAspectDeclaration : IAspectDeclaration
    {
    }

    public interface IChangeValueAspectDeclaration : IAspectDeclaration
    {
    }

    public interface IAspectInheritanceDeclaration : IAspectDeclaration
    {
    }

    public interface IAspectInterfaceMembersDeclaration : IAspectDeclaration
    {
    }

    public interface IAspectTypeDeclaration : IAspectDeclaration
    {
    }

    public interface IAspectEnumMembersDeclaration : IAspectDeclaration
    {
    }

    public interface IAspectTypeMembersDeclaration : IAspectDeclaration
    {
    }

    public interface IAspectAttributesDeclaration : IAspectDeclaration
    {
    }
}