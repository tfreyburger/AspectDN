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

namespace AspectDN.Common
{
    public class AspectDNAssemblyResolver : DefaultAssemblyResolver
    {
        AspectDNAssemblyResolver _AssemblyResolver;
        public AspectDNAssemblyResolver() : base()
        {
            base.ResolveFailure += OnResolveFailure;
        }

        public void AddAssembly(AssemblyDefinition assemblyDefinition)
        {
            RegisterAssembly(assemblyDefinition);
        }

        public void Join(AspectDNAssemblyResolver aspectDNAssemblyResolver)
        {
            if (_AssemblyResolver == null)
                _AssemblyResolver = aspectDNAssemblyResolver;
        }

        internal AssemblyDefinition OnResolveFailure(object sender, AssemblyNameReference assemblyNameReference)
        {
            AssemblyDefinition assemblyDefinition = null;
            if (_AssemblyResolver != null)
                assemblyDefinition = _AssemblyResolver.Resolve(assemblyNameReference);
            return assemblyDefinition;
        }
    }
}