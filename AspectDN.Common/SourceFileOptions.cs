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
using System.Text;
using System.Threading.Tasks;

namespace AspectDN.Common
{
    internal class SourceFileOptions
    {
        internal DirectoryInfo SourceDirectory { get; }

        internal IEnumerable<string> SearchPatterns { get; }

        internal IEnumerable<string> ExcludedSourceFiles { get; }

        internal IEnumerable<DirectoryInfo> ExcludedDirectories { get; }

        internal SourceFileOptions(string sourceDirectory, IEnumerable<string> excludedSourceFiles, IEnumerable<string> excludedDirectories, IEnumerable<string> searchPatterns)
        {
            SourceDirectory = new DirectoryInfo(Helper.GetFullPath(sourceDirectory));
            SearchPatterns = searchPatterns;
            if (excludedSourceFiles == null)
                excludedSourceFiles = new string[0];
            ExcludedSourceFiles = excludedSourceFiles.Select(t => Helper.GetFullPath(t));
            if (excludedDirectories == null)
                excludedDirectories = new string[0];
            ExcludedDirectories = excludedDirectories.Select(t => new DirectoryInfo(Helper.GetFullPath(t)));
        }
    }
}