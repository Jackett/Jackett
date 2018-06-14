using Autofac;
using AutoMapper;
using Jackett.Common;
using Jackett.Common.Models;
using Jackett.Common.Models.Config;
using Jackett.Common.Services;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using Microsoft.AspNetCore.Hosting;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Jackett.Server
{
    public static class Helper
    {
        public static IContainer ApplicationContainer { get; set; }
        public static IApplicationLifetime applicationLifetime;
        private static bool _automapperInitialised = false;
        public static ConsoleOptions ConsoleOptions { get; set; }


        public static void Initialize()
        {
            if (_automapperInitialised == false)
            {
                //Automapper only likes being initialized once per app domain.
                //Since we can restart Jackett from the command line it's possible that we'll build the container more than once. (tests do this too)
                InitAutomapper();
                _automapperInitialised = true;
            }

            ProcessSettings();

            //Load the indexers
            ServerService.Initalize();

            //Kicks off the update checker
            ServerService.Start();
        }

        private static void ProcessSettings()
        {
            RuntimeSettings runtimeSettings = ServerConfiguration.RuntimeSettings;

            if (runtimeSettings.ClientOverride != "httpclient" && runtimeSettings.ClientOverride != "httpclient2")
            {
                Logger.Error($"Client override ({runtimeSettings.ClientOverride}) has been deprecated, please remove it from your start arguments");
                Environment.Exit(1);
            }

            if (runtimeSettings.DoSSLFix != null)
            {
                Logger.Error("SSLFix has been deprecated, please remove it from your start arguments");
                Environment.Exit(1);
            }

            if (runtimeSettings.LogRequests)
            {
                Logger.Info("Logging enabled.");
            }

            if (runtimeSettings.TracingEnabled)
            {
                Logger.Info("Tracing enabled.");
            }

            if (runtimeSettings.IgnoreSslErrors == true)
            {
                Logger.Info("Jackett will ignore SSL certificate errors.");
            }

            if (!string.IsNullOrWhiteSpace(runtimeSettings.CustomDataFolder))
            {
                Logger.Info("Jackett Data will be stored in: " + runtimeSettings.CustomDataFolder);
            }

            if (runtimeSettings.ProxyConnection != null)
            {
                Logger.Info("Proxy enabled. " + runtimeSettings.ProxyConnection);
            }

            if (ConsoleOptions.Install || ConsoleOptions.Uninstall || ConsoleOptions.StartService || ConsoleOptions.StopService || ConsoleOptions.ReserveUrls)
            {
                bool isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;

                if (!isWindows)
                {
                    Logger.Error($"ReserveUrls and service arguments only apply to Windows, please remove them from your start arguments");
                    Environment.Exit(1);
                }
            }


            /*  ======     Actions    =====  */

            // Install service
            if (ConsoleOptions.Install)
            {
                Logger.Info("Initiating Jackett service install");
                ServiceConfigService.Install();
                Environment.Exit(1);
            }

            // Uninstall service
            if (ConsoleOptions.Uninstall)
            {
                Logger.Info("Initiating Jackett service uninstall");
                ServiceConfigService.Uninstall();
                Environment.Exit(1);
            }

            // Start Service
            if (ConsoleOptions.StartService)
            {
                if (!ServiceConfigService.ServiceRunning())
                {
                    Logger.Info("Initiating Jackett service start");
                    ServiceConfigService.Start();
                }
                Environment.Exit(1);
            }

            // Stop Service
            if (ConsoleOptions.StopService)
            {
                if (ServiceConfigService.ServiceRunning())
                {
                    Logger.Info("Initiating Jackett service stop");
                    ServiceConfigService.Stop();
                }
                Environment.Exit(1);
            }

            // Reserve urls
            if (ConsoleOptions.ReserveUrls)
            {
                Logger.Info("Initiating ReserveUrls");
                ServerService.ReserveUrls(doInstall: true);
                Environment.Exit(1);
            }
        }

        public static void RestartWebHost()
        {
            Logger.Info("Restart of the web application host (not process) initiated");
            Program.isWebHostRestart = true;
            applicationLifetime.StopApplication();
        }

        public static void StopWebHost()
        {
            Logger.Info("Jackett is being stopped");
            applicationLifetime.StopApplication();
        }

        public static IConfigurationService ConfigService
        {
            get
            {
                return ApplicationContainer.Resolve<IConfigurationService>();
            }
        }

        public static IServerService ServerService
        {
            get
            {
                return ApplicationContainer.Resolve<IServerService>();
            }
        }

        public static IServiceConfigService ServiceConfigService
        {
            get
            {
                return ApplicationContainer.Resolve<IServiceConfigService>();
            }
        }

        public static ServerConfig ServerConfiguration
        {
            get
            {
                return ApplicationContainer.Resolve<ServerConfig>();
            }
        }

        public static Logger Logger
        {
            get
            {
                return ApplicationContainer.Resolve<Logger>();
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
                        t.CategoryDesc = string.Join(", ", r.Category.Select(x => TorznabCatType.GetCatDesc(x)).Where(x => !string.IsNullOrEmpty(x)));
                    }
                    else
                    {
                        t.CategoryDesc = "";
                    }
                });
            });
        }

        public static void SetupLogging(RuntimeSettings settings, ContainerBuilder builder)
        {
            var logFileName = settings.CustomLogFileName ?? "log.txt";
            var logLevel = settings.TracingEnabled ? NLog.LogLevel.Debug : NLog.LogLevel.Info;
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
    }
}
