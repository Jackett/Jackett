using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using Jackett.Common;
using Jackett.Common.Models.Config;
using Jackett.Common.Utils;
using Microsoft.Win32;
using Jackett;
using Jackett.Utils;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;


namespace Jackett.Tray
{
    public partial class Main : Form
    {
        public Main()
        {
            Hide();
            InitializeComponent();

            toolStripMenuItemAutoStart.Checked = AutoStart;
            toolStripMenuItemAutoStart.CheckedChanged += toolStripMenuItemAutoStart_CheckedChanged;

            toolStripMenuItemWebUI.Click += toolStripMenuItemWebUI_Click;
            toolStripMenuItemShutdown.Click += toolStripMenuItemShutdown_Click;

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            { 
                toolStripMenuItemAutoStart.Visible = true;
            }

            Engine.BuildContainer(new RuntimeSettings(),new WebApi2Module());
            Engine.Server.Initalize();

            if (!Engine.ServiceConfig.ServiceExists())
            {
                // We are not installed as a service so just the web server too and run from the tray.
                Engine.Logger.Info("Starting server from tray");
                Engine.Server.Start();
            }

            Task.Factory.StartNew(WaitForEvent);
        }

        private void WaitForEvent()
        {
            Engine.LockService.WaitForSignal();
            Application.Exit();
        }

        void toolStripMenuItemWebUI_Click(object sender, EventArgs e)
        {
            Process.Start("http://127.0.0.1:" + Engine.ServerConfig.Port);
        }

        void toolStripMenuItemShutdown_Click(object sender, EventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }

        void toolStripMenuItemAutoStart_CheckedChanged(object sender, EventArgs e)
        {
            AutoStart = toolStripMenuItemAutoStart.Checked;
        }

        string ProgramTitle
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Name;
            }
        }

        bool AutoStart
        {
            get
            {
                return File.Exists(ShortcutPath);
            }
            set
            {
                if (value && !AutoStart)
                {
                    CreateShortcut();
                }
                else if (!value && AutoStart)
                {
                    File.Delete(ShortcutPath);
                }
            }
        }

        public string ShortcutPath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Jackett.lnk");
            }
        }

        private void CreateShortcut()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var appPath = Assembly.GetExecutingAssembly().Location;
                var shell = new IWshRuntimeLibrary.WshShell();
                var shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(ShortcutPath);
                shortcut.Description = Assembly.GetExecutingAssembly().GetName().Name;
                shortcut.TargetPath = appPath;
                shortcut.Save();
            }
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            if (Engine.ServiceConfig.ServiceExists())
            {
                backgroundMenuItem.Visible = true;
                serviceControlMenuItem.Visible = true;
                toolStripSeparator1.Visible = true;
                toolStripSeparator2.Visible = true;
                if (Engine.ServiceConfig.ServiceRunning())
                {
                    serviceControlMenuItem.Text = "Stop background service";
                } else
                {
                    serviceControlMenuItem.Text = "Start background service";
                }

                toolStripMenuItemShutdown.Text = "Close tray icon";
            } else
            {
                backgroundMenuItem.Visible = false;
                serviceControlMenuItem.Visible = false;
                toolStripSeparator1.Visible = false;
                toolStripSeparator2.Visible = false;
                toolStripMenuItemShutdown.Text = "Shutdown";
            }
        }

        private void serviceControlMenuItem_Click(object sender, EventArgs e)
        {
            var consolePath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "JackettConsole.exe");

            if (Engine.ServiceConfig.ServiceRunning())
            {
                if (ServerUtil.IsUserAdministrator())
                {
                    Engine.ServiceConfig.Stop();
                    
                } else
                {
                    try
                    {
                        Engine.ProcessService.StartProcessAndLog(consolePath, "--Stop", true);
                    }
                    catch
                    {
                        MessageBox.Show("Failed to get admin rights to stop the service.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                if (ServerUtil.IsUserAdministrator())
                {
                    Engine.ServiceConfig.Start();
                }
                else
                {
                    try
                    {
                        Engine.ProcessService.StartProcessAndLog(consolePath, "--Start", true);
                    }
                    catch
                    {
                        MessageBox.Show("Failed to get admin rights to start the service.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}