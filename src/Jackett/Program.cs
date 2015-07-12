using Jackett.Indexers;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Windows.Forms;
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
        public static string AppConfigDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Jackett");

        public static Server ServerInstance { get; private set; }

        public static bool IsFirstRun { get; private set; }

        public static Logger LoggerInstance { get; private set; }

        public static ManualResetEvent ExitEvent { get; private set; }

        public static bool IsWindows { get { return Environment.OSVersion.Platform == PlatformID.Win32NT; } }



        static void Main(string[] args)
        {
            ExitEvent = new ManualResetEvent(false);

            MigrateSettingsDirectory();

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
                MessageBox.Show("Could not create settings directory. " + ex.Message);
                Application.Exit();
                return;
            }

            var logConfig = new LoggingConfiguration();

            var logFile = new FileTarget();
            logConfig.AddTarget("file", logFile);
            logFile.Layout = "${longdate} ${level} ${message} \n ${exception:format=ToString}\n";
            logFile.FileName = Path.Combine(AppConfigDirectory, "log.txt");
            logFile.ArchiveFileName = "log.{#####}.txt";
            logFile.ArchiveAboveSize = 500000;
            logFile.MaxArchiveFiles = 1;
            logFile.KeepFileOpen = false;
            logFile.ArchiveNumbering = ArchiveNumberingMode.DateAndSequence;
            var logFileRule = new LoggingRule("*", LogLevel.Debug, logFile);
            logConfig.LoggingRules.Add(logFileRule);

            if (Program.IsWindows)
            {
#if !__MonoCS__
                var logAlert = new MessageBoxTarget();
                logConfig.AddTarget("alert", logAlert);
                logAlert.Layout = "${message}";
                logAlert.Caption = "Alert";
                var logAlertRule = new LoggingRule("*", LogLevel.Fatal, logAlert);
                logConfig.LoggingRules.Add(logAlertRule);
#endif
            }

            var logConsole = new ConsoleTarget();
            logConfig.AddTarget("console", logConsole);
            logConsole.Layout = "${longdate} ${level} ${message} ${exception:format=ToString}";
            var logConsoleRule = new LoggingRule("*", LogLevel.Debug, logConsole);
            logConfig.LoggingRules.Add(logConsoleRule);

            LogManager.Configuration = logConfig;
            LoggerInstance = LogManager.GetCurrentClassLogger();

            ReadSettingsFile();

            var serverTask = Task.Run(async () =>
            {
                ServerInstance = new Server();
                await ServerInstance.Start();
            });

            try
            {
                if (Program.IsWindows)
                {
#if !__MonoCS__
                    Application.Run(new Main());
#endif
                }
            }
            catch (Exception)
            {

            }

            Console.WriteLine("Running in headless mode.");



            Task.WaitAll(serverTask);
            Console.WriteLine("Server thread exit");
        }

        static void MigrateSettingsDirectory()
        {
            try
            {
                string oldDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Jackett");
                if (Directory.Exists(oldDir) && !Directory.Exists(AppConfigDirectory))
                {
                    Directory.Move(oldDir, AppConfigDirectory);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR could not migrate settings directory " + ex);
            }
        }

        static void ReadSettingsFile()
        {
            var path = Path.Combine(AppConfigDirectory, "config.json");
            if (!File.Exists(path))
            {
                JObject f = new JObject();
                f.Add("port", Server.DefaultPort);
                f.Add("public", true);
                File.WriteAllText(path, f.ToString());
            }

            var configJson = JObject.Parse(File.ReadAllText(path));
            int port = (int)configJson.GetValue("port");
            Server.Port = port;

            Server.ListenPublic = (bool)configJson.GetValue("public");

            Console.WriteLine("Config file path: " + path);
        }

        static public void RestartAsAdmin()
        {
            var startInfo = new ProcessStartInfo(Application.ExecutablePath.ToString()) { Verb = "runas" };
            Process.Start(startInfo);
            Environment.Exit(0);
        }
    }
}
