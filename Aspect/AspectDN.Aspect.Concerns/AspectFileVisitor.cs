// Author:
//
// T. Freyburger (t.freyburger@gmail.com)
//
// Copyright (c)  Thierry Freyburger
//
// Licensed under the GPLV3 license.
////
using AspectDN.Common;
using AspectDN.Aspect.Weaving.IConcerns;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Runtime.CompilerServices;

namespace AspectDN.Aspect.Concerns
{
    public class AspectFileVisitor
    {
        byte[] _Bytes;
        string _FullAspectRepositoryName;
        ConcernsContainer _ConcernsContainer;
        internal AspectFileVisitor(byte[] bytes, ConcernsContainer concernsContainer)
        {
            _ConcernsContainer = concernsContainer;
            var aspectRepositoryConstant = new byte[]{77, 90, 144, 0, 3, 0, 0, 0, 4, 0, 0, 0, 255, 255, 0, 0, 184, 0, 0, 0, 0, 0, 0, 0, 64, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 128, 0, 0, 0, 14, 31, 186, 14, 0, 180, 9, 205, 33, 184, 1, 76, 205, 33, 84, 104, 105, 115, 32, 112, 114, 111, 103, 114, 97, 109, 32, 99, 97, 110, 110, 111, 116, 32, 98, 101, 32, 114, 117, 110, 32, 105, 110, 32, 68, 79, 83, 32, 109, 111, 100, 101, 46, 13, 13, 10, 36, 0, 0, 0, 0, 0, 0, 0};
            _Bytes = new byte[bytes.Length + aspectRepositoryConstant.Length];
            Array.Copy(aspectRepositoryConstant, _Bytes, aspectRepositoryConstant.Length);
            Array.Copy(bytes, 0, _Bytes, 128, bytes.Length);
        }

        virtual protected internal void GetAspects()
        {
            _FullAspectRepositoryName = Path.GetFileName(Assembly.GetAssembly(this.GetType()).Location);
            var aspectsReporitory = CecilHelper.GetAssembly(_Bytes, false, _ConcernsContainer.ReaderParameters);
            _ConcernsContainer.AssemblyResolver.AddAssembly(aspectsReporitory);
            _VisitAssembly(aspectsReporitory);
        }

        void _VisitAssembly(AssemblyDefinition assemblyDefinition)
        {
            foreach (var prototoypeType in assemblyDefinition.MainModule.Types.Where(t => CecilHelper.HasCustomAttributesOfType(t, typeof(PrototypeTypeDeclarationAttribute))))
            {
                _ConcernsContainer.Add(prototoypeType);
            }

            foreach (var prototypeTypeMappingAttribute in assemblyDefinition.CustomAttributes.Where(t => t.AttributeType.Name == nameof(PrototypeTypeMappingAttribute)))
                _ConcernsContainer.Add(new PrototypeTypeMappingDeclaration(prototypeTypeMappingAttribute));
            foreach (TypeDefinition adviceDeclaration in assemblyDefinition.MainModule.Types.Where(t => CecilHelper.IsTypeOf<IAdviceDeclaration>(t)))
            {
                _BuildAdviceDefinition(adviceDeclaration);
            }

            Assembly assembly = null;
            using (var stream = new MemoryStream())
            {
                assemblyDefinition.Write(stream);
                stream.Position = 0;
                assembly = Assembly.Load(stream.GetBuffer());
            }

            foreach (TypeDefinition pointcutDeclaration in assemblyDefinition.MainModule.Types.Where(t => CecilHelper.IsTypeOf<IPointcutDeclaration>(t)))
            {
                var pointcutInstance = (IPointcutDeclaration)CecilHelper.GetObjectInstance(assembly, pointcutDeclaration);
                _VisitPointcutDeclaration(pointcutDeclaration, assembly);
            }

            var aspectDeclarations = new List<TypeDefinition>();
            foreach (var aspectDeclaration in assemblyDefinition.MainModule.Types.Where(t => CecilHelper.IsTypeOf<IAspectDeclaration>(t)))
                _ConcernsContainer.Add(new AspectDeclaration(aspectDeclaration));
            foreach (var directory in _ConcernsContainer.SearchAspectFilesDirectoryNames)
            {
                foreach (var assemblyReference in assemblyDefinition.MainModule.AssemblyReferences)
                {
                    var assemblyRereferencePath = Path.Combine(directory, assemblyReference.Name);
                    foreach (var item in ((AspectDNAssemblyResolver)_ConcernsContainer.ReaderParameters.AssemblyResolver).GetSearchDirectories())
                    {
                        var filename = Path.ChangeExtension(assemblyRereferencePath, item);
                        if (File.Exists(filename))
                        {
                            _ConcernsContainer.Visit(filename);
                            break;
                        }
                    }
                }
            }
        }

        void _BuildAdviceDefinition(TypeDefinition adviceDeclaration)
        {
            var @interface = adviceDeclaration.Interfaces.FirstOrDefault(t => t.InterfaceType.Resolve().Interfaces.Any(i => i.InterfaceType.Name == nameof(IAdviceDeclaration)));
            switch (@interface.InterfaceType.Name)
            {
                case nameof(ICodeAdviceDeclaration):
                    _BuildCodeAdviceDefinition(adviceDeclaration);
                    break;
                case nameof(IChangeValueAdviceDeclaration):
                    _BuildChangeValueAdviceDefinition(adviceDeclaration);
                    break;
                case nameof(ITypeMembersAdviceDeclaration):
                    _BuildTypeMembersAdviceDefinition(adviceDeclaration);
                    break;
                case nameof(IInterfaceMembersAdviceDeclaration):
                    _BuildInterfaceMembersAdviceDefinition(adviceDeclaration);
                    break;
                case nameof(IEnumMembersAdviceDeclaration):
                    _BuildEnumMembersAdviceDefinition(adviceDeclaration);
                    break;
                case nameof(IInheritedTypesAdviceDeclaration):
                    _BuildInheritedTypesAdviceDefinition(adviceDeclaration);
                    break;
                case nameof(ITypesAdviceDeclaration):
                    _BuildTypesAdviceDefinition(adviceDeclaration);
                    break;
                case nameof(IAttributesAdviceDeclaration):
                    _BuildAttributesAdviceDefinition(adviceDeclaration);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        void _BuildCodeAdviceDefinition(TypeDefinition codeAdviceDeclaration)
        {
            var codeAdviceDefinition = new AdviceCodeDefinition(codeAdviceDeclaration);
            var adviceMember = codeAdviceDefinition.AdviceMemberDefinitions.First();
            _ConcernsContainer.Add(codeAdviceDefinition);
            _VisitPrototypeItemReferences(adviceMember);
            _AddCompilerGeneratedMemberAndNewType(codeAdviceDefinition);
        }

        void _BuildChangeValueAdviceDefinition(TypeDefinition changeValueAdviceDeclaration)
        {
            var changeValueAdviceDefinition = new AdviceStackDefinition(changeValueAdviceDeclaration);
            _ConcernsContainer.Add(changeValueAdviceDefinition);
            _VisitPrototypeItemReferences(changeValueAdviceDefinition.AdviceMemberDefinitions.First());
            _AddCompilerGeneratedMemberAndNewType(changeValueAdviceDefinition);
        }

        void _BuildTypeMembersAdviceDefinition(TypeDefinition typeMembersAdviceDeclaration)
        {
            var typeMembersAdviceDefinition = new AdviceDefinition(typeMembersAdviceDeclaration, AdviceKinds.TypeMembers);
            foreach (FieldDefinition field in typeMembersAdviceDeclaration.Fields.Where(t => !CecilHelper.HasCustomAttributesOfType((IMemberDefinition)t, typeof(ExludedMemberAttribute))))
            {
                if (CecilHelper.HasCustomAttributesOfType(field, typeof(CompilerGeneratedAttribute)) && CecilHelper.IsDelegate(field.FieldType) && typeMembersAdviceDeclaration.Events.Any(t => t.Name == field.Name && t.EventType.FullName == field.FieldType.FullName && CecilHelper.HasCustomAttributesOfType((IMemberDefinition)t, typeof(ExludedMemberAttribute))))
                    continue;
                typeMembersAdviceDefinition.Add(field, AdviceMemberKinds.Field);
            }

            foreach (PropertyDefinition property in typeMembersAdviceDeclaration.Properties.Where(t => !CecilHelper.HasCustomAttributesOfType((IMemberDefinition)t, typeof(ExludedMemberAttribute))))
            {
                typeMembersAdviceDefinition.Add(property, AdviceMemberKinds.Property);
                if (property.GetMethod != null)
                    typeMembersAdviceDefinition.Add(property.GetMethod, AdviceMemberKinds.Method);
                if (property.SetMethod != null)
                    typeMembersAdviceDefinition.Add(property.SetMethod, AdviceMemberKinds.Method);
                if (property.HasOtherMethods)
                {
                    foreach (var otherMethod in property.OtherMethods)
                        typeMembersAdviceDefinition.Add(otherMethod, AdviceMemberKinds.Method);
                }
            }

            foreach (EventDefinition @event in typeMembersAdviceDeclaration.Events.Where(t => !CecilHelper.HasCustomAttributesOfType((IMemberDefinition)t, typeof(ExludedMemberAttribute))))
            {
                typeMembersAdviceDefinition.Add(@event, AdviceMemberKinds.Event);
                if (@event.AddMethod != null)
                    typeMembersAdviceDefinition.Add(@event.AddMethod, AdviceMemberKinds.Method);
                if (@event.RemoveMethod != null)
                    typeMembersAdviceDefinition.Add(@event.RemoveMethod, AdviceMemberKinds.Method);
                if (@event.HasOtherMethods)
                {
                    foreach (var otherMethod in @event.OtherMethods)
                        typeMembersAdviceDefinition.Add(otherMethod, AdviceMemberKinds.Method);
                }
            }

            foreach (MethodDefinition method in typeMembersAdviceDeclaration.Methods.Where(t => !t.IsSpecialName && !CecilHelper.HasCustomAttributesOfType((IMemberDefinition)t, typeof(ExludedMemberAttribute))))
            {
                if (!CecilHelper.HasCustomAttributesOfType(method, typeof(CompilerGeneratedAttribute)))
                    typeMembersAdviceDefinition.Add(method, AdviceMemberKinds.Method);
            }

            foreach (MethodDefinition method in typeMembersAdviceDeclaration.Methods.Where(t => t.IsConstructor && CecilHelper.HasCustomAttributesOfType((IMemberDefinition)t, typeof(AdviceConstructorAttribute))))
            {
                typeMembersAdviceDefinition.Add(method, AdviceMemberKinds.Constructor);
            }

            foreach (MethodDefinition method in typeMembersAdviceDeclaration.Methods.Where(t => CecilHelper.GetMethodCategory(t) == MethodCategories.Operator))
            {
                typeMembersAdviceDefinition.Add(method, AdviceMemberKinds.Operator);
            }

            _ConcernsContainer.Add(typeMembersAdviceDefinition);
            _AddCompilerGeneratedMemberAndNewType(typeMembersAdviceDefinition);
            foreach (var method in typeMembersAdviceDefinition.AdviceMemberDefinitions.Where(t => t.Member is MethodDefinition && !t.ParentAdviceDefinion.IsCompiledGenerated))
                _VisitPrototypeItemReferences(method);
        }

        void _BuildTypesAdviceDefinition(TypeDefinition typesAdviceDeclaration)
        {
            var typesAdviceDefinition = new AdviceDefinition(typesAdviceDeclaration, AdviceKinds.Type);
            foreach (var nestedAdviceType in typesAdviceDeclaration.NestedTypes)
                typesAdviceDefinition.Add(nestedAdviceType, AdviceMemberKinds.Type);
            _ConcernsContainer.Add(typesAdviceDefinition);
        }

        void _BuildInterfaceMembersAdviceDefinition(TypeDefinition interfaceMembersAdviceDeclaration)
        {
            var adviceDefinition = new AdviceDefinition(interfaceMembersAdviceDeclaration, AdviceKinds.InterfaceMembers);
            foreach (PropertyDefinition property in interfaceMembersAdviceDeclaration.Properties.Where(t => !CecilHelper.HasCustomAttributesOfType((IMemberDefinition)t, typeof(ExludedMemberAttribute))))
            {
                adviceDefinition.Add(property, AdviceMemberKinds.Property);
                if (property.GetMethod != null)
                    adviceDefinition.Add(property.GetMethod, AdviceMemberKinds.Method);
                if (property.SetMethod != null)
                    adviceDefinition.Add(property.SetMethod, AdviceMemberKinds.Method);
                if (property.HasOtherMethods)
                {
                    foreach (var otherMethod in property.OtherMethods)
                        adviceDefinition.Add(otherMethod, AdviceMemberKinds.Method);
                }
            }

            foreach (EventDefinition @event in interfaceMembersAdviceDeclaration.Events.Where(t => !CecilHelper.HasCustomAttributesOfType((IMemberDefinition)t, typeof(ExludedMemberAttribute))))
            {
                adviceDefinition.Add(@event, AdviceMemberKinds.Event);
                if (@event.AddMethod != null)
                    adviceDefinition.Add(@event.AddMethod, AdviceMemberKinds.Method);
                if (@event.RemoveMethod != null)
                    adviceDefinition.Add(@event.RemoveMethod, AdviceMemberKinds.Method);
                if (@event.HasOtherMethods)
                {
                    foreach (var otherMethod in @event.OtherMethods)
                        adviceDefinition.Add(otherMethod, AdviceMemberKinds.Method);
                }
            }

            foreach (MethodDefinition method in interfaceMembersAdviceDeclaration.Methods.Where(t => !t.IsSpecialName && !CecilHelper.HasCustomAttributesOfType((IMemberDefinition)t, typeof(PrototypeItemDeclarationAttribute))))
                adviceDefinition.Add(method, AdviceMemberKinds.Method);
            _ConcernsContainer.Add(adviceDefinition);
        }

        void _BuildEnumMembersAdviceDefinition(TypeDefinition enumMembersAdviceDeclaration)
        {
            var enumMembersDefinition = enumMembersAdviceDeclaration.NestedTypes.FirstOrDefault();
            var enumMembersAdviceDefinition = new AdviceEnumMembersDefinition(enumMembersAdviceDeclaration);
            foreach (var field in enumMembersDefinition.Fields.Where(t => (t.Attributes & Mono.Cecil.FieldAttributes.SpecialName) != Mono.Cecil.FieldAttributes.SpecialName).Where(t => !CecilHelper.HasCustomAttributesOfType((IMemberDefinition)t, typeof(ExludedMemberAttribute))))
                enumMembersAdviceDefinition.Add(field, AdviceMemberKinds.EnumMember);
            _ConcernsContainer.Add(enumMembersAdviceDefinition);
        }

        void _BuildInheritedTypesAdviceDefinition(TypeDefinition inheritedTypesDeclaration)
        {
            var inheritedTypesAdviceDefinition = new AdviceDefinition(inheritedTypesDeclaration, AdviceKinds.BaseTypeList);
            foreach (var field in inheritedTypesDeclaration.Fields)
            {
                inheritedTypesAdviceDefinition.Add(field.FieldType, AdviceMemberKinds.Type);
            }

            _ConcernsContainer.Add(inheritedTypesAdviceDefinition);
        }

        void _BuildAttributesAdviceDefinition(TypeDefinition attributesAdviceDeclaration)
        {
            var attributes = attributesAdviceDeclaration.CustomAttributes.Where(t => !CecilHelper.IsTypeOf<ExludedMemberAttribute>(t.AttributeType.Resolve()));
            var attributesAdviceDefinition = new AdviceAttributesDefinition(attributesAdviceDeclaration);
            foreach (var attribute in attributes)
                attributesAdviceDefinition.Add(attribute, AdviceMemberKinds.Attribute);
            _ConcernsContainer.Add(attributesAdviceDefinition);
        }

        void _VisitPointcutDeclaration(TypeDefinition pointcutDeclaration, Assembly assembly)
        {
            var pointcutdeclarationInstance = (IPointcutDeclaration)CecilHelper.GetObjectInstance(assembly, pointcutDeclaration);
            var pointcutType = (PointcutTypes)pointcutdeclarationInstance.GetType().GetCustomAttributesData().First().ConstructorArguments[0].Value;
            var expression = (PointcutTypes)pointcutdeclarationInstance.GetType().GetCustomAttributesData().First().ConstructorArguments[0].Value;
            PointcutDefinition pointcutDefinition;
            switch (pointcutType)
            {
                case PointcutTypes.assemblies:
                    pointcutDefinition = new PointcutAssemblyDefinition(pointcutDeclaration, pointcutType, ((IPointcutAsssemblyDeclaration)pointcutdeclarationInstance).GetDefinition());
                    break;
                case PointcutTypes.classes:
                    pointcutDefinition = new PointcutTypeDefinition(pointcutDeclaration, pointcutType, ((IPointcutTypeDeclaration)pointcutdeclarationInstance).GetDefinition());
                    break;
                case PointcutTypes.interfaces:
                    pointcutDefinition = new PointcutTypeDefinition(pointcutDeclaration, pointcutType, ((IPointcutTypeDeclaration)pointcutdeclarationInstance).GetDefinition());
                    break;
                case PointcutTypes.methods:
                    pointcutDefinition = new PointcutMethodDefinition(pointcutDeclaration, pointcutType, ((IPointcutMethodDeclaration)pointcutdeclarationInstance).GetDefinition());
                    break;
                case PointcutTypes.fields:
                    pointcutDefinition = new PointcutFieldDefinition(pointcutDeclaration, pointcutType, ((IPointcutFieldDeclaration)pointcutdeclarationInstance).GetDefinition());
                    break;
                case PointcutTypes.properties:
                    pointcutDefinition = new PointcutPropertyDefinition(pointcutDeclaration, pointcutType, ((IPointcutPropertyDeclaration)pointcutdeclarationInstance).GetDefinition());
                    break;
                case PointcutTypes.events:
                    pointcutDefinition = new PointcutEventDefinition(pointcutDeclaration, pointcutType, ((IPointcutEventDeclaration)pointcutdeclarationInstance).GetDefinition());
                    break;
                case PointcutTypes.delegates:
                    pointcutDefinition = new PointcutTypeDefinition(pointcutDeclaration, pointcutType, ((IPointcutTypeDeclaration)pointcutdeclarationInstance).GetDefinition());
                    break;
                case PointcutTypes.structs:
                    pointcutDefinition = new PointcutTypeDefinition(pointcutDeclaration, pointcutType, ((IPointcutTypeDeclaration)pointcutdeclarationInstance).GetDefinition());
                    break;
                case PointcutTypes.exceptions:
                    pointcutDefinition = new PointcutTypeDefinition(pointcutDeclaration, pointcutType, ((IPointcutTypeDeclaration)pointcutdeclarationInstance).GetDefinition());
                    break;
                case PointcutTypes.constructors:
                    pointcutDefinition = new PointcutMethodDefinition(pointcutDeclaration, pointcutType, ((IPointcutMethodDeclaration)pointcutdeclarationInstance).GetDefinition());
                    break;
                case PointcutTypes.enums:
                    pointcutDefinition = new PointcutTypeDefinition(pointcutDeclaration, pointcutType, ((IPointcutTypeDeclaration)pointcutdeclarationInstance).GetDefinition());
                    break;
                default:
                    throw new NotImplementedException();
            }

            _ConcernsContainer.Add(pointcutDefinition);
        }

        void _AddCompilerGeneratedMemberAndNewType(AdviceDefinition adviceDefinition)
        {
            var nestedTypeAdvices = adviceDefinition.AdviceDeclaration.NestedTypes.Where(t => CecilHelper.HasCustomAttributesOfType(t, typeof(CompilerGeneratedAttribute)));
            if (nestedTypeAdvices.Any())
            {
                var typesAdviceDefinition = new AdviceDefinition(adviceDefinition.AdviceDeclaration, AdviceKinds.Type, true);
                foreach (var nestedType in nestedTypeAdvices)
                {
                    var member = typesAdviceDefinition.Add(nestedType.Resolve(), AdviceMemberKinds.Type);
                    adviceDefinition.CompiledGeneratedAdviceMemberDefinitions.Add(member);
                }

                _ConcernsContainer.Add(typesAdviceDefinition);
            }

            var compilerGeneratedMembers = new List<MethodDefinition>();
            var globalCompilerGeneratedTypes = new List<TypeDefinition>();
            IEnumerable<MethodDefinition> methods = null;
            switch (adviceDefinition.AdviceKind)
            {
                case AdviceKinds.Attributes:
                case AdviceKinds.Type:
                case AdviceKinds.BaseTypeList:
                case AdviceKinds.EnumMembers:
                case AdviceKinds.TypeMembers:
                case AdviceKinds.InterfaceMembers:
                    methods = adviceDefinition.AdviceDeclaration.Methods.Where(t => !t.IsSpecialName && !CecilHelper.HasCustomAttributesOfType((IMemberDefinition)t, typeof(ExludedMemberAttribute)));
                    break;
                case AdviceKinds.ChangeValue:
                case AdviceKinds.Code:
                    methods = adviceDefinition.AdviceDeclaration.Methods;
                    break;
                default:
                    break;
            }

            foreach (var method in methods)
            {
                if (CecilHelper.HasCustomAttributesOfType(method, typeof(CompilerGeneratedAttribute)) && !adviceDefinition.AdviceMemberDefinitions.Select(t => t.Member).OfType<MethodDefinition>().Any(m => m.FullName == method.FullName))
                {
                    if (method.IsRemoveOn || method.IsAddOn || method.IsOther)
                    {
                        var @event = CecilHelper.GetEvent(method);
                        if (@event != null && CecilHelper.HasCustomAttributesOfType(@event, typeof(ExludedMemberAttribute)))
                            continue;
                    }

                    compilerGeneratedMembers.Add(method);
                }

                var generatedTypes = method.Body.Instructions.Where(t => t.Operand is MemberReference && ((MemberReference)t.Operand).DeclaringType is TypeDefinition && CecilHelper.HasCustomAttributesOfType(((MemberReference)t.Operand).DeclaringType, typeof(CompilerGeneratedAttribute)) && string.IsNullOrEmpty(((MemberReference)t.Operand).DeclaringType.Namespace) && ((MemberReference)t.Operand).DeclaringType.DeclaringType == null).Select(t => (TypeDefinition)((MemberReference)t.Operand).DeclaringType);
                globalCompilerGeneratedTypes.AddRange(generatedTypes);
            }

            var fieldInitIlCodes = adviceDefinition.AdviceDeclaration.Fields.Where(t => !t.IsSpecialName && !CecilHelper.HasCustomAttributesOfType((IMemberDefinition)t, typeof(ExludedMemberAttribute))).Select(t => CecilHelper.GetFieldConstantInitILBlock(t)).Where(t => t != null);
            var fieldGlobalCompilerGeneratedTypes = fieldInitIlCodes.SelectMany(t => t.Instructions).Where(t => t.Operand is MemberReference && ((MemberReference)t.Operand).DeclaringType is TypeDefinition && CecilHelper.HasCustomAttributesOfType(((MemberReference)t.Operand).DeclaringType, typeof(CompilerGeneratedAttribute)) && string.IsNullOrEmpty(((MemberReference)t.Operand).DeclaringType.Namespace) && ((MemberReference)t.Operand).DeclaringType.DeclaringType == null).Select(t => (TypeDefinition)((MemberReference)t.Operand).DeclaringType);
            globalCompilerGeneratedTypes.AddRange(fieldGlobalCompilerGeneratedTypes);
            if (compilerGeneratedMembers.Any())
            {
                var typeMembersAdviceDefinition = new AdviceDefinition(adviceDefinition.AdviceDeclaration, AdviceKinds.TypeMembers, true);
                foreach (var compilerGeneratedMember in compilerGeneratedMembers)
                {
                    var member = typeMembersAdviceDefinition.Add(compilerGeneratedMember, AdviceMemberKinds.Method);
                    adviceDefinition.CompiledGeneratedAdviceMemberDefinitions.Add(member);
                }

                _ConcernsContainer.Add(typeMembersAdviceDefinition);
            }

            if (globalCompilerGeneratedTypes.Any())
            {
                adviceDefinition.AddCompilerGeneratedTypes(globalCompilerGeneratedTypes.Distinct());
            }
        }

        void _VisitPrototypeItemReferences(AdviceMemberDefinition adviceMemberDefinition)
        {
            var memberReferences = ((MethodDefinition)adviceMemberDefinition.Member).Body.Instructions.Where(i => i.Operand is IMemberDefinition);
            var prototypeReferences = new List<int>();
            var list = memberReferences.Where(t => CecilHelper.HasCustomAttributesOfType((IMemberDefinition)t.Operand, typeof(PrototypeItemDeclarationAttribute))).Select(a => a.Offset);
            prototypeReferences.AddRange(list);
            list = memberReferences.Where(t => CecilHelper.HasCustomAttributesOfType((IMemberDefinition)t.Operand, typeof(PrototypeTypeDeclarationAttribute))).Select(a => a.Offset);
            prototypeReferences.AddRange(list);
            list = memberReferences.Where(t => t.Operand is TypeReference && _IsAdviceType((TypeReference)t.Operand)).Select(a => a.Offset);
            prototypeReferences.AddRange(list);
            list = memberReferences.Where(t => t.Operand is MemberReference && ((MemberReference)t.Operand).DeclaringType != null && adviceMemberDefinition.ParentAdviceDefinion.CompiledGeneratedAdviceMemberDefinitions.Any(a => a.AdviceMemberKind == AdviceMemberKinds.Type && ((MemberReference)t.Operand).DeclaringType.FullName == ((TypeDefinition)a.Member).FullName)).Select(a => a.Offset);
            prototypeReferences.AddRange(list);
            list = memberReferences.Where(t => t.Operand is MethodDefinition && adviceMemberDefinition.ParentAdviceDefinion.CompiledGeneratedAdviceMemberDefinitions.Any(a => a.AdviceMemberKind == AdviceMemberKinds.Method && ((MethodDefinition)t.Operand).FullName == ((MethodDefinition)a.Member).FullName)).Select(a => a.Offset);
            prototypeReferences.AddRange(list);
            adviceMemberDefinition.PrototypeReferenceInstructionOffsets = prototypeReferences;
        }

        bool _IsAdviceType(TypeReference typeReference)
        {
            var typeDefinition = CecilHelper.Resolve(typeReference);
            if (typeDefinition.Interfaces.Any(t => t.InterfaceType.FullName == typeof(ITypesAdviceDeclaration).FullName))
                return true;
            if (typeDefinition.DeclaringType != null)
                return _IsAdviceType(typeDefinition.DeclaringType);
            return false;
        }
    }
}