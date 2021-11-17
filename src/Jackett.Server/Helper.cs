using Autofac;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using NLog;
#if !NET461
using Microsoft.Extensions.Hosting;
#endif

namespace Jackett.Server
{
    public static class Helper
    {
        public static IContainer ApplicationContainer { get; set; }

#if NET461
        public static IApplicationLifetime applicationLifetime;
#else
        public static IHostApplicationLifetime applicationLifetime;
#endif

        public static void Initialize()
        {
            //Load the indexers
            ServerService.Initalize();

            //Kicks off the update checker
            ServerService.Start();

            Logger.Debug("Helper initialization complete");
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

        public static IConfigurationService ConfigService => ApplicationContainer.Resolve<IConfigurationService>();

        public static IServerService ServerService => ApplicationContainer.Resolve<IServerService>();

        public static IServiceConfigService ServiceConfigService => ApplicationContainer.Resolve<IServiceConfigService>();

        public static ServerConfig ServerConfiguration => ApplicationContainer.Resolve<ServerConfig>();

        public static Logger Logger => ApplicationContainer.Resolve<Logger>();

        public static void SetupLogging(ContainerBuilder builder) =>
            builder?.RegisterInstance(LogManager.GetCurrentClassLogger()).SingleInstance();

        public static void SetLogLevel(LogLevel level)
        {
            foreach (var rule in LogManager.Configuration.LoggingRules)
            {
                if (rule.LoggerNamePattern == "Microsoft.*")
                {
                    if (!rule.Levels.Contains(LogLevel.Debug))
                    {
                        //don't change the first microsoftRule
                        continue;
                    }

                    var targets = LogManager.Configuration.ConfiguredNamedTargets;
                    if (level == LogLevel.Debug)
                    {
                        foreach (var target in targets)
                        {
                            rule.Targets.Add(target);
                        }
                    }
                    else
                    {
                        foreach (var target in targets)
                        {
                            rule.Targets.Remove(target);
                        }
                    }
                    continue;
                }

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
