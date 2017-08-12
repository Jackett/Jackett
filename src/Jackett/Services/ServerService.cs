using Jackett.Models.Config;
using Jackett.Utils;
using Jackett.Utils.Clients;
using Microsoft.Owin.Hosting;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Web;

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
        Uri ConvertToProxyLink(Uri link, string serverUrl, string indexerId, string action = "dl", string file = "t.torrent");
        string BasePath();
        List<string> notices { get; }
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
        private IWebClient client;
        private IUpdateService updater;
        private List<string> _notices = new List<string>();
        IProtectionService protectionService;

        public ServerService(IIndexerManagerService i, IProcessService p, ISerializeService s, IConfigurationService c, Logger l, IWebClient w, IUpdateService u, IProtectionService protectionService)
        {
            indexerService = i;
            processService = p;
            serializeService = s;
            configService = c;
            logger = l;
            client = w;
            updater = u;
            this.protectionService = protectionService;

            LoadConfig();
            // "TEMPORARY" HACK
            protectionService.InstanceKey = Encoding.UTF8.GetBytes(Config.InstanceId);
        }

        public ServerConfig Config
        {
            get { return config; }
        }

        public List<string> notices
        {
            get
            {
                return _notices;
            }
        }

        public Uri ConvertToProxyLink(Uri link, string serverUrl, string indexerId, string action = "dl", string file = "t.torrent")
        {
            if (link == null || (link.IsAbsoluteUri && link.Scheme == "magnet"))
                return link;

            var encryptedLink = protectionService.Protect(link.ToString());
            var encodedLink = HttpServerUtility.UrlTokenEncode(Encoding.UTF8.GetBytes(encryptedLink));
            string urlEncodedFile = WebUtility.UrlEncode(file);
            var proxyLink = string.Format("{0}{1}/{2}/?jackett_apikey={3}&path={4}&file={5}", serverUrl, action, indexerId, config.APIKey, encodedLink, urlEncodedFile);
            return new Uri(proxyLink);
        }

        public string BasePath()
        {
            if (config.BasePathOverride == null || config.BasePathOverride == "")
            {
                return "/";
            }
            var path = config.BasePathOverride;
            if (!path.EndsWith("/"))
            {
                path = path + "/";
            }
            if (!path.StartsWith("/"))
            {
                path = "/" + path;
            }
            return path;
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
                    config.APIKey = StringUtil.GenerateRandom(32);

                configService.SaveConfig<ServerConfig>(config);
            }

            if (string.IsNullOrWhiteSpace(config.InstanceId))
            {
                config.InstanceId = StringUtil.GenerateRandom(64);
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

                Type monotype = Type.GetType("Mono.Runtime");
                if (monotype != null)
                {
                    MethodInfo displayName = monotype.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                    var monoVersion = "unknown";
                    if (displayName != null)
                        monoVersion = displayName.Invoke(null, null).ToString();
                    logger.Info("mono version: " + monoVersion);

                    var monoVersionO = new Version(monoVersion.Split(' ')[0]);

                    if (monoVersionO.Major < 4)
                    {
                        logger.Error("Your mono version is to old (mono 3 is no longer supported). Please update to the latest version from http://www.mono-project.com/download/");
                        Environment.Exit(2);
                    }
                    else if (monoVersionO.Major == 4 && monoVersionO.Minor == 2)
                    {
                        var notice = "mono version 4.2.* is known to cause problems with Jackett. If you experience any problems please try updating to the latest mono version from http://www.mono-project.com/download/ first.";
                        _notices.Add(notice);
                        logger.Error(notice);
                    }

                    try
                    {
                        // Check for mono-devel
                        // Is there any better way which doesn't involve a hard cashes?
                        var mono_devel_file = Path.Combine(runtimedir, "mono-api-info.exe");
                        if (!File.Exists(mono_devel_file))
                        {
                            var notice = "It looks like the mono-devel package is not installed, please make sure it's installed to avoid crashes.";
                            _notices.Add(notice);
                            logger.Error(notice);
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Error(e, "Error while checking for mono-devel");
                    }

                    try
                    {
                        // Check for ca-certificates-mono
                        var mono_cert_file = Path.Combine(runtimedir, "cert-sync.exe");
                        if (!File.Exists(mono_cert_file))
                        {
                            if ((monoVersionO.Major >= 4 && monoVersionO.Minor >= 8) || monoVersionO.Major >= 5)
                            {
                                var notice = "The ca-certificates-mono package is not installed, HTTPS trackers won't work. Please install it.";
                                _notices.Add(notice);
                                logger.Error(notice);
                            }
                            else
                            {
                                logger.Info("The ca-certificates-mono package is not installed, it will become mandatory once mono >= 4.8 is used.");
                            }

                        }
                    }
                    catch (Exception e)
                    {
                        logger.Error(e, "Error while checking for ca-certificates-mono");
                    }

                    try
                    {
                        Encoding.GetEncoding("windows-1255");
                    }
                    catch (NotSupportedException e)
                    {
                        logger.Debug(e);
                        logger.Error(e.Message + " Most likely the mono-locale-extras package is not installed.");
                        Environment.Exit(2);
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error("Error while getting environment details: " + e);
            }

            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            // Load indexers
            indexerService.InitIndexers(configService.GetCardigannDefinitionsFolders());
            client.Init();
            updater.CleanupTempDir();
        }

        public void Start()
        {
            // Start the server
            logger.Info("Starting web server at " + config.GetListenAddresses()[0]);
            var startOptions = new StartOptions();
            config.GetListenAddresses().ToList().ForEach(u => startOptions.Urls.Add(u));
            Startup.BasePath = BasePath();
            try
            {
                _server = WebApp.Start<Startup>(startOptions);
            }
            catch (TargetInvocationException e)
            {
                var inner = e.InnerException;
                if (inner is SocketException && ((SocketException)inner).SocketErrorCode == SocketError.AddressAlreadyInUse) // Linux (mono)
                {
                    logger.Error("Address already in use: Most likely Jackett is already running.");
                    Environment.Exit(1);
                }
                else if (inner is HttpListenerException && ((HttpListenerException)inner).ErrorCode == 183) // Windows
                {
                    logger.Error(inner.Message + " Most likely Jackett is already running.");
                    Environment.Exit(1);
                }
                throw e;
            }
            logger.Debug("Web server started");
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
    }
}
