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
using System.Collections.Generic;
using System.Linq;
using AspectDN.Aspect.Weaving.IJoinpoints;
using System.IO;

namespace AspectDN.Aspect.Concerns
{
    internal class AspectDefinition : IAspectDefinition
    {
        protected List<AspectMemberDefinition> _AspectMemberDefinitions;
        protected List<PrototypeItemMappingDefinition> _PrototypeItemMappingDefinitions;
        internal long Id { get; }

        internal AspectKinds AspectKind { get; }

        internal AspectDeclaration AspectDeclaration { get; }

        internal IEnumerable<PrototypeItemMappingDefinition> PrototypeItemMappingDefinitions { get => _PrototypeItemMappingDefinitions; }

        internal string FullName
        {
            get
            {
                return AspectDeclaration.FullName;
            }
        }

        internal AdviceDefinition Advice { get; }

        internal PointcutDefinition Pointcut { get; }

        internal List<AspectMemberDefinition> AspectMemberDefinitions => _AspectMemberDefinitions;
        internal string AspectDeclarationName => AspectDeclaration.Name;
        internal string FullAspectDeclarationName => AspectDeclaration.FullName;
        internal string FullAspectRepositoryName => AspectDeclaration.FullAspectRepositoryName;
        internal bool IsCodeAspect => AspectKind == AspectKinds.CodeAspect || AspectKind == AspectKinds.ChangeValueAspect;
        internal AspectDefinition(long id, AspectDeclaration aspectDeclaration, AdviceDefinition adviceDefinition, PointcutDefinition pointcutDefinition, AspectKinds aspectKind, IEnumerable<PrototypeItemMappingDefinition> prototypeItemMappingDefinitions)
        {
            Id = id;
            AspectDeclaration = aspectDeclaration;
            Pointcut = pointcutDefinition;
            Advice = adviceDefinition;
            AspectKind = aspectKind;
            _PrototypeItemMappingDefinitions = new List<PrototypeItemMappingDefinition>(prototypeItemMappingDefinitions);
            _AspectMemberDefinitions = new List<AspectMemberDefinition>();
        }

        internal void Join(IJoinpoint joinpoint)
        {
            AspectDeclaration.Join(joinpoint.Assembly);
        }

        internal void AddPrototypeItemMappingDefinitions(IEnumerable<PrototypeItemMappingDefinition> prototypeItemMappingDefinitions)
        {
            foreach (var prototypeItemMappingDefinition in prototypeItemMappingDefinitions)
            {
                if (_PrototypeItemMappingDefinitions.Exists(t => t.SourceKind == prototypeItemMappingDefinition.SourceKind && t.TargetName == prototypeItemMappingDefinition.TargetName))
                    continue;
                _PrototypeItemMappingDefinitions.Add(prototypeItemMappingDefinition);
            }
        }

#region IAspectDefinition
        long IAspectDefinition.Id => Id;
        AspectKinds IAspectDefinition.AspectKind => AspectKind;
        IPointcutDefinition IAspectDefinition.Pointcut => Pointcut;
        IAdviceDefinition IAspectDefinition.AdviceDefinition => Advice;
        IEnumerable<IAspectMemberDefinition> IAspectDefinition.AspectMemberDefinitions => _AspectMemberDefinitions;
        IEnumerable<IPrototypeItemMappingDefinition> IAspectDefinition.PrototypeItemMappingDefinitions => PrototypeItemMappingDefinitions;
        string IAspectDefinition.AspectDeclarationName => AspectDeclarationName;
        string IAspectDefinition.FullAspectDeclarationName => FullAspectDeclarationName;
        string IAspectDefinition.FullAspectRepositoryName => FullAspectRepositoryName;
        void IAspectDefinition.Join(IJoinpoint joinpoint) => Join(joinpoint);
        bool IAspectDefinition.IsCodeAspect => IsCodeAspect;
#endregion
        internal void AddMember(AspectMemberDefinition aspectMemberDefinition)
        {
            _AspectMemberDefinitions.Add(aspectMemberDefinition);
        }
    }

    internal class AspectMemberDefinition : IAspectMemberDefinition
    {
        internal AspectDefinition ParentAspectDefinition { get; }

        internal AdviceMemberDefinition AdviceMemberDefinition { get; }

#region IAspectMemberDefinition
        IAspectDefinition IAspectMemberDefinition.ParentAspectDefinition => ParentAspectDefinition;
        IAdviceMemberDefinition IAspectMemberDefinition.AdviceMemberDefinition => AdviceMemberDefinition;
        long IAspectDefinition.Id => ParentAspectDefinition.Id;
        AspectKinds IAspectDefinition.AspectKind => ParentAspectDefinition.AspectKind;
        IPointcutDefinition IAspectDefinition.Pointcut => ParentAspectDefinition.Pointcut;
        IAdviceDefinition IAspectDefinition.AdviceDefinition => ParentAspectDefinition.Advice;
        IEnumerable<IAspectMemberDefinition> IAspectDefinition.AspectMemberDefinitions => ParentAspectDefinition.AspectMemberDefinitions;
        IEnumerable<IPrototypeItemMappingDefinition> IAspectDefinition.PrototypeItemMappingDefinitions => ParentAspectDefinition.PrototypeItemMappingDefinitions;
        string IAspectDefinition.AspectDeclarationName => ParentAspectDefinition.AspectDeclarationName;
        string IAspectDefinition.FullAspectDeclarationName => ParentAspectDefinition.FullAspectDeclarationName;
        string IAspectDefinition.FullAspectRepositoryName => ParentAspectDefinition.FullAspectRepositoryName;
        void IAspectDefinition.Join(IJoinpoint joinpoint) => ((IAspectDefinition)ParentAspectDefinition).Join(joinpoint);
        bool IAspectDefinition.IsCodeAspect => false;
#endregion
        public AspectMemberDefinition(AspectDefinition parentAspect, AdviceMemberDefinition adviceMemberDefinition)
        {
            ParentAspectDefinition = parentAspect;
            AdviceMemberDefinition = adviceMemberDefinition;
        }
    }

    internal class AspectCodeDefinition : AspectDefinition, ICodeAspectDefinition
    {
        internal ExecutionTimes ExecutionTime { get; }

        internal ControlFlows ControlFlow { get; }

        internal AspectCodeDefinition(long id, AspectDeclaration aspectDeclaration, AdviceDefinition advice, PointcutDefinition pointcut, IEnumerable<PrototypeItemMappingDefinition> prototypeItemMappingDefinitions, ExecutionTimes executionTime, ControlFlows controlFlow) : base(id, aspectDeclaration, advice, pointcut, AspectKinds.CodeAspect, prototypeItemMappingDefinitions)
        {
            ExecutionTime = executionTime;
            ControlFlow = controlFlow;
            AddMember(new AspectMemberDefinition(this, advice.AdviceMemberDefinitions.First()));
        }

#region IAspectCodeDefinition
        ControlFlows ICodeAspectDefinition.ControlFlow => ControlFlow;
        ExecutionTimes ICodeAspectDefinition.ExecutionTime => ExecutionTime;
        IAspectDefinition IAspectMemberDefinition.ParentAspectDefinition => this;
        IAdviceMemberDefinition IAspectMemberDefinition.AdviceMemberDefinition => this.Advice.AdviceMemberDefinitions.First();
#endregion
    }

    internal class ChangeValueAspectDefinition : AspectDefinition, IChangeValueAspectDefinition
    {
        internal ControlFlows ControlFlow { get; }

        internal ChangeValueAspectDefinition(long id, AspectDeclaration aspectAttributesDefinition, AdviceDefinition adviceDefinition, PointcutDefinition pointcutDefinition, IEnumerable<PrototypeItemMappingDefinition> virtualMappingItems, ControlFlows controlFlow) : base(id, aspectAttributesDefinition, adviceDefinition, pointcutDefinition, AspectKinds.ChangeValueAspect, virtualMappingItems)
        {
            ControlFlow = controlFlow;
            AddMember(new AspectMemberDefinition(this, adviceDefinition.AdviceMemberDefinitions.First()));
        }

#region IAspectCodeDefinition
        ControlFlows IChangeValueAspectDefinition.ControlFlow => ControlFlow;
        IAspectDefinition IAspectMemberDefinition.ParentAspectDefinition => this;
        IAdviceMemberDefinition IAspectMemberDefinition.AdviceMemberDefinition => this.Advice.AdviceMemberDefinitions.First();
#endregion
    }

    internal class AspectTypeDefinition : AspectDefinition, ITypesAspectDefinition
    {
        internal string Namespace { get; }

        internal AspectTypeDefinition(long id, AspectDeclaration aspectAttributesDeclaration, AdviceDefinition adviceDefinition, PointcutDefinition pointcutDefinition, IEnumerable<PrototypeItemMappingDefinition> virtualMappingItems, string @namespace) : base(id, aspectAttributesDeclaration, adviceDefinition, pointcutDefinition, AspectKinds.TypesAspect, virtualMappingItems)
        {
            Namespace = @namespace;
        }

#region
        string ITypesAspectDefinition.Namespace => Namespace;
        IAspectDefinition IAspectMemberDefinition.ParentAspectDefinition => this;
        IAdviceMemberDefinition IAspectMemberDefinition.AdviceMemberDefinition => throw new System.NotImplementedException();
#endregion
    }

    internal class AspectTypeMembersDefinition : AspectDefinition, ITypeMembersAspectDefinition
    {
        internal AspectMemberModifiers MemberModifiers { get; }

        internal AspectTypeMembersDefinition(long id, AspectDeclaration aspectAttributesDefinition, AdviceDefinition adviceDefinition, PointcutDefinition pointcutDefinition, IEnumerable<PrototypeItemMappingDefinition> virtualMappingItems, AspectMemberModifiers memberModifiers) : base(id, aspectAttributesDefinition, adviceDefinition, pointcutDefinition, AspectKinds.TypeMembersApsect, virtualMappingItems)
        {
            MemberModifiers = memberModifiers;
        }

#region
        AspectMemberModifiers ITypeMembersAspectDefinition.MemberModifers => MemberModifiers;
        IAdviceMemberDefinition IAspectMemberDefinition.AdviceMemberDefinition => throw new System.NotImplementedException();
        IAspectDefinition IAspectMemberDefinition.ParentAspectDefinition => throw new System.NotImplementedException();
#endregion
    }

    internal class InheritanceAspectDefinition : AspectDefinition, IInheritanceAspectDefinition
    {
        List<OverrideConstructorDefnition> _ConstructorOverloads;
        internal bool HasSpecificConstructorOverloads => _ConstructorOverloads.Any();
        internal IEnumerable<OverrideConstructorDefnition> ConstructorOverloads => _ConstructorOverloads;
        internal InheritanceAspectDefinition(long id, AspectDeclaration aspectDeclaration, AdviceDefinition adviceDefinition, PointcutDefinition pointcutDefinition, AspectKinds aspectKinds, IEnumerable<PrototypeItemMappingDefinition> virtualMappingItems) : base(id, aspectDeclaration, adviceDefinition, pointcutDefinition, aspectKinds, virtualMappingItems)
        {
            _ConstructorOverloads = new List<OverrideConstructorDefnition>();
        }

        internal void Add(OverrideConstructorDefnition constructorOverload)
        {
            _ConstructorOverloads.Add(constructorOverload);
        }

#region
        IEnumerable<IOverrideConstructorDefinition> IInheritanceAspectDefinition.ConstructorOverloads => ConstructorOverloads;
#endregion
    }

    internal class OverrideConstructorDefnition : IOverrideConstructorDefinition
    {
        internal IEnumerable<ParameterDefinition> OverrideConstructorParameters { get; }

        internal IEnumerable<TypeReference> BaseConstructorParameterValueTypes { get; }

        internal IEnumerable<Instruction>[] BaseConstructeurParameterValues { get; }

        internal OverrideConstructorDefnition(IEnumerable<ParameterDefinition> overrideConstructorParameters, IEnumerable<TypeReference> baseConstructorParameterValueTypes, IEnumerable<Instruction>[] baseConstructorParameterValues)
        {
            OverrideConstructorParameters = overrideConstructorParameters;
            BaseConstructorParameterValueTypes = baseConstructorParameterValueTypes;
            BaseConstructeurParameterValues = baseConstructorParameterValues;
        }

#region IBaseTypeConstructorOverloadAspectDefinition
        IEnumerable<ParameterDefinition> IOverrideConstructorDefinition.OverrideConstructorParameters => OverrideConstructorParameters;
        IEnumerable<TypeReference> IOverrideConstructorDefinition.BaseConstructorParameterValueTypes => BaseConstructorParameterValueTypes;
        IEnumerable<Instruction>[] IOverrideConstructorDefinition.BaseConstructeurParameterValues => BaseConstructeurParameterValues;
#endregion
    }
}