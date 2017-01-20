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
                        Console.WriteLine("Jackett v" + Engine.ConfigService.GetVersion());
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

                    if (options.ListenPublic && options.ListenPrivate)
                    {
                        Console.WriteLine("You can only use listen private OR listen publicly.");
                        Environment.ExitCode = 1;
                        return;
                    }
                    /*  ======     Options    =====  */

                    // SSL Fix
                    Startup.DoSSLFix = options.SSLFix;

                    // Use curl
                    if (options.Client != null)
                        Startup.ClientOverride = options.Client.ToLowerInvariant();

                    // Use Proxy
                    if (options.ProxyConnection != null)
                    {
                        Startup.ProxyConnection = options.ProxyConnection.ToLowerInvariant();
                        Engine.Logger.Info("Proxy enabled. " + Startup.ProxyConnection);
                    }
                    // Logging
                    if (options.Logging)
                        Startup.LogRequests = true;

                    // Tracing
                    if (options.Tracing)
                        Startup.TracingEnabled = true;

                    // Log after the fact as using the logger will cause the options above to be used

                    if (options.Logging)
                        Engine.Logger.Info("Logging enabled.");

                    if (options.Tracing)
                        Engine.Logger.Info("Tracing enabled.");

                    if (options.SSLFix == true)
                        Engine.Logger.Info("SSL ECC workaround enabled.");
                    else if (options.SSLFix == false)
                        Engine.Logger.Info("SSL ECC workaround has been disabled.");

                    // Ignore SSL errors on Curl
                    Startup.IgnoreSslErrors = options.IgnoreSslErrors;
                    if (options.IgnoreSslErrors == true)
                    {
                        Engine.Logger.Info("Jackett will ignore SSL certificate errors.");
                    }

                    // Choose Data Folder
                    if (!string.IsNullOrWhiteSpace(options.DataFolder))
                    {
                        Startup.CustomDataFolder = options.DataFolder.Replace("\"", string.Empty).Replace("'", string.Empty).Replace(@"\\", @"\");
                        Engine.Logger.Info("Jackett Data will be stored in: " + Startup.CustomDataFolder);
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
                        if (Engine.Server.Config.AllowExternal != options.ListenPublic)
                        {
                            Engine.Logger.Info("Overriding external access to " + options.ListenPublic);
                            Engine.Server.Config.AllowExternal = options.ListenPublic;
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

                            Engine.Server.SaveConfig();
                        }
                    }

                    // Override port
                    if (options.Port != 0)
                    {
                        if (Engine.Server.Config.Port != options.Port)
                        {
                            Engine.Logger.Info("Overriding port to " + options.Port);
                            Engine.Server.Config.Port = options.Port;
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

                            Engine.Server.SaveConfig();
                        }
                    }

                    Startup.NoRestart = options.NoRestart;
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
    }
}

