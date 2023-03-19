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
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace AspectDN.Common
{
    internal class AssemblySorter
    {
        IEnumerable<AssemblyFile> _AssemblyFiles;
        internal AssemblySorter(IEnumerable<AssemblyFile> assemblyFiles)
        {
            _AssemblyFiles = assemblyFiles;
        }

        internal IEnumerable<AssemblyFile> Sort()
        {
            foreach (var assemblyFile in _AssemblyFiles.ToArray())
                _GetChild(assemblyFile);
            foreach (var node in _AssemblyFiles.Where(t => !_AssemblyFiles.Any(n => n.Children.Exists(c => c == t))))
                _Sort(node, 1);
            return _AssemblyFiles.OrderBy(t => t.LevelNo);
        }

        void _GetChild(AssemblyFile assemblyNode)
        {
            if (assemblyNode.AssemblyNameReferences.Any())
            {
                foreach (var refAssemblyName in assemblyNode.AssemblyNameReferences)
                {
                    var child = _AssemblyFiles.FirstOrDefault(t => t.AssemblyName == refAssemblyName);
                    if (child == null)
                        continue;
                    assemblyNode.Children.Add(child);
                }
            }
        }

        void _Sort(AssemblyFile assemblyNode, int levelNo)
        {
            if (assemblyNode.LevelNo < levelNo)
            {
                assemblyNode.LevelNo = levelNo;
                foreach (var child in assemblyNode.Children)
                    _Sort(child, levelNo + 1);
            }
        }
    }
}