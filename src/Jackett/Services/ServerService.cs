using Autofac;
using Jackett.Models.Config;
using Jackett.Services;
using Microsoft.Owin.Hosting;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Windows.Forms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Services
{
    public interface IServerService
    {
        void Initalize();
        void Start();
        void Stop();
        void ReserveUrls(bool doInstall = true);
        ServerConfig Config { get; }
        void SaveConfig();
    }

    public class ServerService : IServerService
    {
        private ServerConfig config;

        private IDisposable _server = null;

        private IIndexerManagerService indexerService;
        private IProcessService processService;
        private ISerializeService serializeService;
        private IConfigurationService configService;
        private Logger logger;

        public ServerService(IIndexerManagerService i, IProcessService p, ISerializeService s, IConfigurationService c, Logger l)
        {
            indexerService = i;
            processService = p;
            serializeService = s;
            configService = c;
            logger = l;

            LoadConfig();
        }

        public ServerConfig Config
        {
            get { return config; }
        }

        private void LoadConfig()
        {
            // Load config
            config = configService.GetConfig<ServerConfig>();
            if (config == null)
            {
                config = new ServerConfig();
            }

            if (string.IsNullOrWhiteSpace(config.APIKey))
            {
                // Check for legacy key config
                var apiKeyFile = Path.Combine(configService.GetAppDataFolder(), "api_key.txt");
                if (File.Exists(apiKeyFile))
                {
                    config.APIKey = File.ReadAllText(apiKeyFile);
                }

                // Check for legacy settings

                var path = Path.Combine(configService.GetAppDataFolder(), "config.json"); ;
                var jsonReply = new JObject();
                if (File.Exists(path))
                {
                    jsonReply = JObject.Parse(File.ReadAllText(path));
                    config.Port = (int)jsonReply["port"];
                    config.AllowExternal = (bool)jsonReply["public"];
                }

                if (string.IsNullOrWhiteSpace(config.APIKey))
                {
                    config.APIKey = config.GenerateApi();
                }

                configService.SaveConfig<ServerConfig>(config);
            }
        }

        public void SaveConfig()
        {
            configService.SaveConfig<ServerConfig>(config);
        }

        public void Initalize()
        {
            logger.Info("Starting Jackett " + configService.GetVersion());
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");

            // Allow all SSL.. sucks I know but mono on linux is having problems without it..
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            // Load indexers
            indexerService.InitIndexers();
            
        }

        public void Start()
        {
            // Start the server
            logger.Info("Starting web server at " + config.GetListenAddresses()[0]);
            var startOptions = new StartOptions();
            config.GetListenAddresses().ToList().ForEach(u => startOptions.Urls.Add(u));
            _server = WebApp.Start<Startup>(startOptions);
            logger.Debug("Web server started");
        }

        public void ReserveUrls(bool doInstall = true)
        {
            logger.Debug("Unreserving Urls");
            config.GetListenAddresses(false).ToList().ForEach(u => RunNetSh(string.Format("http delete urlacl {0}", u)));
            config.GetListenAddresses(true).ToList().ForEach(u => RunNetSh(string.Format("http delete urlacl {0}", u)));
            if (doInstall)
            {
                logger.Debug("Reserving Urls");
                config.GetListenAddresses(config.AllowExternal).ToList().ForEach(u => RunNetSh(string.Format("http add urlacl {0} sddl=D:(A;;GX;;;S-1-1-0)", u)));
                logger.Debug("Urls reserved");
            }
        }

        private void RunNetSh(string args)
        {
            processService.StartProcessAndLog("netsh.exe", args);
        }

        public void Stop()
        {
            if (_server != null)
            {
                _server.Dispose();
            }
        }
    }
}
