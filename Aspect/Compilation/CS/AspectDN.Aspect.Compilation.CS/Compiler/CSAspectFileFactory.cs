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
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using AspectDN.Aspect.Weaving.IConcerns;
using Mono.Cecil;
using AspectDN.Common;
using AspectDN.Aspect.Compilation.Foundation;
using AspectDN.Aspect.Concerns;
using TokenizerDN.Common;

namespace AspectDN.Aspect.Compilation.CS
{
    internal class CSAspectAssemblyFactory : AspectFileFactory
    {
        CSAspectCoreCompilation _CSAspectCoreCompilation
        {
            get
            {
                return (CSAspectCoreCompilation)_CoreCompilation;
            }
        }

        internal CSAspectAssemblyFactory(AspectCoreCompilation aspectCoreCompilation) : base(aspectCoreCompilation)
        {
        }

        internal override void _CreateAssembly(string assemblyName, MemoryStream mStream, MemoryStream pdbStream)
        {
            _CSAspectCoreCompilation.CSWorkspace.AssemblyName = assemblyName;
            var compileResults = _CSAspectCoreCompilation.CSWorkspace.Emit(mStream, pdbStream);
            var assemblyBytes = new byte[mStream.Length];
            if (compileResults.Success)
            {
                mStream.Position = 0;
                mStream.Read(assemblyBytes, 0, assemblyBytes.Length);
                mStream.SetLength(0);
                mStream.Write(assemblyBytes, 128, assemblyBytes.Length - 128);
            }
            else
                SetDiagnostics(compileResults.Diagnostics);
        }

        internal void SetDiagnostics(IEnumerable<Diagnostic> diagnostics)
        {
            foreach (Diagnostic diagnostic in diagnostics)
            {
                var sourceLocation = SourceLocation.Empy;
                if (diagnostic.Location != Location.None)
                {
                    var aspectNode = _GetAspectNodeInError(diagnostic);
                    sourceLocation = aspectNode.SynToken.GetSourceLocation();
                }

                _CSAspectCoreCompilation.Errors.Add(AspectDNErrorFactory.GetCSCompilerError(diagnostic.Id, diagnostic.Severity == DiagnosticSeverity.Error ? 100 : 20, diagnostic.GetMessage(), sourceLocation));
            }
        }

        private void GetCCSompilerError(string id, int v1, string v2, SourceLocation sourceLocation)
        {
            throw new NotImplementedException();
        }

        CSAspectNode _GetAspectNodeInError(Diagnostic diagnostic)
        {
            var errorSyntaxSpan = diagnostic.Location.SourceSpan;
            var errorSyntaxNodes = diagnostic.Location.SourceTree.GetRoot().DescendantNodesAndTokens().Where(n => errorSyntaxSpan.Contains(n.Span));
            if (!errorSyntaxNodes.Any())
                errorSyntaxNodes = diagnostic.Location.SourceTree.GetRoot().DescendantNodesAndTokens().Where(n => errorSyntaxSpan.IntersectsWith(n.Span));
            var syntaxNode = errorSyntaxNodes.FirstOrDefault();
            var aspectRoot = diagnostic.Location.SourceTree.GetRoot().GetAnnotations("Id").FirstOrDefault();
            var aspectRootData = aspectRoot.Data;
            var tree = _CSAspectCoreCompilation.CSAspectTrees.Where(t => ((CSAspectNode)t.Root).Id == aspectRootData).First();
            CSAspectNode aspectNode = null;
            while (aspectNode == null && syntaxNode != null && syntaxNode.Parent != null)
            {
                var id = syntaxNode.GetAnnotations("Id").FirstOrDefault();
                while (id == null && syntaxNode.Parent != null)
                {
                    syntaxNode = syntaxNode.Parent;
                    id = syntaxNode.GetAnnotations("Id").FirstOrDefault();
                }

                aspectNode = CSAspectCompilerHelper.GetAspectNode((CSAspectNode)tree.Root, id.Data);
                syntaxNode = syntaxNode.Parent;
            }

            if (aspectNode == null)
                aspectNode = tree.AspectRoot;
            return aspectNode;
        }
    }
}