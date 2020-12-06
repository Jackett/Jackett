using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommandLine;
using CommandLine.Text;
using Jackett.Common.Models.Config;
using Jackett.Common.Services;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Web;

namespace Jackett.Server
{
    public static class Program
    {
        public static IConfiguration Configuration { get; set; }
        private static RuntimeSettings Settings { get; set; }
        public static bool isWebHostRestart = false;

        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            var commandLineParser = new Parser(settings => settings.CaseSensitive = false);
            var optionsResult = commandLineParser.ParseArguments<ConsoleOptions>(args);
            var runtimeDictionary = new Dictionary<string, string>();
            var consoleOptions = new ConsoleOptions();

            optionsResult.WithNotParsed(errors =>
            {
                var text = HelpText.AutoBuild(optionsResult);
                text.Copyright = " ";
                text.Heading = "Jackett " + EnvironmentUtil.JackettVersion();
                Console.WriteLine(text);
                Environment.Exit(1);
            });

            optionsResult.WithParsed(options =>
            {
                if (string.IsNullOrEmpty(options.Client))
                    options.Client = DotNetCoreUtil.IsRunningOnDotNetCore ? "httpclient2" : "httpclient";

                Settings = options.ToRunTimeSettings();
                consoleOptions = options;
                runtimeDictionary = GetValues(Settings);
            });

            LogManager.Configuration = LoggingSetup.GetLoggingConfiguration(Settings);
            var logger = LogManager.GetCurrentClassLogger();
            logger.Info("Starting Jackett " + EnvironmentUtil.JackettVersion());

            // create PID file early
            if (!string.IsNullOrWhiteSpace(Settings.PIDFile))
            {
                try
                {
                    var proc = Process.GetCurrentProcess();
                    File.WriteAllText(Settings.PIDFile, proc.Id.ToString());
                }
                catch (Exception e)
                {
                    logger.Error($"Error while creating the PID file\n{e}");
                }
            }

            Initialisation.CheckEnvironmentalVariables(logger);
            Initialisation.ProcessSettings(Settings, logger);

            ISerializeService serializeService = new SerializeService();
            IProcessService processService = new ProcessService(logger);
            IConfigurationService configurationService = new ConfigurationService(serializeService, processService, logger, Settings);

            if (consoleOptions.Install || consoleOptions.Uninstall || consoleOptions.StartService || consoleOptions.StopService || consoleOptions.ReserveUrls)
            {
                var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;

                if (isWindows)
                {
                    var serverConfig = configurationService.BuildServerConfig(Settings);
                    Initialisation.ProcessWindowsSpecificArgs(consoleOptions, processService, serverConfig, logger);
                }
                else
                {
                    logger.Error("ReserveUrls and service arguments only apply to Windows, please remove them from your start arguments");
                    Environment.Exit(1);
                }
            }

            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(runtimeDictionary);
            builder.AddJsonFile(Path.Combine(configurationService.GetAppDataFolder(), "appsettings.json"), optional: true);

            Configuration = builder.Build();

            do
            {
                if (!isWebHostRestart)
                {
                    if (consoleOptions.Port != 0 || consoleOptions.ListenPublic || consoleOptions.ListenPrivate)
                    {
                        var serverConfiguration = configurationService.BuildServerConfig(Settings);
                        Initialisation.ProcessConsoleOverrides(consoleOptions, processService, serverConfiguration, configurationService, logger);
                    }
                }

                var serverConfig = configurationService.BuildServerConfig(Settings);
                int.TryParse(serverConfig.Port.ToString(), out var configPort);
                var url = serverConfig.GetListenAddresses(serverConfig.AllowExternal);

                isWebHostRestart = false;

                try
                {
                    logger.Debug("Creating web host...");
                    var applicationFolder = Path.Combine(configurationService.ApplicationFolder(), "Content");
                    logger.Debug($"Content root path is: {applicationFolder}");

                    CreateWebHostBuilder(args, url, applicationFolder).Build().Run();
                }
                catch (Exception e)
                {
                    if (e.InnerException is Microsoft.AspNetCore.Connections.AddressInUseException)
                    {
                        logger.Error($"Address already in use: Most likely Jackett is already running. {e.Message}");
                        Environment.Exit(1);
                    }
                    logger.Error(e);
                    throw;
                }
            } while (isWebHostRestart);
        }

        public static Dictionary<string, string> GetValues(object obj)
        {
            return obj
                    .GetType()
                    .GetProperties()
                    .ToDictionary(p => "RuntimeSettings:" + p.Name, p => p.GetValue(obj) == null ? null : p.GetValue(obj).ToString());
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            try
            {
                if (Settings != null && !string.IsNullOrWhiteSpace(Settings.PIDFile))
                {
                    var pidFile = Settings.PIDFile;
                    if (File.Exists(pidFile))
                    {
                        Console.WriteLine("Deleting PID file " + pidFile);
                        File.Delete(pidFile);
                    }
                    LogManager.Shutdown();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString(), "Error while deleting the PID file");
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args, string[] urls, string contentRoot) =>
            WebHost.CreateDefaultBuilder(args)
                .UseContentRoot(contentRoot)
                .UseWebRoot(contentRoot)
                .UseUrls(urls)
                .PreferHostingUrls(true)
                .UseConfiguration(Configuration)
                .UseStartup<Startup>()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                })
                .UseNLog();
    }
}
