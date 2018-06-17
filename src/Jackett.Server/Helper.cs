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
            Logger logger = LogManager.GetCurrentClassLogger();

            if (builder != null)
            {
                builder.RegisterInstance(logger).SingleInstance();
            }
        }

    }
}
