// Author:
//
// T. Freyburger (t.freyburger@gmail.com)
//
// Copyright (c)  Thierry Freyburger
//
// Licensed under the GPLV3 license.
////
using System;
using System.Linq;
using System.Collections.Generic;
using AspectDN.Aspect.Weaving.IJoinpoints;
using AspectDN.Common;
using Mono.Cecil;
using CecilCollection = Mono.Collections.Generic;
using Mono.Cecil.Cil;
using Foundation.Common.Error;

namespace AspectDN.Aspect.Joinpoints
{
    internal class JoinpointVisitor
    {
        JoinpointsContainer _Joinpoints;
        OpCodeTypes[] _IncludedMethodOpCodes = new OpCodeTypes[]{OpCodeTypes.Call, OpCodeTypes.Throw, OpCodeTypes.Field | OpCodeTypes.Ld, OpCodeTypes.Field | OpCodeTypes.St, OpCodeTypes.New};
        internal JoinpointsContainer Visit(JoinpointsContainer joinPoints)
        {
            _Joinpoints = joinPoints;
            foreach (var assemblyFile in _Joinpoints.AssemblyFiles)
            {
                _VisitAssemblyDefinition(assemblyFile.GetAssemblyDefinition());
            }

            var bodies = _Joinpoints.GetJointpoints(JoinpointKinds.body).Select(t => (MethodDefinition)t.Member).ToList();
            foreach (var body in bodies)
                _VisitMethodBody(body);
            return joinPoints;
        }

        void _VisitAssemblyDefinition(AssemblyDefinition assemblyDefinition)
        {
            _Joinpoints.Add(new AssemblyJoinpoint(assemblyDefinition.MainModule));
            if (assemblyDefinition.MainModule.Types != null && assemblyDefinition.MainModule.Types.Count != 0)
            {
                foreach (TypeDefinition typeDefinition in assemblyDefinition.MainModule.Types.Where(t => !t.IsSpecialName))
                {
                    if (typeDefinition.Name == "<Module>")
                        continue;
                    _VisitTypeDefinition(typeDefinition);
                }
            }
        }

        void _VisitTypeDefinition(TypeDefinition typeDefinition)
        {
            if (CecilHelper.GetCustomAttributeOfType(typeDefinition, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute)) != null)
                return;
            var joinpointType = JoinpointKinds.none;
            var category = CecilHelper.GetTypeKinds(typeDefinition);
            switch (category)
            {
                case TypeDefinitionKinds.Struct:
                    joinpointType = JoinpointKinds.structs | JoinpointKinds.declaration;
                    break;
                case TypeDefinitionKinds.Delegate:
                    joinpointType = JoinpointKinds.type_delegates | JoinpointKinds.declaration;
                    break;
                case TypeDefinitionKinds.Class:
                    joinpointType = JoinpointKinds.classes | JoinpointKinds.declaration;
                    break;
                case TypeDefinitionKinds.Enum:
                    joinpointType = JoinpointKinds.enums | JoinpointKinds.declaration;
                    break;
                case TypeDefinitionKinds.Interface:
                    joinpointType = JoinpointKinds.interfaces | JoinpointKinds.declaration;
                    break;
                default:
                    throw ErrorFactory.GetException("UndefinedTypeDefinition", typeDefinition.FullName);
            }

            _Joinpoints.Add(new TypeJointpoint(typeDefinition, joinpointType));
            if (category == TypeDefinitionKinds.Delegate)
                return;
            if (typeDefinition.HasNestedTypes)
            {
                foreach (TypeDefinition nestedClass in typeDefinition.NestedTypes.Where(t => CecilHelper.GetCustomAttributeOfType(t, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute)) == null))
                    _VisitTypeDefinition(nestedClass);
            }

            if (typeDefinition.HasEvents)
            {
                foreach (EventDefinition eventDefinition in typeDefinition.Events)
                    _Joinpoints.Add(new EventJointpoint(eventDefinition, JoinpointKinds.events | JoinpointKinds.declaration));
            }

            if (typeDefinition.HasFields)
            {
                foreach (var fieldDefinition in typeDefinition.Fields.Where(t => CecilHelper.GetCustomAttributeOfType(t, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute)) == null))
                {
                    if (CecilHelper.IsDelegate(fieldDefinition))
                        _Joinpoints.Add(new FieldJoinpoint(fieldDefinition, JoinpointKinds.field_delegates | JoinpointKinds.declaration));
                    else
                        _Joinpoints.Add(new FieldJoinpoint(fieldDefinition, JoinpointKinds.fields | JoinpointKinds.declaration));
                }
            }

            if (typeDefinition.HasProperties)
            {
                foreach (var propertyDefinition in typeDefinition.Properties)
                    _Joinpoints.Add(new PropertyJoinpoint(propertyDefinition, JoinpointKinds.properties | JoinpointKinds.declaration));
            }

            if (typeDefinition.HasMethods)
            {
                foreach (var method in typeDefinition.Methods)
                    _VisitMethod(method);
            }
        }

        void _VisitMethod(MethodDefinition methodDefinition)
        {
            Joinpoint joinpoint = null;
            var methodCategory = CecilHelper.GetMethodCategory(methodDefinition);
            var compilerAttribute = CecilHelper.GetCustomAttributeOfType(methodDefinition, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute));
            if (compilerAttribute == null || methodCategory == MethodCategories.PropertyGetMethod || methodCategory == MethodCategories.PropertySetMethod)
            {
                switch (methodCategory)
                {
                    case MethodCategories.Method:
                    case MethodCategories.Operator:
                        joinpoint = new MethodJoinpoint(methodDefinition, JoinpointKinds.methods | JoinpointKinds.declaration);
                        _Joinpoints.Add(joinpoint);
                        if (methodDefinition.HasBody)
                        {
                            joinpoint = new MethodJoinpoint(methodDefinition, JoinpointKinds.methods | JoinpointKinds.body);
                            _Joinpoints.Add(joinpoint);
                        }

                        break;
                    case MethodCategories.Constructor:
                        joinpoint = new MethodJoinpoint(methodDefinition, JoinpointKinds.constructors | JoinpointKinds.declaration);
                        _Joinpoints.Add(joinpoint);
                        if (methodDefinition.HasBody)
                        {
                            joinpoint = new MethodJoinpoint(methodDefinition, JoinpointKinds.constructors | JoinpointKinds.body);
                            _Joinpoints.Add(joinpoint);
                        }

                        break;
                    case MethodCategories.PropertySetMethod:
                        var property = CecilHelper.GetPropertyFromGetOrSetMethod(methodDefinition);
                        joinpoint = new MethodJoinpoint(property, methodDefinition, JoinpointKinds.properties | JoinpointKinds.set | JoinpointKinds.body);
                        _Joinpoints.Add(joinpoint);
                        break;
                    case MethodCategories.PropertyGetMethod:
                        property = CecilHelper.GetPropertyFromGetOrSetMethod(methodDefinition);
                        joinpoint = new MethodJoinpoint(property, methodDefinition, JoinpointKinds.properties | JoinpointKinds.get | JoinpointKinds.body);
                        _Joinpoints.Add(joinpoint);
                        break;
                    case MethodCategories.EventAddMethod:
                        var @event = CecilHelper.GetEventFromAddorRemoveMethod(methodDefinition);
                        joinpoint = new MethodJoinpoint(@event, methodDefinition, JoinpointKinds.events | JoinpointKinds.add | JoinpointKinds.body);
                        _Joinpoints.Add(joinpoint);
                        break;
                    case MethodCategories.EventRemoveMethod:
                        @event = CecilHelper.GetEventFromAddorRemoveMethod(methodDefinition);
                        joinpoint = new MethodJoinpoint(@event, methodDefinition, JoinpointKinds.events | JoinpointKinds.remove | JoinpointKinds.body);
                        _Joinpoints.Add(joinpoint);
                        break;
                    default:
                        throw ErrorFactory.GetException("UndefinedMethodDefinition", methodDefinition.FullName);
                }
            }
        }

        void _VisitMethodBody(MethodDefinition methodDefinition)
        {
            if (!methodDefinition.HasBody)
                return;
            var ilTree = ILTree.Create(methodDefinition.Body);
            foreach (var ilNode in ilTree.ILNodes)
            {
                switch (ilNode.OpCodeData.OpCodeType)
                {
                    case OpCodeTypes.Call:
                        _VisitCalledMethod(ilNode);
                        break;
                    case OpCodeTypes.Throw:
                        _VisitThrow(ilNode);
                        break;
                    case OpCodeTypes.New:
                        _VisitConstructorCall(ilNode);
                        break;
                    case OpCodeTypes.Ld | OpCodeTypes.Field:
                        _VisitFieldGet(ilNode);
                        break;
                    case OpCodeTypes.St | OpCodeTypes.Field:
                        _VisitFieldSet(ilNode);
                        break;
                }
            }
        }

        void _VisitCalledMethod(ILTree.ILNode ilNode)
        {
            var calledMethod = ((MethodReference)ilNode.Operand).Resolve();
            IMemberDefinition jointpointMember = null;
            jointpointMember = CecilHelper.GetEventFromAddorRemoveMethod(calledMethod);
            if (jointpointMember == null)
            {
                jointpointMember = CecilHelper.GetPropertyFromGetOrSetMethod(calledMethod);
                if (jointpointMember == null)
                    jointpointMember = calledMethod;
            }

            var definedMethod = _Joinpoints.GetJointpointMembers(JoinpointKinds.declaration, (t, m) => t.FullName == jointpointMember.FullName).FirstOrDefault();
            if (definedMethod != null)
            {
                _AddDefinedCallMethod(ilNode);
                return;
            }

            if (CecilHelper.IsDelegate(((MethodReference)ilNode.Operand).Resolve().DeclaringType))
            {
                _AddEvenOrDelegateCallMethod(ilNode);
                return;
            }
        }

        void _AddDefinedCallMethod(ILTree.ILNode ilNode)
        {
            Instruction instruction = ilNode.Instruction;
            MethodDefinition calledMethod = CecilHelper.Resolve((MethodReference)instruction.Operand);
            if (calledMethod.DeclaringType.IsPrimitive)
                return;
            if (calledMethod.SemanticsAttributes != MethodSemanticsAttributes.None)
            {
                switch (calledMethod.SemanticsAttributes)
                {
                    case MethodSemanticsAttributes.Setter:
                        var property = CecilHelper.GetPropertyFromGetOrSetMethod(calledMethod);
                        _Joinpoints.Add(new InstructionJoinpoint(ilNode.MethodBody.Method, property, instruction, JoinpointKinds.properties | JoinpointKinds.set));
                        break;
                    case MethodSemanticsAttributes.Getter:
                        property = CecilHelper.GetPropertyFromGetOrSetMethod(calledMethod);
                        _Joinpoints.Add(new InstructionJoinpoint(ilNode.MethodBody.Method, property, instruction, JoinpointKinds.properties | JoinpointKinds.get));
                        break;
                    case MethodSemanticsAttributes.AddOn:
                        var @event = CecilHelper.GetEventFromAddorRemoveMethod(calledMethod);
                        _Joinpoints.Add(new InstructionJoinpoint(ilNode.MethodBody.Method, @event, instruction, JoinpointKinds.events | JoinpointKinds.add));
                        break;
                    case MethodSemanticsAttributes.RemoveOn:
                        @event = CecilHelper.GetEventFromAddorRemoveMethod(calledMethod);
                        _Joinpoints.Add(new InstructionJoinpoint(ilNode.MethodBody.Method, @event, instruction, JoinpointKinds.events | JoinpointKinds.remove));
                        break;
                    case MethodSemanticsAttributes.Other:
                    case MethodSemanticsAttributes.Fire:
                    default:
                        throw ErrorFactory.GetException("UndefinedSemanticAttributes", Enum.GetName(typeof(MethodSemanticsAttributes), calledMethod.SemanticsAttributes), calledMethod.FullName);
                }
            }
            else
            {
                var joinpointType = JoinpointKinds.methods | JoinpointKinds.call;
                IMemberDefinition invokedMemberDefinition = calledMethod;
                if (joinpointType == (JoinpointKinds.methods | JoinpointKinds.call) && calledMethod.IsConstructor)
                    joinpointType = JoinpointKinds.constructors | JoinpointKinds.call;
                _Joinpoints.Add(new InstructionJoinpoint(ilNode.MethodBody.Method, invokedMemberDefinition, ilNode.Instruction, joinpointType));
            }
        }

        void _AddEvenOrDelegateCallMethod(ILTree.ILNode ilNode)
        {
            var instruction = ilNode.Instruction;
            MethodDefinition calledMethod = CecilHelper.Resolve((MethodReference)instruction.Operand);
            if (calledMethod.Name == "Invoke")
            {
                var ldfld = ilNode.DataBlock.ILNodes.Where(t => t.OpCodeData.OpCodeValue == OpCodeValues.Ldfld).FirstOrDefault();
                if (ldfld != null)
                {
                    var field = ((FieldReference)ldfld.Operand).Resolve();
                    if (CecilHelper.IsDelegate(field))
                        _Joinpoints.Add(new InstructionJoinpoint(ilNode.MethodBody.Method, field, ilNode.Instruction, JoinpointKinds.field_delegates | JoinpointKinds.call));
                    if (CecilHelper.IsFieldEvent(field))
                    {
                        var @event = CecilHelper.GetEvent(field);
                        _Joinpoints.Add(new InstructionJoinpoint(ilNode.MethodBody.Method, @event, ilNode.Instruction, JoinpointKinds.events | JoinpointKinds.call));
                    }
                }
            }
        }

        void _VisitThrow(ILTree.ILNode ilNode)
        {
            TypeDefinition typeDefinition = null;
            switch (ilNode.OpCodeData.OpCodeValue)
            {
                case OpCodeValues.Throw:
                    typeDefinition = CecilHelper.GetConsumedType(ilNode);
                    break;
                case OpCodeValues.Rethrow:
                    var catchClause = CecilHelper.GetExceptionHandler(ilNode.MethodBody, ilNode.Instruction, ExceptionHandlerType.Catch);
                    typeDefinition = CecilHelper.Resolve(catchClause.CatchType, ilNode.Instruction);
                    break;
            }

            _Joinpoints.Add(new InstructionJoinpoint(ilNode.MethodBody.Method, typeDefinition, ilNode.Instruction, JoinpointKinds.exceptions | JoinpointKinds.@throw));
        }

        void _VisitFieldGet(ILTree.ILNode ilNode)
        {
            if (CecilHelper.IsFieldEvent((FieldReference)ilNode.Instruction.Operand) || CecilHelper.IsDelegate((FieldReference)ilNode.Instruction.Operand))
                return;
            if (_Joinpoints.GetFields(JoinpointKinds.fields | JoinpointKinds.declaration, (f, m) => f.FullName == ((FieldReference)ilNode.Instruction.Operand).Resolve().FullName).FirstOrDefault() == null)
                return;
            _Joinpoints.Add(new InstructionJoinpoint(ilNode.MethodBody.Method, ((FieldReference)ilNode.Instruction.Operand).Resolve(), ilNode.Instruction, JoinpointKinds.fields | JoinpointKinds.get));
        }

        void _VisitFieldSet(ILTree.ILNode ilNode)
        {
            if (CecilHelper.IsDelegate((FieldReference)ilNode.Instruction.Operand))
            {
                var previousILNode = ilNode.GetLowestPreviousNodes().FirstOrDefault();
                if (previousILNode.OpCode == OpCodes.Newobj)
                {
                    _Joinpoints.Add(new InstructionJoinpoint(ilNode.MethodBody.Method, ((FieldReference)ilNode.Instruction.Operand).Resolve(), ilNode.Instruction, JoinpointKinds.field_delegates | JoinpointKinds.add));
                    return;
                }

                if (previousILNode.OpCode == OpCodes.Ldnull)
                {
                    _Joinpoints.Add(new InstructionJoinpoint(ilNode.MethodBody.Method, ((FieldReference)ilNode.Instruction.Operand).Resolve(), ilNode.Instruction, JoinpointKinds.field_delegates | JoinpointKinds.remove));
                    return;
                }

                if (previousILNode.OpCode == OpCodes.Castclass)
                {
                    previousILNode = previousILNode.GetLowestPreviousNodes().FirstOrDefault();
                    if (previousILNode.OpCode == OpCodes.Call && ((MethodReference)previousILNode.Operand).Name == "Combine")
                    {
                        _Joinpoints.Add(new InstructionJoinpoint(ilNode.MethodBody.Method, ((FieldReference)ilNode.Instruction.Operand).Resolve(), ilNode.Instruction, JoinpointKinds.field_delegates | JoinpointKinds.add));
                        return;
                    }

                    if (previousILNode.OpCode == OpCodes.Call && ((MethodReference)previousILNode.Operand).Name == "Remove")
                    {
                        _Joinpoints.Add(new InstructionJoinpoint(ilNode.MethodBody.Method, ((FieldReference)ilNode.Instruction.Operand).Resolve(), ilNode.Instruction, JoinpointKinds.field_delegates | JoinpointKinds.remove));
                        return;
                    }
                }

                return;
            }

            if (CecilHelper.IsFieldEvent((FieldReference)ilNode.Instruction.Operand))
                return;
            if (_Joinpoints.GetFields(JoinpointKinds.fields | JoinpointKinds.declaration, (f, m) => f.FullName == ((FieldReference)ilNode.Instruction.Operand).Resolve().FullName).FirstOrDefault() == null)
                return;
            _Joinpoints.Add(new InstructionJoinpoint(ilNode.MethodBody.Method, ((FieldReference)ilNode.Instruction.Operand).Resolve(), ilNode.Instruction, JoinpointKinds.fields | JoinpointKinds.set));
        }

        void _VisitConstructorCall(ILTree.ILNode ilNode)
        {
            if (((MethodReference)ilNode.Instruction.Operand).DeclaringType.IsArray)
                return;
            var method = ((MethodReference)ilNode.Instruction.Operand).Resolve();
            if (_Joinpoints.GetConstructors(JoinpointKinds.constructors | JoinpointKinds.declaration, (f, m) => f.FullName == method.FullName).FirstOrDefault() == null)
                return;
            TypeDefinition newTypeDef = CecilHelper.Resolve(((MethodReference)ilNode.Instruction.Operand).DeclaringType);
            if (CecilHelper.IsDelegate(newTypeDef))
                return;
            _Joinpoints.Add(new InstructionJoinpoint(ilNode.MethodBody.Method, CecilHelper.Resolve((MethodReference)ilNode.Instruction.Operand), ilNode.Instruction, JoinpointKinds.constructors | JoinpointKinds.call));
        }
    }
}