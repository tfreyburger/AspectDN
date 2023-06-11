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
using System.Windows.Forms;
using System.Runtime.InteropServices;
using AspectDN.Wnd;
using Foundation.Common;
using AspectDN.Aspect.Weaving.IConcerns;

namespace AspectDN
{
    internal class Weave
    {
        [STAThread]
        internal static void Main(string[] args)
        {
            if (args == null || args.Length <= 0)
                throw AspectDNErrorFactory.GetException("CommandProcessArgumentInvalid");
            switch (args[0].ToUpper())
            {
                case "-CREATE":
                    if (!File.Exists(args[1]))
                        throw AspectDNErrorFactory.GetException("ProjectConfigurationFileNotExist", args[1]);
                    Create(args[1]);
                    break;
                case "-COMPILE":
                    throw new NotImplementedException();
                case "-WEAVE":
                    throw new NotImplementedException();
                case "-WINDOW":
                    _Window();
                    break;
                case "-HELP":
                    _Help();
                    break;
                default:
                    throw AspectDNErrorFactory.GetException("CommandProcessArgumentInvalid", args[1]);
            }
        }

        public static void Create(string projectFileName)
        {
            XDocument xDoc = null;
            try
            {
                xDoc = XDocument.Parse(File.ReadAllText(projectFileName, Encoding.UTF8));
            }
            catch (Exception ex)
            {
                throw AspectDNErrorFactory.GetException("BadProjectConfigurationFile", projectFileName, ex.Message);
            }

            switch (Path.GetExtension(projectFileName).ToLower())
            {
                case ".aspcfg":
                    AspectSolutionConfiguration aspectSolution = null;
                    try
                    {
                        aspectSolution = new AspectSolutionConfiguration().Accept(new AspectConfigurationVisitor(), xDoc);
                    }
                    catch (Exception ex)
                    {
                        throw AspectDNErrorFactory.GetException("BadProjectConfigurationFile", projectFileName, ex.Message);
                    }

                    new AspectSolutionWeaver(aspectSolution).Weave();
                    break;
                case ".saspprjcfg":
                    TaskEventLogger.Log(null, new TaskEvent()
                    {Message = "Start Process"});
                    SingleAspectProjectConfiguration singleProject = null;
                    try
                    {
                        singleProject = new SingleAspectProjectConfiguration().Setup(xDoc);
                        if (!singleProject.OnError)
                            singleProject.CreateAssembly();
                        else
                            TaskEventLogger.Log(null, new TaskEvent()
                            {Message = AspectDNErrorFactory.GetError("WeavingError").ToString()});
                    }
                    catch (Exception ex)
                    {
                        throw AspectDNErrorFactory.GetException("BadProjectConfigurationFile", projectFileName, ex.Message);
                    }

                    break;
            }
        }

        static void _Window()
        {
            new MainWindow().ShowDialog();
        }

        static void _Help()
        {
            System.Console.WriteLine();
            System.Console.ReadLine();
        }
    }
}