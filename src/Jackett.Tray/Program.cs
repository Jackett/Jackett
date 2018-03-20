using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace Jackett.Tray
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var JacketTrayProcess = Process.GetCurrentProcess();
            var runningProcesses = Process.GetProcesses();
            var currentSessionID = Process.GetCurrentProcess().SessionId;
            var sameAsThisSession = runningProcesses.Where(p => p.SessionId == currentSessionID);
            var sameAsThisSessionJacketTray = sameAsThisSession.Where(p => p.ProcessName == JacketTrayProcess.ProcessName && p.Id != JacketTrayProcess.Id);
            if (sameAsThisSessionJacketTray.Any())
            {
                MessageBox.Show("JackettTray is already running");
            }
            else
            { 
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Main());
            }
        }
    }
}
