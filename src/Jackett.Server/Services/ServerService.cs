using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using NLog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Jackett.Server.Services
{
    public class ServerService : IServerService
    {
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
            if (link == null || (link.IsAbsoluteUri && link.Scheme == "magnet" && action != "bh")) // no need to convert a magnet link to a proxy link unless it's a blackhole link
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
            try
            {
                var x = Environment.OSVersion;
                var runtimedir = RuntimeEnvironment.GetRuntimeDirectory();
                logger.Info("Environment version: " + Environment.Version.ToString() + " (" + runtimedir + ")");
                logger.Info("OS version: " + Environment.OSVersion.ToString() + (Environment.Is64BitOperatingSystem ? " (64bit OS)" : "") + (Environment.Is64BitProcess ? " (64bit process)" : ""));

                try
                {
                    ThreadPool.GetMaxThreads(out int workerThreads, out int completionPortThreads);
                    logger.Info("ThreadPool MaxThreads: " + workerThreads + " workerThreads, " + completionPortThreads + " completionPortThreads");
                }
                catch (Exception e)
                {
                    logger.Error("Error while getting MaxThreads details: " + e);
                }

                logger.Info("App config/log directory: " + configService.GetAppDataFolder());

                try
                {
                    var issuefile = "/etc/issue";
                    if (File.Exists(issuefile))
                    {
                        using (StreamReader reader = new StreamReader(issuefile))
                        {
                            string firstLine = reader.ReadLine();
                            if (firstLine != null)
                                logger.Info("issue: " + firstLine);
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.Error(e, "Error while reading the issue file");
                }

                bool runningOnDotNetCore = RuntimeInformation.FrameworkDescription.IndexOf("Core", StringComparison.OrdinalIgnoreCase) >= 0;

                Type monotype = Type.GetType("Mono.Runtime");
                if (monotype != null && !runningOnDotNetCore)
                {
                    MethodInfo displayName = monotype.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                    var monoVersion = "unknown";
                    if (displayName != null)
                        monoVersion = displayName.Invoke(null, null).ToString();
                    logger.Info("mono version: " + monoVersion);

                    var monoVersionO = new Version(monoVersion.Split(' ')[0]);

                    if (monoVersionO.Major < 5 || (monoVersionO.Major == 5 && monoVersionO.Minor < 8))
                    {
                        //Hard minimum of 5.8
                        //5.4 throws a SIGABRT, looks related to this which was fixed in 5.8 https://bugzilla.xamarin.com/show_bug.cgi?id=60625

                        logger.Error("Your mono version is too old. Please update to the latest version from http://www.mono-project.com/download/");
                        Environment.Exit(2);
                    }

                    if (monoVersionO.Major < 5 || (monoVersionO.Major == 5 && monoVersionO.Minor < 8))
                    {
                        string notice = "A minimum Mono version of 5.8 is required. Please update to the latest version from http://www.mono-project.com/download/";
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
                            var notice = "The ca-certificates-mono package is not installed, HTTPS trackers won't work. Please install it.";
                            _notices.Add(notice);
                            logger.Error(notice);
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

                    // check if the certificate store was initialized using Mono.Security.X509.X509StoreManager.TrustedRootCertificates.Count
                    try
                    {
                        var monoSecurity = Assembly.Load("Mono.Security");
                        Type monoX509StoreManager = monoSecurity.GetType("Mono.Security.X509.X509StoreManager");
                        if (monoX509StoreManager != null)
                        {
                            var TrustedRootCertificatesProperty = monoX509StoreManager.GetProperty("TrustedRootCertificates");
                            var TrustedRootCertificates = (ICollection)TrustedRootCertificatesProperty.GetValue(null);

                            logger.Info("TrustedRootCertificates count: " + TrustedRootCertificates.Count);

                            if (TrustedRootCertificates.Count == 0)
                            {
                                var CACertificatesFiles = new string[] {
                                    "/etc/ssl/certs/ca-certificates.crt", // Debian based
                                    "/etc/pki/tls/certs/ca-bundle.c", // RedHat based
                                    "/etc/ssl/ca-bundle.pem", // SUSE
                                    };

                                var notice = "The mono certificate store is not initialized.<br/>\n";
                                var logSpacer = "                     ";
                                var CACertificatesFile = CACertificatesFiles.Where(f => File.Exists(f)).FirstOrDefault();
                                var CommandRoot = "curl -sS https://curl.haxx.se/ca/cacert.pem | cert-sync /dev/stdin";
                                var CommandUser = "curl -sS https://curl.haxx.se/ca/cacert.pem | cert-sync --user /dev/stdin";
                                if (CACertificatesFile != null)
                                {
                                    CommandRoot = "cert-sync " + CACertificatesFile;
                                    CommandUser = "cert-sync --user " + CACertificatesFile;
                                }
                                notice += logSpacer + "Please run the following command as root:<br/>\n";
                                notice += logSpacer + "<pre>" + CommandRoot + "</pre><br/>\n";
                                notice += logSpacer + "If you don't have root access or you're running MacOS, please run the following command as the jackett user (" + Environment.UserName + "):<br/>\n";
                                notice += logSpacer + "<pre>" + CommandUser + "</pre>";
                                _notices.Add(notice);
                                logger.Error(Regex.Replace(notice, "<.*?>", String.Empty));
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Error(e, "Error while chekcing the mono certificate store");
                    }
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
                config.GetListenAddresses(true).ToList().ForEach(u => RunNetSh(string.Format("http add urlacl {0} sddl=D:(A;;GX;;;S-1-1-0)", u)));
                logger.Debug("Urls reserved");
            }
        }

        private void RunNetSh(string args)
        {
            processService.StartProcessAndLog("netsh.exe", args);
        }

        public void Stop()
        {
            // Only needed for Owin
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
