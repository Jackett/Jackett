using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using Jackett.Models.Config;
using Jackett.Utils.Clients;
using NLog;
using Octokit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Jackett.Services
{
    public interface IUpdateService
    {
        void StartUpdateChecker();
        void CheckForUpdatesNow();
    }

    public class UpdateService: IUpdateService
    {
        Logger logger;
        IWebClient client;
        IConfigurationService configService;
        ManualResetEvent locker = new ManualResetEvent(false);
        ITrayLockService lockService;
        bool forceupdatecheck = false;

        public UpdateService(Logger l, IWebClient c, IConfigurationService cfg, ITrayLockService ls)
        {
            logger = l;
            client = c;
            configService = cfg;
            lockService = ls;
        }

        private string ExePath()
        {
            var location = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase);
            return new FileInfo(location.AbsolutePath).FullName;
        }

        public void StartUpdateChecker()
        {
            Task.Factory.StartNew(UpdateWorkerThread);
        }

        public void CheckForUpdatesNow()
        {
            forceupdatecheck = true;
            locker.Set();
        }

        private async void UpdateWorkerThread()
        {
            while (true)
            {
                locker.WaitOne((int)new TimeSpan(24, 0, 0).TotalMilliseconds);
                locker.Reset();
                await CheckForUpdates();
            }
        }

        private bool AcceptCert(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private async Task CheckForUpdates()
        {
            var config = configService.GetConfig<ServerConfig>();
            if (config.UpdateDisabled && !forceupdatecheck)
            {
                logger.Info($"Skipping update check as it is disabled.");
                return;
            }

            forceupdatecheck = true;

            var isWindows = System.Environment.OSVersion.Platform != PlatformID.Unix;
            if (Debugger.IsAttached)
            {
                logger.Info($"Skipping checking for new releases as the debugger is attached.");
                return;
            }

            try
            {
                if (!isWindows)
                {
                    // Linux distros don't seem to like these certs.. todo local cert verification?
                    System.Net.ServicePointManager.ServerCertificateValidationCallback += AcceptCert;
                }

                var github = new GitHubClient(new ProductHeaderValue("Jackett"));
               
                if(config!=null && !string.IsNullOrWhiteSpace(config.GitHubToken))
                {
                    github.Credentials = new Credentials(config.GitHubToken);
                }

                var releases = await github.Release.GetAll("Jackett", "Jackett");
                if (releases.Count > 0)
                {
                    var latestRelease = releases.OrderByDescending(o => o.CreatedAt).First();
                    var currentVersion = $"v{GetCurrentVersion()}";

                    if (latestRelease.Name != currentVersion && currentVersion != "v0.0.0.0")
                    {
                        logger.Info($"New release found.  Current: {currentVersion} New: {latestRelease.Name}");
                        try
                        {
                            var assets = await github.Release.GetAllAssets("Jackett", "Jackett", latestRelease.Id);

                            var tempDir = await DownloadRelease(assets, isWindows,  latestRelease.Name, config.GitHubToken);
                            // Copy updater
                            var installDir = Path.GetDirectoryName(ExePath());
                            var updaterPath = Path.Combine(tempDir, "Jackett", "JackettUpdater.exe");
                            if (updaterPath != null)
                                StartUpdate(updaterPath, installDir, isWindows);
                        }
                        catch (Exception e)
                        {
                            logger.Error(e, "Error performing update.");
                        }
                    }
                    else
                    {
                        logger.Info($"Checked for a updated release but none was found.");
                    }
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error checking for updates.");
            }
            finally
            {
                if (!isWindows)
                {
                    System.Net.ServicePointManager.ServerCertificateValidationCallback -= AcceptCert;
                }
            }
        }

        private string GetCurrentVersion()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;
        }

        private WebRequest SetDownloadHeaders(WebRequest req, string token)
        {
            req.Headers = new Dictionary<string, string>()
            {
                { "Accept", "application/octet-stream" }
            };

            if (!string.IsNullOrWhiteSpace(token))
            {
                req.Headers.Add("Authorization", "token " + token);
            }

            return req;
        }

        private async Task<string> DownloadRelease(IReadOnlyList<ReleaseAsset> assets, bool isWindows, string version, string token)
        {
            var targetAsset = assets.Where(a => isWindows ? a.BrowserDownloadUrl.ToLowerInvariant().EndsWith(".zip") : a.BrowserDownloadUrl.ToLowerInvariant().EndsWith(".gz")).FirstOrDefault();

            if (targetAsset == null)
            {
                logger.Error("Failed to find asset to download!");
                return null;
            }

            var url = targetAsset.BrowserDownloadUrl;

            if (!string.IsNullOrWhiteSpace(token))
            {
                url = targetAsset.Url;
            }

            var data = await client.GetBytes(SetDownloadHeaders(new WebRequest() { Url = url, EmulateBrowser = true, Type = RequestType.GET }, token));

            while (data.IsRedirect)
            {
                data = await client.GetBytes(new WebRequest() { Url = data.RedirectingTo, EmulateBrowser = true, Type = RequestType.GET });
            }

            var tempDir = Path.Combine(Path.GetTempPath(), "JackettUpdate-" + version + "-" + DateTime.Now.Ticks);

            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }

            Directory.CreateDirectory(tempDir);

            if (isWindows)
            {
                var zipPath = Path.Combine(tempDir, "Update.zip");
                File.WriteAllBytes(zipPath, data.Content);
                var fastZip = new FastZip();
                fastZip.ExtractZip(zipPath, tempDir, null);
            }
            else
            {
                var gzPath = Path.Combine(tempDir, "Update.tar.gz");
                File.WriteAllBytes(gzPath, data.Content);
                Stream inStream = File.OpenRead(gzPath);
                Stream gzipStream = new GZipInputStream(inStream);

                TarArchive tarArchive = TarArchive.CreateInputTarArchive(gzipStream);
                tarArchive.ExtractContents(tempDir);
                tarArchive.Close();
                gzipStream.Close();
                inStream.Close();
            }

            return tempDir;
        }

        private void StartUpdate(string updaterExePath, string installLocation, bool isWindows)
        {
            logger.Info($"updaterExePath: {updaterExePath.ToString()}");
            logger.Info($"installLocation: {installLocation.ToString()}");
            logger.Info($"isWindows: {isWindows.ToString()}");

            var exe = Path.GetFileName(ExePath()).ToLowerInvariant();
            var args = string.Join(" ", Environment.GetCommandLineArgs().Skip(1));

            logger.Info($"exe: {exe.ToString()}");
            logger.Info($"args: {args.ToString()}");

            if (!isWindows)
            {
                // Wrap mono
                args = exe + " " + args;
                exe = "mono";

                logger.Info($"MONOargs: {args.ToString()}");
                logger.Info($"MONOexe: {exe.ToString()}");
            }

            var startInfo = new ProcessStartInfo()
            {
                Arguments = $"--Path \"{installLocation}\" --Type \"{exe}\" --Args \"{args}\"",
                FileName = Path.Combine(updaterExePath)
            };

            logger.Info($"startInfoArguments: {startInfo.Arguments.ToString()}");
            logger.Info($"startInfoFileName: {startInfo.FileName.ToString()}");

            var procInfo = Process.Start(startInfo);
            if (procInfo == null)
            {
                logger.Info($"procInfo is NULL");
            }
            else
            {
                logger.Info($"procInfo: {procInfo.ToString()}");
            }
            
            logger.Info($"Updater started process id: {procInfo.Id}");
            logger.Info("Exiting Jackett..");
            lockService.Signal();
            Environment.Exit(0);
        }
    }
}
