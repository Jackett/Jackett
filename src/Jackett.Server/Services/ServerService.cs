using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using NLog;

namespace Jackett.Server.Services
{
    public class ServerService : IServerService
    {
        private readonly IIndexerManagerService indexerService;
        private readonly IProcessService processService;
        private readonly ISerializeService serializeService;
        private readonly IConfigurationService configService;
        private readonly Logger logger;
        private readonly Common.Utils.Clients.WebClient client;
        private readonly IUpdateService updater;
        private readonly ServerConfig config;
        private readonly IProtectionService _protectionService;
        private bool isDotNetCoreCapable;

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

        public List<string> notices { get; } = new List<string>();

        public Uri ConvertToProxyLink(Uri link, string serverUrl, string indexerId, string action = "dl", string file = "t")
        {
            // no need to convert a magnet link to a proxy link unless it's a blackhole link
            if (link == null || (link.IsAbsoluteUri && link.Scheme == "magnet" && action != "bh"))
                return link;

            var encryptedLink = _protectionService.Protect(link.ToString());
            var encodedLink = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(encryptedLink));
            var urlEncodedFile = WebUtility.UrlEncode(file);
            var proxyLink = $"{serverUrl}{action}/{indexerId}/?jackett_apikey={config.APIKey}&path={encodedLink}&file={urlEncodedFile}";
            return new Uri(proxyLink);
        }

        public string BasePath()
        {
            if (string.IsNullOrEmpty(config.BasePathOverride))
                return "";
            var path = config.BasePathOverride;
            if (path.EndsWith("/"))
                path = path.TrimEnd('/');
            if (!path.StartsWith("/"))
                path = "/" + path;
            return path;
        }

        public void Initalize()
        {
            try
            {
                var x = Environment.OSVersion;
                var runtimedir = RuntimeEnvironment.GetRuntimeDirectory();
                logger.Info($"Environment version: {Environment.Version} ({runtimedir})");
                logger.Info($"OS version: {Environment.OSVersion}" +
                    (Environment.Is64BitOperatingSystem ? " (64bit OS)" : "") +
                    (Environment.Is64BitProcess ? " (64bit process)" : ""));
                var variants = new Variants();
                var variant = variants.GetVariant();
                logger.Info($"Jackett variant: {variant}");

                try
                {
                    var issueFile = "/etc/issue";
                    if (File.Exists(issueFile))
                        using (var reader = new StreamReader(issueFile))
                        {
                            var firstLine = reader.ReadLine();
                            if (firstLine != null)
                                logger.Info($"File {issueFile}: {firstLine}");
                        }
                }
                catch (Exception e)
                {
                    logger.Error($"Error while reading the issue file\n{e}");
                }

                try
                {
                    var dockerMsg = "No";
                    const string cgroupFile = "/proc/1/cgroup";
                    if (File.Exists(cgroupFile) && File.ReadAllText(cgroupFile).Contains("/docker/"))
                    {
                        // this file is created in the Docker image build
                        // https://github.com/linuxserver/docker-jackett/pull/105
                        const string dockerImageFile = "/etc/docker-image";
                        dockerMsg = File.Exists(dockerImageFile)
                            ? "Yes (image build: " + File.ReadAllText(dockerImageFile).Trim() + ")"
                            : "Yes (image build: unknown)";
                    }
                    logger.Info($"Running in Docker: {dockerMsg}");
                }
                catch (Exception e)
                {
                    logger.Error($"Error while reading the Docker cgroup file.\n{e}");
                }

                try
                {
                    ThreadPool.GetMaxThreads(out var workerThreads, out var completionPortThreads);
                    logger.Info(
                        "ThreadPool MaxThreads: " + workerThreads + " workerThreads, " + completionPortThreads +
                        " completionPortThreads");
                }
                catch (Exception e)
                {
                    logger.Error("Error while getting MaxThreads details: " + e);
                }

                logger.Info("App config/log directory: " + configService.GetAppDataFolder());

                logger.Info($"Using proxy: {config.ProxyType}");

                logger.Info("Using FlareSolverr: " + (string.IsNullOrEmpty(config.FlareSolverrUrl) ? "No" : config.FlareSolverrUrl));

                var monotype = Type.GetType("Mono.Runtime");
                if (monotype != null && !DotNetCoreUtil.IsRunningOnDotNetCore)
                {
                    var displayName = monotype.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                    var monoVersion = "unknown";
                    if (displayName != null)
                        monoVersion = displayName.Invoke(null, null).ToString();
                    logger.Info("Mono version: " + monoVersion);

                    var monoVersionO = new Version(monoVersion.Split(' ')[0]);

                    if (monoVersionO.Major < 5 || (monoVersionO.Major == 5 && monoVersionO.Minor < 8))
                    {
                        //Hard minimum of 5.8
                        //5.4 throws a SIGABRT, looks related to this which was fixed in 5.8 https://bugzilla.xamarin.com/show_bug.cgi?id=60625
                        logger.Error(
                            "Your Mono version is too old. Please update to the latest version from http://www.mono-project.com/download/");
                        Environment.Exit(2);
                    }

                    if (monoVersionO.Major < 5 || (monoVersionO.Major == 5 && monoVersionO.Minor < 8))
                    {
                        const string notice = "A minimum Mono version of 5.8 is required. Please update to the latest version from http://www.mono-project.com/download/";
                        notices.Add(notice);
                        logger.Error(notice);
                    }

                    try
                    {
                        // Check for mono-devel
                        // Is there any better way which doesn't involve a hard cashes?
                        var monoDevelFile = Path.Combine(runtimedir, "mono-api-info.exe");
                        if (!File.Exists(monoDevelFile))
                        {
                            const string notice = "It looks like the mono-devel package is not installed, please make sure it's installed to avoid crashes.";
                            notices.Add(notice);
                            logger.Error(notice);
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Error while checking for mono-devel.\n{e}");
                    }

                    try
                    {
                        // Check for ca-certificates-mono
                        var monoCertFile = Path.Combine(runtimedir, "cert-sync.exe");
                        if (!File.Exists(monoCertFile))
                        {
                            const string notice = "The ca-certificates-mono package is not installed, HTTPS trackers won't work. Please install it.";
                            notices.Add(notice);
                            logger.Error(notice);
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Error while checking for ca-certificates-mono.\n{e}");
                    }

                    try
                    {
                        Encoding.GetEncoding("windows-1255");
                    }
                    catch (NotSupportedException e)
                    {
                        logger.Error($"Most likely the mono-locale-extras package is not installed.\n{e}");
                        Environment.Exit(2);
                    }

                    if (monoVersionO.Major < 6)
                    {
                        //We don't check on Mono 6 since Mono.Security was removed
                        // check if the certificate store was initialized using Mono.Security.X509.X509StoreManager.TrustedRootCertificates.Count
                        try
                        {
                            var monoSecurity = Assembly.Load("Mono.Security");
                            var monoX509StoreManager = monoSecurity.GetType("Mono.Security.X509.X509StoreManager");
                            if (monoX509StoreManager != null)
                            {
                                var trustedRootCertificatesProperty = monoX509StoreManager.GetProperty("TrustedRootCertificates");
                                var trustedRootCertificates = (ICollection)trustedRootCertificatesProperty.GetValue(null);

                                logger.Info($"TrustedRootCertificates count: {trustedRootCertificates.Count}");

                                if (trustedRootCertificates.Count == 0)
                                {
                                    var caCertificatesFiles = new[]
                                    {
                                        "/etc/ssl/certs/ca-certificates.crt", // Debian based
                                        "/etc/pki/tls/certs/ca-bundle.c", // RedHat based
                                        "/etc/ssl/ca-bundle.pem", // SUSE
                                    };

                                    const string logSpacer = "                     ";
                                    var notice = "The mono certificate store is not initialized.<br/>\n";
                                    var caCertificatesFile = caCertificatesFiles.Where(File.Exists).FirstOrDefault();
                                    var commandRoot = "curl -sS https://curl.haxx.se/ca/cacert.pem | cert-sync /dev/stdin";
                                    var commandUser = "curl -sS https://curl.haxx.se/ca/cacert.pem | cert-sync --user /dev/stdin";
                                    if (caCertificatesFile != null)
                                    {
                                        commandRoot = "cert-sync " + caCertificatesFile;
                                        commandUser = "cert-sync --user " + caCertificatesFile;
                                    }

                                    notice += logSpacer + "Please run the following command as root:<br/>\n";
                                    notice += logSpacer + "<pre>" + commandRoot + "</pre><br/>\n";
                                    notice += logSpacer +
                                              "If you don't have root access or you're running MacOS, please run the following command as the jackett user (" +
                                              Environment.UserName + "):<br/>\n";
                                    notice += logSpacer + "<pre>" + commandUser + "</pre>";
                                    notices.Add(notice);
                                    logger.Error(Regex.Replace(notice, "<.*?>", string.Empty));
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            logger.Error($"Error while chekcing the mono certificate store.\n{e}");
                        }
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
                    notices.Add(notice);
                    logger.Error(notice);
                }
            }
            catch (Exception e)
            {
                logger.Error($"Error while checking the username.\n{e}");
            }

            //Warn user that they are using an old version of Jackett
            try
            {
                var compiledData = BuildDate.GetBuildDateTime();

                if (compiledData < DateTime.Now.AddMonths(-3))
                {
                    var version = configService.GetVersion();
                    var notice = $"Your version of Jackett {version} is very old. Multiple indexers are likely to fail when using an old version. Update to the latest version of Jackett.";
                    notices.Add(notice);
                    logger.Error(notice);
                }
            }
            catch (Exception e)
            {
                logger.Error($"Error while checking build date of Jackett.Common.\n{e}");
            }

            //Alert user that they no longer need to use Mono
            try
            {
                var variants = new Variants();
                var variant = variants.GetVariant();

                if (variant == Variants.JackettVariant.Mono)
                {
                    var process = new Process
                    {
                        StartInfo =
                        {
                            FileName = "uname",
                            Arguments = "-m",
                            UseShellExecute = false,
                            RedirectStandardOutput = true
                        }
                    };
                    process.Start();
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    logger.Debug($"uname output was: {output}");

                    output = output.ToLower();
                    if (output.Contains("armv7") || output.Contains("armv8") || output.Contains("x86_64"))
                        isDotNetCoreCapable = true;
                }
            }
            catch (Exception e)
            {
                logger.Debug($"Unable to get architecture.\n{e}");
            }

            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");

            // Load indexers
            indexerService.InitIndexers(configService.GetCardigannDefinitionsFolders());
            client.Init();

            updater.CleanupTempDir();
            updater.CheckUpdaterLock();
        }

        public void Start() => updater.StartUpdateChecker();

        public void ReserveUrls(bool doInstall = true)
        {
            logger.Debug("Unreserving Urls");
            config.GetListenAddresses(false).ToList().ForEach(u => RunNetSh($"http delete urlacl {u}"));
            config.GetListenAddresses(true).ToList().ForEach(u => RunNetSh($"http delete urlacl {u}"));
            if (doInstall)
            {
                logger.Debug("Reserving Urls");
                config.GetListenAddresses(true).ToList().ForEach(u => RunNetSh($"http add urlacl {u} sddl=D:(A;;GX;;;S-1-1-0)"));
                logger.Debug("Urls reserved");
            }
        }

        private void RunNetSh(string args) => processService.StartProcessAndLog("netsh.exe", args);

        public void Stop()
        {
            // Only needed for Owin
        }

        public string GetServerUrl(HttpRequest request)
        {
            var scheme = request.Scheme;
            var port = request.HttpContext.Request.Host.Port;

            // Check for protocol headers added by reverse proxys
            // X-Forwarded-Proto: A de facto standard for identifying the originating protocol of an HTTP request
            var xForwardedProto = request.Headers.Where(x => x.Key == "X-Forwarded-Proto").Select(x => x.Value).FirstOrDefault();
            if (xForwardedProto.Count > 0)
                scheme = xForwardedProto.First();
            // Front-End-Https: Non-standard header field used by Microsoft applications and load-balancers
            else if (request.Headers.Where(x => x.Key == "Front-End-Https" && x.Value.FirstOrDefault() == "on").Any())
                scheme = "https";

            //default to 443 if the Host header doesn't contain the port (needed for reverse proxy setups)
            if (scheme == "https" && !request.HttpContext.Request.Host.Value.Contains(":"))
                port = 443;

            return $"{scheme}://{request.HttpContext.Request.Host.Host}:{port}{BasePath()}/";
        }

        public string GetBlackholeDirectory() => config.BlackholeDir;

        public string GetApiKey() => config.APIKey;

        public bool MonoUserCanRunNetCore() => isDotNetCoreCapable;
    }
}
