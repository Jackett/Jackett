using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Jackett
{
    class Program
    {
        public static string AppConfigDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Jackett");

        public static Server ServerInstance { get; private set; }

        public static bool IsFirstRun { get; private set; }

        public static Logger LoggerInstance { get; private set; }

        public static ManualResetEvent ExitEvent { get; private set; }

        static void Main(string[] args)
        {
            ExitEvent = new ManualResetEvent(false);

            try
            {
                if (!Directory.Exists(AppConfigDirectory))
                {
                    IsFirstRun = true;
                    Directory.CreateDirectory(AppConfigDirectory);
                }
                Console.WriteLine("App config/log directory: " + AppConfigDirectory);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not create settings directory.");
                Application.Exit();
                return;
            }

            var logConfig = new LoggingConfiguration();

            var logFile = new FileTarget();
            logConfig.AddTarget("file", logFile);
            logFile.FileName = Path.Combine(AppConfigDirectory, "log.txt");
            logFile.Layout = "${longdate} ${level} ${message} \n ${exception:format=ToString}\n";
            var logFileRule = new LoggingRule("*", LogLevel.Debug, logFile);

            var logAlert = new MessageBoxTarget();
            logConfig.AddTarget("alert", logAlert);
            logAlert.Layout = "${message}";
            logAlert.Caption = "Alert";
            var logAlertRule = new LoggingRule("*", LogLevel.Fatal, logAlert);

            var logConsole = new ConsoleTarget();
            logConfig.AddTarget("console", logConsole);
            logConsole.Layout = "${longdate} ${level} ${message} ${exception:format=ToString}";
            var logConsoleRule = new LoggingRule("*", LogLevel.Debug, logConsole);

            logConfig.LoggingRules.Add(logFileRule);
            logConfig.LoggingRules.Add(logAlertRule);
            logConfig.LoggingRules.Add(logConsoleRule);
            LogManager.Configuration = logConfig;
            LoggerInstance = LogManager.GetCurrentClassLogger();

            var serverTask = Task.Run(async () =>
            {
                ServerInstance = new Server();
                await ServerInstance.Start();
            });

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Main());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Running in headless mode.");
            }

            Task.WaitAll(serverTask);
            Console.WriteLine("Server thread exit");
        }

        static public void RestartAsAdmin()
        {
            var startInfo = new ProcessStartInfo(Application.ExecutablePath.ToString()) { Verb = "runas" };
            Process.Start(startInfo);
            Environment.Exit(0);
        }
    }
}
