using CommandLine;
using CommandLine.Text;
using Jackett;
using Jackett.Console;
using Jackett.Indexers;
using Jackett.Utils;
using System;
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
using Jacket.Common;

namespace JackettConsole
{
    public class Program
    {
      
        static void Main(string[] args)
        {
            try
            {
                var options = new ConsoleOptions();
                if (!Parser.Default.ParseArguments(args, options) || options.ShowHelp == true)
                {
                    if (options.LastParserState != null && options.LastParserState.Errors.Count > 0)
                    {
                        var help = new HelpText();
                        var errors = help.RenderParsingErrorsText(options, 2); // indent with two spaces
                        Console.WriteLine("Jackett v" + JackettStartup.JackettVersion);
                        Console.WriteLine("Switch error: " + errors);
                        Console.WriteLine("See --help for further details on switches.");
                        Environment.ExitCode = 1;
                        return;
                    }
                    else
                    {

                        var text = HelpText.AutoBuild(options, (HelpText current) => HelpText.DefaultParsingErrorsHandler(options, current));
                        text.Copyright = " ";
                        text.Heading = "Jackett v" + Engine.ConfigService.GetVersion() + " options:";
                        Console.WriteLine(text);
                        Environment.ExitCode = 1;
                        return;
                    }
                }
                else
                {
                    SetJacketOptions(options);
                    // Initialize autofac, logger, etc. We cannot use any calls to Engine before the container is set up.
                    Engine.BuildContainer(new WebApi2Module());
                    
                    if (options.Logging)
                        Engine.Logger.Info("Logging enabled.");

                    if (options.Tracing)
                        Engine.Logger.Info("Tracing enabled.");

                    if (options.IgnoreSslErrors == true)
                    {
                        Engine.Logger.Info("Jackett will ignore SSL certificate errors.");
                    }

                    if (options.SSLFix == true)
                        Engine.Logger.Info("SSL ECC workaround enabled.");
                    else if (options.SSLFix == false)
                        Engine.Logger.Info("SSL ECC workaround has been disabled.");
                    // Choose Data Folder
                    if (!string.IsNullOrWhiteSpace(options.DataFolder))
                    {
                        Engine.Logger.Info("Jackett Data will be stored in: " + JackettStartup.CustomDataFolder);
                    }


                    // Use Proxy
                    if (options.ProxyConnection != null)
                    {
                        Engine.Logger.Info("Proxy enabled. " + JackettStartup.ProxyConnection);
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
                        Console.WriteLine("Jackett v" + Engine.ConfigService.GetVersion());
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
                                    Environment.ExitCode = 1;
                                    return;
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
                                    Environment.ExitCode = 1;
                                    return;
                                }
                            }

                            Engine.SaveServerConfig();
                        }
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
        }

        static void SetJacketOptions(ConsoleOptions options)
        {
            // Logging
            if (options.Logging)
                JackettStartup.LogRequests = true;

            // Tracing
            if (options.Tracing)
                JackettStartup.TracingEnabled = true;

            if (options.ListenPublic && options.ListenPrivate)
            {
                Console.WriteLine("You can only use listen private OR listen publicly.");
                Environment.ExitCode = 1;
                return;
            }

            // SSL Fix
            JackettStartup.DoSSLFix = options.SSLFix;

            // Use curl
            if (options.Client != null)
                JackettStartup.ClientOverride = options.Client.ToLowerInvariant();

            // Use Proxy
            if (options.ProxyConnection != null)
            {
                JackettStartup.ProxyConnection = options.ProxyConnection.ToLowerInvariant();
            }



            // Ignore SSL errors on Curl
            JackettStartup.IgnoreSslErrors = options.IgnoreSslErrors;



            JackettStartup.NoRestart = options.NoRestart;

        }

    }
}

