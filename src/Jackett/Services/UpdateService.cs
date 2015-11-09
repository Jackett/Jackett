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
using System.Reflection;
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

        public UpdateService(Logger l, IWebClient c, IConfigurationService cfg)
        {
            logger = l;
            client = c;
            configService = cfg;
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

        private async Task CheckForUpdates()
        {
            var config = configService.GetConfig<ServerConfig>();
            if (config.UpdateDisabled)
            {
                logger.Info($"Skipping update check as it is disabled.");
                return;
            }

            var isWindows = System.Environment.OSVersion.Platform != PlatformID.Unix;
            if (Debugger.IsAttached)
            {
                logger.Info($"Skipping checking for new releases as the debugger is attached.");
                return;
            }

            try
            {
                var github = new GitHubClient(new ProductHeaderValue("Jackett"));
                if(config!=null && !string.IsNullOrWhiteSpace(config.GitHubUsername))
                {
                    github.Credentials = new Credentials(config.GitHubUsername, config.GitHubPassword);
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
                            var tempDir = await DownloadRelease(latestRelease.Name, isWindows);
                            // Copy updater
                            var installDir = Path.GetDirectoryName(ExePath());
                            var updaterPath = Path.Combine(tempDir, "Jackett", "JackettUpdater.exe");
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
        }

        private string GetCurrentVersion()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;
        }

        private async Task<string> DownloadRelease(string version, bool isWindows)
        {
            var downloadUrl = $"https://github.com/Jackett/Jackett/releases/download/{version}/";
           
            if (isWindows)
            {
                downloadUrl += "Jackett.Binaries.Windows.zip";
            }
            else
            {
                downloadUrl += "Jackett.Binaries.Mono.tar.gz";
            }

            var data = await client.GetBytes(new WebRequest() { Url = downloadUrl, EmulateBrowser = true, Type = RequestType.GET });

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
            var exe = Path.GetFileName(ExePath()).ToLowerInvariant();
            var args = string.Join(" ", Environment.GetCommandLineArgs().Skip(1));

            if (!isWindows)
            {
                // Wrap mono
                args = exe + " " + args;
                exe = "mono";
            }

            var startInfo = new ProcessStartInfo()
            {
                Arguments = $"--Path \"{installLocation}\" --Type \"{exe}\" --Args \"{args}\"",
                FileName = Path.Combine(updaterExePath)
            };

            var procInfo = Process.Start(startInfo);
            logger.Info($"Updater started process id: {procInfo.Id}");
            Environment.Exit(0);
        }
    }
}
