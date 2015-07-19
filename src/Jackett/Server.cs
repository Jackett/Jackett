using Autofac;
using Jackett.Services;
using Microsoft.Owin.Hosting;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Windows.Forms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett
{
    public class Server
    {
        private static IContainer container = null;
        private static  string baseAddress = "http://localhost:9000/";
        private static IDisposable _server = null;

        static Server()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<JackettModule>();
            container = builder.Build();


            // Register the container in itself to allow for late resolves
            var secondaryBuilder = new ContainerBuilder();
            secondaryBuilder.RegisterInstance<IContainer>(container);
            SetupLogging(secondaryBuilder, container.Resolve<IConfigurationService>());
            secondaryBuilder.Update(container);
        }

        private static void SetupLogging(ContainerBuilder builder, IConfigurationService config)
        {
            var logConfig = new LoggingConfiguration();

            var logFile = new FileTarget();
            logConfig.AddTarget("file", logFile);
            logFile.Layout = "${longdate} ${level} ${message} \n ${exception:format=ToString}\n";
            logFile.FileName = Path.Combine(config.GetAppDataFolder(), "log.txt");
            logFile.ArchiveFileName = "log.{#####}.txt";
            logFile.ArchiveAboveSize = 500000;
            logFile.MaxArchiveFiles = 1;
            logFile.KeepFileOpen = false;
            logFile.ArchiveNumbering = ArchiveNumberingMode.DateAndSequence;
            var logFileRule = new LoggingRule("*", LogLevel.Debug, logFile);
            logConfig.LoggingRules.Add(logFileRule);

        /*    if (WebServer.IsWindows)
            {
#if !__MonoCS__
                var logAlert = new MessageBoxTarget();
                logConfig.AddTarget("alert", logAlert);
                logAlert.Layout = "${message}";
                logAlert.Caption = "Alert";
                var logAlertRule = new LoggingRule("*", LogLevel.Fatal, logAlert);
                logConfig.LoggingRules.Add(logAlertRule);
#endif
            }*/

            var logConsole = new ConsoleTarget();
            logConfig.AddTarget("console", logConsole);
            logConsole.Layout = "${longdate} ${level} ${message} ${exception:format=ToString}";
            var logConsoleRule = new LoggingRule("*", LogLevel.Debug, logConsole);
            logConfig.LoggingRules.Add(logConsoleRule);

            LogManager.Configuration = logConfig;
            builder.RegisterInstance<Logger>(LogManager.GetCurrentClassLogger()).SingleInstance();
        }

        public static void Start()
        {
            _server = WebApp.Start<Startup>(url: baseAddress);
        }

        public static void Stop()
        {
            if (_server != null)
            {
                _server.Dispose();
            }
        }

        public static IContainer GetContainer()
        {
            return container;
        }
    }
}
