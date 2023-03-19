// Author:
//
// T. Freyburger (t.freyburger@gmail.com)
//
// Copyright (c)  Thierry Freyburger
//
// Licensed under the GPLV3 license.
////
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace AspectDN.Aspect.Weaving.IJoinpoints
{
    public interface IInstructionJoinpoint : IJoinpoint
    {
        string FullName { get; }

        IMemberDefinition InvokedMemberDefinition { get; }

        Instruction Instruction { get; }

        MethodDefinition CallingMethod { get; }
    }
}