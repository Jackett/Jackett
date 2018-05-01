using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web.Http;
using Jackett.Common;
using Jackett.Common.Models;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Utils;
using NLog;

namespace Jackett.Controllers
{
    [RoutePrefix("api/v2.0/server")]
    [JackettAuthorized]
    [JackettAPINoCache]
    public class ServerConfigurationController : ApiController
    {
        private readonly IConfigurationService configService;
        private ServerConfig serverConfig;
        private IServerService serverService;
        private IProcessService processService;
        private IIndexerManagerService indexerService;
        private ISecuityService securityService;
        private IUpdateService updater;
        private ILogCacheService logCache;
        private Logger logger;

        public ServerConfigurationController(IConfigurationService c, IServerService s, IProcessService p,  IIndexerManagerService i, ISecuityService ss, IUpdateService u, ILogCacheService lc, Logger l, ServerConfig sc)
        {
            configService = c;
            serverConfig = sc;
            serverService = s;
            processService = p;
            indexerService = i;
            securityService = ss;
            updater = u;
            logCache = lc;
            logger = l;
        }

        [HttpPost]
        public void AdminPassword([FromBody]string password)
        {
            var oldPassword = serverConfig.AdminPassword;
            if (string.IsNullOrEmpty(password))
                password = null;

            if (oldPassword != password)
            {
                serverConfig.AdminPassword = securityService.HashPassword(password);
                configService.SaveConfig(serverConfig);
            }
        }

        [HttpPost]
        public void Update()
        {
            updater.CheckForUpdatesNow();
        }

        [HttpGet]
        public Common.Models.DTO.ServerConfig Config()
        {

            var dto = new Common.Models.DTO.ServerConfig(serverService.notices, serverConfig, configService.GetVersion());
            return dto;
        }

        [ActionName("Config")]
        [HttpPost]
        public void UpdateConfig([FromBody]Common.Models.DTO.ServerConfig config)
        {
            var originalPort = serverConfig.Port;
            var originalAllowExternal = serverConfig.AllowExternal;
            int port = config.port;
            bool external = config.external;
            string saveDir = config.blackholedir;
            bool updateDisabled = config.updatedisabled;
            bool preRelease = config.prerelease;
            bool logging = config.logging;
            string basePathOverride = config.basepathoverride;
            if (basePathOverride != null)
            {
                basePathOverride = basePathOverride.TrimEnd('/');
                if (!string.IsNullOrWhiteSpace(basePathOverride) && !basePathOverride.StartsWith("/"))
                    throw new Exception("The Base Path Override must start with a /");
            }

            string omdbApiKey = config.omdbkey;

            serverConfig.UpdateDisabled = updateDisabled;
            serverConfig.UpdatePrerelease = preRelease;
            serverConfig.BasePathOverride = basePathOverride;
            serverConfig.RuntimeSettings.BasePath = Engine.Server.BasePath();
            configService.SaveConfig(serverConfig);

            Engine.SetLogLevel(logging ? LogLevel.Debug : LogLevel.Info);
            serverConfig.RuntimeSettings.TracingEnabled = logging;

            if (omdbApiKey != serverConfig.OmdbApiKey)
            {
                serverConfig.OmdbApiKey = omdbApiKey;
                 configService.SaveConfig(serverConfig);
                // HACK
                indexerService.InitAggregateIndexer();
            }

            if (config.proxy_type != serverConfig.ProxyType ||
                config.proxy_url != serverConfig.ProxyUrl ||
                config.proxy_port != serverConfig.ProxyPort ||
                config.proxy_username != serverConfig.ProxyUsername ||
                config.proxy_password != serverConfig.ProxyPassword)
            {
                if (config.proxy_port < 1 || config.proxy_port > 65535)
                    throw new Exception("The port you have selected is invalid, it must be below 65535.");

                serverConfig.ProxyUrl = config.proxy_url;
                serverConfig.ProxyType = config.proxy_type;
                serverConfig.ProxyPort = config.proxy_port;
                serverConfig.ProxyUsername = config.proxy_username;
                serverConfig.ProxyPassword = config.proxy_password;
                configService.SaveConfig(serverConfig);
            }

            if (port != serverConfig.Port || external != serverConfig.AllowExternal)
            {

                if (ServerUtil.RestrictedPorts.Contains(port))
                    throw new Exception("The port you have selected is restricted, try a different one.");

                if (port < 1 || port > 65535)
                    throw new Exception("The port you have selected is invalid, it must be below 65535.");

                // Save port to the config so it can be picked up by the if needed when running as admin below.
                serverConfig.AllowExternal = external;
                serverConfig.Port = port;
                configService.SaveConfig(serverConfig);

                // On Windows change the url reservations
                if (System.Environment.OSVersion.Platform != PlatformID.Unix)
                {
                    if (!ServerUtil.IsUserAdministrator())
                    {
                        try
                        {
                            processService.StartProcessAndLog(System.Windows.Forms.Application.ExecutablePath, "--ReserveUrls", true);
                        }
                        catch
                        {
                            serverConfig.Port = originalPort;
                            serverConfig.AllowExternal = originalAllowExternal;
                            configService.SaveConfig(serverConfig);

                            throw new Exception("Failed to acquire admin permissions to reserve the new port.");
                        }
                    }
                    else
                    {
                        serverService.ReserveUrls(true);
                    }
                }

            (new Thread(() =>
            {
                Thread.Sleep(500);
                serverService.Stop();
                Engine.BuildContainer(serverConfig.RuntimeSettings, new WebApi2Module());
                Engine.Server.Initalize();
                Engine.Server.Start();
            })).Start();
            }

            if (saveDir != serverConfig.BlackholeDir)
            {
                if (!string.IsNullOrEmpty(saveDir))
                {
                    if (!Directory.Exists(saveDir))
                    {
                        throw new Exception("Blackhole directory does not exist");
                    }
                }

                serverConfig.BlackholeDir = saveDir;
                configService.SaveConfig(serverConfig);
            }
            
            serverConfig.ConfigChanged();
        }

        [HttpGet]
        public List<CachedLog> Logs()
        {
            return logCache.Logs;
        }


    }
}
