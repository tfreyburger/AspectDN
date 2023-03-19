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
    internal class QueryExpressionAspect : NonAssignmentExpressionAspect
    {
        internal FromClauseAspect FromClause { get => (FromClauseAspect)this[0]; }

        internal QueryBodyAspect QueryBody { get => (QueryBodyAspect)this[1]; }

        internal QueryExpressionAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.QueryExpression(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class QueryBodyAspect : CSAspectNode
    {
        internal IEnumerable<QueryBodyClauseAspect> QueryBodyClauses { get => CSAspectCompilerHelper.GetDescendingNodesOfType<QueryBodyClauseAspect>(this); }

        internal SelectOrGroupClauseAspect SelectOrGroupClause { get => CSAspectCompilerHelper.GetDescendingNodesOfType<SelectOrGroupClauseAspect>(this).FirstOrDefault(); }

        internal QueryContinuationAspect QueryContinuation { get => CSAspectCompilerHelper.GetDescendingNodesOfType<QueryContinuationAspect>(this).FirstOrDefault(); }

        internal QueryBodyAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.QueryBody(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal abstract class QueryBodyClauseAspect : CSAspectNode
    {
        internal QueryBodyClauseAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            throw AspectDNErrorFactory.GetException("NotImplementedException");
        }
    }

    internal class FromClauseAspect : QueryBodyClauseAspect
    {
        internal TypeAspect Type { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeAspect>(this, false).FirstOrDefault(); }

        internal IdentifierNameAspect Identifier { get => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this, false, Type).FirstOrDefault(); }

        internal ExpressionAspect InExpression { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false, Identifier).FirstOrDefault(); }

        internal FromClauseAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.FromClause(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class LetClauseAspect : QueryBodyClauseAspect
    {
        internal IdentifierNameAspect Identifier { get => (IdentifierNameAspect)this[0]; }

        internal ExpressionAspect Expression { get => (ExpressionAspect)this[1]; }

        internal LetClauseAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.LetClause(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class WhereClauseAspect : QueryBodyClauseAspect
    {
        internal ExpressionAspect Expression => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false).FirstOrDefault();
        internal WhereClauseAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.WhereClause(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class JoinClauseAspect : QueryBodyClauseAspect
    {
        internal TypeAspect Type => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeAspect>(this, false).FirstOrDefault();
        internal IdentifierNameAspect Identifier => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this, false, Type).FirstOrDefault();
        internal ExpressionAspect InExpression => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false, Identifier).FirstOrDefault();
        internal ExpressionAspect LeftExpression => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false, InExpression).FirstOrDefault();
        internal ExpressionAspect RightExpression => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false, LeftExpression).FirstOrDefault();
        internal JoinClauseAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.JoinClause(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class JoinIntoClauseAspect : JoinClauseAspect
    {
        internal IdentifierNameAspect IntoIdentifier => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this, false, RightExpression).FirstOrDefault();
        internal JoinIntoClauseAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.JoinIntoClause(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class OrderByClauseAspect : QueryBodyClauseAspect
    {
        internal IEnumerable<OrderingAspect> Orderings => CSAspectCompilerHelper.GetDescendingNodesOfType<OrderingAspect>(this);
        internal OrderByClauseAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.OrderByClause(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class OrderingAspect : CSAspectNode
    {
        internal ExpressionAspect Expression => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false).FirstOrDefault();
        internal OrderingDirectionAspect Direction => CSAspectCompilerHelper.GetDescendingNodesOfType<OrderingDirectionAspect>(this, false).FirstOrDefault();
        internal OrderingAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.Ordering(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class OrderingDirectionAspect : CSAspectNode
    {
        internal OrderingDirections OrderingDifrection { get; }

        internal OrderingDirectionAspect(ISynToken token, OrderingDirections orderingDirection) : base(token)
        {
            OrderingDifrection = orderingDirection;
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.OrderingDirection(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal enum OrderingDirections
    {
        NONE,
        ASCENDING,
        DESCENDING
    }

    internal abstract class SelectOrGroupClauseAspect : CSAspectNode
    {
        internal SelectOrGroupClauseAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            throw AspectDNErrorFactory.GetException("NotImplementedException");
        }
    }

    internal class SelectClauseAspect : SelectOrGroupClauseAspect
    {
        internal ExpressionAspect Expression { get => (ExpressionAspect)ChildAspectNodes[0]; }

        internal SelectClauseAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.SelectClause(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class GroupClauseAspect : SelectOrGroupClauseAspect
    {
        internal ExpressionAspect GroupExpression { get => (ExpressionAspect)ChildAspectNodes[0]; }

        internal ExpressionAspect ByExpression { get => (ExpressionAspect)ChildAspectNodes[1]; }

        internal GroupClauseAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.GroupClause(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class QueryContinuationAspect : CSAspectNode
    {
        internal IdentifierNameAspect Identifier { get => (IdentifierNameAspect)ChildAspectNodes[0]; }

        internal QueryBodyAspect QueryBody { get => (QueryBodyAspect)ChildAspectNodes[1]; }

        internal QueryContinuationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.QueryContinuation(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }
}