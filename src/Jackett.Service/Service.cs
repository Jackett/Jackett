using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using Jackett.Common.Models.Config;
using Jackett.Common.Services;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using NLog;

namespace Jackett.Service
{
    public partial class Service : ServiceBase
    {
        private readonly IProcessService _processService;
        private Process _consoleProcess;
        private readonly Logger _logger;
        private bool _serviceStopInitiated;

        public Service()
        {
            InitializeComponent();
            var runtimeSettings = new RuntimeSettings { CustomLogFileName = "ServiceLog.txt" };
            LogManager.Configuration = LoggingSetup.GetLoggingConfiguration(runtimeSettings);
            _logger = LogManager.GetCurrentClassLogger();
            _logger.Info($"Initiating Jackett Service v{EnvironmentUtil.JackettVersion}");
            _processService = new ProcessService(_logger);
        }

        protected override void OnStart(string[] args)
        {
            _logger.Info("Service starting");
            _serviceStopInitiated = false;
            StartConsoleApplication();
        }

        protected override void OnStop()
        {
            _logger.Info("Service stopping");
            _serviceStopInitiated = true;
            StopConsoleApplication();
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
            _logger.Info("Console process exited");
            if (!_serviceStopInitiated)
            {
                _logger.Info("Service stop not responsible for process exit");
                Stop();
            }
        }

        private void StopConsoleApplication()
        {
            if (_consoleProcess?.HasExited == false)
            {
                _consoleProcess.StandardInput.Close();
                _consoleProcess.WaitForExit(2000);
                if (_consoleProcess?.HasExited == false)
                    _consoleProcess.Kill();
            }
        }
    }
}
