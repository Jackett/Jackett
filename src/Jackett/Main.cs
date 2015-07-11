#if !__MonoCS__
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Jackett
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

            if (Program.IsFirstRun)
                AutoStart = true;
        }

        void toolStripMenuItemWebUI_Click(object sender, EventArgs e)
        {
            Process.Start("http://127.0.0.1:" + Server.Port);
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
            var appPath = Assembly.GetExecutingAssembly().Location;
            var shell = new IWshRuntimeLibrary.WshShell();
            var shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(ShortcutPath);
            shortcut.Description = Assembly.GetExecutingAssembly().GetName().Name;
            shortcut.TargetPath = appPath;
            shortcut.Save();
        }
    }
}
#endif