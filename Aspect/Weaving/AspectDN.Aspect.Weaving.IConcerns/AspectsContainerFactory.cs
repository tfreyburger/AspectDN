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
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using System.Reflection;
using AspectDN.Common;

namespace AspectDN.Aspect.Weaving.IConcerns
{
    public class AspectsContainerFactory
    {
        public static IAspectsContainer Get(string aspectsContainerFactoryAssemblyPath, IEnumerable<string> aspectFiles)
        {
            var aspectsContainerFactoryAssembly = Assembly.LoadFrom(Helper.GetFullPath(aspectsContainerFactoryAssemblyPath));
            var aspectsContainerCreator = aspectsContainerFactoryAssembly.GetTypes().FirstOrDefault(t => t.Name == "ConcernsContainer");
            var aspectsContainerCreatorMethod = aspectsContainerCreator.GetMethod("Create", new Type[]{typeof(string[]), typeof(string[])});
            var searchAspectFileDirectoryNames = aspectFiles.Select(t => Path.GetDirectoryName(t)).Where(d => !string.IsNullOrEmpty(d)).Distinct().ToArray();
            var aspectsContainer = (IAspectsContainer)aspectsContainerCreatorMethod.Invoke(null, new object[]{aspectFiles.ToArray(), searchAspectFileDirectoryNames});
            return aspectsContainer;
        }

        public static IAspectsContainer Get(IEnumerable<byte[]> aspectFiles, string[] searchAspectRepositoryDirectoryNames)
        {
            var aspectsContainerAssembly = Assembly.LoadFrom(Helper.GetFullPath(@"..\AspectDN.Aspect.Concerns.dll"));
            var aspectsContainerCreator = aspectsContainerAssembly.GetTypes().FirstOrDefault(t => t.Name == "ConcernsContainer");
            var aspectsContainerCreatorMethod = aspectsContainerCreator.GetMethod("Create", new Type[]{typeof(byte[][]), typeof(string[])});
            var aspectsContainer = (IAspectsContainer)aspectsContainerCreatorMethod.Invoke(null, new object[]{aspectFiles.ToArray(), searchAspectRepositoryDirectoryNames});
            return aspectsContainer;
        }
    }
}