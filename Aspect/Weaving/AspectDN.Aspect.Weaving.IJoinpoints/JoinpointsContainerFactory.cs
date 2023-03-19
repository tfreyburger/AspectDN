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
using Mono.Cecil;
using System.Reflection;
using AspectDN.Common;

namespace AspectDN.Aspect.Weaving.IJoinpoints
{
    public class JoinpointsContainerFactory
    {
        public static IJoinpointsContainer Get(string joinpointsContainerAssemblyPath, IEnumerable<AssemblyFile> assemblyTargetFilenames)
        {
            var joinpointsContainerAssembly = Assembly.LoadFrom(Helper.GetFullPath(joinpointsContainerAssemblyPath));
            var joinpointsContainerCreator = joinpointsContainerAssembly.GetTypes().FirstOrDefault(t => t.Name == "JoinpointsContainer");
            var joinpointsContainerCreatorMethod = joinpointsContainerCreator.GetMethod("Create");
            var joinpointsContainer = (IJoinpointsContainer)joinpointsContainerCreatorMethod.Invoke(null, new object[]{assemblyTargetFilenames.ToList()});
            return joinpointsContainer;
        }

        public static IJoinpointsContainer Get(IEnumerable<AssemblyDefinition> assemblyTargets)
        {
            var joinpointsContainerAssembly = Assembly.LoadFrom(@"AspectDN.Aspect.Joinpoints.dll");
            var joinpointsContainerCreator = joinpointsContainerAssembly.GetTypes().FirstOrDefault(t => t.Name == "JoinpointsContainer");
            var joinpointsContainerCreatorMethod = joinpointsContainerCreator.GetMethod("Create");
            var joinpointsContainer = (IJoinpointsContainer)joinpointsContainerCreatorMethod.Invoke(null, new object[]{assemblyTargets});
            return joinpointsContainer;
        }
    }
}