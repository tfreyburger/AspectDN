// Author:
//
// T. Freyburger (t.freyburger@gmail.com)
//
// Copyright (c)  Thierry Freyburger
//
// Licensed under the GPLV3 license.
////
using System;
using AspectDN.Common;
using System.Xml.Linq;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspectDN
{
    public class Weave
    {
        public static void Main(string[] args)
        {
            if (args == null || args.Length <= 0)
                throw AspectDNErrorFactory.GetException("NoProjectConfigurationFile");
            if (!File.Exists(args[1]))
                throw AspectDNErrorFactory.GetException("ProjectConfigurationFileNotExist", args[1]);
            XDocument xDoc = null;
            try
            {
                xDoc = XDocument.Parse(File.ReadAllText(args[1], Encoding.UTF8));
            }
            catch (Exception ex)
            {
                throw AspectDNErrorFactory.GetException("BadProjectConfigurationFile", args[1], ex.Message);
            }

            switch (Path.GetExtension(args[1]).ToLower())
            {
                case ".aspcfg":
                    AspectSolutionConfiguration aspectSolution = null;
                    try
                    {
                        aspectSolution = new AspectSolutionConfiguration().Accept(new AspectConfigurationVisitor(), xDoc);
                    }
                    catch (Exception ex)
                    {
                        throw AspectDNErrorFactory.GetException("BadProjectConfigurationFile", args[1], ex.Message);
                    }

                    _Process(aspectSolution);
                    break;
                case ".saspprjcfg":
                    SingleAspectProjectConfiguration singleProject = null;
                    try
                    {
                        singleProject = new SingleAspectProjectConfiguration().Setup(xDoc);
                    }
                    catch (Exception ex)
                    {
                        throw AspectDNErrorFactory.GetException("BadProjectConfigurationFile", args[1], ex.Message);
                    }

                    _Process(singleProject, args[0]);
                    break;
            }
        }

        static void _Process(AspectSolutionConfiguration aspectSolution)
        {
            new AspectSolutionWeaver(aspectSolution).Weave();
        }

        static void _Process(SingleAspectProjectConfiguration singleProject, string processKind)
        {
            switch (processKind.ToLower())
            {
                case "-create":
                    singleProject.CreateAssembly();
                    break;
                case "-compile":
                    throw new NotImplementedException();
                case "-weave":
                    throw new NotImplementedException();
            }
        }
    }
}