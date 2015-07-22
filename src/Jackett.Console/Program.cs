using Jackett;
using Jackett.Indexers;
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
                foreach (var arg in args)
                {
                    switch (arg.ToLowerInvariant())
                    {
                        case "/i": // Install
                            Engine.ServiceConfig.Install();
                            return;
                        case "/r": // Reserve port/url & install
                            Engine.Server.ReserveUrls(doInstall: true);
                            return;
                        case "/c": // Change port
                            Engine.Server.ReserveUrls(doInstall: false);
                            return;
                        case "/u": // Uninstall
                            Engine.Server.ReserveUrls(doInstall: false);
                            Engine.ServiceConfig.Uninstall();
                            return;
                        case "/l":  // Logging
                            Engine.LogRequests = true;
                            break;
                        case "/t":  // Tracing
                            Engine.TracingEnabled = true;
                            break;
                        case "/curlsafe":  // Curl safe mode
                            Engine.CurlSafe = true;
                            break;
                        case "/start":  // Start Service
                            if (!Engine.ServiceConfig.ServiceRunning())
                            {
                                Engine.ServiceConfig.Start();
                            }
                            return;
                        case "/stop":  // Stop Service
                            if (Engine.ServiceConfig.ServiceRunning())
                            {
                                Engine.ServiceConfig.Stop();
                            }
                            return;
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

