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
            try
            {
                var xDoc = XDocument.Parse(File.ReadAllText(projectFileName, Encoding.UTF8));
                switch (Path.GetExtension(projectFileName).ToLower())
                {
                    case ".aspcfg":
                        _CreateProject(xDoc, projectFileName);
                        break;
                    case ".saspprjcfg":
                        _CreateSingleProject(xDoc, projectFileName);
                        break;
                }
            }
            catch (Exception ex)
            {
                TaskEventLogger.Log(null, new TaskEvent()
                {Message = AspectDNErrorFactory.GetException("BadProjectConfigurationFile", projectFileName, ex.Message).ToString()});
            }
        }

        static void _CreateSingleProject(XDocument xDoc, string projectFileName)
        {
            TaskEventLogger.Log(null, new TaskEvent()
            {Message = "Start Process"});
            try
            {
                var singleProject = new SingleAspectProjectConfiguration().Setup(xDoc);
                if (!singleProject.OnError)
                    singleProject.CreateAssembly();
                else
                    TaskEventLogger.Log(null, new TaskEvent()
                    {Message = AspectDNErrorFactory.GetError("WeavingError").ToString()});
            }
            catch (Exception ex)
            {
                TaskEventLogger.Log(null, new TaskEvent()
                {Message = AspectDNErrorFactory.GetError("WeavingError").ToString()});
                throw;
            }
        }

        static void _CreateProject(XDocument xDoc, string projectFileName)
        {
            AspectSolutionConfiguration aspectSolution = null;
            try
            {
                aspectSolution = new AspectSolutionConfiguration().Accept(new AspectConfigurationVisitor(), xDoc);
            }
            catch (Exception ex)
            {
                TaskEventLogger.Log(null, new TaskEvent()
                {Message = AspectDNErrorFactory.GetError("WeavingError").ToString()});
            }

            new AspectSolutionWeaver(aspectSolution).Weave();
        }

        static void _Window()
        {
            var window = new MainWindow();
            window.ShowDialog();
            window.Close();
        }

        static void _Help()
        {
            System.Console.WriteLine();
            System.Console.ReadLine();
        }
    }
}