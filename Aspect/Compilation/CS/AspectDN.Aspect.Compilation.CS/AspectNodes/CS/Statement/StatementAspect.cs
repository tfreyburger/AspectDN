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
using AspectDN.Aspect.Compilation.Foundation;
using Microsoft.CodeAnalysis;
using AspectDN.Common;

namespace AspectDN.Aspect.Compilation.CS
{
    internal abstract class StatementAspect : CSAspectNode
    {
        internal StatementAspect(ISynToken token) : base(token)
        {
        }
    }

    internal abstract class DeclarationStatementAspect : StatementAspect
    {
        internal DeclarationStatementAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class LocalConstantDeclarationAspect : DeclarationStatementAspect
    {
        internal CSAspectNode TypeOrIdenfifier => CSAspectCompilerHelper.GetDescendingNodesOfType<CSAspectNode>(this, false).FirstOrDefault();
        internal IEnumerable<ConstantDeclaratorAspect> ConstantDeclarators { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ConstantDeclaratorAspect>(this, false); }

        internal LocalConstantDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.LocalConstantDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class LocalVariableDeclarationAspect : DeclarationStatementAspect
    {
        internal CSAspectNode TypeOrIdenfifier => CSAspectCompilerHelper.GetDescendingNodesOfType<CSAspectNode>(this, false).FirstOrDefault();
        internal IEnumerable<LocalVariableDeclaratorAspect> VariablesDeclarators { get => CSAspectCompilerHelper.GetDescendingNodesOfType<LocalVariableDeclaratorAspect>(this, false); }

        internal LocalVariableDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.LocalVariableDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class VariableDeclarationAspect : DeclarationStatementAspect
    {
        internal CSAspectNode TypeOrIdenfifier => CSAspectCompilerHelper.GetDescendingNodesOfType<CSAspectNode>(this, false).FirstOrDefault();
        internal IEnumerable<LocalVariableDeclaratorAspect> VariablesDeclarators { get => CSAspectCompilerHelper.GetDescendingNodesOfType<LocalVariableDeclaratorAspect>(this, false); }

        internal VariableDeclarationAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.VariableDeclaration(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class LabeledStatementAspect : StatementAspect
    {
        internal IdentifierNameAspect Identifier { get => (IdentifierNameAspect)this[0]; }

        internal StatementAspect Statement { get => (StatementAspect)this[1]; }

        internal LabeledStatementAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.LabelStatement(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal abstract class EmbeddedStatementAspect : StatementAspect
    {
        internal EmbeddedStatementAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class BlockAspect : EmbeddedStatementAspect
    {
        internal IEnumerable<StatementAspect> Statements { get => CSAspectCompilerHelper.GetDescendingNodesOfType<StatementAspect>(this); }

        internal BlockAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.Block(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class ExpressionStatementAspect : EmbeddedStatementAspect
    {
        internal ExpressionAspect Expression => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false).FirstOrDefault();
        internal ExpressionStatementAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ExpressionStatement(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class IfStatementAspect : EmbeddedStatementAspect
    {
        internal ExpressionAspect Condition => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false).FirstOrDefault();
        internal EmbeddedStatementAspect Then => CSAspectCompilerHelper.GetDescendingNodesOfType<EmbeddedStatementAspect>(this, false).FirstOrDefault();
        internal EmbeddedStatementAspect Else => CSAspectCompilerHelper.GetDescendingNodesOfType<EmbeddedStatementAspect>(this, false, Then).FirstOrDefault();
        internal IfStatementAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.IfStatement(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class SwitchStatementAspect : EmbeddedStatementAspect
    {
        internal ExpressionAspect Expression => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false).FirstOrDefault();
        internal IEnumerable<SwitchSectionAspect> Sections { get => CSAspectCompilerHelper.GetDescendingNodesOfType<SwitchSectionAspect>(this, true); }

        internal SwitchStatementAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.SwitchStatement(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class SwitchSectionAspect : CSAspectNode
    {
        internal IEnumerable<SwitchLabelAspect> Labels { get => CSAspectCompilerHelper.GetDescendingNodesOfType<SwitchLabelAspect>(this); }

        internal IEnumerable<StatementAspect> Statements { get => CSAspectCompilerHelper.GetDescendingNodesOfType<StatementAspect>(this); }

        internal SwitchSectionAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.SwitchSection(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class SwitchLabelAspect : CSAspectNode
    {
        internal ExpressionAspect Expression => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false).FirstOrDefault();
        internal SwitchLabelAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.SwitchLabel(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class WhileStatementAspect : EmbeddedStatementAspect
    {
        internal ExpressionAspect Conditiion => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false).FirstOrDefault();
        internal StatementAspect Statement => CSAspectCompilerHelper.GetDescendingNodesOfType<StatementAspect>(this, false).FirstOrDefault();
        internal WhileStatementAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.WhileStatement(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class DoStatementAspect : EmbeddedStatementAspect
    {
        internal StatementAspect Statement => CSAspectCompilerHelper.GetDescendingNodesOfType<StatementAspect>(this, false).FirstOrDefault();
        internal ExpressionAspect Conditiion => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false).FirstOrDefault();
        internal DoStatementAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.DoStatement(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class ForStatementAspect : EmbeddedStatementAspect
    {
        internal ForInitializerAspect ForInitializer { get => (ForInitializerAspect)ChildCSAspectNodes.Where(t => t is ForInitializerAspect).FirstOrDefault(); }

        internal ForConditionAspect ForCondition { get => (ForConditionAspect)ChildCSAspectNodes.Where(t => t is ForConditionAspect).FirstOrDefault(); }

        internal ForIteratorAspect ForIterator { get => (ForIteratorAspect)ChildCSAspectNodes.Where(t => t is ForIteratorAspect).FirstOrDefault(); }

        internal StatementAspect Statement { get => (StatementAspect)ChildCSAspectNodes.Where(t => t is StatementAspect).FirstOrDefault(); }

        internal ForStatementAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ForStatement(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class ForInitializerAspect : CSAspectNode
    {
        internal IEnumerable<CSAspectNode> Expressions => ChildCSAspectNodes;
        internal ForInitializerAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            throw AspectDNErrorFactory.GetException("NotImplementedException");
        }
    }

    internal class ForConditionAspect : CSAspectNode
    {
        internal ExpressionAspect Expression => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false).FirstOrDefault();
        internal ForConditionAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ForCondition(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class ForIteratorAspect : CSAspectNode
    {
        internal IEnumerable<ExpressionAspect> Expressions { get => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false); }

        internal ForIteratorAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            throw AspectDNErrorFactory.GetException("NotImplementedException");
        }
    }

    internal class ForeachStatementAspect : EmbeddedStatementAspect
    {
        internal CSAspectNode TypeOrIdenfifier => CSAspectCompilerHelper.GetDescendingNodesOfType<CSAspectNode>(this, false).FirstOrDefault();
        internal IdentifierNameAspect Identifier => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this, false, TypeOrIdenfifier).FirstOrDefault();
        internal ExpressionAspect Expression => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false, Identifier).FirstOrDefault();
        internal StatementAspect Statement => CSAspectCompilerHelper.GetDescendingNodesOfType<StatementAspect>(this, false).FirstOrDefault();
        internal ForeachStatementAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ForEachStatement(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal abstract class JumpStatementAspect : EmbeddedStatementAspect
    {
        internal JumpStatementAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class BreakStatementAspect : JumpStatementAspect
    {
        internal BreakStatementAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.BreakStatement(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class ContinueStatementAspect : JumpStatementAspect
    {
        internal ContinueStatementAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ContinueStatement(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class SimpleGotoStatementAspect : JumpStatementAspect
    {
        internal IdentifierNameAspect Identifier => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this, false).FirstOrDefault();
        internal SimpleGotoStatementAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.SimpleGotoStatement(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class SwitchGotoStatementAspect : JumpStatementAspect
    {
        internal ExpressionAspect Expression => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false).FirstOrDefault();
        internal SwitchGotoStatementAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.SwitchGotoStatement(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class ReturnStatementAspect : JumpStatementAspect
    {
        internal ExpressionAspect Expression => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false).FirstOrDefault();
        internal ReturnStatementAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ReturnStatement(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class ThrowStatementAspect : JumpStatementAspect
    {
        internal ExpressionAspect Expression => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false).FirstOrDefault();
        internal ThrowStatementAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.ThrowStatement(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class TryStatementAspect : StatementAspect
    {
        internal IEnumerable<CatchClauseAspect> CatchClausesAspects { get => CSAspectCompilerHelper.GetDescendingNodesOfType<CatchClauseAspect>(this, false); }

        internal FinallyClauseAspect FinallyClause { get => CSAspectCompilerHelper.GetDescendingNodesOfType<FinallyClauseAspect>(this).FirstOrDefault(); }

        internal BlockAspect Block { get => CSAspectCompilerHelper.GetDescendingNodesOfType<BlockAspect>(this, false).FirstOrDefault(); }

        internal TryStatementAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.TryStatement(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class CatchClauseAspect : CSAspectNode
    {
        internal TypeAspect Type { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeAspect>(this, false).FirstOrDefault(); }

        internal IdentifierNameAspect Identifier { get => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this, false).FirstOrDefault(); }

        internal BlockAspect Block { get => CSAspectCompilerHelper.GetDescendingNodesOfType<BlockAspect>(this, false).FirstOrDefault(); }

        internal CatchClauseAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.CatchClause(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class FinallyClauseAspect : CSAspectNode
    {
        internal BlockAspect Block => CSAspectCompilerHelper.GetDescendingNodesOfType<BlockAspect>(this, false).FirstOrDefault();
        internal FinallyClauseAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.FinallyClause(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class CheckedStatementAspect : StatementAspect
    {
        internal BlockAspect Block => CSAspectCompilerHelper.GetDescendingNodesOfType<BlockAspect>(this, false).FirstOrDefault();
        internal CheckedStatementAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.CheckedStatement(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class UnCheckedStatementAspect : StatementAspect
    {
        internal BlockAspect Block { get => (BlockAspect)this[0]; }

        internal UnCheckedStatementAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.UnCheckedStatement(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class LockStatementAspect : StatementAspect
    {
        internal ExpressionAspect Expression { get => (ExpressionAspect)this[0]; }

        internal StatementAspect Statement { get => (StatementAspect)this[1]; }

        internal LockStatementAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.LockStatement(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class UsingStatementAspect : StatementAspect
    {
        internal CSAspectNode ResourceAcquisition => CSAspectCompilerHelper.GetDescendingNodesOfType<CSAspectNode>(this, false).FirstOrDefault();
        internal StatementAspect Statement => CSAspectCompilerHelper.GetDescendingNodesOfType<StatementAspect>(this, false, ResourceAcquisition).FirstOrDefault();
        internal UsingStatementAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.UsingStatement(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class YieldStatementAspect : StatementAspect
    {
        internal ExpressionAspect Expression => CSAspectCompilerHelper.GetDescendingNodesOfType<ExpressionAspect>(this, false).FirstOrDefault();
        internal YieldStatementAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.YieldStatement(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class EmptyStatementAspect : StatementAspect
    {
        internal EmptyStatementAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.EmptyStatement(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }
}