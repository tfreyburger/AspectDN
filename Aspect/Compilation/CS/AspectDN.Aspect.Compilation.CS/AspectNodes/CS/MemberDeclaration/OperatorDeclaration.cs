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
using System.Text;
using System.Threading.Tasks;
using TokenizerDN.Common.SourceAnalysis;
using AspectDN.Aspect.Compilation.Foundation;
using Microsoft.CodeAnalysis;

namespace AspectDN.Aspect.Compilation.CS
{
    internal abstract class OperatorDeclarationAspect : TypeMemberDeclarationAspect
    {
        internal OperatorDeclarationAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class UnaryOperatorDeclaratorAspect : OperatorDeclarationAspect
    {
        internal IEnumerable<AttributeSectionAspect> AttributeSections { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeSectionAspect>(this); }

        internal IEnumerable<ModifierAspect> Modifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ModifierAspect>(this, false).ToList(); }

        internal ReturnTypeAspect Type { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ReturnTypeAspect>(this).FirstOrDefault(); }

        internal OverloadableUnaryOperatorAspect Operator { get => CSAspectCompilerHelper.GetDescendingNodesOfType<OverloadableUnaryOperatorAspect>(this).FirstOrDefault(); }

        internal OperatorType1Aspect Type1 { get => CSAspectCompilerHelper.GetDescendingNodesOfType<OperatorType1Aspect>(this).FirstOrDefault(); }

        internal OperatorIdentifier1Aspect Identifier1 { get => CSAspectCompilerHelper.GetDescendingNodesOfType<OperatorIdentifier1Aspect>(this).FirstOrDefault(); }

        internal BlockAspect Block { get => CSAspectCompilerHelper.GetDescendingNodesOfType<BlockAspect>(this).FirstOrDefault(); }

        internal UnaryOperatorDeclaratorAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.UnaryOperatorDeclarator(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class OperatorType1Aspect : CSAspectNode
    {
        internal TypeAspect Type { get => (TypeAspect)ChildAspectNodes[0]; }

        internal OperatorType1Aspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            throw AspectDNErrorFactory.GetException("NotImplementedException");
        }
    }

    internal class OperatorIdentifier1Aspect : CSAspectNode
    {
        internal IdentifierNameAspect Identifier { get => (IdentifierNameAspect)ChildAspectNodes[0]; }

        internal OperatorIdentifier1Aspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            throw AspectDNErrorFactory.GetException("NotImplementedException");
        }
    }

    internal class OperatorType2Aspect : CSAspectNode
    {
        internal TypeAspect Type { get => (TypeAspect)ChildAspectNodes[0]; }

        internal OperatorType2Aspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            throw AspectDNErrorFactory.GetException("NotImplementedException");
        }
    }

    internal class OperatorIdentifier2Aspect : CSAspectNode
    {
        internal IdentifierNameAspect Identifier { get => (IdentifierNameAspect)ChildAspectNodes[0]; }

        internal OperatorIdentifier2Aspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            throw AspectDNErrorFactory.GetException("NotImplementedException");
        }
    }

    internal class BinaryOperatorDeclaratorAspect : OperatorDeclarationAspect
    {
        internal IEnumerable<AttributeSectionAspect> AttributeSections { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeSectionAspect>(this); }

        internal IEnumerable<ModifierAspect> Modifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ModifierAspect>(this, false).ToList(); }

        internal ReturnTypeAspect Type { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ReturnTypeAspect>(this).FirstOrDefault(); }

        internal OverloadableBinaryOperatorAspect Operator { get => CSAspectCompilerHelper.GetDescendingNodesOfType<OverloadableBinaryOperatorAspect>(this).FirstOrDefault(); }

        internal OperatorType1Aspect Type1 { get => CSAspectCompilerHelper.GetDescendingNodesOfType<OperatorType1Aspect>(this).FirstOrDefault(); }

        internal OperatorIdentifier1Aspect Identifier1 { get => CSAspectCompilerHelper.GetDescendingNodesOfType<OperatorIdentifier1Aspect>(this).FirstOrDefault(); }

        internal OperatorType2Aspect Type2 { get => CSAspectCompilerHelper.GetDescendingNodesOfType<OperatorType2Aspect>(this).FirstOrDefault(); }

        internal OperatorIdentifier2Aspect Identifier2 { get => CSAspectCompilerHelper.GetDescendingNodesOfType<OperatorIdentifier2Aspect>(this).FirstOrDefault(); }

        internal BlockAspect Block { get => CSAspectCompilerHelper.GetDescendingNodesOfType<BlockAspect>(this).FirstOrDefault(); }

        internal BinaryOperatorDeclaratorAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.BinaryOperatorDeclaratorAspect(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class ConversionOperationDeclaratorAspect : OperatorDeclarationAspect
    {
        internal IEnumerable<AttributeSectionAspect> AttributeSections { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AttributeSectionAspect>(this); }

        internal IEnumerable<ModifierAspect> Modifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ModifierAspect>(this, false).ToList(); }

        internal ReturnTypeAspect Type { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ReturnTypeAspect>(this).FirstOrDefault(); }

        internal ConvertsionOperatorType Operator { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ConvertsionOperatorType>(this).FirstOrDefault(); }

        internal OperatorType1Aspect Type1 { get => CSAspectCompilerHelper.GetDescendingNodesOfType<OperatorType1Aspect>(this).FirstOrDefault(); }

        internal OperatorIdentifier1Aspect Identifier1 { get => CSAspectCompilerHelper.GetDescendingNodesOfType<OperatorIdentifier1Aspect>(this).FirstOrDefault(); }

        internal BlockAspect Block { get => CSAspectCompilerHelper.GetDescendingNodesOfType<BlockAspect>(this).FirstOrDefault(); }

        internal ConversionOperationDeclaratorAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ConversionOperationDeclaratpr(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class OverloadableUnaryOperatorAspect : CSAspectNode
    {
        internal OverloadableUnaryOperatorAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            throw AspectDNErrorFactory.GetException("NotImplementedException");
        }
    }

    internal class OverloadableBinaryOperatorAspect : CSAspectNode
    {
        internal OverloadableBinaryOperatorAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            throw AspectDNErrorFactory.GetException("NotImplementedException");
        }
    }

    internal class ConvertsionOperatorType : CSAspectNode
    {
        internal ConvertsionOperatorType(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            throw AspectDNErrorFactory.GetException("NotImplementedException");
        }
    }
}