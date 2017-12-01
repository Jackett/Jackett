using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JackettTray
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var JacketTrayProcessName = Process.GetCurrentProcess().ProcessName;
            var runningProcesses = Process.GetProcesses();
            var currentSessionID = Process.GetCurrentProcess().SessionId;
            var sameAsThisSession = runningProcesses.Where(p => p.SessionId == currentSessionID);
            var sameAsThisSessionJacketTray = sameAsThisSession.Where(p => p.ProcessName == JacketTrayProcessName);
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
