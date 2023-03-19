// Author:
//
// T. Freyburger (t.freyburger@gmail.com)
//
// Copyright (c)  Thierry Freyburger
//
// Licensed under the GPLV3 license.
////
using System.IO;
using AspectDN.Aspect.Weaving.IConcerns;
using AspectDN.Aspect.Weaving.IJoinpoints;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using CecilCollection = Mono.Collections.Generic;
using AspectDN.Common;
using AspectDN.Aspect.Weaving.Marker;
using Foundation.Common.Error;

namespace AspectDN.Aspect.Weaving
{
    sealed public class Weaver : IWeaver
    {
        IEnumerable<AssemblyDefinition> _AssemblyTargets;
        List<IError> _Errors;
        List<(AssemblyNameReference assemblyReference, IAssemblyJoinpoint joinpoint)> _NewAssemblyReferences;
        string _OutputDirectory;
        internal List<WeaveItem> WeaveItems;
        internal List<WeaveItemMember> WeaveItemMembers;
        internal IEnumerable<WeaveItem> SafeWeaveItems => WeaveItems.Where(t => !t.OnError);
        internal IEnumerable<WeaveItemMember> SafeWeaveItemMembers => WeaveItemMembers.Where(t => !t.WeaveItem.OnError);
        internal IEnumerable<IError> Errors => _Errors;
        internal IJoinpointsContainer JointpointsContainer { get; }

        internal IAspectsContainer AspectContainer { get; }

        internal Dictionary<string, PrototypeTypeMapping> PrototypeTypeMappings;
        internal Weaver(IJoinpointsContainer joinpointsContainer, IAspectsContainer aspectsContainer, IEnumerable<AssemblyDefinition> assemblyTargets, string outputDirectory = null)
        {
            JointpointsContainer = joinpointsContainer;
            AspectContainer = aspectsContainer;
            _AssemblyTargets = assemblyTargets;
            _NewAssemblyReferences = new List<(AssemblyNameReference assemblyReference, IAssemblyJoinpoint joinpoint)>();
            _OutputDirectory = Helper.GetFullPath(outputDirectory);
            _Errors = new List<IError>();
            WeaveItems = new List<WeaveItem>();
            WeaveItemMembers = new List<WeaveItemMember>();
            PrototypeTypeMappings = new Dictionary<string, PrototypeTypeMapping>();
            _Errors.AddRange(aspectsContainer.Errors);
        }

        internal void Weave()
        {
            ClearOutputDirectory();
            Weave(new WeaveItemBuilder(this).CreateWeaveItems());
            if (!_Errors.Any())
                SaveAssemblyChanges();
        }

        internal void AddReference(string assemblyReferencePathName, string jointpointAssemblyTarget)
        {
            var joinpointAssembly = (IAssemblyJoinpoint)JointpointsContainer.GetAssemblies(new Func<ModuleDefinition, MethodDefinition, bool>((t, m) => t.Assembly.MainModule.Name == jointpointAssemblyTarget)).First();
            var assemblyReference = CecilHelper.GetAssembly(assemblyReferencePathName, false).Name;
            _NewAssemblyReferences.Add((assemblyReference, joinpointAssembly));
        }

        internal void AddNewReferenceToTarget(AssemblyDefinition assemblyReference, AssemblyNameDefinition jointpointAssemblyTargetName)
        {
            var joinpointAssembly = (IAssemblyJoinpoint)JointpointsContainer.GetAssemblies(new Func<ModuleDefinition, MethodDefinition, bool>((t, m) => t.Assembly.Name.FullName == jointpointAssemblyTargetName.FullName)).First();
            if (!joinpointAssembly.Assembly.MainModule.AssemblyReferences.Any(t => t.FullName == assemblyReference.FullName))
            {
                joinpointAssembly.SetAsChanged();
                joinpointAssembly.Assembly.MainModule.AssemblyReferences.Add((assemblyReference.Name));
            }
        }

        internal void Weave(WeaveItemMember weaveItemMember)
        {
            switch (weaveItemMember)
            {
                case NewAssemblyType newAssemblyType:
                    newAssemblyType.JoinpointAssembly.MainModule.Types.Add(newAssemblyType.ClonedType);
                    if (!_AspectMarketAlreadyExists(newAssemblyType.JoinpointAssembly.CustomAttributes, newAssemblyType.FullAspectDeclarationName, newAssemblyType.FullAspectRepositoryName))
                    {
                        var marker = WeaverHelper.CreateAspectDNMarker(newAssemblyType.TargetAssembly.MainModule, newAssemblyType.FullAspectDeclarationName, newAssemblyType.FullAspectRepositoryName, DateTime.Now);
                        if (marker != null)
                            newAssemblyType.JoinpointAssembly.CustomAttributes.Add(marker);
                    }

                    break;
                case NewNestedType newNestedType:
                    newNestedType.JoinpointType.NestedTypes.Add(newNestedType.ClonedType);
                    if (!_AspectMarketAlreadyExists(newNestedType.JoinpointType.CustomAttributes, newNestedType.FullAspectDeclarationName, newNestedType.FullAspectRepositoryName))
                    {
                        var marker = WeaverHelper.CreateAspectDNMarker(newNestedType.TargetAssembly.MainModule, newNestedType.FullAspectDeclarationName, newNestedType.FullAspectRepositoryName, DateTime.Now);
                        if (marker != null)
                            newNestedType.JoinpointType.CustomAttributes.Add(marker);
                    }

                    break;
                case NewFieldMember newFiedMember:
                    newFiedMember.JoinpointDeclaringType.Fields.Add(newFiedMember.ClonedField);
                    if (!_AspectMarketAlreadyExists(newFiedMember.JoinpointDeclaringType.CustomAttributes, newFiedMember.FullAspectDeclarationName, newFiedMember.FullAspectRepositoryName))
                    {
                        var marker = WeaverHelper.CreateAspectDNMarker(newFiedMember.TargetAssembly.MainModule, newFiedMember.FullAspectDeclarationName, newFiedMember.FullAspectRepositoryName, DateTime.Now);
                        if (marker != null)
                            newFiedMember.JoinpointDeclaringType.CustomAttributes.Add(marker);
                    }

                    break;
                case NewMethodMember newMethodMember:
                    newMethodMember.JoinpointDeclaringType.Methods.Add(newMethodMember.ClonedMethod);
                    if (newMethodMember.OverloadedTypeMembers.Any())
                        _SetOverloadedTypeMembers(newMethodMember.OverloadedTypeMembers);
                    if (!_AspectMarketAlreadyExists(newMethodMember.JoinpointDeclaringType.CustomAttributes, newMethodMember.FullAspectDeclarationName, newMethodMember.FullAspectRepositoryName))
                    {
                        var marker = WeaverHelper.CreateAspectDNMarker(newMethodMember.TargetAssembly.MainModule, newMethodMember.FullAspectDeclarationName, newMethodMember.FullAspectRepositoryName, DateTime.Now);
                        if (marker != null)
                            newMethodMember.JoinpointDeclaringType.CustomAttributes.Add(marker);
                    }

                    break;
                case NewPropertyMember newPropertyMember:
                    newPropertyMember.JoinpointDeclaringType.Properties.Add(newPropertyMember.ClonedProperty);
                    if (!_AspectMarketAlreadyExists(newPropertyMember.JoinpointDeclaringType.CustomAttributes, newPropertyMember.FullAspectDeclarationName, newPropertyMember.FullAspectRepositoryName))
                    {
                        var marker = WeaverHelper.CreateAspectDNMarker(newPropertyMember.TargetAssembly.MainModule, newPropertyMember.FullAspectDeclarationName, newPropertyMember.FullAspectRepositoryName, DateTime.Now);
                        if (marker != null)
                            newPropertyMember.JoinpointDeclaringType.CustomAttributes.Add(marker);
                    }

                    break;
                case NewEventMember newEventMember:
                    newEventMember.JoinpointDeclaringType.Events.Add(newEventMember.ClonedEvent);
                    if (!_AspectMarketAlreadyExists(newEventMember.JoinpointDeclaringType.CustomAttributes, newEventMember.FullAspectDeclarationName, newEventMember.FullAspectRepositoryName))
                    {
                        var marker = WeaverHelper.CreateAspectDNMarker(newEventMember.TargetAssembly.MainModule, newEventMember.FullAspectDeclarationName, newEventMember.FullAspectRepositoryName, DateTime.Now);
                        if (marker != null)
                            newEventMember.JoinpointDeclaringType.CustomAttributes.Add(marker);
                    }

                    break;
                case NewConstructorMember newConstructorMember:
                    newConstructorMember.JoinpointDeclaringType.Methods.Add(newConstructorMember.ClonedMethod);
                    if (!_AspectMarketAlreadyExists(newConstructorMember.JoinpointDeclaringType.CustomAttributes, newConstructorMember.FullAspectDeclarationName, newConstructorMember.FullAspectRepositoryName))
                    {
                        var marker = WeaverHelper.CreateAspectDNMarker(newConstructorMember.TargetAssembly.MainModule, newConstructorMember.FullAspectDeclarationName, newConstructorMember.FullAspectRepositoryName, DateTime.Now);
                        if (marker != null)
                            newConstructorMember.JoinpointDeclaringType.CustomAttributes.Add(marker);
                    }

                    break;
                case NewOperatorMember newOperatorMember:
                    newOperatorMember.JoinpointDeclaringType.Methods.Add(newOperatorMember.ClonedMethod);
                    if (!_AspectMarketAlreadyExists(newOperatorMember.JoinpointDeclaringType.CustomAttributes, newOperatorMember.FullAspectDeclarationName, newOperatorMember.FullAspectRepositoryName))
                    {
                        var marker = WeaverHelper.CreateAspectDNMarker(newOperatorMember.TargetAssembly.MainModule, newOperatorMember.FullAspectDeclarationName, newOperatorMember.FullAspectRepositoryName, DateTime.Now);
                        if (marker != null)
                            newOperatorMember.JoinpointDeclaringType.CustomAttributes.Add(marker);
                    }

                    break;
                case NewEnumMember newEnumMember:
                    newEnumMember.ResolvedDeclaringType.Fields.Add(newEnumMember.ClonedField);
                    if (!_AspectMarketAlreadyExists(newEnumMember.ResolvedDeclaringType.CustomAttributes, newEnumMember.FullAspectDeclarationName, newEnumMember.FullAspectRepositoryName))
                    {
                        var marker = WeaverHelper.CreateAspectDNMarker(newEnumMember.TargetAssembly.MainModule, newEnumMember.FullAspectDeclarationName, newEnumMember.FullAspectRepositoryName, DateTime.Now);
                        if (marker != null)
                            newEnumMember.ResolvedDeclaringType.CustomAttributes.Add(marker);
                    }

                    break;
                case NewAttributeMember newAttributeMember:
                    newAttributeMember.TargetMember.CustomAttributes.Add(newAttributeMember.ClonedAttribute);
                    if (!_AspectMarketAlreadyExists(newAttributeMember.TargetMember.CustomAttributes, newAttributeMember.FullAspectDeclarationName, newAttributeMember.FullAspectRepositoryName))
                    {
                        var marker = WeaverHelper.CreateAspectDNMarker(newAttributeMember.TargetAssembly.MainModule, newAttributeMember.FullAspectDeclarationName, newAttributeMember.FullAspectRepositoryName, DateTime.Now);
                        if (marker != null)
                            newAttributeMember.TargetMember.CustomAttributes.Add(marker);
                    }

                    break;
                case NewInheritedType newInheritedType:
                    if (newInheritedType.IsInterface)
                    {
                        newInheritedType.JoinpointType.Interfaces.Add(newInheritedType.TargetInterfaceImplementation);
                        if (newInheritedType.OverloadedTypeMembers.Any())
                            _SetOverloadedTypeMembers(newInheritedType.OverloadedTypeMembers);
                    }
                    else
                    {
                        newInheritedType.JoinpointType.BaseType = newInheritedType.TargetBaseType;
                        foreach (var overloadedConstructor in newInheritedType.OverloadConstructors)
                            overloadedConstructor.JoinpointConstructor.Body = overloadedConstructor.NewJoinpointConstructorBody;
                    }

                    if (!_AspectMarketAlreadyExists(newInheritedType.JoinpointType.CustomAttributes, newInheritedType.FullAspectDeclarationName, newInheritedType.FullAspectRepositoryName))
                    {
                        var marker = WeaverHelper.CreateAspectDNMarker(newInheritedType.TargetAssembly.MainModule, newInheritedType.FullAspectDeclarationName, newInheritedType.FullAspectRepositoryName, DateTime.Now);
                        if (marker != null)
                            newInheritedType.JoinpointType.CustomAttributes.Add(marker);
                    }

                    break;
                case NewCode newCode:
                    newCode.TargetMethod.Body = newCode.NewMethodBody;
                    if (!_AspectMarketAlreadyExists(newCode.TargetMethod.CustomAttributes, newCode.FullAspectDeclarationName, newCode.FullAspectRepositoryName))
                    {
                        var marker = WeaverHelper.CreateAspectDNMarker(newCode.TargetAssembly.MainModule, newCode.FullAspectDeclarationName, newCode.FullAspectRepositoryName, DateTime.Now);
                        if (marker != null)
                            newCode.TargetMethod.CustomAttributes.Add(marker);
                    }

                    break;
                case NewChangeValue newChangeValue:
                    newChangeValue.TargetMethod.Body = newChangeValue.NewMethodBody;
                    if (!_AspectMarketAlreadyExists(newChangeValue.TargetMethod.CustomAttributes, newChangeValue.FullAspectDeclarationName, newChangeValue.FullAspectRepositoryName))
                    {
                        var marker = WeaverHelper.CreateAspectDNMarker(newChangeValue.TargetAssembly.MainModule, newChangeValue.FullAspectDeclarationName, newChangeValue.FullAspectRepositoryName, DateTime.Now);
                        if (marker != null)
                            newChangeValue.TargetMethod.CustomAttributes.Add(marker);
                    }

                    break;
                case NewFieldInitCode newFieldInitCode:
                    newFieldInitCode.TargetConstructor.Body = newFieldInitCode.NewMethodBody;
                    if (!_AspectMarketAlreadyExists(newFieldInitCode.TargetMethod.CustomAttributes, newFieldInitCode.FullAspectDeclarationName, newFieldInitCode.FullAspectRepositoryName))
                    {
                        var marker = WeaverHelper.CreateAspectDNMarker(newFieldInitCode.TargetAssembly.MainModule, newFieldInitCode.FullAspectDeclarationName, newFieldInitCode.FullAspectRepositoryName, DateTime.Now);
                        if (marker != null)
                            newFieldInitCode.TargetMethod.CustomAttributes.Add(marker);
                    }

                    break;
                default:
                    throw new NotImplementedException();
            }

            JointpointsContainer.SetAsChanged(weaveItemMember.WeaveItem.Joinpoint);
        }

        bool _AspectMarketAlreadyExists(IEnumerable<CustomAttribute> customAttributes, string adviceName, string repositoryName)
        {
            return customAttributes.Any(t => t.AttributeType.FullName == typeof(AspectDNMarkerAttribute).FullName && CecilHelper.GetCustomAttributePropertyValue(t, nameof(AspectDNMarkerAttribute.AdviceName)).ToString() == adviceName && CecilHelper.GetCustomAttributePropertyValue(t, nameof(AspectDNMarkerAttribute.AspectRepositoryName)).ToString() == repositoryName);
        }

        internal void AddError(IError error)
        {
            _Errors.Add(error);
        }

        internal void ClearOutputDirectory()
        {
            if (Directory.Exists(_OutputDirectory))
                Directory.Delete(_OutputDirectory, true);
        }

        internal void SaveAssemblyChanges()
        {
            if (!Directory.Exists(_OutputDirectory))
                Directory.CreateDirectory(_OutputDirectory);
            foreach (IAssemblyJoinpoint assemblyJointpoint in JointpointsContainer.GetAssemblies().Where(t => t.HasChanged))
            {
                string filename = $"{assemblyJointpoint.ModuleDefinition.Name}.dll";
                if (!string.IsNullOrEmpty(_OutputDirectory))
                    filename = Path.Combine(_OutputDirectory, Path.GetFileNameWithoutExtension(filename));
                assemblyJointpoint.Assembly.Write(filename);
                var references = assemblyJointpoint.ModuleDefinition.AssemblyReferences;
                foreach (var assemblyReference in references)
                {
                    if (!CecilHelper.IsAssemblyInGAC(assemblyReference.FullName))
                    {
                        var assembly = assemblyJointpoint.Assembly.MainModule.AssemblyResolver.Resolve(assemblyReference);
                        if (!string.IsNullOrEmpty(assembly.MainModule.FileName))
                        {
                            var from = assembly.MainModule.FileName;
                            var to = $"{_OutputDirectory}\\{Path.GetFileName(from)}";
                            if (File.Exists(to))
                                File.Delete(to);
                            File.Copy(from, to);
                        }
                    }
                }
            }
        }

        internal void Close()
        {
            if (_AssemblyTargets == null)
                return;
            foreach (var assembluDefinition in _AssemblyTargets)
                assembluDefinition.Dispose();
        }

        internal void Weave(WeaveItemBuilder weaveItemBuilder)
        {
            foreach (var newAssemblyReference in _NewAssemblyReferences)
            {
                newAssemblyReference.joinpoint.Assembly.MainModule.AssemblyReferences.Add(newAssemblyReference.assemblyReference);
                newAssemblyReference.joinpoint.SetAsChanged();
            }

            foreach (WeaveItemMember weaveItemMember in weaveItemBuilder.SafeWeaveItemMembers)
                Weave(weaveItemMember);
            var weaveItemMembersByTargetAssemblies = weaveItemBuilder.SafeWeaveItemMembers.Where(t => t.WeaveItem.CompilerGeneratedTypeMappings.Any()).GroupBy(t => t.WeaveItem.Joinpoint.Assembly);
            foreach (var weaveItemMembersByTargetAssembly in weaveItemMembersByTargetAssemblies)
            {
                var joinpointAssembly = JointpointsContainer.GetAssemblies((t, m) => t.Assembly == weaveItemMembersByTargetAssembly.Key).First();
                foreach (var compilerGeneratedType in weaveItemMembersByTargetAssembly.SelectMany(t => t.WeaveItem.CompilerGeneratedTypeMappings).Distinct())
                {
                    joinpointAssembly.Assembly.MainModule.Types.Add(compilerGeneratedType.targetCompilerGeneratedTypeDefinition);
                }
            }
        }

        void _SetOverloadedTypeMembers(IEnumerable<IMemberDefinition> overloadedTypeMembers)
        {
            var addedAttributs = MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final;
            foreach (var overloadedMember in overloadedTypeMembers)
            {
                switch (overloadedMember)
                {
                    case MethodDefinition method:
                        method.Attributes |= addedAttributs;
                        break;
                    case PropertyDefinition property:
                        if (property.GetMethod != null)
                            property.GetMethod.Attributes |= addedAttributs;
                        if (property.SetMethod != null)
                            property.SetMethod.Attributes |= addedAttributs;
                        property.OtherMethods.ToList().ForEach(t => t.Attributes |= addedAttributs);
                        break;
                    case EventDefinition @event:
                        if (@event.AddMethod != null)
                            @event.AddMethod.Attributes |= addedAttributs;
                        if (@event.RemoveMethod != null)
                            @event.RemoveMethod.Attributes |= addedAttributs;
                        @event.OtherMethods.ToList().ForEach(t => t.Attributes |= addedAttributs);
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
        }

#region IWeaver
        void IWeaver.Weave()
        {
            this.Weave();
        }

        void IWeaver.AddReference(string assemblyRefName, string assemblyTargetPathName)
        {
            this.AddReference(assemblyRefName, assemblyTargetPathName);
        }
#endregion
    }
}