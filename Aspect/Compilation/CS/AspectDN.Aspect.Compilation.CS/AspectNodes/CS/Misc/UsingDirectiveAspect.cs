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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using AspectDN.Common;

namespace AspectDN.Aspect.Compilation.CS
{
    internal abstract class UsingDirectiveAspect : CSAspectNode
    {
        internal UsingDirectiveAspect(ISynToken token) : base(token)
        {
        }
    }

    internal class UsingAliasDirectiveAspect : UsingDirectiveAspect
    {
        internal IdentifierNameAspect IdentiferName => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this, false).FirstOrDefault();
        internal NamespaceOrTypenameAspect NamespaceOrTypename => CSAspectCompilerHelper.GetDescendingNodesOfType<NamespaceOrTypenameAspect>(this, false).FirstOrDefault();
        internal UsingAliasDirectiveAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.UsingAliasDirective(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class UsingNamespaceDirectiveAspect : UsingDirectiveAspect
    {
        internal CSAspectNode Name => CSAspectCompilerHelper.GetDescendingNodesOfType<CSAspectNode>(this, false).FirstOrDefault();
        internal UsingNamespaceDirectiveAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.UsingNamespaceDirective(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }

    internal class NamespaceOrTypenameAspect : TypeAspect
    {
        internal NameAspect Left => (NameAspect)ChildCSAspectNodes.First();
        internal NameAspect Right => (NameAspect)(ChildAspectNodes.Count() == 2 ? ChildCSAspectNodes.Last() : null);
        internal NamespaceOrTypenameAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.NamespaceOrTypename(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }

        internal override string GetName()
        {
            var namespaceOrTypeName = Left.GetName();
            if (Right != null)
                namespaceOrTypeName += "." + Right.GetName();
            return namespaceOrTypeName;
        }
    }

    internal abstract class NameAspect : PrimaryExpressionAspect
    {
        internal NameAspect(ISynToken token) : base(token)
        {
        }

        internal abstract string GetName();
    }

    internal class IdentifierNameAspect : NameAspect
    {
        internal IdentifierNameAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.IdentifierName(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }

        internal SyntaxNodeOrToken? GetSyntaxNode(Type type)
        {
            SyntaxNodeOrToken syntaxNodeOrToken = null;
            if (type.FullName == typeof(LiteralExpressionSyntax).FullName)
                syntaxNodeOrToken = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(TokenValue));
            if (type.FullName == typeof(SyntaxToken).FullName)
                syntaxNodeOrToken = SyntaxFactory.Identifier(TokenValue);
            if (syntaxNodeOrToken == null)
                throw new NotImplementedException();
            return syntaxNodeOrToken.WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }

        internal override string GetName()
        {
            return TokenValue;
        }
    }

    internal class GenericNameAspect : NameAspect
    {
        internal IdentifierNameAspect IdentfierName => CSAspectCompilerHelper.GetDescendingNodesOfType<IdentifierNameAspect>(this, false).FirstOrDefault();
        internal TypeArgumentListAspect TypeArgumentList => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeArgumentListAspect>(this, false).FirstOrDefault();
        internal GenericNameAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.GenericName(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }

        internal override string GetName()
        {
            StringBuilder sb = new StringBuilder().Append("<");
            for (int i = 0; i < TypeArgumentList.TypeArguments.Count; i++)
                sb.Append(i != 0 ? "," : "").Append(TypeArgumentList.TypeArguments[i].TokenValue);
            sb = new StringBuilder().Append(">");
            return $"{IdentfierName.GetName()}.{sb.ToString()}";
        }
    }

    internal class QualifiedIdentifierAspect : NameAspect
    {
        internal NameAspect Left => (NameAspect)CSAspectCompilerHelper.GetDescendingNodesOfType<CSAspectNode>(this, false).FirstOrDefault();
        internal NameAspect Right => (NameAspect)CSAspectCompilerHelper.GetDescendingNodesOfType<CSAspectNode>(this, false, Left).FirstOrDefault();
        internal QualifiedIdentifierAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.QualifiedName(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }

        internal override string GetName()
        {
            return $"{Left.GetName()}.{Right.GetName()}";
        }
    }

    internal class QualifiedAliasMemberAspect : NameAspect
    {
        internal NameAspect Left => (NameAspect)CSAspectCompilerHelper.GetDescendingNodesOfType<CSAspectNode>(this, false).FirstOrDefault();
        internal NameAspect Right => (NameAspect)CSAspectCompilerHelper.GetDescendingNodesOfType<CSAspectNode>(this, false, Left).FirstOrDefault();
        internal QualifiedAliasMemberAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.QualifiedAliasMember(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }

        internal override string GetName()
        {
            return $"{Left.TokenValue} '::' {Right.TokenValue}";
        }
    }

    internal class TypeArgumentListAspect : CSAspectNode
    {
        internal List<TypeAspect> TypeArguments { get => CSAspectCompilerHelper.GetDescendingNodesOfType<TypeAspect>(this, false).ToList(); }

        internal TypeArgumentListAspect(ISynToken token) : base(token)
        {
        }

        internal override SyntaxNodeOrToken? GetSyntaxNode()
        {
            return CSAspectCompilerHelper.TypeArgumentListSyntax(this).WithAdditionalAnnotations(new SyntaxAnnotation(nameof(Id), Id));
        }
    }
}