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
                    var text = HelpText.AutoBuild(options, (HelpText current) => HelpText.DefaultParsingErrorsHandler(options, current));
                    text.Copyright = " ";
                    text.Heading = "Jackett v" + Engine.ConfigService.GetVersion() + " options:";
                    Console.WriteLine(text);
                    Environment.ExitCode = 1;
                    return;
                }
                else
                {
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
                        Engine.ConfigService.CreateOrMigrateSettings();
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
                        if (Engine.ServiceConfig.ServiceRunning())
                        {
                            Engine.ConfigService.PerformMigration();
                        }
                        return;
                    }


                    // Show Version
                    if (options.ShowVersion)
                    {
                        Console.WriteLine("Jackett v" + Engine.ConfigService.GetVersion());
                        return;
                    }

                    /*  ======     Options    =====  */

                    // Logging
                    if (options.Logging)
                    {
                        Startup.LogRequests = true;
                    }

                    // Tracing
                    if (options.Tracing)
                    {
                        Startup.TracingEnabled = true;
                    }

                    // Use curl
                    if (options.UseCurlExec)
                    {
                        Startup.CurlSafe = true;
                    }

                    // Override listen public
                    if(options.ListenPublic.HasValue)
                    {
                        if(Engine.Server.Config.AllowExternal != options.ListenPublic)
                        {
                            Engine.Server.Config.AllowExternal = options.ListenPublic.Value;
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
                    if(options.Port != 0)
                    {
                        if (Engine.Server.Config.Port != options.Port)
                        {
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
                }

                Engine.Server.Initalize();
                Engine.Server.Start();
                Engine.Logger.Info("Running in console mode!");
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

