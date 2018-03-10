using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Autofac;
using AutoMapper;
using Jackett.Common.Models;
using Jackett.Common.Models.Config;
using Jackett.Common.Plumbing;
using Jackett.Common.Services;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;
using NLog.Config;
using NLog.LayoutRenderers;
using NLog.Targets;

namespace Jackett.Common
{
    public class Engine
    {
        public static Type WebClientType;

        private static IContainer container = null;
        private static bool _automapperInitialised = false;

        public static void BuildContainer(RuntimeSettings settings, params Autofac.Module[] ApplicationSpecificModules)
        {
            var builder = new ContainerBuilder();
            SetupLogging(settings, builder);
            if (_automapperInitialised == false)
            {
                //Automapper only likes being initialized once per app domain.
                //Since we can restart Jackett from the command line it's possible that we'll build the container more than once. (tests do this too)
                InitAutomapper();
                _automapperInitialised = true;
            }

            builder.RegisterModule(new JackettModule(settings));
            foreach (var module in ApplicationSpecificModules)
            {
                builder.RegisterModule(module);
            }
            container = builder.Build();

            // create PID file early
            if (!string.IsNullOrWhiteSpace(settings.PIDFile))
            {
                try
                {
                    var proc = Process.GetCurrentProcess();
                    File.WriteAllText(settings.PIDFile, proc.Id.ToString());
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Error while creating the PID file");
                }
            }
        }

        private static void InitAutomapper()
        {
            Mapper.Initialize(cfg =>
            {
                cfg.CreateMap<WebClientByteResult, WebClientStringResult>().ForMember(x => x.Content, opt => opt.Ignore()).AfterMap((be, str) =>
                {
                    var encoding = be.Request.Encoding ?? Encoding.UTF8;
                    str.Content = encoding.GetString(be.Content);
                });

                cfg.CreateMap<WebClientStringResult, WebClientByteResult>().ForMember(x => x.Content, opt => opt.Ignore()).AfterMap((str, be) =>
                {
                    if (!string.IsNullOrEmpty(str.Content))
                    {
                        var encoding = str.Request.Encoding ?? Encoding.UTF8;
                        be.Content = encoding.GetBytes(str.Content);
                    }
                });

                cfg.CreateMap<WebClientStringResult, WebClientStringResult>();
                cfg.CreateMap<WebClientByteResult, WebClientByteResult>();
                cfg.CreateMap<ReleaseInfo, ReleaseInfo>();

                cfg.CreateMap<ReleaseInfo, TrackerCacheResult>().AfterMap((r, t) =>
                {
                    if (r.Category != null)
                    {
                        var CategoryDesc = string.Join(", ", r.Category.Select(x => TorznabCatType.GetCatDesc(x)).Where(x => !string.IsNullOrEmpty(x)));
                        t.CategoryDesc = CategoryDesc;
                    }
                    else
                    {
                        t.CategoryDesc = "";
                    }
                });
            });
        }

        public static IContainer GetContainer()
        {
            return container;
        }

        public static IConfigurationService ConfigService
        {
            get
            {
                return container.Resolve<IConfigurationService>();
            }
        }

        public static IProcessService ProcessService
        {
            get
            {
                return container.Resolve<IProcessService>();
            }
        }

        public static IServiceConfigService ServiceConfig
        {
            get
            {
                return container.Resolve<IServiceConfigService>();
            }
        }

        public static ITrayLockService LockService
        {
            get
            {
                return container.Resolve<ITrayLockService>();
            }
        }

        public static IServerService Server
        {
            get
            {
                return container.Resolve<IServerService>();
            }
        }

        public static ServerConfig ServerConfig
        {
            get
            {
                return container.Resolve<ServerConfig>();
            }
        }

        public static IRunTimeService RunTime
        {
            get
            {
                return container.Resolve<IRunTimeService>();
            }
        }

        public static Logger Logger
        {
            get
            {
                return container.Resolve<Logger>();
            }
        }

        public static ISecuityService SecurityService
        {
            get
            {
                return container.Resolve<ISecuityService>();
            }
        }

        private static void SetupLogging(RuntimeSettings settings, ContainerBuilder builder)
        {
            var logFileName = settings.CustomLogFileName ?? "log.txt";
            var logLevel = settings.TracingEnabled ? LogLevel.Debug : LogLevel.Info;
            // Add custom date time format renderer as the default is too long
            ConfigurationItemFactory.Default.LayoutRenderers.RegisterDefinition("simpledatetime", typeof(SimpleDateTimeRenderer));

            var logConfig = new LoggingConfiguration();
            var logFile = new FileTarget();
            logConfig.AddTarget("file", logFile);
            logFile.Layout = "${longdate} ${level} ${message} ${exception:format=ToString}";
            logFile.FileName = Path.Combine(settings.DataFolder, logFileName);
            logFile.ArchiveFileName = "log.{#####}.txt";
            logFile.ArchiveAboveSize = 500000;
            logFile.MaxArchiveFiles = 5;
            logFile.KeepFileOpen = false;
            logFile.ArchiveNumbering = ArchiveNumberingMode.DateAndSequence;
            var logFileRule = new LoggingRule("*", logLevel, logFile);
            logConfig.LoggingRules.Add(logFileRule);

            var logConsole = new ColoredConsoleTarget();
            logConfig.AddTarget("console", logConsole);

            logConsole.Layout = "${simpledatetime} ${level} ${message} ${exception:format=ToString}";
            var logConsoleRule = new LoggingRule("*", logLevel, logConsole);
            logConfig.LoggingRules.Add(logConsoleRule);

            var logService = new LogCacheService();
            logConfig.AddTarget("service", logService);
            var serviceRule = new LoggingRule("*", logLevel, logService);
            logConfig.LoggingRules.Add(serviceRule);

            LogManager.Configuration = logConfig;
            if (builder != null)
            {
                builder.RegisterInstance(LogManager.GetCurrentClassLogger()).SingleInstance();
            }
        }

        public static void SetLogLevel(LogLevel level)
        {
            foreach (var rule in LogManager.Configuration.LoggingRules)
            {
                if (level == LogLevel.Debug)
                {
                    if (!rule.Levels.Contains(LogLevel.Debug))
                    {
                        rule.EnableLoggingForLevel(LogLevel.Debug);
                    }
                }
                else
                {
                    if (rule.Levels.Contains(LogLevel.Debug))
                    {
                        rule.DisableLoggingForLevel(LogLevel.Debug);
                    }
                }
            }

            LogManager.ReconfigExistingLoggers();
        }

        public static void Exit(int exitCode)
        {
            try
            {
                if (Engine.ServerConfig != null &&
                    Engine.ServerConfig.RuntimeSettings != null &&
                    !string.IsNullOrWhiteSpace(Engine.ServerConfig.RuntimeSettings.PIDFile))
                {
                    var PIDFile = Engine.ServerConfig.RuntimeSettings.PIDFile;
                    if (File.Exists(PIDFile))
                    {
                        Engine.Logger.Info("Deleting PID file " + PIDFile);
                        File.Delete(PIDFile);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Error while deleting the PID file");
            }

            Environment.Exit(exitCode);
        }

        public static void SaveServerConfig()
        {
            ConfigService.SaveConfig(ServerConfig);
        }
    }

    [LayoutRenderer("simpledatetime")]
    public class SimpleDateTimeRenderer : LayoutRenderer
    {
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            builder.Append(DateTime.Now.ToString("MM-dd HH:mm:ss"));
        }
    }
}