using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Web.Http;
using Jackett.Models;
using Jackett.Services;
using Jackett.Services.Interfaces;
using Jackett.Utils;
using NLog;

namespace Jackett.Controllers.V20
{
    [RoutePrefix("api/v2.0/server")]
    [JackettAuthorized]
    [JackettAPINoCache]
    public class ServerConfigurationController : ApiController
    {
        public ServerConfigurationController(IConfigurationService c, IServerService s, IProcessService p, IIndexerManagerService i, ISecuityService ss, IUpdateService u, ILogCacheService lc, Logger l)
        {
            config = c;
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
            var oldPassword = serverService.Config.AdminPassword;
            if (string.IsNullOrEmpty(password))
                password = string.Empty;

            if (oldPassword != password)
            {
                serverService.Config.AdminPassword = securityService.HashPassword(password);
                serverService.SaveConfig();
            }
        }

        [HttpPost]
        public void Update()
        {
            updater.CheckForUpdatesNow();
        }

        [HttpGet]
        public Models.DTO.ServerConfig Config()
        {

            var dto = new Models.DTO.ServerConfig(serverService.notices, serverService.Config, config.GetVersion());
            return dto;
        }

        [ActionName("Config")]
        [HttpPost]
        public void UpdateConfig([FromBody]Models.DTO.ServerConfig config)
        {
            var originalPort = Engine.Server.Config.Port;
            var originalAllowExternal = Engine.Server.Config.AllowExternal;
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

            Engine.Server.Config.UpdateDisabled = updateDisabled;
            Engine.Server.Config.UpdatePrerelease = preRelease;
            Engine.Server.Config.BasePathOverride = basePathOverride;
            Startup.BasePath = Engine.Server.BasePath();
            Engine.Server.SaveConfig();

            Engine.SetLogLevel(logging ? LogLevel.Debug : LogLevel.Info);
            Startup.TracingEnabled = logging;

            if (omdbApiKey != Engine.Server.Config.OmdbApiKey)
            {
                Engine.Server.Config.OmdbApiKey = omdbApiKey;
                Engine.Server.SaveConfig();
                // HACK
                indexerService.InitAggregateIndexer();
            }

            if (port != Engine.Server.Config.Port || external != Engine.Server.Config.AllowExternal)
            {

                if (ServerUtil.RestrictedPorts.Contains(port))
                    throw new Exception("The port you have selected is restricted, try a different one.");

                if (port < 1 || port > 65535)
                    throw new Exception("The port you have selected is invalid, it must be below 65535.");

                // Save port to the config so it can be picked up by the if needed when running as admin below.
                Engine.Server.Config.AllowExternal = external;
                Engine.Server.Config.Port = port;
                Engine.Server.SaveConfig();

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
                            Engine.Server.Config.Port = originalPort;
                            Engine.Server.Config.AllowExternal = originalAllowExternal;
                            Engine.Server.SaveConfig();

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
                Engine.BuildContainer();
                Engine.Server.Initalize();
                Engine.Server.Start();
            })).Start();
            }

            if (saveDir != Engine.Server.Config.BlackholeDir)
            {
                if (!string.IsNullOrEmpty(saveDir))
                {
                    if (!Directory.Exists(saveDir))
                    {
                        throw new Exception("Blackhole directory does not exist");
                    }
                }

                Engine.Server.Config.BlackholeDir = saveDir;
                Engine.Server.SaveConfig();
            }
        }

        [HttpGet]
        public List<CachedLog> Logs()
        {
            return logCache.Logs;
        }

        private IConfigurationService config;
        private IServerService serverService;
        private IProcessService processService;
        private IIndexerManagerService indexerService;
        private ISecuityService securityService;
        private IUpdateService updater;
        private ILogCacheService logCache;
        private Logger logger;
    }
}
