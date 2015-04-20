using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
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
            Application.Exit();
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
                RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (rkApp.GetValue(ProgramTitle) == null)
                    return false;
                else
                    return true;
            }
            set
            {
                RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                if (value && !AutoStart)
                    rkApp.SetValue(ProgramTitle, Application.ExecutablePath.ToString());
                else if (!value && AutoStart)
                    rkApp.DeleteValue(ProgramTitle, false);
            }
        }
    }
}
