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
                if (args.Length > 0)
                {
                    switch (args[0].ToLowerInvariant())
                    {
                        case "/i": // install
                            Engine.ServiceConfig.Install();
                            return;
                        case "/r": // reserve port/url & install
                            Engine.Server.ReserveUrls(doInstall: true);
                            return;
                        case "/c": // change port
                            Engine.Server.ReserveUrls(doInstall: false);
                            return;
                        case "/u": // uninstall
                            Engine.Server.ReserveUrls(doInstall: false);
                            Engine.ServiceConfig.Uninstall();
                            return;
                    }
                }

                Engine.Server.Start();
                Engine.Logger.Info("Running in headless mode.");
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

