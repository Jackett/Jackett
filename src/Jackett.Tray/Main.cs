using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Jackett.Common.Models.Config;
using Jackett.Common.Services;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using NLog;

namespace Jackett.Tray
{
    public partial class Main : Form
    {
        private readonly IProcessService _processService;
        private readonly IServiceConfigService _windowsService;
        private readonly ITrayLockService _trayLockService;
        private readonly ISerializeService _serializeService;
        private readonly IConfigurationService _configurationService;
        private readonly ServerConfig _serverConfig;
        private Process _consoleProcess;
        private readonly Logger _logger;
        private bool _closeApplicationInitiated;

        public Main(string updatedVersion)
        {
            Hide();
            InitializeComponent();
            Opacity = 0;
            Enabled = false;
            WindowState = FormWindowState.Minimized;
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            var runtimeSettings = new RuntimeSettings { CustomLogFileName = "TrayLog.txt" };
            LogManager.Configuration = LoggingSetup.GetLoggingConfiguration(runtimeSettings);
            _logger = LogManager.GetCurrentClassLogger();
            _logger.Info($"Starting Jackett Tray v{EnvironmentUtil.JackettVersion}");
            _processService = new ProcessService(_logger);
            _windowsService = new WindowsServiceConfigService(_processService, _logger);
            _trayLockService = new TrayLockService();
            _serializeService = new SerializeService();
            _configurationService = new ConfigurationService(_serializeService, _processService, _logger, runtimeSettings);
            _serverConfig = _configurationService.BuildServerConfig(runtimeSettings);
            toolStripMenuItemAutoStart.Checked = AutoStart;
            toolStripMenuItemAutoStart.CheckedChanged += toolStripMenuItemAutoStart_CheckedChanged;
            toolStripMenuItemWebUI.Click += toolStripMenuItemWebUI_Click;
            toolStripMenuItemShutdown.Click += toolStripMenuItemShutdown_Click;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                toolStripMenuItemAutoStart.Visible = true;
            if (!_windowsService.ServiceExists())
            {
                // We are not installed as a service so just start the web server via JackettConsole and run from the tray.
                _logger.Info("Starting server from tray");
                StartConsoleApplication();
            }

            updatedVersion = updatedVersion.Equals("yes", StringComparison.OrdinalIgnoreCase)
                ? EnvironmentUtil.JackettVersion
                : updatedVersion;
            if (!string.IsNullOrWhiteSpace(updatedVersion))
            {
                notifyIcon1.BalloonTipTitle = "Jackett";
                notifyIcon1.BalloonTipText = $"Jackett has updated to version {updatedVersion}";
                notifyIcon1.BalloonTipIcon = ToolTipIcon.Info;
                notifyIcon1.ShowBalloonTip(10000);
                _logger.Info($"Display balloon tip, updated to {updatedVersion}");
            }

            Task.Factory.StartNew(WaitForEvent);
        }

        private void WaitForEvent()
        {
            _trayLockService.WaitForSignal();
            _logger.Info("Received signal from tray lock service");
            if (_windowsService.ServiceExists() && _windowsService.ServiceRunning())
            {
                //We won't be able to start the tray app up again from the updater, as when running via a windows service there is no interaction with the desktop
                //Fire off a console process that will start the tray 20 seconds later
                var trayExePath = Assembly.GetEntryAssembly().Location;
                var startInfo = new ProcessStartInfo
                {
                    Arguments = $"/c timeout 20 > NUL & \"{trayExePath}\" --UpdatedVersion yes",
                    FileName = "cmd.exe",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                _logger.Info(
                    $"Starting 20 second delay tray launch as Jackett is running as a Windows service: {startInfo.FileName} {startInfo.Arguments}");
                Process.Start(startInfo);
            }

            CloseTrayApplication();
        }

        private void toolStripMenuItemWebUI_Click(object sender, EventArgs e)
        {
            var psi = new ProcessStartInfo { FileName = $"http://127.0.0.1:{_serverConfig.Port}", UseShellExecute = true };
            Process.Start(psi);
        }

        private void toolStripMenuItemShutdown_Click(object sender, EventArgs e) => CloseTrayApplication();

        private void toolStripMenuItemAutoStart_CheckedChanged(object sender, EventArgs e) =>
            AutoStart = toolStripMenuItemAutoStart.Checked;

        private string ProgramTitle => Assembly.GetExecutingAssembly().GetName().Name;

        private bool AutoStart
        {
            get => File.Exists(ShortcutPath) || File.Exists(
                       Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Jackett.lnk"));
            set
            {
                if (value && !AutoStart)
                    CreateShortcut();
                else if (!value && AutoStart)
                    File.Delete(ShortcutPath);
            }
        }

        public string ShortcutPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Jackett.url");

        private void CreateShortcut()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                using (var writer = new StreamWriter(ShortcutPath))
                {
                    var appPath = Assembly.GetExecutingAssembly().Location;
                    writer.WriteLine("[InternetShortcut]");
                    writer.WriteLine($"URL=file:///{appPath}");
                    writer.WriteLine("IconIndex=0");
                    var icon = appPath.Replace('\\', '/');
                    writer.WriteLine($"IconFile={icon}");
                }
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            if (_windowsService.ServiceExists())
            {
                backgroundMenuItem.Visible = true;
                serviceControlMenuItem.Visible = true;
                toolStripSeparator1.Visible = true;
                toolStripSeparator2.Visible = true;
                if (_windowsService.ServiceRunning())
                {
                    serviceControlMenuItem.Text = "Stop background service";
                    backgroundMenuItem.Text = "Jackett is running as a background service";
                    toolStripMenuItemWebUI.Enabled = true;
                }
                else
                {
                    serviceControlMenuItem.Text = "Start background service";
                    backgroundMenuItem.Text = "Jackett will run as a background service";
                    toolStripMenuItemWebUI.Enabled = false;
                }

                toolStripMenuItemShutdown.Text = "Close tray icon";
            }
            else
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
            if (_windowsService.ServiceRunning())
            {
                if (ServerUtil.IsUserAdministrator())
                    _windowsService.Stop();
                else
                    try
                    {
                        _processService.StartProcessAndLog(consolePath, "--Stop", true);
                    }
                    catch
                    {
                        MessageBox.Show(
                            "Failed to get admin rights to stop the service.", "Error", MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
            }
            else
            {
                if (ServerUtil.IsUserAdministrator())
                    _windowsService.Start();
                else
                    try
                    {
                        _processService.StartProcessAndLog(consolePath, "--Start", true);
                    }
                    catch
                    {
                        MessageBox.Show(
                            "Failed to get admin rights to start the service.", "Error", MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
            }
        }

        private void CloseTrayApplication()
        {
            _closeApplicationInitiated = true;
            _logger.Info("Close of tray application initiated");

            //Clears notify icon, otherwise icon will still appear on taskbar until you hover the mouse over
            notifyIcon1.Icon = null;
            notifyIcon1.Dispose();
            Application.DoEvents();
            if (_consoleProcess?.HasExited == false)
            {
                _consoleProcess.StandardInput.Close();
                Thread.Sleep(1000);
                if (_consoleProcess?.HasExited == false)
                    _consoleProcess.Kill();
            }

            Application.Exit();
        }

        private void StartConsoleApplication()
        {
            var applicationFolder = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            var exePath = Path.Combine(applicationFolder, "JackettConsole.exe");
            var startInfo = new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = exePath,
                RedirectStandardInput = true,
                RedirectStandardError = true
            };
            _consoleProcess = Process.Start(startInfo);
            _consoleProcess.EnableRaisingEvents = true;
            _consoleProcess.Exited += ProcessExited;
            _consoleProcess.ErrorDataReceived += ProcessErrorDataReceived;
        }

        private void ProcessErrorDataReceived(object sender, DataReceivedEventArgs e) => _logger.Error(e.Data);

        private void ProcessExited(object sender, EventArgs e)
        {
            if (!_closeApplicationInitiated)
            {
                _logger.Info("Tray icon not responsible for process exit");
                CloseTrayApplication();
            }
        }
    }
}
