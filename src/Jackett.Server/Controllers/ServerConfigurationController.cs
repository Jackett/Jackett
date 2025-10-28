using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using Jackett.Common.Models;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Cache;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace Jackett.Server.Controllers
{
    [ApiController]
    [Route("api/v2.0/server/[action]")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class ServerConfigurationController : Controller
    {
        private readonly IConfigurationService configService;
        private readonly ServerConfig serverConfig;
        private readonly IServerService serverService;
        private readonly IProcessService processService;
        private readonly IIndexerManagerService indexerService;
        private readonly ISecurityService securityService;
        private readonly CacheManager _cacheManager;
        private readonly IUpdateService updater;
        private readonly ILogCacheService logCache;
        private readonly Logger logger;

        public ServerConfigurationController(IConfigurationService c, IServerService s, IProcessService p,
            IIndexerManagerService i, ISecurityService ss, IUpdateService u, ILogCacheService lc,
            Logger l, ServerConfig sc, CacheManager cacheManager)
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
            _cacheManager = cacheManager;
        }

        [HttpPost]
        public IActionResult AdminPassword([FromBody] string password)
        {
            var oldPassword = serverConfig.AdminPassword;
            if (string.IsNullOrEmpty(password))
                password = null;

            if (oldPassword != password)
            {
                serverConfig.AdminPassword = securityService.HashPassword(password);
                configService.SaveConfig(serverConfig);
            }

            return new NoContentResult();
        }

        [HttpPost]
        public void Update() => updater.CheckForUpdatesNow();

        [HttpGet]
        public Common.Models.DTO.ServerConfig Config()
        {
            var dto = new Common.Models.DTO.ServerConfig(serverService.notices, serverConfig, configService.GetVersion(), serverService.MonoUserCanRunNetCore());
            return dto;
        }

        [ActionName("Config")]
        [HttpPost]
        public IActionResult UpdateConfig([FromBody] Common.Models.DTO.ServerConfig config)
        {
            var webHostRestartNeeded = false;

            var originalPort = serverConfig.Port;
            var originalLocalBindAddress = serverConfig.LocalBindAddress;
            var originalAllowExternal = serverConfig.AllowExternal;
            var port = config.port;
            var local_bind_address = config.local_bind_address;
            var external = config.external;
            var cors = config.cors;
            var saveDir = config.blackholedir;
            var updateDisabled = config.updatedisabled;
            var preRelease = config.prerelease;
            var enhancedLogging = config.logging;

            var basePathOverride = config.basepathoverride;
            if (basePathOverride != null)
            {
                basePathOverride = basePathOverride.TrimEnd('/');
                if (!string.IsNullOrWhiteSpace(basePathOverride) && !basePathOverride.StartsWith("/"))
                    throw new Exception("The Base Path Override must start with a /");
            }

            var baseUrlOverride = config.baseurloverride;
            if (baseUrlOverride != serverConfig.BaseUrlOverride)
            {
                baseUrlOverride = baseUrlOverride.TrimEnd('/');
                if (string.IsNullOrWhiteSpace(baseUrlOverride))
                    baseUrlOverride = "";
                else if (!Uri.TryCreate(baseUrlOverride, UriKind.Absolute, out var uri)
                    || !(uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                    || !Uri.IsWellFormedUriString(baseUrlOverride, UriKind.Absolute))
                    throw new Exception("Base URL Override is invalid. Example: http://jackett:9117");

                serverConfig.BaseUrlOverride = baseUrlOverride;
                configService.SaveConfig(serverConfig);
            }

            var cacheTtl = config.cache_ttl;
            var cacheMaxResultsPerIndexer = config.cache_max_results_per_indexer;
            var omdbApiKey = config.omdbkey;
            var omdbApiUrl = config.omdburl;

            if (config.basepathoverride != serverConfig.BasePathOverride)
                webHostRestartNeeded = true;

            if (config.cors != serverConfig.AllowCORS)
                webHostRestartNeeded = true;

            serverConfig.AllowCORS = cors;
            serverConfig.UpdateDisabled = updateDisabled;
            serverConfig.UpdatePrerelease = preRelease;
            serverConfig.BasePathOverride = basePathOverride;
            serverConfig.BaseUrlOverride = baseUrlOverride;

            var cacheType = config.cache_type;
            var cacheConString = config.cache_connection_string;

            serverConfig.CacheTtl = cacheTtl;
            serverConfig.CacheMaxResultsPerIndexer = cacheMaxResultsPerIndexer;

            _cacheManager.ChangeCacheType(cacheType, cacheConString);
            serverConfig.CacheConnectionString = _cacheManager.GetCacheConnectionString();
            serverConfig.CacheType = cacheType;

            serverConfig.RuntimeSettings.BasePath = serverService.BasePath();
            configService.SaveConfig(serverConfig);

            if (config.flaresolverrurl != serverConfig.FlareSolverrUrl ||
                config.flaresolverr_maxtimeout != serverConfig.FlareSolverrMaxTimeout)
            {
                if (string.IsNullOrWhiteSpace(config.flaresolverrurl))
                    config.flaresolverrurl = "";
                else if (!Uri.TryCreate(config.flaresolverrurl, UriKind.Absolute, out var uri)
                    || !(uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                    || !Uri.IsWellFormedUriString(config.flaresolverrurl, UriKind.Absolute))
                    throw new Exception("FlareSolverr API URL is invalid. Example: http://127.0.0.1:8191");

                if (config.flaresolverr_maxtimeout < 5000)
                    throw new Exception("FlareSolverr Max Timeout must be greater than 5000 ms.");

                serverConfig.FlareSolverrUrl = config.flaresolverrurl;
                serverConfig.FlareSolverrMaxTimeout = config.flaresolverr_maxtimeout;
                configService.SaveConfig(serverConfig);
                webHostRestartNeeded = true;
            }

            if (omdbApiKey != serverConfig.OmdbApiKey || omdbApiUrl != serverConfig.OmdbApiUrl)
            {
                serverConfig.OmdbApiKey = omdbApiKey;
                serverConfig.OmdbApiUrl = omdbApiUrl.TrimEnd('/');
                configService.SaveConfig(serverConfig);
                // HACK
                indexerService.InitMetaIndexers();
            }

            if (config.proxy_type != serverConfig.ProxyType ||
                config.proxy_url != serverConfig.ProxyUrl ||
                config.proxy_port != serverConfig.ProxyPort ||
                config.proxy_username != serverConfig.ProxyUsername ||
                config.proxy_password != serverConfig.ProxyPassword)
            {
                if (config.proxy_port < 1 || config.proxy_port > 65535)
                {
                    throw new Exception("The port you have selected is invalid, it must be between 1 and 65535.");
                }

                serverConfig.ProxyType = string.IsNullOrWhiteSpace(config.proxy_url) ? ProxyType.Disabled : config.proxy_type;
                serverConfig.ProxyUrl = config.proxy_url;
                serverConfig.ProxyPort = config.proxy_port;
                serverConfig.ProxyUsername = config.proxy_username;
                serverConfig.ProxyPassword = config.proxy_password;
                configService.SaveConfig(serverConfig);
                webHostRestartNeeded = true;

                // Remove all results from cache so we can test the new proxy
                _cacheManager.CleanCache();
            }

            if (port != serverConfig.Port || external != serverConfig.AllowExternal || local_bind_address != serverConfig.LocalBindAddress)
            {
                if (ServerUtil.RestrictedPorts.Contains(port))
                {
                    throw new Exception("The port you have selected is restricted, try a different one.");
                }

                if (port < 1 || port > 65535)
                {
                    throw new Exception("The port you have selected is invalid, it must be between 1 and 65535.");
                }

                if (string.IsNullOrWhiteSpace(local_bind_address))
                {
                    throw new Exception("You need to provide a local bind address.");
                }

                if (!IPAddress.IsLoopback(IPAddress.Parse(local_bind_address)))
                {
                    throw new Exception("The local address you selected is not a loopback one.");
                }

                // Save port and LocalAddr to the config so they can be picked up by the if needed when running as admin below.
                serverConfig.AllowExternal = external;
                serverConfig.Port = port;
                serverConfig.LocalBindAddress = local_bind_address;
                configService.SaveConfig(serverConfig);

                // On Windows change the url reservations
                if (Environment.OSVersion.Platform != PlatformID.Unix)
                {
                    if (!ServerUtil.IsUserAdministrator())
                    {
                        try
                        {
                            var consoleExePath = EnvironmentUtil.JackettExecutablePath().Replace(".dll", ".exe");
                            processService.StartProcessAndLog(consoleExePath, "--ReserveUrls", true);
                        }
                        catch
                        {
                            serverConfig.LocalBindAddress = originalLocalBindAddress;
                            serverConfig.Port = originalPort;
                            serverConfig.AllowExternal = originalAllowExternal;
                            configService.SaveConfig(serverConfig);

                            throw new Exception("Failed to acquire admin permissions to reserve the new local_bind_address/port.");
                        }
                    }
                    else
                    {
                        serverService.ReserveUrls();
                    }
                }

                webHostRestartNeeded = true;
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

            if (webHostRestartNeeded)
            {
                // we have to restore log level when the server restarts because we are not saving the state in the
                // configuration. when the server restarts the UI is inconsistent with the active log level
                // https://github.com/Jackett/Jackett/issues/8315
                SetEnhancedLogLevel(false);

                Thread.Sleep(500);
                logger.Info("Restarting webhost due to configuration change");
                Helper.RestartWebHost();
            }
            else
                SetEnhancedLogLevel(enhancedLogging);

            serverConfig.ConfigChanged();

            return Json(serverConfig);
        }

        [HttpGet]
        public List<CachedLog> Logs() => logCache.Logs;

        private void SetEnhancedLogLevel(bool enabled)
        {
            Helper.SetLogLevel(enabled ? LogLevel.Debug : LogLevel.Info);
            serverConfig.RuntimeSettings.TracingEnabled = enabled;
        }
    }
}
