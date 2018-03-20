using System;
using CommandLine;
using CommandLine.Text;
using Jackett.Common;
using Jackett.Common.Models.Config;
using Jackett.Common.Utils;
using Jackett.Utils;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Jackett.Console
{
    public class Program
    {

        static void Main(string[] args)
        {

            var optionsResult = Parser.Default.ParseArguments<ConsoleOptions>(args);
            optionsResult.WithNotParsed(errors =>
            {
                var text = HelpText.AutoBuild(optionsResult);
                text.Copyright = " ";
                text.Heading = "Jackett v" + EnvironmentUtil.JackettVersion + " options:";
                System.Console.WriteLine(text);
                Environment.ExitCode = 1;
                return;
            });

            optionsResult.WithParsed(options =>
            {
                try
                {
                    var runtimeSettings = options.ToRunTimeSettings();

                    // Initialize autofac, logger, etc. We cannot use any calls to Engine before the container is set up.
                    Engine.BuildContainer(runtimeSettings, new WebApi2Module());

                    if (runtimeSettings.LogRequests)
                        Engine.Logger.Info("Logging enabled.");

                    if (runtimeSettings.TracingEnabled)
                        Engine.Logger.Info("Tracing enabled.");

                    if (runtimeSettings.IgnoreSslErrors == true)
                    {
                        Engine.Logger.Info("Jackett will ignore SSL certificate errors.");
                    }

                    if (runtimeSettings.DoSSLFix == true)
                        Engine.Logger.Info("SSL ECC workaround enabled.");
                    else if (runtimeSettings.DoSSLFix == false)
                        Engine.Logger.Info("SSL ECC workaround has been disabled.");
                    // Choose Data Folder
                    if (!string.IsNullOrWhiteSpace(runtimeSettings.CustomDataFolder))
                    {
                        Engine.Logger.Info("Jackett Data will be stored in: " + runtimeSettings.CustomDataFolder);
                    }


                    // Use Proxy
                    if (options.ProxyConnection != null)
                    {
                        Engine.Logger.Info("Proxy enabled. " + runtimeSettings.ProxyConnection);
                    }

                    /*  ======     Actions    =====  */

                    // Install service
                    if (options.Install)
                    {
                        Engine.ServiceConfig.Install();
                        return;
                    }

                    // Uninstall service
                    if (options.Uninstall)
                    {
                        Engine.Server.ReserveUrls(doInstall: false);
                        Engine.ServiceConfig.Uninstall();
                        return;
                    }

                    // Reserve urls
                    if (options.ReserveUrls)
                    {
                        Engine.Server.ReserveUrls(doInstall: true);
                        return;
                    }

                    // Start Service
                    if (options.StartService)
                    {
                        if (!Engine.ServiceConfig.ServiceRunning())
                        {
                            Engine.ServiceConfig.Start();
                        }
                        return;
                    }

                    // Stop Service
                    if (options.StopService)
                    {
                        if (Engine.ServiceConfig.ServiceRunning())
                        {
                            Engine.ServiceConfig.Stop();
                        }
                        return;
                    }

                    // Migrate settings
                    if (options.MigrateSettings)
                    {
                        Engine.ConfigService.PerformMigration();
                        return;
                    }


                    // Show Version
                    if (options.ShowVersion)
                    {
                        System.Console.WriteLine("Jackett v" + EnvironmentUtil.JackettVersion);
                        return;
                    }

                    /*  ======     Overrides    =====  */

                    // Override listen public
                    if (options.ListenPublic || options.ListenPrivate)
                    {
                        if (Engine.ServerConfig.AllowExternal != options.ListenPublic)
                        {
                            Engine.Logger.Info("Overriding external access to " + options.ListenPublic);
                            Engine.ServerConfig.AllowExternal = options.ListenPublic;
                            if (System.Environment.OSVersion.Platform != PlatformID.Unix)
                            {
                                if (ServerUtil.IsUserAdministrator())
                                {
                                    Engine.Server.ReserveUrls(doInstall: true);
                                }
                                else
                                {
                                    Engine.Logger.Error("Unable to switch to public listening without admin rights.");
                                    Engine.Exit(1);
                                }
                            }

                            Engine.SaveServerConfig();
                        }
                    }

                    // Override port
                    if (options.Port != 0)
                    {
                        if (Engine.ServerConfig.Port != options.Port)
                        {
                            Engine.Logger.Info("Overriding port to " + options.Port);
                            Engine.ServerConfig.Port = options.Port;
                            if (System.Environment.OSVersion.Platform != PlatformID.Unix)
                            {
                                if (ServerUtil.IsUserAdministrator())
                                {
                                    Engine.Server.ReserveUrls(doInstall: true);
                                }
                                else
                                {
                                    Engine.Logger.Error("Unable to switch ports when not running as administrator");
                                    Engine.Exit(1);
                                }
                            }

                            Engine.SaveServerConfig();
                        }
                    }

                    Engine.Server.Initalize();
                    Engine.Server.Start();
                    Engine.RunTime.Spin();
                    Engine.Logger.Info("Server thread exit");
                }
                catch (Exception e)
                {
                    Engine.Logger.Error(e, "Top level exception");
                }
            });
    }
        

       

    }
}

