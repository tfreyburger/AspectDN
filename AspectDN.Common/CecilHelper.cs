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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CecilCollection = Mono.Collections.Generic;
using Ref = System.Reflection;
using System.Runtime.CompilerServices;
using Foundation.Common.Error;

namespace AspectDN.Common
{
    internal static class CecilHelper
    {
        static IEnumerable<string> _OverloadedOps;
        static List<string> _BasicTypes;
        internal static IEnumerable<string> OverloadedOps
        {
            get
            {
                if (_OverloadedOps == null)
                {
                    _OverloadedOps = new string[]{"op_UnaryPlus", "op_UnaryNegation ", "op_Increment", "op_Decrement", "op_LogicalNot", "op_Addition", "op_Subtraction", "op_Multiply", "op_Division", "op_BitwiseAnd", "op_BitwiseOr", "op_ExclusiveOr", "op_Equality", "op_Inequality", "op_LessThan", "op_GreaterThan", "op_LessThanOrEqual", "op_GreaterThanOrEqual", "op_LeftShift", "op_RightShift", "op_Modulus", "op_Implicit", "op_True", "op_False", };
                }

                return _OverloadedOps;
            }
        }

        static List<string> _BuiltInTypes
        {
            get
            {
                if (_BasicTypes == null)
                {
                    _BasicTypes = new List<string>(new string[]{typeof(void).ToString(), "System.Boolean", "System.Byte", "System.SByte", "System.Decimal", "System.Double", "System.Single", "System.Int32", "System.UInt32", "System.Int64", "System.UInt64", "System.Object", "System.Int16", "System.UInt16", "System.String"});
                }

                return _BasicTypes;
            }
        }

        public static bool IsAssemblyInGAC(string assemblyFullName)
        {
            try
            {
                return IsAssemblyInGAC(Ref.Assembly.ReflectionOnlyLoad(assemblyFullName));
            }
            catch
            {
                return false;
            }
        }

        public static bool IsAssemblyInGAC(Ref.Assembly assembly)
        {
            return assembly.GlobalAssemblyCache;
        }

        static internal bool IsBaseTypeNullOrEmpty(TypeDefinition typeDefinition)
        {
            if (typeDefinition.BaseType == null)
                return true;
            return IsObjectOrValueTypeObject(typeDefinition.BaseType);
        }

        static internal bool IsObjectOrValueTypeObject(TypeReference type)
        {
            if (type.IsValueType)
                return type.FullName == typeof(System.ValueType).FullName;
            else
                return type.FullName == typeof(System.Object).FullName;
        }

        internal static IEnumerable<AssemblyFile> GetAssemblies(SourceFileOptions sourceFileOptions)
        {
            var assemblyFiles = new List<AssemblyFile>();
            foreach (string searchPattern in sourceFileOptions.SearchPatterns)
            {
                foreach (FileInfo file in sourceFileOptions.SourceDirectory.GetFiles(searchPattern, SearchOption.AllDirectories))
                {
                    if (sourceFileOptions.ExcludedSourceFiles.Any(t => t == file.FullName) || sourceFileOptions.ExcludedDirectories.Any(t => file.FullName.StartsWith(t.FullName)))
                        continue;
                    if (!assemblyFiles.Exists(t => t.FullFileName == file.FullName))
                    {
                        var readerParameters = new ReaderParameters(ReadingMode.Immediate);
                        var assemblyResolver = new AspectDNAssemblyResolver();
                        assemblyResolver.AddSearchDirectory(sourceFileOptions.SourceDirectory.FullName);
                        assemblyResolver.AddSearchDirectory("*.aspdn");
                        readerParameters.ReadWrite = false;
                        readerParameters.AssemblyResolver = assemblyResolver;
                        readerParameters.ThrowIfSymbolsAreNotMatching = false;
                        readerParameters.ReadSymbols = File.Exists(Path.ChangeExtension(file.FullName, "pdb"));
                        readerParameters.SymbolReaderProvider = null;
                        if (readerParameters.ReadSymbols)
                            readerParameters.SymbolReaderProvider = new Mono.Cecil.Pdb.NativePdbReaderProvider();
                        using (var assemblyDefinition = CecilHelper.GetAssembly(file.FullName, false))
                            assemblyFiles.Add(new AssemblyDefinitionFile(file.FullName, assemblyDefinition.FullName, assemblyDefinition.MainModule.AssemblyReferences.Select(t => t.FullName), readerParameters));
                    }
                }
            }

            return new AssemblySorter(assemblyFiles).Sort();
        }

        internal static AssemblyDefinition GetAssembly(string assemblyPathName, bool readWrite, ReaderParameters readerParameters = null)
        {
            if (readerParameters == null)
            {
                readerParameters = new ReaderParameters(ReadingMode.Immediate);
                var assemblyResolver = new AspectDNAssemblyResolver();
                assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPathName));
                assemblyResolver.AddSearchDirectory("*.aspdn");
                readerParameters.ReadWrite = readWrite;
                readerParameters.AssemblyResolver = assemblyResolver;
                readerParameters.ThrowIfSymbolsAreNotMatching = false;
                readerParameters.ReadSymbols = File.Exists(Path.ChangeExtension(assemblyPathName, "pdb"));
                readerParameters.SymbolReaderProvider = null;
                if (readerParameters.ReadSymbols)
                    readerParameters.SymbolReaderProvider = new Mono.Cecil.Pdb.NativePdbReaderProvider();
            }

            var assembly = AssemblyDefinition.ReadAssembly(assemblyPathName, readerParameters);
            return assembly;
        }

        internal static AssemblyDefinition GetAssembly(byte[] bytes, bool readWrite, ReaderParameters readerParameters)
        {
            var stream = new MemoryStream(bytes);
            stream.Position = 0;
            var assembly = AssemblyDefinition.ReadAssembly(stream, readerParameters);
            return assembly;
        }

        internal static Instruction GetIlObjectCtor(IEnumerable<Instruction> instructions, TypeDefinition typeCtor)
        {
            var ilObjectCtor = instructions.First(t => (t.OpCode == OpCodes.Call || t.OpCode == OpCodes.Callvirt) && t.Operand is MethodReference && ((MethodReference)t.Operand).DeclaringType.FullName == typeCtor.FullName && ((MethodReference)t.Operand).Name == ".ctor");
            return ilObjectCtor;
        }

        internal static bool IsBuildInType(TypeDefinition typeDefintion)
        {
            return _BuiltInTypes.Count(t => t == typeDefintion.FullName) > 0;
        }

        internal static (int Pop, int Push, ExceptionHandler handler) GetStackEffect(MethodBody methodBody, Instruction instruction)
        {
            int pop = 0;
            int push = 0;
            ExceptionHandler handler = null;
            var opIL = OpCodeDatas.Get(instruction.OpCode);
            switch (opIL.Pop)
            {
                case -1:
                    switch (opIL.OpCodeValue)
                    {
                        case OpCodeValues.Ret:
                            pop = methodBody.Method.ReturnType.ToString() != typeof(void).ToString() ? 1 : 0;
                            break;
                        case OpCodeValues.Calli:
                        case OpCodeValues.Call:
                        case OpCodeValues.Callvirt:
                            var calledMethod = (MethodReference)instruction.Operand;
                            if (calledMethod.DeclaringType.IsArray)
                                pop = calledMethod.Parameters.Count + 1;
                            else
                                pop = calledMethod.Parameters.Count + (CecilHelper.Resolve(calledMethod).IsStatic ? 0 : 1);
                            break;
                        case OpCodeValues.Newobj:
                            pop = ((MethodReference)instruction.Operand).Parameters.Count;
                            break;
                        default:
                            throw ErrorFactory.GetException("UndefinedOpCodeValue", Enum.GetName(typeof(OpCodeValues), opIL.OpCodeValue));
                    }

                    break;
                default:
                    pop = opIL.Pop;
                    break;
            }

            switch (opIL.Push)
            {
                case -1:
                    switch (opIL.OpCodeValue)
                    {
                        case OpCodeValues.Call:
                        case OpCodeValues.Callvirt:
                            push = ((MethodReference)instruction.Operand).ReturnType.ToString() == typeof(void).ToString() ? 0 : 1;
                            break;
                        default:
                            throw ErrorFactory.GetException("UndefinedOpCodeValue", Enum.GetName(typeof(OpCodeValues), opIL.OpCodeValue));
                    }

                    break;
                default:
                    push = opIL.Push;
                    break;
            }

            if (methodBody.HasExceptionHandlers)
            {
                foreach (var exception in methodBody.ExceptionHandlers)
                {
                    var index = methodBody.Instructions.IndexOf(instruction);
                    if (index == methodBody.Instructions.IndexOf(exception.HandlerStart) || index == (exception.FilterStart != null ? methodBody.Instructions.IndexOf(exception.FilterStart) : -1))
                    {
                        push += 1;
                        handler = exception;
                        break;
                    }
                }
            }

            return (pop, push, handler);
        }

        internal static bool IsInTryHandler(MethodBody methodBody, Instruction instruction)
        {
            return methodBody.ExceptionHandlers.Where(t => t.TryStart.Offset <= instruction.Offset && t.TryEnd.Offset >= instruction.Offset).FirstOrDefault() != null;
        }

        internal static bool IsInCatchHandler(MethodBody methodBody, Instruction instruction)
        {
            return methodBody.ExceptionHandlers.Where(t => t.HandlerEnd.Offset <= instruction.Offset && t.HandlerEnd.Offset >= instruction.Offset && t.HandlerType == ExceptionHandlerType.Catch).FirstOrDefault() != null;
        }

        internal static bool IsInFinallyHandler(MethodBody methodBody, Instruction instruction)
        {
            return methodBody.ExceptionHandlers.Where(t => t.HandlerEnd.Offset <= instruction.Offset && t.HandlerEnd.Offset >= instruction.Offset && t.HandlerType == ExceptionHandlerType.Finally).FirstOrDefault() != null;
        }

        internal static ExceptionHandler GetExceptionHandler(MethodBody methodBody, Instruction instruction, params ExceptionHandlerType[] types)
        {
            ExceptionHandler handler = null;
            if (types == null)
                handler = methodBody.ExceptionHandlers.Where(t => t.TryStart.Offset <= instruction.Offset && t.TryEnd.Offset >= instruction.Offset).FirstOrDefault();
            else
                handler = methodBody.ExceptionHandlers.Where(t => t.HandlerEnd.Offset >= instruction.Offset && t.HandlerStart.Offset <= instruction.Offset && types.Contains(t.HandlerType)).FirstOrDefault();
            return handler;
        }

        internal static MethodDefinition Resolve(MethodReference methodReference)
        {
            MethodDefinition method = null;
            method = methodReference.Resolve();
            if (method == null)
            {
                throw new NotImplementedException($"Method {methodReference.FullName} not resolved");
            }

            return method;
        }

        internal static TypeDefinition Resolve(TypeReference typeReference)
        {
            TypeDefinition typeDefinition = null;
            if (typeReference is GenericInstanceType)
                typeDefinition = Resolve(((GenericInstanceType)typeReference).ElementType);
            else
            {
                if (typeReference is GenericParameter)
                {
                    throw new NotImplementedException("type reference CelcilHper TypeDefinition Resolve(TypeReference typeReference)");
                }

                typeDefinition = typeReference.Resolve();
            }

            if (typeDefinition == null)
                throw new NotImplementedException($"Unable to resolve {typeDefinition.FullName}");
            return typeDefinition;
        }

        internal static TypeDefinition Resolve(TypeReference typeReference, Instruction instruction)
        {
            if (typeReference is GenericParameter)
            {
                var genericParameter = (GenericParameter)typeReference;
                var method = (MethodReference)instruction.Operand;
                if (genericParameter.Owner.GenericParameterType == GenericParameterType.Method)
                {
                    typeReference = ((GenericInstanceMethod)method).GenericArguments[genericParameter.Position];
                }
                else
                {
                    typeReference = ((GenericInstanceType)method.DeclaringType).GenericArguments[genericParameter.Position];
                }

                return Resolve(typeReference);
            }
            else
                return Resolve(typeReference);
        }

        internal static bool IsObjectConstructor(MethodDefinition method)
        {
            if (method.IsConstructor && method.DeclaringType.FullName == typeof(System.Object).FullName)
                return true;
            return false;
        }

        internal static MethodCategories GetMethodCategory(MethodDefinition methodDefinition)
        {
            if (methodDefinition.IsSpecialName)
            {
                if (methodDefinition.IsConstructor)
                    return MethodCategories.Constructor;
                else
                {
                    if (methodDefinition.IsSetter)
                        return MethodCategories.PropertySetMethod;
                    else
                    {
                        if (methodDefinition.IsGetter)
                            return MethodCategories.PropertyGetMethod;
                        else
                        {
                            if (methodDefinition.IsAddOn)
                                return MethodCategories.EventAddMethod;
                            else
                            {
                                if (methodDefinition.IsRemoveOn)
                                    return MethodCategories.EventRemoveMethod;
                                else
                                {
                                    if (methodDefinition.Name.StartsWith("op_"))
                                        return MethodCategories.Operator;
                                }
                            }
                        }
                    }
                }
            }

            return MethodCategories.Method;
        }

        internal static bool CanBeDelegateOrEventInvocation(MethodDefinition method)
        {
            return CecilHelper.IsBaseTypeOf(typeof(System.MulticastDelegate), CecilHelper.Resolve(method.DeclaringType)) || method.DeclaringType.FullName == typeof(Delegate).FullName;
        }

        internal static TypeDefinition GetStackedType(Instruction instruction, MethodDefinition method)
        {
            TypeDefinition typeDefinition = null;
            var opCodeData = OpCodeDatas.Get(instruction.OpCode);
            switch (opCodeData.OpCodeType)
            {
                case OpCodeTypes.Elem | OpCodeTypes.Ld:
                    typeDefinition = CecilHelper.Resolve(((TypeReference)instruction.Operand), instruction);
                    break;
                case OpCodeTypes.Call:
                    var returnType = ((MethodReference)instruction.Operand).ReturnType;
                    typeDefinition = CecilHelper.Resolve(returnType, instruction);
                    break;
                case OpCodeTypes.New:
                    var type = ((MethodReference)instruction.Operand).DeclaringType.Resolve();
                    typeDefinition = CecilHelper.Resolve((TypeReference)type, instruction);
                    break;
                default:
                    var member = CecilHelper.GetMember(instruction, method);
                    if (member == null)
                        throw ErrorFactory.GetException("UndefinedMemberOrVarOrArg", instruction.ToString(), method.FullName);
                    switch (member)
                    {
                        case VariableReference variableReference:
                            typeDefinition = CecilHelper.Resolve(variableReference.VariableType, instruction);
                            break;
                        case TypeReference typeReference:
                            typeDefinition = CecilHelper.Resolve(typeReference, instruction);
                            break;
                        case ParameterDefinition paremeter:
                            typeDefinition = CecilHelper.Resolve(paremeter.ParameterType, instruction);
                            break;
                        default:
                            throw ErrorFactory.GetException("UndefinedMemberOrVarOrArg", instruction.ToString(), method.FullName);
                    }

                    break;
            }

            return typeDefinition;
        }

        internal static TypeDefinition GetConsumedType(ILTree.ILNode ilNode)
        {
            var dataflow = ilNode.DataBlock;
            var ilInitValueStmt = ilNode;
            while ((ilInitValueStmt = ilInitValueStmt.GetLowestPreviousNodes().FirstOrDefault()) != null && ilInitValueStmt.OpCodeData.Push == 0)
                ;
            if (ilInitValueStmt == null)
                throw ErrorFactory.GetException("NoILLoadTypeFound", ilNode.Instruction.ToString(), ilNode.Method.FullName);
            return GetStackedType(ilInitValueStmt.Instruction, ilNode.Method);
        }

        internal static object GetConsumedMember(ILTree.ILNode ilNode)
        {
            var dataflow = ilNode.DataBlock;
            var ilInitValueStmt = ilNode;
            while ((ilInitValueStmt = ilInitValueStmt.GetLowestPreviousNodes().FirstOrDefault()) != null && ilInitValueStmt.OpCodeData.Push == 0)
                ;
            if (ilInitValueStmt == null)
                throw ErrorFactory.GetException("NoILLoadTypeFound", ilNode.Instruction.ToString(), ilNode.Method.FullName);
            return GetMember(ilInitValueStmt.Instruction, ilNode.Method);
        }

        internal static bool MemberInTypeExists(TypeDefinition typeDefinition, string memberName, CecilCollection.Collection<ParameterDefinition> @params = null)
        {
            return GetMemberFromType(typeDefinition, memberName, @params) != null;
        }

        internal static bool IsSimpleNameMember(IMemberDefinition member)
        {
            return member is FieldDefinition || (member is PropertyDefinition && !((PropertyDefinition)member).HasParameters) || member is EventDefinition || member is TypeDefinition;
        }

        internal static IEnumerable<IMemberDefinition> GetTypeMembers(TypeDefinition typeDefinition, string memberName)
        {
            return GetTypeMembers(typeDefinition).Where(t => t.Name == memberName);
        }

        internal static IEnumerable<IMemberDefinition> GetTypeMembers(TypeDefinition typeDefinition, bool specialNameIncluding = true)
        {
            var members = new List<IMemberDefinition>(typeDefinition.Fields);
            members.AddRange(typeDefinition.Properties);
            members.AddRange(typeDefinition.Methods);
            members.AddRange(typeDefinition.Events);
            members.AddRange(typeDefinition.NestedTypes);
            if (!specialNameIncluding)
                members = members.Where(m => !m.IsRuntimeSpecialName && !m.IsSpecialName).ToList();
            return members;
        }

        internal static IMemberDefinition GetMemberFromType(TypeDefinition typeDefinition, string memberName, CecilCollection.Collection<ParameterDefinition> @params = null)
        {
            IMemberDefinition member = null;
            member = typeDefinition.Fields.Where(t => t.Name == memberName).FirstOrDefault();
            if (member != null)
                return member;
            if (@params == null)
                member = typeDefinition.Properties.Where(t => t.Name == memberName).FirstOrDefault();
            else
                member = typeDefinition.Properties.Where(t => t.Name == memberName && CecilHelper.AreParametersEqual(t.Parameters, @params)).FirstOrDefault();
            if (member != null)
                return member;
            if (@params == null)
                member = typeDefinition.Methods.Where(t => t.Name == memberName).FirstOrDefault();
            else
                member = typeDefinition.Methods.Where(t => t.Name == memberName && CecilHelper.AreParametersEqual(t.Parameters, @params)).FirstOrDefault();
            if (member != null)
                return member;
            member = typeDefinition.Events.Where(t => t.Name == memberName).FirstOrDefault();
            if (member != null)
                return member;
            member = typeDefinition.NestedTypes.Where(t => t.Name == memberName).FirstOrDefault();
            if (member != null)
                return member;
            return member;
        }

        internal static TypeDefinition GetTypeDefinition(TypeReference typeRefefence)
        {
            switch (typeRefefence)
            {
                case GenericParameter genericParameter:
                    return GetGenericParameters(genericParameter.Owner)[genericParameter.Position].GetElementType().Resolve();
                case GenericInstanceType genericInstanceType:
                    return genericInstanceType.ElementType.Resolve();
                case TypeReference sourceTypeReference:
                    return sourceTypeReference.Resolve();
                default:
                    throw new NotImplementedException();
            }
        }

        internal static Mono.Collections.Generic.Collection<GenericParameter> GetGenericParameters(IGenericParameterProvider genericParameterProvider)
        {
            switch (genericParameterProvider)
            {
                case TypeReference typeReference:
                    return typeReference.GenericParameters;
                case MethodReference methodReference:
                    return methodReference.GenericParameters;
                default:
                    throw new NotImplementedException();
            }
        }

        internal static Mono.Collections.Generic.Collection<GenericParameter> GetGenericParameters(TypeReference typeReference)
        {
            if (typeReference.GenericParameters.Any())
                return typeReference.GenericParameters;
            else
            {
                typeReference = typeReference.GetElementType();
                if (typeReference.GenericParameters.Any())
                    return typeReference.GenericParameters;
            }

            return new Mono.Collections.Generic.Collection<GenericParameter>();
        }

        internal static IEnumerable<TypeReference> GetGenericArguments(TypeReference typeReference)
        {
            var genericArguments = CecilHelper.GetGenericParameters(typeReference).Cast<TypeReference>();
            if (typeReference is GenericInstanceType)
                genericArguments = ((GenericInstanceType)typeReference).GenericArguments;
            return genericArguments;
        }

        internal static Mono.Collections.Generic.Collection<GenericParameter> GetGenericParameters(MethodReference methodReference)
        {
            if (methodReference.HasGenericParameters)
            {
                if (methodReference.GenericParameters.Any())
                    return methodReference.GenericParameters;
                else
                {
                    methodReference = methodReference.GetElementMethod();
                    if (methodReference.HasGenericParameters)
                    {
                        if (methodReference.GenericParameters.Any())
                            return methodReference.GenericParameters;
                    }
                }
            }

            return null;
        }

        internal static TypeReference GetGenericParameter(TypeReference typeReference, int position)
        {
            return GetGenericParameters(typeReference)[position];
        }

        internal static TypeReference GetGenericParameter(MethodReference methodReference, int position)
        {
            return GetGenericParameters(methodReference)[position];
        }

        internal static TypeDefinition GetHisghestDeclaringType(TypeDefinition typeDefinition, Type adviceInterfaceType)
        {
            var declarantType = typeDefinition;
            while (declarantType.DeclaringType != null && !declarantType.DeclaringType.Interfaces.Any(t => t.InterfaceType.FullName == adviceInterfaceType.FullName))
                declarantType = declarantType.DeclaringType;
            return declarantType;
        }

        internal static TypeDefinition GetNestedType(TypeDefinition fromType, string nestedfullname)
        {
            nestedfullname = nestedfullname.Substring(nestedfullname.IndexOf("/") + 1);
            if (fromType.Name == nestedfullname)
                return fromType;
            var nestedFullNames = nestedfullname.Split('/');
            var nestedType = fromType;
            for (int i = 0; i < nestedFullNames.Length && nestedType != null; i++)
                nestedType = nestedType.NestedTypes.FirstOrDefault(t => t.Name == nestedFullNames[i]);
            return nestedType;
        }

        internal static object GetEventInvokedMember(ILTree.ILNode ilNode)
        {
            object consumedMember = null;
            var consumedDataBlock = GetConsumedDataBlocks(ilNode).FirstOrDefault();
            if (consumedDataBlock != null)
            {
                var consumedIlNode = consumedDataBlock.ILNodes.Last();
                switch (consumedIlNode.OpCodeData.OpCodeValue)
                {
                    case OpCodeValues.Ldfld:
                        if (consumedIlNode.Operand is FieldReference && (CecilHelper.IsFieldEvent((FieldReference)consumedIlNode.Operand)))
                            consumedMember = consumedIlNode.Operand;
                        break;
                    default:
                        break;
                }
            }

            return consumedMember;
        }

        internal static object GetDelegateInvokedMember(ILTree.ILNode ilNode)
        {
            object consumedMember = null;
            var consumedDataBlock = GetConsumedDataBlocks(ilNode).FirstOrDefault();
            if (consumedDataBlock != null)
            {
                var consumedIlNode = consumedDataBlock.ILNodes.Last();
                switch (consumedIlNode.OpCodeData.OpCodeValue)
                {
                    case OpCodeValues.Ldfld:
                        if (consumedIlNode.Operand is FieldReference && (CecilHelper.IsDelegate((FieldReference)consumedIlNode.Operand)))
                            consumedMember = consumedIlNode.Operand;
                        break;
                    case OpCodeValues.Newobj:
                        if (consumedIlNode.Operand is MethodReference && (CecilHelper.IsDelegate(((MethodReference)consumedIlNode.Operand).ReturnType)))
                        {
                            throw new NotImplementedException();
                        }

                        break;
                    default:
                        break;
                }
            }

            return consumedMember;
        }

        internal static IEnumerable<ILBlockNode> GetConsumedDataBlocks(ILTree.ILNode iLNode)
        {
            var stackComsumption = GetStackEffect(iLNode.MethodBody, iLNode.Instruction);
            if (stackComsumption.Pop == 0)
                return new ILBlockNode[0];
            var blocks = new List<ILBlockNode>();
            var from = iLNode.GetLowestPreviousNodes().FirstOrDefault();
            if (from != null)
            {
                var current = from;
                while (current != null)
                {
                    stackComsumption = GetStackEffect(from.MethodBody, from.Instruction);
                    if (stackComsumption.Pop == 0)
                    {
                        blocks.Insert(0, new ILBlockNode(from.ILTree, current, from));
                        from = current.GetLowestPreviousNodes().FirstOrDefault();
                    }

                    current = current.GetLowestPreviousNodes().FirstOrDefault();
                }

                if (from != null)
                    blocks.Insert(0, new ILBlockNode(from.ILTree, iLNode.DataBlock.ILNodes.FirstOrDefault(), from));
            }

            return blocks;
        }

        internal static ILTree.ILNode GetStLocILNode(ILTree.ILNode from, string variableName)
        {
            var stLocFound = false;
            var next = from.GetLowestPreviousNodes().FirstOrDefault();
            while (next != null && !stLocFound)
            {
                stLocFound = ((OpCodeDatas.Get(next.OpCode).OpCodeType & OpCodeTypes.LocVar) == OpCodeTypes.LocVar && (OpCodeDatas.Get(next.OpCode).OpCodeType & OpCodeTypes.St) == OpCodeTypes.St && GetVariable(next).ToString() == variableName);
                from = next;
                next = from.GetLowestPreviousNodes().FirstOrDefault();
            }

            return from;
        }

        internal static object GetMember(ILTree.ILNode ilNode)
        {
            return GetMember(ilNode.Instruction, ilNode.Method);
        }

        internal static object GetMember(Instruction instruction, MethodDefinition method)
        {
            var opCodeData = OpCodeDatas.Get(instruction.OpCode);
            switch (opCodeData.OpCodeType)
            {
                case OpCodeTypes.Arg | OpCodeTypes.Ld:
                case OpCodeTypes.Arg | OpCodeTypes.St:
                    return GetArgMember(method, opCodeData, instruction.Operand);
                case OpCodeTypes.LocVar | OpCodeTypes.Ld:
                case OpCodeTypes.LocVar | OpCodeTypes.St:
                    return GetVariable(method.Body, opCodeData, instruction.Operand);
                case OpCodeTypes.Field | OpCodeTypes.Ld:
                case OpCodeTypes.Field | OpCodeTypes.St:
                    return GetField(method.Body, opCodeData, instruction.Operand);
                case OpCodeTypes.New:
                    return ((MethodReference)instruction.Operand).ReturnType;
                case OpCodeTypes.Call:
                    return ((MethodReference)instruction.Operand).ReturnType;
            }

            throw ErrorFactory.GetException("UndefinedMemberOrVarOrArg", instruction.ToString(), method.FullName);
        }

        internal static IEnumerable<TypeDefinition> GlobalCompilerGeneratedTypes(IEnumerable<Instruction> instructions)
        {
            return instructions.Where(t => t.Operand is MemberReference && ((MemberReference)t.Operand).DeclaringType is TypeDefinition && CecilHelper.HasCustomAttributesOfType(((MemberReference)t.Operand).DeclaringType, typeof(CompilerGeneratedAttribute)) && string.IsNullOrEmpty(((MemberReference)t.Operand).DeclaringType.Namespace) && ((MemberReference)t.Operand).DeclaringType.DeclaringType == null).Select(t => (TypeDefinition)((MemberReference)t.Operand).DeclaringType);
        }

        internal static Instruction CloneInstruction(Instruction sourceIL)
        {
            var targetInstruction = sourceIL;
            if (sourceIL.OpCode.OperandType == OperandType.InlineNone)
            {
                targetInstruction = Instruction.Create(sourceIL.OpCode);
            }
            else
            {
                switch (sourceIL.Operand)
                {
                    case TypeReference typeReference:
                        targetInstruction = Instruction.Create(sourceIL.OpCode, typeReference);
                        break;
                    case Mono.Cecil.CallSite callSite:
                        targetInstruction = Instruction.Create(sourceIL.OpCode, callSite);
                        break;
                    case MethodReference methodReference:
                        targetInstruction = Instruction.Create(sourceIL.OpCode, methodReference);
                        break;
                    case FieldReference fieldReference:
                        targetInstruction = Instruction.Create(sourceIL.OpCode, fieldReference);
                        break;
                    case string @string:
                        targetInstruction = Instruction.Create(sourceIL.OpCode, @string);
                        break;
                    case sbyte @sbyte:
                        targetInstruction = Instruction.Create(sourceIL.OpCode, @sbyte);
                        break;
                    case byte @byte:
                        targetInstruction = Instruction.Create(sourceIL.OpCode, @byte);
                        break;
                    case int @int:
                        targetInstruction = Instruction.Create(sourceIL.OpCode, @int);
                        break;
                    case long @long:
                        targetInstruction = Instruction.Create(sourceIL.OpCode, @long);
                        break;
                    case float @float:
                        targetInstruction = Instruction.Create(sourceIL.OpCode, @float);
                        break;
                    case double @double:
                        targetInstruction = Instruction.Create(sourceIL.OpCode, @double);
                        break;
                    case VariableDefinition variable:
                        targetInstruction = Instruction.Create(sourceIL.OpCode, variable);
                        break;
                    case ParameterDefinition parameter:
                        targetInstruction = Instruction.Create(sourceIL.OpCode, parameter);
                        break;
                    case Instruction instruction:
                        targetInstruction = Instruction.Create(sourceIL.OpCode, instruction);
                        break;
                    case Instruction[] instructions:
                        targetInstruction = Instruction.Create(sourceIL.OpCode, instructions);
                        break;
                    default:
                        throw new NotSupportedException($"unable to clone this instruction as it is unknown");
                }
            }

            return targetInstruction;
        }

        internal static IEnumerable<Instruction> OffsetRecalculation(IEnumerable<Instruction> ils)
        {
            var offset = 0;
            foreach (var il in ils)
            {
                il.Offset = offset;
                offset += il.GetSize();
            }

            return ils;
        }

        internal static object GetArgMember(MethodDefinition method, OpCodeData opCodeData, object operand)
        {
            object member = null;
            var index = opCodeData.Index;
            if (index == -1)
                index = ((ParameterDefinition)operand).Index;
            if (method.IsStatic)
                member = method.Parameters[index];
            else
            {
                if (index == 0)
                    member = method.DeclaringType;
                else
                    member = method.Parameters[index - 1];
            }

            return member;
        }

        internal static Instruction GetLoadOrStoreILArgInstruction(bool load, ParameterDefinition parameter, bool targetIsStatic)
        {
            int index = parameter.Index + (!targetIsStatic ? 1 : 0);
            if (load)
            {
                switch (index)
                {
                    case 0:
                        return Instruction.Create(OpCodes.Ldarg_0);
                    case 1:
                        return Instruction.Create(OpCodes.Ldarg_1);
                    case 2:
                        return Instruction.Create(OpCodes.Ldarg_2);
                    case 3:
                        return Instruction.Create(OpCodes.Ldarg_3);
                    default:
                        return Instruction.Create(OpCodes.Ldarga_S, parameter);
                }
            }
            else
                return Instruction.Create(parameter.Index > int.MaxValue ? OpCodes.Starg : OpCodes.Starg_S, parameter);
        }

        internal static VariableDefinition GetVariable(ILTree.ILNode ilNode)
        {
            return GetVariable(ilNode.MethodBody, ilNode.OpCodeData, ilNode.Operand);
        }

        internal static VariableDefinition GetVariable(MethodBody methodBody, OpCodeData opCodeData, object operand)
        {
            VariableDefinition member = null;
            var index = opCodeData.Index;
            if (index != -1)
                member = methodBody.Variables[index];
            else
                member = (VariableDefinition)operand;
            return member;
        }

        internal static string GetVariableName(MethodDefinition method, VariableDefinition variable)
        {
            string name = null;
            if (method.DebugInformation != null)
                method.DebugInformation.TryGetName(variable, out name);
            return name;
        }

        internal static FieldDefinition GetField(MethodBody methodBody, OpCodeData opCodeData, object operand)
        {
            return (FieldDefinition)operand;
        }

        internal static TypeDefinition GetDelegateType(MethodBody methodBody, OpCodeData opCodeData, object operand)
        {
            return CecilHelper.Resolve(((MethodReference)operand).DeclaringType);
        }

        internal static bool ExistsInCoreAssembliy(TypeDefinition typeDefinition)
        {
            string assemblyName = typeDefinition.Module.Assembly.Name.Name;
            return assemblyName == "System" || assemblyName == "System.Core" || assemblyName == "mscorlib";
        }

        internal static bool IsFieldEvent(FieldReference fieldReference)
        {
            return GetEvent(fieldReference) != null;
        }

        internal static EventDefinition GetEvent(FieldReference fieldReference)
        {
            return fieldReference.DeclaringType.Resolve().Events.Where(t => t.FullName == ((FieldReference)fieldReference).FullName).FirstOrDefault();
        }

        internal static bool IsTypeException(TypeReference typeReference)
        {
            return IsBaseTypeOf(typeof(Exception), typeReference.GetElementType().Resolve());
        }

        internal static EventDefinition GetEvent(MethodBody methodBody, OpCodeData opCodeData, object operand)
        {
            if (operand is EventDefinition)
                return ((EventDefinition)operand).Resolve();
            return null;
        }

        internal static EventDefinition GetEvent(MethodDefinition eventMethod)
        {
            var @event = eventMethod.DeclaringType.Events.FirstOrDefault(t => (t.AddMethod != null && t.AddMethod.FullName == eventMethod.FullName) || (t.RemoveMethod != null && t.RemoveMethod.FullName == eventMethod.FullName) || t.OtherMethods.Any(o => o.FullName == eventMethod.FullName));
            return @event;
        }

        internal static PropertyDefinition GetPropertyFromGetOrSetMethod(MethodDefinition methodDefinition)
        {
            return methodDefinition.DeclaringType.Properties.Where(t => t.GetMethod == methodDefinition || t.SetMethod == methodDefinition).FirstOrDefault();
        }

        internal static EventDefinition GetEventFromAddorRemoveMethod(MethodDefinition methodDefinition)
        {
            return methodDefinition.DeclaringType.Events.Where(t => t.AddMethod == methodDefinition || t.RemoveMethod == methodDefinition).FirstOrDefault();
        }

        internal static IEnumerable<Instruction> GetAroundILs(MethodDefinition method)
        {
            return method.Body.Instructions.Where(t => t.OpCode == OpCodes.Call && ((MethodReference)t.Operand).Name == ConcernValues.AroundStatement);
        }

        internal static Instruction GetCtorInstruction(MethodBody methodBody)
        {
            return methodBody.Instructions.Where(t => t.OpCode == OpCodes.Call && t.Operand is MethodReference && ((MethodReference)t.Operand).Name == ".ctor" && ((MethodReference)t.Operand).ReturnType.FullName == typeof(void).FullName).FirstOrDefault();
        }

        internal static Instruction GetReturnLabel(MethodBody methodBody)
        {
            var returnLabel = methodBody.Instructions.Last();
            if (returnLabel.OpCode == OpCodes.Throw)
            {
                return returnLabel.Previous;
            }

            if (OpCodeDatas.Get(methodBody.Instructions.Last().OpCode).OpCodeType == OpCodeTypes.Return && methodBody.Method.ReturnType.FullName != typeof(void).ToString())
            {
                returnLabel = ILTree.Create(methodBody).GetDataBlock(returnLabel.Previous).FirstIL;
            }

            return returnLabel;
        }

        internal static ILBlockNode GetFieldConstantInitILBlock(FieldDefinition field)
        {
            var constructor = field.DeclaringType.Methods.FirstOrDefault(t => t.IsSpecialName && t.IsConstructor);
            if (constructor != null && constructor.HasBody)
            {
                var setFieldIL = constructor.Body.Instructions.FirstOrDefault(t => t.OpCode == OpCodes.Stfld && ((FieldReference)t.Operand).Name == field.Name);
                if (setFieldIL != null)
                {
                    var ctor = GetCtorInstruction(constructor.Body);
                    if (ctor.Offset > setFieldIL.Offset)
                        return ILTree.Create(constructor.Body).GetDataBlock(setFieldIL);
                }
            }

            return null;
        }

        internal static string GetExplainedPropertyName(PropertyDefinition property)
        {
            return property.FullName.Substring(property.FullName.IndexOf("::") + 2);
        }

        internal static string GetFullMemberName(string fullTypeName, string memberName, IEnumerable<TypeReference> parameterTypeReferences = null)
        {
            var parametersName = "";
            if (parameterTypeReferences != null)
            {
                StringBuilder sb = new StringBuilder().Append("(");
                foreach (var typeReference in parameterTypeReferences)
                    sb.Append(sb.Length != 1 ? ";" : "").Append(typeReference.FullName);
                parametersName = sb.Append(")").ToString();
            }

            return $"{fullTypeName}::{memberName}{parametersName}";
        }

        internal static bool IsTypenameEqual(TypeDefinition typeDefinition, Type type)
        {
            return IsTypenameEqual(typeDefinition, type.FullName.Replace('+', '.'));
        }

        internal static bool IsTypenameEqual(TypeDefinition typeDefinition, string normalizedfullTypename)
        {
            return typeDefinition.FullName.Replace("/", ".") == normalizedfullTypename;
        }

        internal static bool IsMemberEqual(IMemberDefinition memberA, IMemberDefinition memberB, bool compareName = false)
        {
            if (memberA.GetType().FullName != memberB.GetType().FullName)
                return false;
            switch (memberA)
            {
                case TypeDefinition typeDefinitionA:
                    return IsTypeReferenceEqual(typeDefinitionA, (TypeDefinition)memberB);
                case FieldDefinition fieldDefinitionA:
                    return IsFieldEqual(fieldDefinitionA, (FieldDefinition)memberB);
                case PropertyDefinition propertyDefinitionA:
                    return IsPropertyEqual(propertyDefinitionA, (PropertyDefinition)memberB);
                case MethodDefinition methodDefinitionA:
                    return IsMethodEqual(methodDefinitionA, (MethodDefinition)memberB);
                case EventDefinition eventDefinitionA:
                    return IsEventEqual(eventDefinitionA, (EventDefinition)memberB);
                default:
                    throw new NotImplementedException();
            }
        }

        internal static bool IsFieldEqual(FieldDefinition fieldA, FieldDefinition fieldB, bool compareName = false)
        {
            if (!IsTypeReferenceEqual(fieldA.FieldType, fieldB.FieldType))
                return false;
            return compareName && fieldA.Name == fieldB.Name;
        }

        internal static bool IsPropertyEqual(PropertyDefinition propertyA, PropertyDefinition propertyB, bool compareName = false)
        {
            if (compareName && propertyA.Name != propertyB.Name)
                return false;
            if (!IsTypeReferenceEqual(propertyA.PropertyType, propertyA.PropertyType))
                return false;
            if (propertyA.Parameters.Count() != propertyB.Parameters.Count())
                return false;
            if (propertyA.HasParameters && !AreParametersEqual(propertyA.Parameters, propertyB.Parameters))
                return false;
            if ((propertyA.GetMethod != null && propertyB.GetMethod == null) || (propertyA.GetMethod == null && propertyB.GetMethod != null))
                return false;
            if ((propertyA.SetMethod != null && propertyB.SetMethod == null) || (propertyA.SetMethod == null && propertyB.SetMethod != null))
                return false;
            return true;
        }

        internal static bool IsEventEqual(EventDefinition eventA, EventDefinition eventB, bool compareName = false)
        {
            if (compareName && eventA.Name != eventB.Name)
                return false;
            if (!IsTypeReferenceEqual(eventA.EventType, eventA.EventType))
                return false;
            if ((eventA.AddMethod != null && eventB.AddMethod == null) || (eventA.AddMethod == null && eventB.AddMethod != null))
                return false;
            if ((eventA.RemoveMethod != null && eventB.RemoveMethod == null) || (eventA.RemoveMethod == null && eventB.RemoveMethod != null))
                return false;
            return true;
        }

        internal static bool IsVariableNamed(MethodDefinition method, VariableDefinition variable, string name)
        {
            string varName;
            method.DebugInformation.TryGetName(variable, out varName);
            if (string.IsNullOrEmpty(varName))
                varName = variable.ToString();
            return varName == name;
        }

        internal static bool IsUnderExceptionHandler(MethodBody methodBody, Instruction instruction)
        {
            int index = methodBody.Instructions.IndexOf(instruction);
            foreach (ExceptionHandler handler in methodBody.ExceptionHandlers)
            {
                if (methodBody.Instructions.IndexOf(handler.HandlerStart) <= index && methodBody.Instructions.IndexOf(handler.HandlerEnd) >= index)
                    return true;
                if (methodBody.Instructions.IndexOf(handler.TryStart) <= index && methodBody.Instructions.IndexOf(handler.TryEnd) >= index)
                    return true;
            }

            return false;
        }

        internal static bool IsTypeReferenceEqual(TypeReference aType, TypeReference bType, bool compareName = false)
        {
            if (aType is TypeDefinition && ((TypeDefinition)aType).IsGenericParameter)
            {
                if (!(bType is TypeDefinition && ((TypeDefinition)bType).IsGenericParameter))
                    return false;
                return true;
            }

            if (compareName && aType.Name != bType.Name)
                return false;
            switch (aType)
            {
                case GenericInstanceType aGenericInstanceType:
                    if (bType is GenericInstanceType)
                        return false;
                    if (!IsTypeReferenceEqual(aType.GetElementType(), bType.GetElementType()))
                        return false;
                    if (((GenericInstanceType)aType).HasGenericArguments != ((GenericInstanceType)bType).HasGenericArguments)
                        return false;
                    if (((GenericInstanceType)aType).HasGenericArguments)
                    {
                        if (!AreTypeReferencesEqual(((GenericInstanceType)aType).GenericArguments, ((GenericInstanceType)aType).GenericArguments))
                            return false;
                    }

                    return true;
                case GenericParameter aGenericParameter:
                    if (bType is TypeReference)
                        return false;
                    switch (aGenericParameter.Owner.GenericParameterType)
                    {
                        case GenericParameterType.Type:
                            if (!IsTypeReferenceEqual((TypeReference)aGenericParameter.Owner, (TypeReference)((GenericParameter)bType).Owner, false))
                                return false;
                            break;
                        case GenericParameterType.Method:
                            throw new NotImplementedException();
                        default:
                            break;
                    }

                    break;
                case TypeReference aTypeReference:
                    if (!(bType is TypeReference))
                        return false;
                    if (aType.HasGenericParameters != bType.HasGenericParameters)
                        return false;
                    if (aType.HasGenericParameters && !AreSameGenericParameters(aType.GenericParameters, bType.GenericParameters))
                        return false;
                    break;
            }

            return true;
        }

        internal static TypeReferenceKinds GetTypeReferenceKind(TypeReference typeReference)
        {
            switch (typeReference)
            {
                case ByReferenceType byReferenceType:
                    return TypeReferenceKinds.ByReferenceType;
                case GenericParameter genericParameter:
                    return TypeReferenceKinds.GenericParameter;
                case GenericInstanceType genericInstanceType:
                    return TypeReferenceKinds.GenericInstance;
                case TypeReference simpleTypeReference:
                    return TypeReferenceKinds.SimpleTypeReference;
                default:
                    throw new NotImplementedException();
            }
        }

        internal static T GetConstructorArgumentValue<T>(CustomAttributeArgument customAttributeArgument)
            where T : class
        {
            switch (customAttributeArgument.Value)
            {
                case CustomAttributeArgument customAttributeArgumentValue:
                    return (T)customAttributeArgumentValue.Value;
                default:
                    return (T)customAttributeArgument.Value;
            }
        }

        internal static IEnumerable<T> GetConstructorArgumentValues<T>(CustomAttributeArgument[] customAttributeArguments)
        {
            if (customAttributeArguments == null)
                return null;
            if (customAttributeArguments.Length == 0)
                return new T[]{};
            var newArr = new List<T>(customAttributeArguments.Length);
            newArr.AddRange(customAttributeArguments.Select(t => (T)t.Value));
            return newArr;
        }

        internal static bool IsMethodEqual(MethodDefinition methodA, MethodDefinition methodB, bool compareName = false)
        {
            bool isEqual = !compareName || (methodA.Name == methodB.Name);
            if (isEqual)
                isEqual = AreParametersEqual(methodA.Parameters, methodB.Parameters);
            if (isEqual)
                isEqual = AreSameGenericParameters(methodA.GenericParameters, methodB.GenericParameters);
            if (isEqual)
                isEqual = IsTypeReferenceEqual(methodA.MethodReturnType.ReturnType, methodB.MethodReturnType.ReturnType);
            return isEqual;
        }

        internal static bool AreSameModifiers(MethodAttributes aAttributes, MethodAttributes bAttributes)
        {
            var aValues = Enum.GetValues(typeof(MethodAttributes));
            foreach (MethodAttributes attribute in aValues)
            {
                if ((aAttributes & attribute) != (bAttributes & attribute))
                    return false;
            }

            return true;
        }

        internal static bool AreSameParameterAttributes(GenericParameterAttributes aAttributes, GenericParameterAttributes bAttributes)
        {
            var aValues = Enum.GetValues(typeof(GenericParameterAttributes));
            foreach (GenericParameterAttributes attribute in aValues)
            {
                if ((aAttributes & attribute) != (bAttributes & attribute))
                    return false;
            }

            return true;
        }

        internal static bool AreSameGenericParameters(CecilCollection.Collection<GenericParameter> aGenericParameters, CecilCollection.Collection<GenericParameter> bGenericParameters)
        {
            if (aGenericParameters.Count() != bGenericParameters.Count())
                return false;
            var aList = aGenericParameters.ToList();
            var bList = bGenericParameters.ToList();
            for (int i = 0; i < bList.Count(); i++)
            {
                foreach (var constraint in aList[i].Constraints)
                {
                    if (bList[i].Constraints.Any(t => t.ToString() == constraint.ToString()))
                        return false;
                    if (AreSameParameterAttributes(aList[i].Attributes, bList[i].Attributes))
                        return false;
                }
            }

            return true;
        }

        internal static bool AreTypeReferencesEqual(IEnumerable<TypeReference> aTypeRefs, IEnumerable<TypeReference> bTypeRefs)
        {
            if (aTypeRefs.Count() != bTypeRefs.Count())
                return false;
            var aList = aTypeRefs.ToList();
            var bList = bTypeRefs.ToList();
            for (int i = 0; i < bList.Count(); i++)
            {
                if (!IsTypeReferenceEqual(aList[i], bList[i]))
                    return false;
            }

            return true;
        }

        internal static bool AreParametersEqual(CecilCollection.Collection<ParameterDefinition> parametersA, CecilCollection.Collection<ParameterDefinition> parametersB)
        {
            bool isEqual = parametersA.Count == parametersB.Count;
            if (isEqual)
            {
                for (int i = 0; i < parametersA.Count; i++)
                {
                    isEqual = parametersA[i].ParameterType.FullName == parametersB[i].ParameterType.FullName;
                    if (!isEqual)
                        break;
                }
            }

            return isEqual;
        }

        internal static bool IsBrancheCodeOp(OpCode opCode)
        {
            switch (opCode.FlowControl)
            {
                case FlowControl.Branch:
                case FlowControl.Cond_Branch:
                    return true;
                default:
                    return false;
            }
        }

        internal static IMemberDefinition GetPropertyOrEventMember(MethodDefinition method)
        {
            var member = method.DeclaringType.Properties.Where(t => (t.SetMethod != null && t.SetMethod.FullName == method.FullName) || (t.GetMethod != null && t.GetMethod.FullName == method.FullName)).FirstOrDefault();
            if (member != null)
                return member;
            return method.DeclaringType.Events.Where(t => t.AddMethod.FullName == method.FullName || t.RemoveMethod.FullName == method.FullName).FirstOrDefault();
        }

        internal static Instruction GetVariableIL(OpCodeData opCodeData, VariableDefinition variable)
        {
            return GetLocVarInstruction(opCodeData, variable, variable.Index);
        }

        internal static Instruction GetLocVarInstruction(OpCodeData opCodeData, VariableDefinition variable, int variableIndex)
        {
            if (!opCodeData.IsAddress)
            {
                switch (variableIndex)
                {
                    case 0:
                        return Instruction.Create((opCodeData.OpCodeType & OpCodeTypes.Ld) == OpCodeTypes.Ld ? OpCodes.Ldloc_0 : OpCodes.Stloc_0);
                    case 1:
                        return Instruction.Create((opCodeData.OpCodeType & OpCodeTypes.Ld) == OpCodeTypes.Ld ? OpCodes.Ldloc_1 : OpCodes.Stloc_1);
                    case 2:
                        return Instruction.Create((opCodeData.OpCodeType & OpCodeTypes.Ld) == OpCodeTypes.Ld ? OpCodes.Ldloc_2 : OpCodes.Stloc_2);
                    case 3:
                        return Instruction.Create((opCodeData.OpCodeType & OpCodeTypes.Ld) == OpCodeTypes.Ld ? OpCodes.Ldloc_3 : OpCodes.Stloc_3);
                    default:
                        return Instruction.Create((opCodeData.OpCodeType & OpCodeTypes.Ld) == OpCodeTypes.Ld ? OpCodes.Ldloc_S : OpCodes.Stloc_S, variable);
                }
            }
            else
            {
                return Instruction.Create(OpCodes.Ldloca_S, variable);
            }
        }

        internal static OpCodeValues GetOpCodeValue(OpCode opCode)
        {
            return GetOpCodeValue(opCode.Code.ToString());
        }

        internal static OpCodeValues GetOpCodeValue(string opCodeName)
        {
            return (OpCodeValues)Enum.Parse(typeof(OpCodeValues), opCodeName, true);
        }

        internal static bool IsDelegate(FieldReference fieldReference)
        {
            return fieldReference.FieldType.IsDefinition && IsDelegate(CecilHelper.Resolve(fieldReference.FieldType));
        }

        internal static bool IsDelegate(TypeReference typeReference)
        {
            if (!typeReference.IsDefinition)
                typeReference = typeReference.GetElementType().Resolve();
            return IsDelegate(CecilHelper.Resolve(typeReference));
        }

        internal static bool IsDelegate(TypeDefinition typeDefinition)
        {
            if (typeDefinition.FullName == typeof(Delegate).FullName || typeDefinition.FullName == typeof(MulticastDelegate).FullName)
                return true;
            if (typeDefinition.BaseType == null || typeDefinition.BaseType.FullName == typeof(object).FullName)
                return false;
            return IsDelegate(CecilHelper.Resolve(typeDefinition.BaseType));
        }

        internal static bool IsDelegateOrEvent(FieldReference fieldReference)
        {
            return fieldReference.FieldType.IsDefinition && IsDelegateOrEvent(fieldReference.Resolve());
        }

        internal static bool IsDelegateOrEvent(FieldDefinition fieldDefinition)
        {
            return IsFieldEvent(fieldDefinition) || IsDelegate(fieldDefinition);
        }

        internal static bool HasCustomAttributesOfBaseType(IMemberDefinition memberDefinition, Type baseType)
        {
            return GetCustomAttributesOfBaseType(memberDefinition, baseType).Count() != 0;
        }

        internal static CustomAttribute GetCustomAttributeOfBaseType(IMemberDefinition memberDefinition, Type baseType)
        {
            return GetCustomAttributesOfBaseType(memberDefinition, baseType).FirstOrDefault();
        }

        internal static IEnumerable<CustomAttribute> GetCustomAttributesOfBaseType(IMemberDefinition memberDefinition, Type baseType)
        {
            if (memberDefinition.HasCustomAttributes)
            {
                foreach (CustomAttribute customAttribute in memberDefinition.CustomAttributes.Where(t => IsTypeOf(baseType, CecilHelper.Resolve(((TypeReference)t.AttributeType)))))
                    yield return customAttribute;
            }
        }

        internal static bool IsTypeOf<TypeOf>(TypeDefinition typeDefinition)
        {
            var baseType = typeDefinition;
            while (baseType != null)
            {
                if (ConvertFullTypeNameCsToCecil(baseType) == typeof(TypeOf).FullName)
                    return true;
                foreach (var @interface in typeDefinition.Interfaces)
                {
                    if (ConvertFullTypeNameCsToCecil(@interface.InterfaceType.Resolve()) == typeof(TypeOf).FullName)
                        return true;
                    if (@interface.InterfaceType.Resolve().HasInterfaces)
                    {
                        foreach (var baseInterface in @interface.InterfaceType.Resolve().Interfaces)
                        {
                            if (IsTypeOf<TypeOf>(baseInterface.InterfaceType.Resolve()))
                                return true;
                        }
                    }
                }

                baseType = baseType.BaseType?.Resolve();
            }

            return false;
        }

        internal static bool IsTypeOf(string fullTypeOfName, TypeDefinition typeDefinition)
        {
            var baseType = typeDefinition;
            while (baseType != null)
            {
                if (ConvertFullTypeNameCsToCecil(baseType) == fullTypeOfName)
                    return true;
                foreach (var @interface in typeDefinition.Interfaces)
                {
                    if (ConvertFullTypeNameCsToCecil(@interface.InterfaceType.Resolve()) == fullTypeOfName)
                        return true;
                    if (@interface.InterfaceType.Resolve().HasInterfaces)
                    {
                        foreach (var baseInterface in @interface.InterfaceType.Resolve().Interfaces)
                        {
                            if (IsTypeOf(fullTypeOfName, baseInterface.InterfaceType.Resolve()))
                                return true;
                        }
                    }
                }

                baseType = baseType.BaseType?.Resolve();
            }

            return false;
        }

        internal static bool IsTypeDefinionOf(TypeDefinition resolvedTypeDefinitionOf, TypeDefinition resolvedBaseTypeDefinition)
        {
            var baseType = resolvedBaseTypeDefinition;
            while (baseType != null)
            {
                if (baseType.FullName == resolvedTypeDefinitionOf.FullName)
                    return true;
                foreach (var @interface in resolvedBaseTypeDefinition.Interfaces)
                {
                    if (@interface.InterfaceType.FullName == resolvedTypeDefinitionOf.FullName)
                        return true;
                    if (@interface.InterfaceType.Resolve().HasInterfaces)
                    {
                        foreach (var baseInterface in @interface.InterfaceType.Resolve().Interfaces)
                        {
                            if (IsTypeDefinionOf(resolvedTypeDefinitionOf, baseInterface.InterfaceType.Resolve()))
                                return true;
                        }
                    }
                }

                if (baseType.BaseType != null)
                    baseType = baseType.BaseType.Resolve();
                else
                    baseType = null;
            }

            return false;
        }

        internal static bool IsTypeReferenceOf(TypeReference resolvedTypeReferenceOf, TypeReference resolvedBaseTypeReference)
        {
            if (resolvedTypeReferenceOf is TypeDefinition)
            {
                if (resolvedBaseTypeReference is TypeDefinition)
                    return IsTypeDefinionOf((TypeDefinition)resolvedBaseTypeReference, (TypeDefinition)resolvedBaseTypeReference);
            }
            else
            {
                if (resolvedBaseTypeReference is TypeDefinition)
                    return false;
            }

            var typeReferenceToCheckKind = CecilHelper.GetTypeReferenceKind(resolvedTypeReferenceOf);
            if (typeReferenceToCheckKind != CecilHelper.GetTypeReferenceKind(resolvedBaseTypeReference))
                return false;
            switch (typeReferenceToCheckKind)
            {
                case TypeReferenceKinds.GenericInstance:
                    var genericInstanceToCheck = (GenericInstanceType)resolvedTypeReferenceOf;
                    var genericInstanceOf = (GenericInstanceType)resolvedBaseTypeReference;
                    if (!IsTypeDefinionOf(Resolve(genericInstanceToCheck.GetElementType()), Resolve(genericInstanceOf.GetElementType())))
                        return false;
                    if (genericInstanceToCheck.HasGenericArguments || genericInstanceOf.HasGenericArguments)
                    {
                        if (genericInstanceToCheck.HasGenericArguments && genericInstanceOf.HasGenericArguments)
                        {
                            if (genericInstanceToCheck.GenericArguments.Count == genericInstanceOf.GenericArguments.Count)
                            {
                                for (int i = 0; i < genericInstanceToCheck.GenericArguments.Count; i++)
                                {
                                    if (!IsTypeReferenceEqual(genericInstanceToCheck.GenericArguments[i], genericInstanceOf.GenericArguments[i]))
                                        return false;
                                }
                            }
                            else
                                return false;
                        }
                        else
                            return false;
                    }

                    return true;
                case TypeReferenceKinds.SimpleTypeReference:
                    return IsTypeDefinionOf(Resolve(resolvedTypeReferenceOf.GetElementType()), Resolve(resolvedBaseTypeReference.GetElementType()));
                case TypeReferenceKinds.ByReferenceType:
                    return IsTypeReferenceOf(((ByReferenceType)resolvedTypeReferenceOf).ElementType, ((ByReferenceType)resolvedBaseTypeReference).ElementType);
                case TypeReferenceKinds.GenericParameter:
                    var resolvedGenericParameterOf = (GenericParameter)resolvedTypeReferenceOf;
                    var resolvedBaseGenericParameter = (GenericParameter)resolvedBaseTypeReference;
                    switch (resolvedGenericParameterOf.Owner)
                    {
                        case TypeReference typeReferenceOwner:
                            if (!(resolvedBaseGenericParameter.Owner is TypeReference))
                                return false;
                            return IsTypeReferenceEqual((TypeReference)resolvedGenericParameterOf.Owner, (TypeReference)resolvedBaseGenericParameter.Owner);
                        case MethodReference methodReferenceOwner:
                            if (!(resolvedBaseGenericParameter.Owner is MethodDefinition))
                                return false;
                            return IsMethodEqual((MethodDefinition)resolvedGenericParameterOf.Owner, (MethodDefinition)resolvedBaseGenericParameter.Owner, true);
                        default:
                            throw new NotSupportedException();
                    }

                default:
                    throw new NotSupportedException();
            }
        }

        internal static bool IsTypeOf(Type typeOf, TypeDefinition typeDefinition)
        {
            if (typeDefinition.FullName == typeOf.FullName)
                return true;
            return IsBaseTypeOf(typeOf, typeDefinition);
        }

        internal static bool IsBaseTypeOf<TypeOf>(TypeDefinition typeDefinition)
        {
            TypeDefinition baseType = GetTypeWithBaseTypeName(typeof(TypeOf).FullName, typeDefinition);
            if (baseType != null)
                return true;
            return false;
        }

        internal static bool IsBaseTypeOf(Type typeOf, TypeDefinition typeDefinition)
        {
            TypeDefinition baseType = GetTypeWithBaseTypeName(typeOf.FullName, typeDefinition);
            if (baseType != null)
                return true;
            return false;
        }

        internal static bool HasParentWtihBaseTypeOf(Type typeOf, TypeDefinition typeDefinition)
        {
            return GetParentWithBaseTypeOf(typeOf, typeDefinition) != null;
        }

        internal static TypeDefinition GetParentWithBaseTypeOf(Type typeOf, TypeDefinition typeDefinition)
        {
            while (typeDefinition != null)
            {
                var baseType = GetTypeWithBaseTypeName(typeOf.FullName, typeDefinition);
                if (baseType != null)
                    return typeDefinition;
                typeDefinition = typeDefinition.DeclaringType;
            }

            return null;
        }

        internal static TypeDefinition GetTypeWithBaseTypeName(Type typeOf, TypeDefinition typeDefinition)
        {
            return GetTypeWithBaseTypeName(typeOf.FullName, typeDefinition);
        }

        internal static TypeDefinition GetTypeWithBaseTypeName(string fullTypeOfName, TypeDefinition typeDefinition)
        {
            TypeReference baseType = typeDefinition.BaseType;
            while (baseType != null)
            {
                if (baseType.FullName == fullTypeOfName)
                    return baseType.Resolve();
                baseType = CecilHelper.Resolve(baseType).BaseType;
            }

            if (baseType == null)
            {
                foreach (var @interface in typeDefinition.Interfaces)
                {
                    if (@interface.InterfaceType.FullName == fullTypeOfName)
                    {
                        return typeDefinition;
                    }

                    if (@interface.InterfaceType.Resolve().HasInterfaces)
                    {
                        foreach (var baseInterface in @interface.InterfaceType.Resolve().Interfaces)
                        {
                            if (GetTypeWithBaseTypeName(fullTypeOfName, baseInterface.InterfaceType.Resolve()) != null)
                                return typeDefinition;
                        }
                    }
                }
            }

            if (baseType == null)
            {
                if (typeDefinition.DeclaringType != null)
                    return GetTypeWithBaseTypeName(fullTypeOfName, typeDefinition.DeclaringType);
            }

            return null;
        }

        internal static bool HasCustomAttributesOfType(MemberReference memberReference, Type attributeType)
        {
            var memberDefinition = memberReference.Resolve();
            if (memberDefinition == null)
                return false;
            return HasCustomAttributesOfType(memberDefinition, attributeType);
        }

        public static int GetMaskedAttributes(ushort self, ushort mask)
        {
            return self & mask;
        }

        internal static bool HasCustomAttributesOfType(IMemberDefinition memberDefinition, Type attributeType, bool inherited = false)
        {
            var attributes = GetCustomAttributesOfType(memberDefinition, attributeType);
            var hasAttributes = attributes != null && attributes.Any();
            while (inherited && !hasAttributes && memberDefinition.DeclaringType != null)
            {
                memberDefinition = memberDefinition.DeclaringType;
                attributes = GetCustomAttributesOfType(memberDefinition, attributeType);
                hasAttributes = attributes != null && attributes.Any();
            }

            return hasAttributes;
        }

        internal static IEnumerable<CustomAttribute> GetCustomAttributesOfType(AssemblyDefinition assembly, Type attributeType)
        {
            if (!assembly.HasCustomAttributes)
                return null;
            var customAttributes = assembly.CustomAttributes.Where(t => IsCustomAttributesOfType(t, attributeType));
            return customAttributes;
        }

        internal static IEnumerable<CustomAttribute> GetCustomAttributesOfType(IMemberDefinition memberDefinition, Type attributeType)
        {
            if (!memberDefinition.HasCustomAttributes)
                return null;
            var customAttributes = memberDefinition.CustomAttributes.Where(t => IsCustomAttributesOfType(t, attributeType));
            return customAttributes;
        }

        internal static bool IsCustomAttributesOfType(CustomAttribute customAttribute, Type attributeType)
        {
            var customAttributeType = customAttribute.AttributeType.Resolve();
            while (customAttributeType != null && customAttributeType.FullName != attributeType.FullName)
                customAttributeType = customAttributeType.BaseType?.Resolve();
            return customAttributeType != null;
        }

        internal static object GetValueCustomAttributeOfType(IMemberDefinition memberDefinition, Type attributeType, int constructorArgumentIndex)
        {
            var customAttribute = memberDefinition.CustomAttributes.Where(t => t.AttributeType.FullName == attributeType.FullName).FirstOrDefault();
            if (customAttribute == null)
                return null;
            return customAttribute.ConstructorArguments[constructorArgumentIndex].Value;
        }

        internal static CustomAttribute GetCustomAttributeOfType(IMemberDefinition memberDefinition, Type attributeType)
        {
            return memberDefinition.CustomAttributes.Where(t => t.AttributeType.FullName == attributeType.FullName).FirstOrDefault();
        }

        internal static IEnumerable<CustomAttribute> GetCustomAttributeOfTypes(IMemberDefinition memberDefinition, Type attributeType)
        {
            return memberDefinition.CustomAttributes.Where(t => IsTypeOf(attributeType, Resolve(t.AttributeType)));
        }

        internal static CustomAttributeNamedArgument? GetCustomAttributeProperty(IMemberDefinition memberDefinition, Type attributeType, string propertyName)
        {
            CustomAttribute attribute = GetCustomAttributeOfType(memberDefinition, attributeType);
            if (attribute != null)
                return attribute.Properties.Where(t => t.Name == propertyName).FirstOrDefault();
            return null;
        }

        internal static CustomAttributeNamedArgument? GetCustomAttributeProperty(CustomAttribute attribute, string propertyName)
        {
            if (attribute != null)
                return attribute.Properties.Where(t => t.Name == propertyName).FirstOrDefault();
            return null;
        }

        internal static object GetCustomAttributePropertyValue(CustomAttribute attribute, string propertyName)
        {
            var value = GetCustomAttributePropertyValue(GetCustomAttributeProperty(attribute, propertyName));
            return value;
        }

        internal static object GetCustomAttributePropertyValue(IMemberDefinition memberDefinition, Type attributeType, string propertyName)
        {
            return GetCustomAttributePropertyValue(GetCustomAttributeProperty(memberDefinition, attributeType, propertyName));
        }

        internal static object GetCustomAttributePropertyValue(CustomAttributeNamedArgument? property)
        {
            if (property != null)
                return property.Value.Argument.Value;
            return null;
        }

        internal static object GetObjectInstance(Ref.Assembly assembly, TypeDefinition typeDefinition)
        {
            var typename = typeDefinition.FullName.Replace("*", "\\*");
            var type = assembly.GetType(typename);
            var @object = Activator.CreateInstance(type);
            return @object;
        }

        internal static IEnumerable<Instruction> GetInstructions(OpCode opCode, MethodDefinition method)
        {
            foreach (Instruction instruction in method.Body.Instructions)
            {
                if (instruction.OpCode == opCode)
                    yield return instruction;
            }
        }

        internal static string GetNamespace(TypeReference targetType)
        {
            string @namespace = targetType.Namespace;
            while (string.IsNullOrEmpty(@namespace) && targetType.DeclaringType != null)
            {
                targetType = targetType.DeclaringType;
                @namespace = targetType.Namespace;
            }

            return @namespace;
        }

        internal static string ConvertFullTypeNameCecilToCS(TypeDefinition typeDefinition)
        {
            return typeDefinition.FullName;
        }

        internal static string ConvertFullTypeNameCecilToCS(string fullTypeName)
        {
            int index = fullTypeName.IndexOf("+");
            if (index != -1)
            {
                string @namespace = fullTypeName.Substring(0, index);
                string typename = fullTypeName.Substring(index + 1).Replace("+", "/");
                fullTypeName = $"{@namespace}/{typename}";
            }

            return fullTypeName;
        }

        internal static string ConvertFullTypeNameCsToCecil(TypeDefinition typeDefinition)
        {
            return ConvertFullTypeNameCsToCecil(typeDefinition.FullName);
        }

        internal static string ConvertFullTypeNameCsToCecil(string fullTypeName)
        {
            int index = fullTypeName.IndexOf("/");
            if (index != -1)
            {
                string @namespace = fullTypeName.Substring(0, index);
                string typename = fullTypeName.Substring(index + 1).Replace("/", "+");
                fullTypeName = $"{@namespace}+{typename}";
            }

            return fullTypeName;
        }

        internal static TypeDefinitionKinds GetTypeKinds(TypeDefinition typeDefinition)
        {
            if (typeDefinition.IsEnum)
                return TypeDefinitionKinds.Enum;
            else
            {
                if (CecilHelper.IsDelegate(typeDefinition))
                    return TypeDefinitionKinds.Delegate;
                else
                {
                    if (typeDefinition.IsInterface)
                        return TypeDefinitionKinds.Interface;
                    else
                    {
                        if (typeDefinition.IsValueType)
                            return TypeDefinitionKinds.Struct;
                        else
                            return TypeDefinitionKinds.Class;
                    }
                }
            }
        }

        internal static FieldReference ImportReference(AssemblyDefinition targetAssembly, FieldReference field)
        {
            if (field.Module != null && field.Module != targetAssembly.MainModule)
                return targetAssembly.MainModule.ImportReference(field);
            return field;
        }

        internal static MethodReference ImportReference(AssemblyDefinition targetAssembly, MethodReference method)
        {
            if (method.Module != null && method.Module != targetAssembly.MainModule)
            {
                return targetAssembly.MainModule.ImportReference(method);
            }

            return method;
        }

        internal static TypeReference ImportReference(AssemblyDefinition targetAssembly, TypeReference typeReference)
        {
            if (typeReference.Module != null && typeReference.Module != targetAssembly.MainModule)
            {
                return targetAssembly.MainModule.ImportReference(typeReference);
            }

            return typeReference;
        }

        internal static (string @interfaceName, string simplename) GetMemberNames(string memberName)
        {
            if (!memberName.Contains("."))
                return (null, memberName);
            var interfaceName = memberName.Substring(0, memberName.LastIndexOf("."));
            var simplename = memberName.Substring(memberName.LastIndexOf(".") + 1);
            return (interfaceName, simplename);
        }
    }
}