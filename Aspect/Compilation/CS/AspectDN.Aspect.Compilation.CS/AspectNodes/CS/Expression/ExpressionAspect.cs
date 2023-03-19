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
using TokenizerDN.Common.SourceAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using AspectDN.Common;

namespace AspectDN.Aspect.Compilation.CS
{
    internal abstract class ExpressionAspect : CSAspectNode
    {
        internal ExpressionAspect(ISynToken token) : base(token)
        {
        }
    }

    internal abstract class PrimaryExpressionAspect : ExpressionAspect
    {
        internal PrimaryExpressionAspect(ISynToken token) : base(token)
        {
        }
    }

    internal abstract class NonAssignmentExpressionAspect : ExpressionAspect
    {
        internal NonAssignmentExpressionAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class BinaryExpressionAspect : NonAssignmentExpressionAspect
    {
        internal CSAspectNode Left => ChildCSAspectNodes.FirstOrDefault();
        internal OperatorAspect Operator => CSAspectCompilerHelper.GetDescendingNodesOfType<OperatorAspect>(this, false).FirstOrDefault();
        internal CSAspectNode Right => CSAspectCompilerHelper.GetDescendingNodesOfType<CSAspectNode>(this, false, Operator).FirstOrDefault();
        internal BinaryExpressionAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.BinaryExpression(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class BinaryConditionalExpressionAspect : NonAssignmentExpressionAspect
    {
        internal ExpressionAspect LeftExpression => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false).FirstOrDefault();
        internal OperatorAspect Operator => CSAspectCompilerHelper.GetDescendingNodesOfType<OperatorAspect>(this, false).FirstOrDefault();
        internal ExpressionAspect RightExpression => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false, LeftExpression).FirstOrDefault();
        internal BinaryConditionalExpressionAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.BinaryConditionalExpression(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class ConditionalExpressionAspect : NonAssignmentExpressionAspect
    {
        internal ExpressionAspect Expression => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false).FirstOrDefault();
        internal ExpressionAspect LeftExpression => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false, Expression).FirstOrDefault();
        internal ExpressionAspect RightExpression => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false, LeftExpression ?? Expression).FirstOrDefault();
        internal ConditionalExpressionAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ConditionalExpression(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class ParenthesizedExpressionAspect : PrimaryExpressionAspect
    {
        internal ExpressionAspect Expression => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false).FirstOrDefault();
        internal ParenthesizedExpressionAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ParenthesizedExpression(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class MemberAccessExpressionAspect : PrimaryExpressionAspect
    {
        internal CSAspectNode Left => ChildAspectNodes.Any() ? this[0] : null;
        internal CSAspectNode Right => ChildAspectNodes.Count() >= 1 ? this[1] : null;
        internal TypeArgumentListAspect TypeArgumentList => (TypeArgumentListAspect)(ChildAspectNodes.Count() >= 2 ? this[2] : null);
        internal MemberAccessExpressionAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.MemberAccesssExpression(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class InvocationExpressionAspect : PrimaryExpressionAspect
    {
        internal PrimaryExpressionAspect PrimaryExpression { get => CSAspectCompilerHelper.GetDescendingNodesOfType<PrimaryExpressionAspect>(this).FirstOrDefault(); }

        internal ArgumentListAspect ArgumentList { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ArgumentListAspect>(this).FirstOrDefault(); }

        internal InvocationExpressionAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.InvocationExpression(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class ElementAccessExpressionAspect : PrimaryExpressionAspect
    {
        internal PrimaryExpressionAspect PrimaryExpression { get => CSAspectCompilerHelper.GetDescendingNodesOfType<PrimaryExpressionAspect>(this).FirstOrDefault(); }

        internal IEnumerable<ArgumentAspect> Arguments => CSAspectCompilerHelper.GetDescendingNodesOfType<ArgumentAspect>(this);
        internal ElementAccessExpressionAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ElementExpression(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class ThisElementAccessExpressionAspect : PrimaryExpressionAspect
    {
        internal ThisElementAccessExpressionAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ThisExpression(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class BaseElementAccessExpressionAspect : PrimaryExpressionAspect
    {
        internal IdentifierNameAspect Identifier => ChildCSAspectNodes.OfType<IdentifierNameAspect>().FirstOrDefault();
        internal IEnumerable<ArgumentAspect> Arguments => ChildCSAspectNodes.OfType<ArgumentAspect>();
        internal BaseElementAccessExpressionAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.BaseAccessExpression(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class ObjectCreationExpressionAspect : PrimaryExpressionAspect
    {
        internal TypeAspect Type => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeAspect>(this, false).FirstOrDefault();
        internal ArgumentListAspect ArgumentList => ChildCSAspectNodes.OfType<ArgumentListAspect>().FirstOrDefault();
        internal InitiatializerExpressionAspect Initializer => ChildCSAspectNodes.OfType<InitiatializerExpressionAspect>().FirstOrDefault();
        internal ObjectCreationExpressionAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ObjectCreationExpression(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class DelegateCreationExpressionAspect : PrimaryExpressionAspect
    {
        internal TypeAspect Type => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeAspect>(this, false).FirstOrDefault();
        internal ExpressionAspect Argument => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false).FirstOrDefault();
        internal DelegateCreationExpressionAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.DelegateCreationExpression(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AnonymousObjectCreationExpressionAspect : PrimaryExpressionAspect
    {
        internal IEnumerable<AnonymousMemberDeclaratorAspect> MemberDeclarators { get => CSAspectCompilerHelper.GetDescendingNodesOfType<AnonymousMemberDeclaratorAspect>(this); }

        internal AnonymousObjectCreationExpressionAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AnonymousObjectCreationExpression(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AnonymousMemberDeclaratorAspect : CSAspectNode
    {
        internal ExpressionAspect Expression { get => (ExpressionAspect)(ChildCSAspectNodes.Count() == 1 ? this[0] : this[1]); }

        internal IdentifierNameAspect IdentifierName { get => (IdentifierNameAspect)(ChildCSAspectNodes.Count() > 1 ? this[0] : null); }

        internal AnonymousMemberDeclaratorAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AnonymousMemberDeclarator(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class ArrayCreationExpressionAspect : PrimaryExpressionAspect
    {
        internal TypeAspect Type { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeAspect>(this, false).FirstOrDefault(); }

        internal IEnumerable<ExpressionAspect> Expressions { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false, Type).Where(t => !(t is ArrayInitializerAspect)); }

        internal IEnumerable<RankSpecifierAspect> RankSpecifiers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<RankSpecifierAspect>(this); }

        internal ArrayInitializerAspect ArrayInitializer { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ArrayInitializerAspect>(this).FirstOrDefault(); }

        internal ArrayCreationExpressionAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ArrayCreationExpression(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class RankSpecifierAspect : CSAspectNode
    {
        internal IEnumerable<DimSeparator> DimSeperators { get => CSAspectCompilerHelper.GetDescendingNodesOfType<DimSeparator>(this); }

        internal RankSpecifierAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ArrayRankSpecifier(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder("[");
            for (int i = 0; i < DimSeperators.Count(); i++)
                stringBuilder.Append(",");
            stringBuilder.Append("]");
            return stringBuilder.ToString();
        }
    }

    internal class DimSeparator : CSAspectNode
    {
        internal DimSeparator(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            throw AspectDNErrorFactory.GetException("NotImplementedException");
        }
    }

    internal class VariableInitializerAspect : CSAspectNode
    {
        internal ExpressionAspect Expression => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false).FirstOrDefault();
        internal VariableInitializerAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.VariableInitalizer(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class ArrayInitializerAspect : PrimaryExpressionAspect
    {
        internal IEnumerable<VariableInitializerAspect> VariableInitializers { get => CSAspectCompilerHelper.GetDescendingNodesOfType<VariableInitializerAspect>(this); }

        internal ArrayInitializerAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ArrayInitializerExpression(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class CheckedExpressionAspect : PrimaryExpressionAspect
    {
        internal ExpressionAspect Expression => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false).FirstOrDefault();
        internal CheckedExpressionAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.CheckedExpression(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class UnCheckedExpressionAspect : PrimaryExpressionAspect
    {
        internal ExpressionAspect Expression => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false).FirstOrDefault();
        internal UnCheckedExpressionAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.UnCheckedExpression(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class TypeOfExpressionAspect : PrimaryExpressionAspect
    {
        internal TypeAspect Type => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeAspect>(this, false).FirstOrDefault();
        internal TypeOfExpressionAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.Typeof(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class AliasUnboundTypeNameAspect : TypeAspect
    {
        internal CSAspectNode Left => CSAspectCompilerHelper.GetDescendingNodesOfType<CSAspectNode>(this, false).FirstOrDefault();
        internal IdentifierNameAspect Right => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this, false, Left).FirstOrDefault();
        internal GenericDimensionSpecifierAspect GenericDimensionSpecifier { get => CSAspectCompilerHelper.GetDescendingNodesOfType<GenericDimensionSpecifierAspect>(this, false).FirstOrDefault(); }

        internal AliasUnboundTypeNameAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AliasUnboundTypeName(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }

        internal override string GetName()
        {
            string name = Left.TokenValue + "::" + Right.TokenValue;
            if (GenericDimensionSpecifier != null)
                name += GenericDimensionSpecifier.GetName();
            return name;
        }
    }

    internal class QualifiedUnboundTypeNameAspect : TypeAspect
    {
        internal NameAspect Left => (NameAspect)CSAspectCompilerHelper.GetDescendingNodesOfType<CSAspectNode>(this, false).FirstOrDefault();
        internal IdentifierNameAspect Right => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this, false, Left).FirstOrDefault();
        internal GenericDimensionSpecifierAspect GenericDimensionSpecifier { get => CSAspectCompilerHelper.GetDescendingNodesOfType<GenericDimensionSpecifierAspect>(this, false).FirstOrDefault(); }

        internal QualifiedUnboundTypeNameAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.QualifiedUnboundTypeName(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }

        internal override string GetName()
        {
            var name = Left.GetName();
            if (Right != null)
                name += '.' + Right.GetName();
            if (GenericDimensionSpecifier != null)
                name += GenericDimensionSpecifier.GetName();
            return name;
        }
    }

    internal class GenericDimensionSpecifierAspect : CSAspectNode
    {
        internal GenericDimensionSpecifierAspect(ISynToken token) : base(token)
        {
        }

        internal int CountCommas()
        {
            return CSAspectCompilerHelper.GetDescendingNodesOfType<CommaAspect>(this, true).Count();
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.GenericDimensionSpecifier(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }

        internal string GetName()
        {
            string dimension = "<";
            for (int i = 0; i < CountCommas(); i++)
                dimension += ",";
            dimension += ">";
            return dimension;
        }
    }

    internal class CommaAspect : CSAspectNode
    {
        internal CommaAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            throw AspectDNErrorFactory.GetException("NotImplementedException");
        }
    }

    internal class DefaultValueTypeExpressionAspect : PrimaryExpressionAspect
    {
        internal TypeAspect Type => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeAspect>(this, false).FirstOrDefault();
        internal DefaultValueTypeExpressionAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.DefaultValueType(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class CastExpressionAspect : UnaryExpressionAspect
    {
        internal TypeAspect Type => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeAspect>(this, false).FirstOrDefault();
        internal ExpressionAspect Expression => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false, Type).FirstOrDefault();
        internal CastExpressionAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.CastExpression(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class PostExpressionAspect : PrimaryExpressionAspect
    {
        internal IncrDecrOperators IncrDecrOperator { get; }

        internal ExpressionAspect Expression => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false).FirstOrDefault();
        internal PostExpressionAspect(ISynToken token, IncrDecrOperators incrDecrOperator) : base(token)
        {
            IncrDecrOperator = incrDecrOperator;
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PostExpression(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class PreExpressionAspect : PrimaryExpressionAspect
    {
        internal IncrDecrOperators IncrDecrOperator { get; }

        internal ExpressionAspect Expression => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false).FirstOrDefault();
        internal PreExpressionAspect(ISynToken token, IncrDecrOperators incrDecrOperator) : base(token)
        {
            IncrDecrOperator = incrDecrOperator;
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.PreExpression(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal enum IncrDecrOperators
    {
        Increment,
        Decrement
    }

    internal class UnaryOperationExpressionAspect : UnaryExpressionAspect
    {
        internal UnaryOperatorAspect UnaryOperator => CSAspectCompilerHelper.GetDescendingNodesOfType<UnaryOperatorAspect>(this, false).FirstOrDefault();
        internal ExpressionAspect Expression => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false).FirstOrDefault();
        internal UnaryOperationExpressionAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.UnaryOperationExpression(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal abstract class UnaryExpressionAspect : ExpressionAspect
    {
        internal UnaryExpressionAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            throw AspectDNErrorFactory.GetException("NotImplementedException");
        }
    }

    internal class UnaryOperatorAspect : CSAspectNode
    {
        internal UnaryOperatorAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            throw AspectDNErrorFactory.GetException("NotImplementedException");
        }
    }

    internal class AssignmentExpressionAspect : ExpressionAspect
    {
        internal ExpressionAspect Left { get => (ExpressionAspect)this[0]; }

        internal OperatorAspect Operator => CSAspectCompilerHelper.GetDescendingNodesOfType<OperatorAspect>(this, false).FirstOrDefault();
        internal CSAspectNode Right => CSAspectCompilerHelper.GetDescendingNodesOfType<CSAspectNode>(this, false, Operator).FirstOrDefault();
        internal AssignmentExpressionAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.AssignmentExpression(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }
}