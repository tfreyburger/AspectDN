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
using log4net;
using log4net.Repository.Hierarchy;
using log4net.Appender;
using log4net.Layout;

namespace AspectDN.Common
{
    internal class LogWriter
    {
        ILog _Log;
        internal LogWriter(string loggerName, string logFilename)
        {
            _Log = LogManager.GetLogger(loggerName);
            _SetLevel(loggerName, "ALL");
            _AddAppender(loggerName, _CreateFileAppender("LogErr", logFilename));
            _Log.Logger.Repository.Configured = true;
        }

        internal void LogInfo(string info)
        {
            _Log.Info(info);
        }

        internal void ShutDown()
        {
            _Log.Logger.Repository.Shutdown();
        }

        void _SetLevel(string loggerName, string levelName)
        {
            var logger = (Logger)LogManager.GetLogger(loggerName).Logger;
            logger.Level = logger.Hierarchy.LevelMap[levelName];
        }

        void _AddAppender(string loggerName, IAppender appender)
        {
            ILog log = log4net.LogManager.GetLogger(loggerName);
            var logger = (Logger)log.Logger;
            logger.AddAppender(appender);
        }

        IAppender _CreateFileAppender(string name, string fileName)
        {
            var appender = new log4net.Appender.FileAppender();
            appender.Name = name;
            appender.File = fileName;
            appender.AppendToFile = true;
            var layout = new PatternLayout();
            layout.ConversionPattern = "%date %level %logger - %message%newline";
            layout.ActivateOptions();
            appender.Layout = layout;
            appender.ActivateOptions();
            return appender;
        }

        IAppender _FindAppender(string appenderName)
        {
            return log4net.LogManager.GetRepository().GetAppenders().Where(t => t.Name == appenderName).FirstOrDefault();
        }
    }
}