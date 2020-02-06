using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using Jackett.Common.Services.Interfaces;
using NLog;

namespace Jackett.Common.Services
{
    public class WindowsServiceConfigService : IServiceConfigService
    {
        private const string Name = "Jackett";
        private const string Description = "API Support for your favorite torrent trackers";
        private const string Serviceexe = "JackettService.exe";

        private readonly IProcessService _processService;
        private readonly Logger _logger;

        public WindowsServiceConfigService(IProcessService p, Logger l)
        {
            _processService = p;
            _logger = l;
        }

        public bool ServiceExists() => GetService(Name) != null;

        public bool ServiceRunning()
        {
            var service = GetService(Name);
            return service == null ? false : service.Status == ServiceControllerStatus.Running;
        }

        public void Start()
        {
            var service = GetService(Name);
            service.Start();
        }

        public void Stop()
        {
            var service = GetService(Name);
            service.Stop();
        }

        public ServiceController GetService(string serviceName) => ServiceController
                                                                   .GetServices().FirstOrDefault(
                                                                       c => string.Equals(
                                                                           c.ServiceName, serviceName,
                                                                           StringComparison.InvariantCultureIgnoreCase));

        public void Install()
        {
            if (ServiceExists())
                _logger.Warn("The service is already installed!");
            else
            {
                var applicationFolder = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
                var exePath = Path.Combine(applicationFolder, Serviceexe);
                if (!File.Exists(exePath) && Debugger.IsAttached)
                    exePath = Path.Combine(applicationFolder, "..\\..\\..\\Jackett.Service\\bin\\Debug", Serviceexe);
                var arg = $"create {Name} start= auto binpath= \"{exePath}\" DisplayName= {Name}";
                _processService.StartProcessAndLog("sc.exe", arg, true);
                _processService.StartProcessAndLog("sc.exe", $"description {Name} \"{Description}\"", true);
            }
        }

        public void Uninstall()
        {
            RemoveService();
            _processService.StartProcessAndLog("sc.exe", $"delete {Name}", true);
            _logger.Info("The service was uninstalled.");
        }

        public void RemoveService()
        {
            var service = GetService(Name);
            if (service == null)
            {
                _logger.Warn("The service is already uninstalled");
                return;
            }

            if (service.Status != ServiceControllerStatus.Stopped)
            {
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(60));
                service.Refresh();
                if (service.Status == ServiceControllerStatus.Stopped)
                    _logger.Info("Service stopped.");
                else
                    _logger.Error("Failed to stop the service");
            }
            else
                _logger.Warn("The service was already stopped");
        }
    }
}
