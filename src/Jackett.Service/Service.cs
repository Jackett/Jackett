using System;
using System.Diagnostics;
using System.IO;
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
        private readonly IProcessService processService;
        private Process consoleProcess;
        private readonly Logger logger;
        private bool serviceStopInitiated;

        public Service()
        {
            InitializeComponent();

            var runtimeSettings = new RuntimeSettings()
            {
                CustomLogFileName = "ServiceLog.txt"
            };

            LogManager.Configuration = LoggingSetup.GetLoggingConfiguration(runtimeSettings);
            logger = LogManager.GetCurrentClassLogger();

            logger.Info("Initiating Jackett Service " + EnvironmentUtil.JackettVersion());

            processService = new ProcessService(logger);
        }

        protected override void OnStart(string[] args)
        {
            logger.Info("Service starting");
            serviceStopInitiated = false;
            StartConsoleApplication();
        }

        protected override void OnStop()
        {
            logger.Info("Service stopping");
            serviceStopInitiated = true;
            StopConsoleApplication();
        }

        private void StartConsoleApplication()
        {
            var exePath = Path.Combine(EnvironmentUtil.JackettInstallationPath(), "JackettConsole.exe");

            var startInfo = new ProcessStartInfo()
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = exePath,
                RedirectStandardInput = true,
                RedirectStandardError = true
            };

            consoleProcess = Process.Start(startInfo);
            consoleProcess.EnableRaisingEvents = true;
            consoleProcess.Exited += ProcessExited;
            consoleProcess.ErrorDataReceived += ProcessErrorDataReceived;
        }

        private void ProcessErrorDataReceived(object sender, DataReceivedEventArgs e) => logger.Error(e.Data);

        private void ProcessExited(object sender, EventArgs e)
        {
            logger.Info("Console process exited");

            if (!serviceStopInitiated)
            {
                logger.Info("Service stop not responsible for process exit");
                Stop();
            }
        }

        private void StopConsoleApplication()
        {
            if (consoleProcess is { HasExited: false })
            {
                consoleProcess.StandardInput.Close();
                consoleProcess.WaitForExit(2000);
                if (consoleProcess is { HasExited: false })
                {
                    consoleProcess.Kill();
                }
            }
        }
    }
}
