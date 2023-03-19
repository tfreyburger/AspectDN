// Author:
//
// T. Freyburger (t.freyburger@gmail.com)
//
// Copyright (c)  Thierry Freyburger
//
// Licensed under the GPLV3 license.
////
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using AspectDN.Aspect.Weaving.IConcerns;
using System.Collections;
using AspectDN.Common;
using System.Reflection;
using Mono.Cecil.Cil;
using TokenizerDN.Common;
using Foundation.Common.Error;

namespace AspectDN.Aspect.Concerns
{
    internal class ConcernsContainer : IAspectsContainer
    {
        public static IAspectsContainer Create(IEnumerable<byte[]> aspectFiles, params string[] searchAspectRepositoryDirectoryNames)
        {
            var concernsContainer = new ConcernsContainer(aspectFiles, searchAspectRepositoryDirectoryNames);
            concernsContainer.Visit();
            return concernsContainer;
        }

        public static IAspectsContainer Create(IEnumerable<string> aspectFileNames, params string[] searchAspectFilesDirectoryNames)
        {
            var concernsContainer = new ConcernsContainer(aspectFileNames, searchAspectFilesDirectoryNames);
            concernsContainer.Visit();
            return concernsContainer;
        }

        public static IAspectsContainer Create(string aspectRepositoryDirectoryName)
        {
            DirectoryInfo assembliesDirectory = new DirectoryInfo(aspectRepositoryDirectoryName);
            var files = Directory.GetFiles(aspectRepositoryDirectoryName, "*.aspdn", SearchOption.AllDirectories);
            return Create(files, files.Select(t => Path.GetDirectoryName(t)).Distinct().ToArray());
        }

        List<ICompilerError> _Errors;
        List<AspectDefinition> _AspectDefinitions;
        Dictionary<string, AdviceDefinition> _AdviceDefinitions;
        Dictionary<string, PointcutDefinition> _PointcutDefinitions;
        List<AspectDeclaration> _AspectDeclarations;
        Dictionary<string, TypeDefinition> _PrototypeTypeDeclarations;
        Dictionary<string, PrototypeTypeMappingDeclaration> _PrototypeTypeMappingDeclarations;
        List<IPrototypeTypeMappingDefinition> _PrototypeTypeMappingDefinitions;
        IEnumerable<byte[]> AspectFiles { get; }

        internal string[] SearchAspectFilesDirectoryNames { get; }

        internal IEnumerable<IAspectDefinition> AspectDefinitions => _AspectDefinitions;
        internal IEnumerable<IPointcutDefinition> PointcutDefinitions => _PointcutDefinitions.Values;
        internal IEnumerable<IAdviceDefinition> AdviceDefinitions => _AdviceDefinitions.Values;
        internal IEnumerable<IError> Errors => _Errors;
        internal ReaderParameters ReaderParameters { get; }

        internal AspectDNAssemblyResolver AssemblyResolver { get; }

        internal ConcernsContainer(string[] searchAspectFilesDirectoryNames)
        {
            _AspectDefinitions = new List<AspectDefinition>(100000);
            _AdviceDefinitions = new Dictionary<string, AdviceDefinition>(100000);
            _AspectDeclarations = new List<AspectDeclaration>(100000);
            _PointcutDefinitions = new Dictionary<string, PointcutDefinition>(100000);
            _PrototypeTypeDeclarations = new Dictionary<string, TypeDefinition>(100000);
            _PrototypeTypeMappingDeclarations = new Dictionary<string, PrototypeTypeMappingDeclaration>(100000);
            _Errors = new List<ICompilerError>();
            _PrototypeTypeMappingDefinitions = new List<IPrototypeTypeMappingDefinition>(100000);
            ReaderParameters = new ReaderParameters(ReadingMode.Immediate);
            ReaderParameters.ReadWrite = ReaderParameters.ReadWrite;
            AssemblyResolver = new AspectDNAssemblyResolver();
            ReaderParameters.AssemblyResolver = AssemblyResolver;
            AssemblyResolver.AddSearchDirectory("aspdn");
            AssemblyResolver.AddSearchDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            searchAspectFilesDirectoryNames.ToList().ForEach(t => AssemblyResolver.AddSearchDirectory(t));
            SearchAspectFilesDirectoryNames = searchAspectFilesDirectoryNames ?? new string[0];
        }

        internal ConcernsContainer(IEnumerable<string> aspectFileFilenames, string[] searchAspectFilesDirectoryNames) : this(searchAspectFilesDirectoryNames)
        {
            AspectFiles = new List<byte[]>(aspectFileFilenames.Count());
            foreach (var aspectFileName in aspectFileFilenames)
                ((List<byte[]>)AspectFiles).Add(File.ReadAllBytes(aspectFileName));
        }

        internal ConcernsContainer(IEnumerable<byte[]> aspectFiles, string[] searchAspectRepositoryDirectoryNames) : this(searchAspectRepositoryDirectoryNames)
        {
            AspectFiles = aspectFiles;
        }

        internal void Visit()
        {
            foreach (var aspectFile in AspectFiles)
                Visit(aspectFile);
            _ImportDeductedReferencedPrototypeTypes();
            foreach (var prototypeTypeMappingDeclaration in _PrototypeTypeMappingDeclarations.Values)
                _BuildPrototypeTypeMapping(prototypeTypeMappingDeclaration);
            var aspectId = (long)1;
            foreach (var aspectDeclaration in _AspectDeclarations)
                _BuildAspectDefinition(aspectId++, aspectDeclaration);
            _ImportMappingFromPrototypeTypes();
        }

        internal void Visit(byte[] aspectFile)
        {
            new AspectFileVisitor(aspectFile, this).GetAspects();
        }

        internal void Visit(string aspectFilename)
        {
            Visit(File.ReadAllBytes(aspectFilename));
        }

        internal void Add(AspectDeclaration aspectDeclaration)
        {
            if (!_AspectDeclarations.Any(t => t.FullName == aspectDeclaration.FullName))
                _AspectDeclarations.Add(aspectDeclaration);
        }

        internal void Add(AdviceDefinition adviceDefinition)
        {
            if (!_AdviceDefinitions.ContainsKey(adviceDefinition.FullAdviceName))
                _AdviceDefinitions.Add(adviceDefinition.FullAdviceName, adviceDefinition);
        }

        internal void Add(PointcutDefinition pointcutDefinition)
        {
            if (!_PointcutDefinitions.ContainsKey(pointcutDefinition.FullDeclarationName))
                _PointcutDefinitions.Add(pointcutDefinition.FullDeclarationName, pointcutDefinition);
        }

        internal void Add(TypeDefinition prototypeDeclaration)
        {
            if (!_PrototypeTypeDeclarations.ContainsKey(prototypeDeclaration.FullName))
                _PrototypeTypeDeclarations.Add(prototypeDeclaration.FullName, prototypeDeclaration);
        }

        internal void Add(ICompilerError compilerError)
        {
            _Errors.Add(compilerError);
        }

        internal void Add(PrototypeTypeMappingDeclaration prototypeTypeMappingDeclaration)
        {
            if (!_PrototypeTypeMappingDeclarations.ContainsKey(prototypeTypeMappingDeclaration.FullPrototypeName))
                _PrototypeTypeMappingDeclarations.Add(prototypeTypeMappingDeclaration.FullPrototypeName, prototypeTypeMappingDeclaration);
        }

        internal void Add(AspectDefinition aspectDefinition)
        {
            _AspectDefinitions.Add(aspectDefinition);
        }

        void _BuildPrototypeTypeMapping(PrototypeTypeMappingDeclaration prototypeTypeMappingDeclaration)
        {
            var prototypeType = _PrototypeTypeDeclarations[prototypeTypeMappingDeclaration.FullPrototypeName];
            _PrototypeTypeMappingDefinitions.Add(new PrototypeTypeMappingDefinition(prototypeType, prototypeTypeMappingDeclaration));
        }

        void _BuildAspectDefinition(long aspectId, AspectDeclaration aspectDeclaration)
        {
            PointcutDefinition pointcut = null;
            if (_PointcutDefinitions.ContainsKey(aspectDeclaration.FullPointcutName))
                pointcut = _PointcutDefinitions[aspectDeclaration.FullPointcutName];
            else
            {
                Add(CompilerErrorFactory.GetCompilerError("InvalidPointcutReference", SourceLocation.Empy, $"{aspectDeclaration.FullPointcutName}", aspectDeclaration.FullName));
            }

            AdviceDefinition advice = null;
            if (_AdviceDefinitions.ContainsKey(aspectDeclaration.FullAdviceName))
                advice = _AdviceDefinitions[aspectDeclaration.FullAdviceName];
            else
            {
                Add(CompilerErrorFactory.GetCompilerError("InvalidAdviceReference", SourceLocation.Empy, $"{aspectDeclaration.FullAdviceName}", aspectDeclaration.FullName));
            }

            if (advice == null || pointcut == null)
                return;
            var prototypeItemMappingDefinitions = aspectDeclaration.GetPrototypeItemMappingDefinitions(advice);
            switch (aspectDeclaration.AspectKind)
            {
                case AspectKinds.CodeAspect:
                    _BuildCodeAspectDefinition(aspectId, aspectDeclaration, advice, pointcut, prototypeItemMappingDefinitions);
                    break;
                case AspectKinds.ChangeValueAspect:
                    _BuildChangeValueAspectDefinition(aspectId, aspectDeclaration, advice, pointcut, prototypeItemMappingDefinitions);
                    break;
                case AspectKinds.TypeMembersApsect:
                    _BuildAspectTypeMembersDefinition(aspectId, aspectDeclaration, advice, pointcut, prototypeItemMappingDefinitions);
                    break;
                case AspectKinds.TypesAspect:
                    _BuildAspectTypeDefinition(aspectId, aspectDeclaration, advice, pointcut, prototypeItemMappingDefinitions);
                    break;
                case AspectKinds.EnumMembersAspect:
                    _BuildAspectEnumMembersDefinition(aspectId, aspectDeclaration, advice, pointcut, prototypeItemMappingDefinitions);
                    break;
                case AspectKinds.AttributesAspect:
                    _BuildAspectAttributesDefinition(aspectId, aspectDeclaration, advice, pointcut, prototypeItemMappingDefinitions);
                    break;
                case AspectKinds.InterfaceMembersAspect:
                    _BuildAspectInterfaceMembersDefinition(aspectId, aspectDeclaration, advice, pointcut, prototypeItemMappingDefinitions);
                    break;
                case AspectKinds.InheritedTypesAspect:
                    _BuildInheritedTypesAspectDefinition(aspectId, aspectDeclaration, advice, pointcut, prototypeItemMappingDefinitions);
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        void _BuildCodeAspectDefinition(long aspectId, AspectDeclaration aspectDeclaration, AdviceDefinition advice, PointcutDefinition pointcut, IEnumerable<PrototypeItemMappingDefinition> prototypeMappings)
        {
            var aspectDefinition = new AspectCodeDefinition(aspectId, aspectDeclaration, advice, pointcut, prototypeMappings, aspectDeclaration.ExecutionTime, aspectDeclaration.ControlFlow);
            Add(aspectDefinition);
            _GenerateCompilerGeneratedAspect(aspectId, aspectDeclaration, advice, pointcut, prototypeMappings);
        }

        void _GenerateCompilerGeneratedAspect(long aspectId, AspectDeclaration aspectDeclaration, AdviceDefinition advice, PointcutDefinition pointcut, IEnumerable<PrototypeItemMappingDefinition> prototypeMappings)
        {
            var compiledGeneratedAdvices = _AdviceDefinitions.Where(t => t.Value.AdviceDeclaration.FullName == advice.AdviceDeclaration.FullName && t.Value.IsCompiledGenerated).Select(t => t.Value);
            foreach (var compiledGeneratedAdvice in compiledGeneratedAdvices)
            {
                switch (compiledGeneratedAdvice.AdviceKind)
                {
                    case AdviceKinds.Type:
                        var compiledAspectTypeDefinition = new AspectTypeDefinition(aspectId, aspectDeclaration, compiledGeneratedAdvice, pointcut, prototypeMappings, null);
                        foreach (var adviceMember in compiledGeneratedAdvice.AdviceMemberDefinitions)
                            compiledAspectTypeDefinition.AddMember(new AspectMemberDefinition(compiledAspectTypeDefinition, adviceMember));
                        Add(compiledAspectTypeDefinition);
                        break;
                    case AdviceKinds.TypeMembers:
                        var compiledAspectTypeMembersDefinition = new AspectTypeMembersDefinition(aspectId, aspectDeclaration, compiledGeneratedAdvice, pointcut, prototypeMappings, AspectMemberModifiers.none);
                        foreach (var adviceMember in compiledGeneratedAdvice.AdviceMemberDefinitions)
                            compiledAspectTypeMembersDefinition.AddMember(new AspectMemberDefinition(compiledAspectTypeMembersDefinition, adviceMember));
                        Add(compiledAspectTypeMembersDefinition);
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        void _BuildChangeValueAspectDefinition(long aspectId, AspectDeclaration aspectDeclaration, AdviceDefinition advice, PointcutDefinition pointcut, IEnumerable<PrototypeItemMappingDefinition> prototypeMappings)
        {
            var aspectDefinition = new ChangeValueAspectDefinition(aspectId, aspectDeclaration, advice, pointcut, prototypeMappings, aspectDeclaration.ControlFlow);
            Add(aspectDefinition);
            _GenerateCompilerGeneratedAspect(aspectId, aspectDeclaration, advice, pointcut, prototypeMappings);
        }

        void _BuildInheritedTypesAspectDefinition(long aspectId, AspectDeclaration aspectDeclaration, AdviceDefinition advice, PointcutDefinition pointcut, IEnumerable<PrototypeItemMappingDefinition> prototypeItemMappingDefinitions)
        {
            var aspectDefinition = new InheritanceAspectDefinition(aspectId, aspectDeclaration, advice, pointcut, AspectKinds.InheritedTypesAspect, prototypeItemMappingDefinitions);
            var overrideConstructorDefinitions = aspectDefinition.Advice.AdviceDeclaration.Methods.Where(t => CecilHelper.HasCustomAttributesOfType(t, typeof(OverloadingConstructorAttribute)));
            if (overrideConstructorDefinitions.Any())
            {
                foreach (var overrideConstructorDefinition in overrideConstructorDefinitions)
                {
                    var baseConstructorParameterValueTypes = overrideConstructorDefinition.Body.Variables.Select(t => t.VariableType);
                    var iltree = ILTree.Create(overrideConstructorDefinition);
                    var baseConstructorParameterValues = new IEnumerable<Instruction>[overrideConstructorDefinition.Body.Variables.Count];
                    for (int i = 0; i < baseConstructorParameterValues.Length; i++)
                    {
                        var instruction = overrideConstructorDefinition.Body.Instructions.First(t => OpCodeDatas.Get(t.OpCode).OpCodeType == (OpCodeTypes.LocVar | OpCodeTypes.St) && CecilHelper.GetVariable(overrideConstructorDefinition.Body, OpCodeDatas.Get(t.OpCode), t.Operand) == overrideConstructorDefinition.Body.Variables[i]);
                        baseConstructorParameterValues[i] = iltree.GetFullDataBlock(instruction).ILNodes.Select(t => t.Instruction);
                    }

                    aspectDefinition.Add(new OverrideConstructorDefnition(overrideConstructorDefinition.Parameters, baseConstructorParameterValueTypes, baseConstructorParameterValues));
                }
            }

            foreach (var adviceMember in advice.AdviceMemberDefinitions)
                aspectDefinition.AddMember(new AspectMemberDefinition(aspectDefinition, adviceMember));
            Add(aspectDefinition);
        }

        void _BuildAspectInterfaceMembersDefinition(long aspectId, AspectDeclaration aspectDeclaration, AdviceDefinition advice, PointcutDefinition pointcut, IEnumerable<PrototypeItemMappingDefinition> prototypeMappings)
        {
            var aspectDefinition = new AspectDefinition(aspectId, aspectDeclaration, advice, pointcut, AspectKinds.InterfaceMembersAspect, prototypeMappings);
            foreach (var adviceMember in advice.AdviceMemberDefinitions)
                aspectDefinition.AddMember(new AspectMemberDefinition(aspectDefinition, adviceMember));
            Add(aspectDefinition);
        }

        void _BuildAspectTypeDefinition(long aspectId, AspectDeclaration aspectDeclaration, AdviceDefinition advice, PointcutDefinition pointcut, IEnumerable<PrototypeItemMappingDefinition> prototypeMappings)
        {
            var aspectDefinition = new AspectTypeDefinition(aspectId, aspectDeclaration, advice, pointcut, prototypeMappings, aspectDeclaration.NamespaceOrTypename);
            foreach (var adviceMember in advice.AdviceMemberDefinitions)
                aspectDefinition.AddMember(new AspectMemberDefinition(aspectDefinition, adviceMember));
            Add(aspectDefinition);
        }

        void _BuildAspectEnumMembersDefinition(long aspectId, AspectDeclaration aspectDeclaration, AdviceDefinition advice, PointcutDefinition pointcut, IEnumerable<PrototypeItemMappingDefinition> prototypeMappings)
        {
            var aspectDefinition = new AspectDefinition(aspectId, aspectDeclaration, advice, pointcut, AspectKinds.EnumMembersAspect, prototypeMappings);
            foreach (var adviceMember in advice.AdviceMemberDefinitions)
                aspectDefinition.AddMember(new AspectMemberDefinition(aspectDefinition, adviceMember));
            Add(aspectDefinition);
        }

        void _BuildAspectTypeMembersDefinition(long aspectId, AspectDeclaration aspectDeclaration, AdviceDefinition advice, PointcutDefinition pointcut, IEnumerable<PrototypeItemMappingDefinition> prototypeMappings)
        {
            var aspectDefinition = new AspectTypeMembersDefinition(aspectId, aspectDeclaration, advice, pointcut, prototypeMappings, aspectDeclaration.Modifiers);
            foreach (var adviceMember in advice.AdviceMemberDefinitions)
                aspectDefinition.AddMember(new AspectMemberDefinition(aspectDefinition, adviceMember));
            Add(aspectDefinition);
            _GenerateCompilerGeneratedAspect(aspectId, aspectDeclaration, advice, pointcut, prototypeMappings);
        }

        void _BuildAspectAttributesDefinition(long aspectId, AspectDeclaration aspectDeclaration, AdviceDefinition advice, PointcutDefinition pointcut, IEnumerable<PrototypeItemMappingDefinition> prototypeMappings)
        {
            var aspectDefinition = new AspectDefinition(aspectId, aspectDeclaration, advice, pointcut, AspectKinds.AttributesAspect, prototypeMappings);
            foreach (var adviceMember in advice.AdviceMemberDefinitions)
                aspectDefinition.AddMember(new AspectMemberDefinition(aspectDefinition, adviceMember));
            Add(aspectDefinition);
        }

        void _ImportMappingFromPrototypeTypes()
        {
            var aspectwithRefProtoTypes = _AspectDefinitions.Where(t => t.Advice.ReferencedPrototypeTypes.Any());
            var prototypeTypes = aspectwithRefProtoTypes.SelectMany(t => t.Advice.ReferencedPrototypeTypes).Select(t => t.GetElementType().Resolve()).GroupBy(t => t.FullName).Select(t => t.FirstOrDefault());
            var mappings = new List<(TypeDefinition prototypeType, IEnumerable<PrototypeItemMappingDefinition> mappingDefinitions)>();
            foreach (var prototypeType in prototypeTypes)
            {
                if (!mappings.Exists(t => t.prototypeType.FullName == prototypeType.FullName))
                {
                    var relateds = CecilHelper.GetTypeMembers(prototypeType, true).Where(m => CecilHelper.HasCustomAttributesOfType(m, typeof(AdviceMemberOrign))).Select(m => (TypeDefinition)CecilHelper.GetCustomAttributeOfType(m, typeof(AdviceMemberOrign)).ConstructorArguments.First().Value).Where(ma => CecilHelper.HasCustomAttributesOfType(ma, typeof(AspectParentAttribute))).Select(aspect => (TypeDefinition)CecilHelper.GetCustomAttributeOfType(aspect, typeof(AspectParentAttribute)).ConstructorArguments.First().Value).Select(aspect => _AspectDefinitions.First(t => t.FullName == aspect.FullName));
                    var itemMappingDefinitions = relateds.SelectMany(t => t.PrototypeItemMappingDefinitions).Where(t => t.SourceKind == PrototypeItemMappingSourceKinds.AdviceType);
                    mappings.Add((prototypeType, itemMappingDefinitions));
                }
            }

            foreach (var aspectwithRefProtoType in aspectwithRefProtoTypes)
            {
                var relatedMappings = aspectwithRefProtoType.Advice.ReferencedPrototypeTypes.SelectMany(t => mappings.First(m => m.prototypeType.FullName == t.FullName).mappingDefinitions);
                aspectwithRefProtoType.AddPrototypeItemMappingDefinitions(relatedMappings);
            }
        }

        void _ImportDeductedReferencedPrototypeTypes()
        {
            var advices = _AdviceDefinitions.Where(t => t.Value.ReferencedPrototypeTypes.Any()).Select(t => t.Value);
            var prototypeTypes = advices.SelectMany(t => t.ReferencedPrototypeTypes).Select(t => t.GetElementType().Resolve());
            var addedPrototypeTypes = new List<(TypeDefinition prototypeType, IEnumerable<TypeDefinition> addedPrototypeTypes)>();
            foreach (var prototypeType in prototypeTypes)
            {
                if (addedPrototypeTypes.Any(t => t.prototypeType.FullName == prototypeType.FullName))
                    continue;
                var newPrototypeTypes = new ConsumedPrototypeTypesVisitor(prototypeType).Get();
                if (newPrototypeTypes.Any())
                {
                    addedPrototypeTypes.Add((prototypeType, newPrototypeTypes));
                    if (_PrototypeTypeMappingDeclarations.ContainsKey(prototypeType.FullName))
                    {
                        var internalReferencedPrototypeTypes = newPrototypeTypes.Where(t => t.FullName != prototypeType.FullName);
                        _PrototypeTypeMappingDeclarations[prototypeType.FullName].InternalReferencedPrototypeTypes.AddRange(internalReferencedPrototypeTypes);
                    }
                }
            }

            foreach (var advice in advices)
            {
                foreach (var prototypeType in advice.ReferencedPrototypeTypes.Select(t => t.GetElementType().Resolve()).Where(t => addedPrototypeTypes.Exists(p => p.prototypeType.FullName == t.FullName)).ToList())
                {
                    advice.AddReferencedPrototypeTypes(addedPrototypeTypes.First(t => t.prototypeType.FullName == prototypeType.FullName).addedPrototypeTypes);
                }
            }
        }

#region IAspectContainer
        IEnumerable<IPrototypeTypeMappingDefinition> IAspectsContainer.PrototypeTypeMappingDefinitions => _PrototypeTypeMappingDefinitions;
        IEnumerable<IAspectDefinition> IAspectsContainer.Aspects => _AspectDefinitions;
        IEnumerable<IError> IAspectsContainer.Errors => _Errors;
        ReaderParameters IAspectsContainer.ReaderParameters => ReaderParameters;
#endregion
    }
}