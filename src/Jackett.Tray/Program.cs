using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using CommandLine;

namespace Jackett.Tray
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            var jacketTrayProcess = Process.GetCurrentProcess();
            var runningProcesses = Process.GetProcesses();
            var currentSessionId = Process.GetCurrentProcess().SessionId;
            var sameAsThisSession = runningProcesses.Where(p => p.SessionId == currentSessionId);
            var sameAsThisSessionJacketTray = sameAsThisSession.Where(
                p => p.ProcessName == jacketTrayProcess.ProcessName && p.Id != jacketTrayProcess.Id);
            if (sameAsThisSessionJacketTray.Any())
                MessageBox.Show("JackettTray is already running");
            else
            {
                var newVersion = "";
                var commandLineParser = new Parser(settings => settings.CaseSensitive = false);
                try
                {
                    var optionsResult = commandLineParser.ParseArguments<TrayConsoleOptions>(args);
                    optionsResult.WithParsed(
                        options =>
                        {
                            if (!string.IsNullOrWhiteSpace(options.UpdatedVersion))
                                newVersion = options.UpdatedVersion;
                        });
                }
                catch (Exception)
                {
                    newVersion = "";
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Main(newVersion));
            }
        }
    }
}
