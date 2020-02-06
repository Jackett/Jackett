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
using WebClient = Jackett.Common.Utils.Clients.WebClient;

namespace Jackett.Server.Services
{
    public class ServerService : IServerService
    {
        private readonly IIndexerManagerService _indexerService;
        private readonly IProcessService _processService;
        private readonly ISerializeService _serializeService;
        private readonly IConfigurationService _configService;
        private readonly Logger _logger;
        private readonly WebClient _client;
        private readonly IUpdateService _updater;
        private readonly ServerConfig _config;
        private readonly IProtectionService _protectionService;
        private bool _isDotNetCoreCapable;

        public ServerService(IIndexerManagerService i, IProcessService p, ISerializeService s, IConfigurationService c,
                             Logger l, WebClient w, IUpdateService u, IProtectionService protectionService,
                             ServerConfig serverConfig)
        {
            _indexerService = i;
            _processService = p;
            _serializeService = s;
            _configService = c;
            _logger = l;
            _client = w;
            _updater = u;
            _config = serverConfig;
            _protectionService = protectionService;
        }

        public List<string> notices { get; } = new List<string>();

        public Uri ConvertToProxyLink(Uri link, string serverUrl, string indexerId, string action = "dl", string file = "t")
        {
            if (link == null || (link.IsAbsoluteUri && link.Scheme == "magnet" && action != "bh")
                ) // no need to convert a magnet link to a proxy link unless it's a blackhole link
                return link;
            var encryptedLink = _protectionService.Protect(link.ToString());
            var encodedLink = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(encryptedLink));
            var urlEncodedFile = WebUtility.UrlEncode(file);
            var proxyLink = string.Format(
                "{0}{1}/{2}/?jackett_apikey={3}&path={4}&file={5}", serverUrl, action, indexerId, _config.APIKey, encodedLink,
                urlEncodedFile);
            return new Uri(proxyLink);
        }

        public string BasePath()
        {
            if (_config.BasePathOverride == null || _config.BasePathOverride == "")
                return "";
            var path = _config.BasePathOverride;
            if (path.EndsWith("/"))
                path = path.TrimEnd('/');
            if (!path.StartsWith("/"))
                path = $"/{path}";
            return path;
        }

        public void Initalize()
        {
            try
            {
                var x = Environment.OSVersion;
                var runtimedir = RuntimeEnvironment.GetRuntimeDirectory();
                _logger.Info($"Environment version: {Environment.Version} ({runtimedir})");
                _logger.Info(
                    $"OS version: {Environment.OSVersion}{(Environment.Is64BitOperatingSystem ? " (64bit OS)" : "")}{(Environment.Is64BitProcess ? " (64bit process)" : "")}");
                var variants = new Variants();
                var variant = variants.GetVariant();
                _logger.Info($"Jackett variant: {variant}");
                try
                {
                    ThreadPool.GetMaxThreads(out var workerThreads, out var completionPortThreads);
                    _logger.Info(
                        $"ThreadPool MaxThreads: {workerThreads} workerThreads, {completionPortThreads} completionPortThreads");
                }
                catch (Exception e)
                {
                    _logger.Error($"Error while getting MaxThreads details: {e}");
                }

                _logger.Info($"App config/log directory: {_configService.GetAppDataFolder()}");
                try
                {
                    var issuefile = "/etc/issue";
                    if (File.Exists(issuefile))
                        using (var reader = new StreamReader(issuefile))
                        {
                            var firstLine = reader.ReadLine();
                            if (firstLine != null)
                                _logger.Info($"issue: {firstLine}");
                        }
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Error while reading the issue file");
                }

                var monotype = Type.GetType("Mono.Runtime");
                if (monotype != null && !DotNetCoreUtil.IsRunningOnDotNetCore)
                {
                    var displayName = monotype.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                    var monoVersion = "unknown";
                    if (displayName != null)
                        monoVersion = displayName.Invoke(null, null).ToString();
                    _logger.Info($"mono version: {monoVersion}");
                    var monoVersionO = new Version(monoVersion.Split(' ')[0]);
                    if (monoVersionO.Major < 5 || (monoVersionO.Major == 5 && monoVersionO.Minor < 8))
                    {
                        //Hard minimum of 5.8
                        //5.4 throws a SIGABRT, looks related to this which was fixed in 5.8 https://bugzilla.xamarin.com/show_bug.cgi?id=60625
                        _logger.Error(
                            "Your mono version is too old. Please update to the latest version from http://www.mono-project.com/download/");
                        Environment.Exit(2);
                    }

                    if (monoVersionO.Major < 5 || (monoVersionO.Major == 5 && monoVersionO.Minor < 8))
                    {
                        var notice =
                            "A minimum Mono version of 5.8 is required. Please update to the latest version from http://www.mono-project.com/download/";
                        notices.Add(notice);
                        _logger.Error(notice);
                    }

                    try
                    {
                        // Check for mono-devel
                        // Is there any better way which doesn't involve a hard cashes?
                        var monoDevelFile = Path.Combine(runtimedir, "mono-api-info.exe");
                        if (!File.Exists(monoDevelFile))
                        {
                            var notice =
                                "It looks like the mono-devel package is not installed, please make sure it's installed to avoid crashes.";
                            notices.Add(notice);
                            _logger.Error(notice);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "Error while checking for mono-devel");
                    }

                    try
                    {
                        // Check for ca-certificates-mono
                        var monoCertFile = Path.Combine(runtimedir, "cert-sync.exe");
                        if (!File.Exists(monoCertFile))
                        {
                            var notice =
                                "The ca-certificates-mono package is not installed, HTTPS trackers won't work. Please install it.";
                            notices.Add(notice);
                            _logger.Error(notice);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "Error while checking for ca-certificates-mono");
                    }

                    try
                    {
                        Encoding.GetEncoding("windows-1255");
                    }
                    catch (NotSupportedException e)
                    {
                        _logger.Debug(e);
                        _logger.Error($"{e.Message} Most likely the mono-locale-extras package is not installed.");
                        Environment.Exit(2);
                    }

                    // check if the certificate store was initialized using Mono.Security.X509.X509StoreManager.TrustedRootCertificates.Count
                    try
                    {
                        var monoSecurity = Assembly.Load("Mono.Security");
                        var monoX509StoreManager = monoSecurity.GetType("Mono.Security.X509.X509StoreManager");
                        if (monoX509StoreManager != null)
                        {
                            var trustedRootCertificatesProperty =
                                monoX509StoreManager.GetProperty("TrustedRootCertificates");
                            var trustedRootCertificates = (ICollection)trustedRootCertificatesProperty.GetValue(null);
                            _logger.Info($"TrustedRootCertificates count: {trustedRootCertificates.Count}");
                            if (trustedRootCertificates.Count == 0)
                            {
                                var caCertificatesFiles = new[]
                                {
                                    "/etc/ssl/certs/ca-certificates.crt", // Debian based
                                    "/etc/pki/tls/certs/ca-bundle.c", // RedHat based
                                    "/etc/ssl/ca-bundle.pem" // SUSE
                                };
                                var notice = "The mono certificate store is not initialized.<br/>\n";
                                var logSpacer = "                     ";
                                var caCertificatesFile = caCertificatesFiles.Where(f => File.Exists(f)).FirstOrDefault();
                                var commandRoot = "curl -sS https://curl.haxx.se/ca/cacert.pem | cert-sync /dev/stdin";
                                var commandUser =
                                    "curl -sS https://curl.haxx.se/ca/cacert.pem | cert-sync --user /dev/stdin";
                                if (caCertificatesFile != null)
                                {
                                    commandRoot = $"cert-sync {caCertificatesFile}";
                                    commandUser = $"cert-sync --user {caCertificatesFile}";
                                }

                                notice += $"{logSpacer}Please run the following command as root:<br/>\n";
                                notice += $"{logSpacer}<pre>{commandRoot}</pre><br/>\n";
                                notice +=
                                    $"{logSpacer}If you don't have root access or you're running MacOS, please run the following command as the jackett user ({Environment.UserName}):<br/>\n";
                                notice += $"{logSpacer}<pre>{commandUser}</pre>";
                                notices.Add(notice);
                                _logger.Error(Regex.Replace(notice, "<.*?>", string.Empty));
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "Error while chekcing the mono certificate store");
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Error while getting environment details: {e}");
            }

            try
            {
                if (Environment.UserName == "root")
                {
                    var notice = "Jackett is running with root privileges. You should run Jackett as an unprivileged user.";
                    notices.Add(notice);
                    _logger.Error(notice);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error while checking the username");
            }

            //Warn user that they are using an old version of Jackett
            try
            {
                var compiledData = BuildDate.GetBuildDateTime();
                if (compiledData < DateTime.Now.AddMonths(-3))
                {
                    var version = _configService.GetVersion();
                    var notice =
                        $"Your version of Jackett v{version} is very old. Multiple indexers are likely to fail when using an old version. Update to the latest version of Jackett.";
                    notices.Add(notice);
                    _logger.Error(notice);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error while checking build date of Jackett.Common");
            }

            //Alert user that they no longer need to use Mono
            try
            {
                var variants = new Variants();
                var variant = variants.GetVariant();
                if (variant == Variants.JackettVariant.Mono)
                {
                    var process = new Process();
                    process.StartInfo.FileName = "uname";
                    process.StartInfo.Arguments = "-m";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.Start();
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    _logger.Debug($"uname output was: {output}");
                    output = output.ToLower();
                    if (output.Contains("armv7") || output.Contains("armv8") || output.Contains("x86_64"))
                        _isDotNetCoreCapable = true;
                }
            }
            catch (Exception e)
            {
                _logger.Debug(e, "Unable to get architecture");
            }

            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
            // Load indexers
            _indexerService.InitIndexers(_configService.GetCardigannDefinitionsFolders());
            _client.Init();
            _updater.CleanupTempDir();
        }

        public void Start() => _updater.StartUpdateChecker();

        public void ReserveUrls(bool doInstall = true)
        {
            _logger.Debug("Unreserving Urls");
            _config.GetListenAddresses(false).ToList().ForEach(u => RunNetSh(string.Format("http delete urlacl {0}", u)));
            _config.GetListenAddresses(true).ToList().ForEach(u => RunNetSh(string.Format("http delete urlacl {0}", u)));
            if (doInstall)
            {
                _logger.Debug("Reserving Urls");
                _config.GetListenAddresses(true).ToList().ForEach(
                    u => RunNetSh(string.Format("http add urlacl {0} sddl=D:(A;;GX;;;S-1-1-0)", u)));
                _logger.Debug("Urls reserved");
            }
        }

        private void RunNetSh(string args) => _processService.StartProcessAndLog("netsh.exe", args);

        public void Stop()
        {
            // Only needed for Owin
        }

        public string GetServerUrl(HttpRequest request)
        {
            var serverUrl = "";
            var scheme = request.Scheme;
            var port = request.HttpContext.Request.Host.Port;

            // Check for protocol headers added by reverse proxys
            // X-Forwarded-Proto: A de facto standard for identifying the originating protocol of an HTTP request
            var xForwardedProto = request.Headers.Where(x => x.Key == "X-Forwarded-Proto").Select(x => x.Value)
                                           .FirstOrDefault();
            if (xForwardedProto.Count > 0)
                scheme = xForwardedProto.First();
            // Front-End-Https: Non-standard header field used by Microsoft applications and load-balancers
            else if (request.Headers.Where(x => x.Key == "Front-End-Https" && x.Value.FirstOrDefault() == "on").Any())
                scheme = "https";

            //default to 443 if the Host header doesn't contain the port (needed for reverse proxy setups)
            if (scheme == "https" && !request.HttpContext.Request.Host.Value.Contains(":"))
                port = 443;
            serverUrl = string.Format("{0}://{1}:{2}{3}/", scheme, request.HttpContext.Request.Host.Host, port, BasePath());
            return serverUrl;
        }

        public string GetBlackholeDirectory() => _config.BlackholeDir;

        public string GetApiKey() => _config.APIKey;

        public bool MonoUserCanRunNetCore() => _isDotNetCoreCapable;
    }
}
