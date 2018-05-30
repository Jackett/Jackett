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
    public class Program
    {
        public static IConfiguration Configuration { get; set; }

        public static void Main(string[] args)
        {
            var optionsResult = Parser.Default.ParseArguments<ConsoleOptions>(args);
            optionsResult.WithNotParsed(errors =>
            {
                var text = HelpText.AutoBuild(optionsResult);
                text.Copyright = " ";
                text.Heading = "Jackett v" + EnvironmentUtil.JackettVersion + " options:";
                Environment.Exit(1);
                return;
            });

            var runtimeDictionary = new Dictionary<string, string>();
            RuntimeSettings runtimeSettings = new RuntimeSettings();
            ConsoleOptions consoleOptions = new ConsoleOptions();
            optionsResult.WithParsed(options =>
            {
                runtimeSettings = options.ToRunTimeSettings();
                consoleOptions = options;
                runtimeDictionary = GetValues(runtimeSettings);
            });

            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(runtimeDictionary);

            Configuration = builder.Build();

            //hack TODO: Get the configuration without any DI
            var containerBuilder = new ContainerBuilder();
            Initialisation.SetupLogging(runtimeSettings, containerBuilder);
            containerBuilder.RegisterModule(new JackettModule(runtimeSettings));
            containerBuilder.RegisterType<ServerService>().As<IServerService>();
            containerBuilder.RegisterType<SecuityService>().As<ISecuityService>();
            containerBuilder.RegisterType<ProtectionService>().As<IProtectionService>();
            var tempContainer = containerBuilder.Build();

            Logger logger = tempContainer.Resolve<Logger>();
            ServerConfig serverConfig = tempContainer.Resolve<ServerConfig>();
            IConfigurationService configurationService = tempContainer.Resolve<IConfigurationService>();
            IServerService serverService = tempContainer.Resolve<IServerService>();
            Int32.TryParse(serverConfig.Port.ToString(), out Int32 configPort);

            DirectoryInfo dataProtectionFolder = new DirectoryInfo(Path.Combine(runtimeSettings.DataFolder, "DataProtection"));
            if (!dataProtectionFolder.Exists)
            {
                dataProtectionFolder.Create();
            }

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

            string[] url = serverConfig.GetListenAddresses(serverConfig.AllowExternal).Take(1).ToArray(); //Kestrel doesn't need 127.0.0.1 and localhost to be registered, remove once off OWIN

            tempContainer.Dispose();
            tempContainer = null;

            CreateWebHostBuilder(args, url).Build().Run();
        }

        public static Dictionary<string, string> GetValues(object obj)
        {
            return obj
                    .GetType()
                    .GetProperties()
                    .ToDictionary(p => "RuntimeSettings:" + p.Name, p => p.GetValue(obj) == null ? null : p.GetValue(obj).ToString());
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args, string[] urls) =>
            WebHost.CreateDefaultBuilder(args)
                .UseConfiguration(Configuration)
            .UseUrls(urls)
            .PreferHostingUrls(true)
                .UseStartup<Startup>();
    }
}
