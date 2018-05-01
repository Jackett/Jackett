using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Jackett.Server.Services
{
    public class ServerService : IServerService
    {
        private IDisposable _server = null;

        private IIndexerManagerService indexerService;
        private IProcessService processService;
        private ISerializeService serializeService;
        private IConfigurationService configService;
        private Logger logger;
        private Common.Utils.Clients.WebClient client;
        private IUpdateService updater;
        private List<string> _notices = new List<string>();
        private ServerConfig config;
        private IProtectionService _protectionService;

        public ServerService(IIndexerManagerService i, IProcessService p, ISerializeService s, IConfigurationService c, Logger l, Common.Utils.Clients.WebClient w, IUpdateService u, IProtectionService protectionService, ServerConfig serverConfig)
        {
            indexerService = i;
            processService = p;
            serializeService = s;
            configService = c;
            logger = l;
            client = w;
            updater = u;
            config = serverConfig;
            _protectionService = protectionService;
        }

        public List<string> notices
        {
            get
            {
                return _notices;
            }
        }

        public Uri ConvertToProxyLink(Uri link, string serverUrl, string indexerId, string action = "dl", string file = "t")
        {
            if (link == null || (link.IsAbsoluteUri && link.Scheme == "magnet"))
                return link;

            var encryptedLink = _protectionService.Protect(link.ToString());
            var encodedLink = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(encryptedLink));
            string urlEncodedFile = WebUtility.UrlEncode(file);
            var proxyLink = string.Format("{0}{1}/{2}/?jackett_apikey={3}&path={4}&file={5}", serverUrl, action, indexerId, config.APIKey, encodedLink, urlEncodedFile);
            return new Uri(proxyLink);
        }

        public string BasePath()
        {
            if (config.BasePathOverride == null || config.BasePathOverride == "")
            {
                return "";
            }
            var path = config.BasePathOverride;
            if (path.EndsWith("/"))
            {
                path = path.TrimEnd('/');
            }
            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }
            return path;
        }

        public void Initalize()
        {
            logger.Info("Starting Jackett " + configService.GetVersion());
            try
            {
                var x = Environment.OSVersion;
                var runtimedir = RuntimeEnvironment.GetRuntimeDirectory();
                logger.Info("Environment version: " + Environment.Version.ToString() + " (" + runtimedir + ")");
                logger.Info("OS version: " + Environment.OSVersion.ToString() + (Environment.Is64BitOperatingSystem ? " (64bit OS)" : "") + (Environment.Is64BitProcess ? " (64bit process)" : ""));

                try
                {
                    int workerThreads;
                    int completionPortThreads;
                    ThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);
                    logger.Info("ThreadPool MaxThreads: " + workerThreads + " workerThreads, " + completionPortThreads + " completionPortThreads");
                }
                catch (Exception e)
                {
                    logger.Error("Error while getting MaxThreads details: " + e);
                }

                try
                {
                    var issuefile = "/etc/issue";
                    if (File.Exists(issuefile))
                    {
                        using (StreamReader reader = new StreamReader(issuefile))
                        {
                            string firstLine;
                            firstLine = reader.ReadLine();
                            if (firstLine != null)
                                logger.Info("issue: " + firstLine);
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.Error(e, "Error while reading the issue file");
                }
            }
            catch (Exception e)
            {
                logger.Error("Error while getting environment details: " + e);
            }

            try
            {
                if (Environment.UserName == "root")
                {
                    var notice = "Jackett is running with root privileges. You should run Jackett as an unprivileged user.";
                    _notices.Add(notice);
                    logger.Error(notice);
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error while checking the username");
            }

            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            // Load indexers
            indexerService.InitIndexers(configService.GetCardigannDefinitionsFolders());
            client.Init();
            updater.CleanupTempDir();
        }

        public void Start()
        {
            updater.StartUpdateChecker();
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

        public string GetServerUrl(Object obj)
        {
            string serverUrl = "";

            if (obj is HttpRequest request)
            {
                var scheme = request.Scheme;
                var port = request.HttpContext.Request.Host.Port;

                // Check for protocol headers added by reverse proxys
                // X-Forwarded-Proto: A de facto standard for identifying the originating protocol of an HTTP request
                var X_Forwarded_Proto = request.Headers.Where(x => x.Key == "X-Forwarded-Proto").Select(x => x.Value).FirstOrDefault();
                if (X_Forwarded_Proto.Count > 0)
                {
                    scheme = X_Forwarded_Proto.First();
                }
                // Front-End-Https: Non-standard header field used by Microsoft applications and load-balancers
                else if (request.Headers.Where(x => x.Key == "Front-End-Https" && x.Value.FirstOrDefault() == "on").Any())
                {
                    scheme = "https";
                }

                //default to 443 if the Host header doesn't contain the port (needed for reverse proxy setups)
                if (scheme == "https" && !request.HttpContext.Request.Host.Value.Contains(":"))
                {
                    port = 443;
                }

                serverUrl = string.Format("{0}://{1}:{2}{3}/", scheme, request.HttpContext.Request.Host.Host, port, BasePath());
            }

            return serverUrl;
        }

        public string GetBlackholeDirectory()
        {
            return config.BlackholeDir;
        }

        public string GetApiKey()
        {
            return config.APIKey;
        }
    }
}
