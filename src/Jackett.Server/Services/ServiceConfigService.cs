using NLog;
using System;
using System.Collections.Specialized;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using Jackett.Common.Services.Interfaces;

namespace Jackett.Services
{

    public class ServiceConfigService : IServiceConfigService
    {
        private const string NAME = "Jackett";
        private const string DESCRIPTION = "Additional indexers for Sonarr";
        private const string SERVICEEXE = "JackettService.exe";

        private IConfigurationService configService;
        private Logger logger;

        public ServiceConfigService(IConfigurationService c, Logger l)
        {
            configService = c;
            logger = l;
        }

        public bool ServiceExists()
        {
            return GetService(NAME) != null;
        }

        public bool ServiceRunning()
        {
            var service = GetService(NAME);
            if (service == null)
                return false;
            return service.Status == ServiceControllerStatus.Running;
        }

        public void Start()
        {

            var service = GetService(NAME);
            service.Start();
        }

        public void Stop()
        {
            var service = GetService(NAME);
            service.Stop();
        }

        public ServiceController GetService(string serviceName)
        {
            return ServiceController.GetServices().FirstOrDefault(c => String.Equals(c.ServiceName, serviceName, StringComparison.InvariantCultureIgnoreCase));
        }

        public void Install()
        {
            if (ServiceExists())
            {
                logger.Warn("The service is already installed!");
            }
            else
            {
                var installer = new ServiceProcessInstaller
                {
                    Account = ServiceAccount.LocalSystem
                };

                var serviceInstaller = new ServiceInstaller();

                var exePath = Path.Combine(configService.ApplicationFolder(), SERVICEEXE);
                if (!File.Exists(exePath) && Debugger.IsAttached)
                {
                    exePath = Path.Combine(configService.ApplicationFolder(), "..\\..\\..\\Jackett.Service\\bin\\Debug", SERVICEEXE);
                }

                string[] cmdline = { @"/assemblypath=" + exePath};

                var context = new InstallContext("jackettservice_install.log", cmdline);                
                serviceInstaller.Context = context;
                serviceInstaller.DisplayName = NAME;
                serviceInstaller.ServiceName = NAME;
                serviceInstaller.Description = DESCRIPTION;
                serviceInstaller.StartType = ServiceStartMode.Automatic;
                serviceInstaller.Parent = installer;

                serviceInstaller.Install(new ListDictionary());
            }
        }

        public void Uninstall()
        {
            RemoveService();

            var serviceInstaller = new ServiceInstaller();
            var context = new InstallContext("jackettservice_uninstall.log", null);
            serviceInstaller.Context = context;
            serviceInstaller.ServiceName = NAME;
            serviceInstaller.Uninstall(null);

            logger.Info("The service was uninstalled.");
        }

        public void RemoveService()
        {
            var service = GetService(NAME);
            if(service == null)
            {
                logger.Warn("The service is already uninstalled");
                return;
            }
            if (service.Status != ServiceControllerStatus.Stopped)
            {
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(60));

                service.Refresh();
                if (service.Status == ServiceControllerStatus.Stopped)
                    logger.Info("Service stopped.");
                else
                    logger.Error("Failed to stop the service");
            }
            else
                logger.Warn("The service was already stopped");
        }
    }
}
