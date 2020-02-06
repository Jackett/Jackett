using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Jackett.Common.Models;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace Jackett.Server.Controllers
{
    [Route("api/v2.0/server/[action]"), ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class ServerConfigurationController : Controller
    {
        private readonly IConfigurationService _configService;
        private readonly ServerConfig _serverConfig;
        private readonly IServerService _serverService;
        private readonly IProcessService _processService;
        private readonly IIndexerManagerService _indexerService;
        private readonly ISecuityService _securityService;
        private readonly IUpdateService _updater;
        private readonly ILogCacheService _logCache;
        private readonly Logger _logger;

        public ServerConfigurationController(IConfigurationService c, IServerService s, IProcessService p,
                                             IIndexerManagerService i, ISecuityService ss, IUpdateService u,
                                             ILogCacheService lc, Logger l, ServerConfig sc)
        {
            _configService = c;
            _serverConfig = sc;
            _serverService = s;
            _processService = p;
            _indexerService = i;
            _securityService = ss;
            _updater = u;
            _logCache = lc;
            _logger = l;
        }

        [HttpPost]
        public IActionResult AdminPassword([FromBody] string password)
        {
            var oldPassword = _serverConfig.AdminPassword;
            if (string.IsNullOrEmpty(password))
                password = null;
            if (oldPassword != password)
            {
                _serverConfig.AdminPassword = _securityService.HashPassword(password);
                _configService.SaveConfig(_serverConfig);
            }

            return new NoContentResult();
        }

        [HttpPost]
        public void Update() => _updater.CheckForUpdatesNow();

        [HttpGet]
        public Common.Models.DTO.ServerConfig Config()
        {
            var dto = new Common.Models.DTO.ServerConfig(
                _serverService.notices, _serverConfig, _configService.GetVersion(), _serverService.MonoUserCanRunNetCore());
            return dto;
        }

        [ActionName("Config"), HttpPost]
        public IActionResult UpdateConfig([FromBody] Common.Models.DTO.ServerConfig config)
        {
            var webHostRestartNeeded = false;
            var originalPort = _serverConfig.Port;
            var originalAllowExternal = _serverConfig.AllowExternal;
            var port = config.port;
            var external = config.external;
            var saveDir = config.blackholedir;
            var updateDisabled = config.updatedisabled;
            var preRelease = config.prerelease;
            var logging = config.logging;
            var basePathOverride = config.basepathoverride;
            if (basePathOverride != null)
            {
                basePathOverride = basePathOverride.TrimEnd('/');
                if (!string.IsNullOrWhiteSpace(basePathOverride) && !basePathOverride.StartsWith("/"))
                    throw new Exception("The Base Path Override must start with a /");
            }

            var omdbApiKey = config.omdbkey;
            var omdbApiUrl = config.omdburl;
            if (config.basepathoverride != _serverConfig.BasePathOverride)
                webHostRestartNeeded = true;
            _serverConfig.UpdateDisabled = updateDisabled;
            _serverConfig.UpdatePrerelease = preRelease;
            _serverConfig.BasePathOverride = basePathOverride;
            _serverConfig.RuntimeSettings.BasePath = _serverService.BasePath();
            _configService.SaveConfig(_serverConfig);
            Helper.SetLogLevel(logging ? LogLevel.Debug : LogLevel.Info);
            _serverConfig.RuntimeSettings.TracingEnabled = logging;
            if (omdbApiKey != _serverConfig.OmdbApiKey || omdbApiUrl != _serverConfig.OmdbApiUrl)
            {
                _serverConfig.OmdbApiKey = omdbApiKey;
                _serverConfig.OmdbApiUrl = omdbApiUrl.TrimEnd('/');
                _configService.SaveConfig(_serverConfig);
                // HACK
                _indexerService.InitAggregateIndexer();
            }

            if (config.proxy_type != _serverConfig.ProxyType || config.proxy_url != _serverConfig.ProxyUrl ||
                config.proxy_port != _serverConfig.ProxyPort || config.proxy_username != _serverConfig.ProxyUsername ||
                config.proxy_password != _serverConfig.ProxyPassword)
            {
                if (config.proxy_port < 1 || config.proxy_port > 65535)
                    throw new Exception("The port you have selected is invalid, it must be below 65535.");
                _serverConfig.ProxyUrl = config.proxy_url;
                _serverConfig.ProxyType = config.proxy_type;
                _serverConfig.ProxyPort = config.proxy_port;
                _serverConfig.ProxyUsername = config.proxy_username;
                _serverConfig.ProxyPassword = config.proxy_password;
                _configService.SaveConfig(_serverConfig);
                webHostRestartNeeded = true;
            }

            if (port != _serverConfig.Port || external != _serverConfig.AllowExternal)
            {
                if (ServerUtil.RestrictedPorts.Contains(port))
                    throw new Exception("The port you have selected is restricted, try a different one.");
                if (port < 1 || port > 65535)
                    throw new Exception("The port you have selected is invalid, it must be below 65535.");

                // Save port to the config so it can be picked up by the if needed when running as admin below.
                _serverConfig.AllowExternal = external;
                _serverConfig.Port = port;
                _configService.SaveConfig(_serverConfig);

                // On Windows change the url reservations
                if (Environment.OSVersion.Platform != PlatformID.Unix)
                {
                    if (!ServerUtil.IsUserAdministrator())
                        try
                        {
                            var consoleExePath = Assembly.GetExecutingAssembly().CodeBase.Replace(".dll", ".exe");
                            _processService.StartProcessAndLog(consoleExePath, "--ReserveUrls", true);
                        }
                        catch
                        {
                            _serverConfig.Port = originalPort;
                            _serverConfig.AllowExternal = originalAllowExternal;
                            _configService.SaveConfig(_serverConfig);
                            throw new Exception("Failed to acquire admin permissions to reserve the new port.");
                        }
                    else
                        _serverService.ReserveUrls();
                }

                webHostRestartNeeded = true;
            }

            if (saveDir != _serverConfig.BlackholeDir)
            {
                if (!string.IsNullOrEmpty(saveDir))
                    if (!Directory.Exists(saveDir))
                        throw new Exception("Blackhole directory does not exist");
                _serverConfig.BlackholeDir = saveDir;
                _configService.SaveConfig(_serverConfig);
            }

            if (webHostRestartNeeded)
            {
                Thread.Sleep(500);
                _logger.Info("Restarting webhost due to configuration change");
                Helper.RestartWebHost();
            }

            _serverConfig.ConfigChanged();
            return Json(_serverConfig);
        }

        [HttpGet]
        public List<CachedLog> Logs() => _logCache.Logs;
    }
}
