using Autofac;
using CommandLine;
using CommandLine.Text;
using Jackett.Common.Models.Config;
using Jackett.Common.Plumbing;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Server.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using NLog;
using System;
using System.Collections.Generic;
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

            var commandLineParser = new Parser();
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

            Helper.ConsoleOptions = consoleOptions;

            LogManager.Configuration = Initialisation.SetupLogging(Settings);
            Logger logger = LogManager.GetCurrentClassLogger();
            logger.Info("Starting Jackett v" + EnvironmentUtil.JackettVersion);

            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(runtimeDictionary);

            Configuration = builder.Build();

            do
            {
                //hack TODO: Get the configuration without any DI
                var containerBuilder = new ContainerBuilder();
                Helper.SetupLogging(Settings, containerBuilder);
                containerBuilder.RegisterModule(new JackettModule(Settings));
                containerBuilder.RegisterType<ServerService>().As<IServerService>();
                containerBuilder.RegisterType<SecuityService>().As<ISecuityService>();
                containerBuilder.RegisterType<ProtectionService>().As<IProtectionService>();
                var tempContainer = containerBuilder.Build();

                ServerConfig serverConfig = tempContainer.Resolve<ServerConfig>();
                IConfigurationService configurationService = tempContainer.Resolve<IConfigurationService>();
                IServerService serverService = tempContainer.Resolve<IServerService>();
                Int32.TryParse(serverConfig.Port.ToString(), out Int32 configPort);

                if (!isWebHostRestart)
                {
                    // Override port
                    if (consoleOptions.Port != 0)
                    {
                        if (configPort != consoleOptions.Port)
                        {
                            logger.Info("Overriding port to " + consoleOptions.Port);
                            serverConfig.Port = consoleOptions.Port;
                            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
                            if (isWindows)
                            {
                                if (ServerUtil.IsUserAdministrator())
                                {
                                    serverService.ReserveUrls(doInstall: true);
                                }
                                else
                                {
                                    logger.Error("Unable to switch ports when not running as administrator");
                                    Environment.Exit(1);
                                }
                            }
                            configurationService.SaveConfig(serverConfig);
                        }
                    }
                }

                string[] url = serverConfig.GetListenAddresses(serverConfig.AllowExternal).Take(1).ToArray(); //Kestrel doesn't need 127.0.0.1 and localhost to be registered, remove once off OWIN

                isWebHostRestart = false;
                tempContainer.Dispose();
                tempContainer = null;

                try
                {
                    CreateWebHostBuilder(args, url).Build().Run();
                }
                catch (Exception ex)
                {
                    if (ex.InnerException is Microsoft.AspNetCore.Connections.AddressInUseException)
                    {
                        Console.WriteLine("Address already in use: Most likely Jackett is already running. " + ex.Message);
                        Environment.Exit(1);
                    }
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
