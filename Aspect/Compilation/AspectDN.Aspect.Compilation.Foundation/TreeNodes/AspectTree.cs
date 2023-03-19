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
using TokenizerDN.Common;
using TokenizerDN.Common.SourceAnalysis;

namespace AspectDN.Aspect.Compilation.Foundation
{
    internal abstract class AspectTree : ITree
    {
        IDocument _Document;
        protected AspectNode _Root;
        internal AspectNode Root
        {
            get
            {
                return _Root;
            }

            set
            {
                _Root = value;
            }
        }

        ISourceAnalyzer _SrcAnalyzer;
        string _SyntaxName;
        public IDocument Document { get => _Document; }

        protected bool _HasTreeChanged;
        internal bool HasTreeChanged
        {
            get
            {
                return _HasTreeChanged;
            }
        }

        bool _SourceChanged;
        internal bool SourceChanged
        {
            get
            {
                return _SourceChanged;
            }
        }

        internal string SyntaxName
        {
            get
            {
                return _SyntaxName;
            }
        }

        protected ITokenVisitor _TokenVisitor;
        List<ICompilerError> _Errors;
        internal IReadOnlyList<ICompilerError> Errors
        {
            get
            {
                return _Errors;
            }
        }

        public string Id { get; }

        public AspectTree(string syntaxName, string source, string sourceName)
        {
            _SyntaxName = syntaxName;
            _Errors = new List<ICompilerError>();
            if (source != null)
            {
                _Document = new Document(source, sourceName ?? "");
                _SourceChanged = true;
            }

            Id = Guid.NewGuid().ToString("N");
        }

        internal abstract AspectNode CreateRoot(ISynToken token = null);
        internal void SetHasChanged(bool changed = true)
        {
            _HasTreeChanged = changed;
        }

        internal void Parse()
        {
            ParseFromRule(null);
        }

        internal void Parse(string source, string sourceName)
        {
            ParseFromRule(null, source, sourceName);
        }

        internal void Parse(string filename)
        {
            ParseFromRule(null, filename);
        }

        internal void ParseFromRule(string ruleName)
        {
            if (_Document != null)
            {
                if (_SrcAnalyzer == null || _TokenVisitor == null)
                    _SetSyntax();
                _SrcAnalyzer.Parse(_Document, ruleName).AcceptVisitor(_TokenVisitor);
                _SourceChanged = false;
                _HasTreeChanged = true;
            }
        }

        internal void ParseFromRule(string ruleName, string source, string sourceName)
        {
            _Document = new Document(source, sourceName ?? "");
            _SourceChanged = true;
            ParseFromRule(ruleName);
        }

        internal void ParseFromRule(string ruleName, string filename)
        {
            using (StreamReader sr = new StreamReader(filename))
            {
                _Document = new Document(sr.ReadToEnd(), filename);
            }

            _SourceChanged = true;
            ParseFromRule(ruleName);
        }

        void _SetSyntax()
        {
            _TokenVisitor = CompilerAspectRepository.GetTokenVisitor(_SyntaxName, this, _Errors);
            _SrcAnalyzer = CompilerAspectRepository.GetSourceAnalyzer(_SyntaxName);
        }
    }
}