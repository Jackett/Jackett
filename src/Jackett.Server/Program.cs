using CommandLine;
using CommandLine.Text;
using Jackett.Common.Models.Config;
using Jackett.Common.Services;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

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
            ConsoleOptions consoleOptions = new ConsoleOptions();

            optionsResult.WithNotParsed(errors =>
            {
                var text = HelpText.AutoBuild(optionsResult);
                text.Copyright = " ";
                text.Heading = "Jackett v" + EnvironmentUtil.JackettVersion;
                Console.WriteLine(text);
                Environment.Exit(1);
                return;
            });

            optionsResult.WithParsed(options =>
            {
                if (string.IsNullOrEmpty(options.Client))
                {
                    //TODO: Remove libcurl once off owin
                    options.Client = "httpclient";
                }

                Settings = options.ToRunTimeSettings();
                consoleOptions = options;
                runtimeDictionary = GetValues(Settings);
            });

            LogManager.Configuration = Initialisation.SetupLogging(Settings);
            Logger logger = LogManager.GetCurrentClassLogger();
            logger.Info("Starting Jackett v" + EnvironmentUtil.JackettVersion);

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
                    logger.Error(e, "Error while creating the PID file");
                }
            }

            Initialisation.ProcessSettings(Settings, logger);

            ISerializeService serializeService = new SerializeService();
            IProcessService processService = new ProcessService(logger);
            IConfigurationService configurationService = new ConfigurationService(serializeService, processService, logger, Settings);

            if (consoleOptions.Install || consoleOptions.Uninstall || consoleOptions.StartService || consoleOptions.StopService || consoleOptions.ReserveUrls)
            {
                bool isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;

                if (isWindows)
                {
                    ServerConfig serverConfig = configurationService.BuildServerConfig(Settings);
                    Initialisation.ProcessWindowsSpecificArgs(consoleOptions, processService, serverConfig, logger);
                }
                else
                {
                    logger.Error($"ReserveUrls and service arguments only apply to Windows, please remove them from your start arguments");
                    Environment.Exit(1);
                }
            }

            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(runtimeDictionary);

            Configuration = builder.Build();

            do
            {
                if (!isWebHostRestart)
                {
                    if (consoleOptions.Port != 0 || consoleOptions.ListenPublic || consoleOptions.ListenPrivate)
                    {
                        ServerConfig serverConfiguration = configurationService.BuildServerConfig(Settings);
                        Initialisation.ProcessConsoleOverrides(consoleOptions, processService, serverConfiguration, configurationService, logger);
                    }
                }

                ServerConfig serverConfig = configurationService.BuildServerConfig(Settings);
                Int32.TryParse(serverConfig.Port.ToString(), out Int32 configPort);
                string[] url = serverConfig.GetListenAddresses(serverConfig.AllowExternal).Take(1).ToArray(); //Kestrel doesn't need 127.0.0.1 and localhost to be registered, remove once off OWIN

                isWebHostRestart = false;

                try
                {
                    CreateWebHostBuilder(args, url).Build().Run();
                }
                catch (Exception ex)
                {
                    if (ex.InnerException is Microsoft.AspNetCore.Connections.AddressInUseException)
                    {
                        logger.Error("Address already in use: Most likely Jackett is already running. " + ex.Message);
                        Environment.Exit(1);
                    }
                    logger.Error(ex);
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
                    var PIDFile = Settings.PIDFile;
                    if (File.Exists(PIDFile))
                    {
                        Console.WriteLine("Deleting PID file " + PIDFile);
                        File.Delete(PIDFile);
                    }
                    LogManager.Shutdown();
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString(), "Error while deleting the PID file");
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args, string[] urls) =>
            WebHost.CreateDefaultBuilder(args)
                .UseConfiguration(Configuration)
                .UseUrls(urls)
                .PreferHostingUrls(true)
                .UseStartup<Startup>();
    }
}
