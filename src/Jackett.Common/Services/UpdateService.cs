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
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.Zip;
using Jackett.Common.Models.Config;
using Jackett.Common.Models.GitHub;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Jackett.Common.Utils.Clients;
using Newtonsoft.Json;
using NLog;

namespace Jackett.Common.Services
{
    public class UpdateService : IUpdateService
    {
        private readonly Logger logger;
        private readonly WebClient client;
        private readonly ManualResetEvent locker = new ManualResetEvent(false);
        private readonly ITrayLockService lockService;
        private readonly IServiceConfigService windowsService;
        private readonly IFilePermissionService filePermissionService;
        private readonly ServerConfig serverConfig;
        private bool forceUpdateCheck; // false by default
        private Variants.JackettVariant variant;

        public UpdateService(Logger l, WebClient c, ITrayLockService ls, IServiceConfigService ws, IFilePermissionService fps, ServerConfig sc)
        {
            logger = l;
            client = c;
            lockService = ls;
            windowsService = ws;
            serverConfig = sc;
            filePermissionService = fps;

            variant = new Variants().GetVariant();
        }

        public void StartUpdateChecker() => Task.Factory.StartNew(UpdateWorkerThread);

        public void CheckForUpdatesNow()
        {
            forceUpdateCheck = true;
            locker.Set();
        }

        private async void UpdateWorkerThread()
        {
            var delayHours = 1; // first check after 1 hour (for users not running jackett 24/7)
            while (true)
            {
                locker.WaitOne((int)new TimeSpan(delayHours, 0, 0).TotalMilliseconds);
                locker.Reset();
                await CheckForUpdates();
                delayHours = 24; // following checks only once/24 hours
            }
        }

        private bool AcceptCert(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true;

        private async Task CheckForUpdates()
        {
            logger.Info($"Checking for updates... Jackett variant: {variant}");

            if (serverConfig.RuntimeSettings.NoUpdates)
            {
                logger.Info("Updates are disabled via --NoUpdates.");
                return;
            }
            if (serverConfig.UpdateDisabled && !forceUpdateCheck)
            {
                logger.Info("Skipping update check as it is disabled.");
                return;
            }
            forceUpdateCheck = false; // Used when updates are disabled and the user click update button
            if (Debugger.IsAttached)
            {
                logger.Info("Skipping checking for new releases as the debugger is attached.");
                return;
            }
            var currentVersion = EnvironmentUtil.JackettVersion();
            if (currentVersion == "v0.0.0")
            {
                logger.Info("Skipping checking for new releases because Jackett is runing in IDE.");
                return;
            }

            var isWindows = Environment.OSVersion.Platform != PlatformID.Unix;

            var trayIsRunning = false;
            if (isWindows)
                trayIsRunning = Process.GetProcessesByName("JackettTray").Length > 0;

            try
            {
                var response = await client.GetResultAsync(new WebRequest()
                {
                    Url = "https://api.github.com/repos/Jackett/Jackett/releases",
                    Encoding = Encoding.UTF8,
                    EmulateBrowser = false
                });

                if (response.Status != System.Net.HttpStatusCode.OK)
                    logger.Error($"Failed to get the release list: {response.Status}");

                var releases = JsonConvert.DeserializeObject<List<Release>>(response.ContentString);

                if (!serverConfig.UpdatePrerelease)
                    releases = releases.Where(r => !r.Prerelease).ToList();

                if (releases.Count > 0)
                {
                    var latestRelease = releases.OrderByDescending(o => o.Created_at).First();
                    if (latestRelease.Name != currentVersion)
                    {
                        logger.Info($"New release found. Current version: {currentVersion} New version: {latestRelease.Name}");
                        logger.Info($"Downloading release {latestRelease.Name} It could take a while...");
                        try
                        {
                            var tempDir = await DownloadRelease(latestRelease.Assets, isWindows, latestRelease.Name);
                            // Copy updater
                            var installDir = EnvironmentUtil.JackettInstallationPath();
                            var updaterPath = GetUpdaterPath(tempDir);
                            if (updaterPath != null)
                                StartUpdate(updaterPath, installDir, isWindows, serverConfig.RuntimeSettings.NoRestart, trayIsRunning);
                        }
                        catch (Exception e)
                        {
                            logger.Error($"Error performing update.\n{e}");
                        }
                    }
                    else
                        logger.Info($"Jackett is already updated. Current version: {currentVersion}");
                }
            }
            catch (Exception e)
            {
                logger.Error($"Error checking for updates.\n{e}");
            }
            finally
            {
                if (!isWindows)
                    System.Net.ServicePointManager.ServerCertificateValidationCallback -= AcceptCert;
            }
        }

        private string GetUpdaterPath(string tempDirectory) =>
            variant == Variants.JackettVariant.CoreMacOs || variant == Variants.JackettVariant.CoreLinuxAmdx64 ||
            variant == Variants.JackettVariant.CoreLinuxArm32 || variant == Variants.JackettVariant.CoreLinuxArm64
                ? Path.Combine(tempDirectory, "Jackett", "JackettUpdater")
                : Path.Combine(tempDirectory, "Jackett", "JackettUpdater.exe");

        private WebRequest SetDownloadHeaders(WebRequest req)
        {
            req.Headers = new Dictionary<string, string>
            {
                { "Accept", "application/octet-stream" }
            };
            return req;
        }

        public void CleanupTempDir()
        {
            var tempDir = Path.GetTempPath();

            if (!Directory.Exists(tempDir))
            {
                logger.Error($"Temp dir doesn't exist: {tempDir}");
                return;
            }

            try
            {
                var d = new DirectoryInfo(tempDir);
                foreach (var dir in d.GetDirectories("JackettUpdate-*"))
                    try
                    {
                        logger.Info("Deleting JackettUpdate temp files from " + dir.FullName);
                        dir.Delete(true);
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Error while deleting temp files from: {dir.FullName}\n{e}");
                    }
            }
            catch (Exception e)
            {
                logger.Error($"Unexpected error while deleting temp files from: {tempDir}\n{e}");
            }
        }

        public void CheckUpdaterLock()
        {
            // check .lock file to detect errors in the update process
            var lockFilePath = Path.Combine(EnvironmentUtil.JackettInstallationPath(), ".lock");
            if (File.Exists(lockFilePath))
            {
                logger.Error("An error occurred during the last update. If this error occurs again, you need to reinstall " +
                             "Jackett following the documentation. If the problem continues after reinstalling, " +
                             "report the issue and attach the Jackett and Updater logs.");
                File.Delete(lockFilePath);
            }
        }

        private async Task<string> DownloadRelease(List<Asset> assets, bool isWindows, string version)
        {
            var variants = new Variants();
            var artifactFileName = variants.GetArtifactFileName(variant);
            var targetAsset = assets.FirstOrDefault(a => a.Browser_download_url.EndsWith(artifactFileName, StringComparison.OrdinalIgnoreCase) && artifactFileName.Length > 0);

            if (targetAsset == null)
            {
                logger.Error("Failed to find asset to download!");
                return null;
            }

            var url = targetAsset.Browser_download_url;

            var data = await client.GetResultAsync(SetDownloadHeaders(new WebRequest() { Url = url, EmulateBrowser = true, Type = RequestType.GET }));

            while (data.IsRedirect)
                data = await client.GetResultAsync(new WebRequest() { Url = data.RedirectingTo, EmulateBrowser = true, Type = RequestType.GET });

            var tempDir = Path.Combine(Path.GetTempPath(), "JackettUpdate-" + version + "-" + DateTime.Now.Ticks);

            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);

            Directory.CreateDirectory(tempDir);

            if (isWindows)
            {
                var zipPath = Path.Combine(tempDir, "Update.zip");
                File.WriteAllBytes(zipPath, data.ContentBytes);
                var fastZip = new FastZip();
                fastZip.ExtractZip(zipPath, tempDir, null);
            }
            else
            {
                var gzPath = Path.Combine(tempDir, "Update.tar.gz");
                File.WriteAllBytes(gzPath, data.ContentBytes);
                Stream inStream = File.OpenRead(gzPath);
                Stream gzipStream = new GZipInputStream(inStream);

                var tarArchive = TarArchive.CreateInputTarArchive(gzipStream);
                tarArchive.ExtractContents(tempDir);
                tarArchive.Close();
                gzipStream.Close();
                inStream.Close();

                if (variant == Variants.JackettVariant.CoreMacOs || variant == Variants.JackettVariant.CoreLinuxAmdx64
                || variant == Variants.JackettVariant.CoreLinuxArm32 || variant == Variants.JackettVariant.CoreLinuxArm64
                || variant == Variants.JackettVariant.Mono)
                {
                    //Calling the file permission service to limit usage to netcoreapp. The Mono.Posix.NETStandard library causes issues outside of .NET Core
                    //https://github.com/xamarin/XamarinComponents/issues/282

                    // When the files get extracted, the execute permission for jackett and JackettUpdater don't get carried across

                    var jackettPath = tempDir + "/Jackett/jackett";
                    filePermissionService.MakeFileExecutable(jackettPath);

                    var jackettUpdaterPath = tempDir + "/Jackett/JackettUpdater";
                    filePermissionService.MakeFileExecutable(jackettUpdaterPath);

                    if (variant == Variants.JackettVariant.CoreMacOs)
                    {
                        filePermissionService.MakeFileExecutable(tempDir + "/Jackett/install_service_macos");
                        filePermissionService.MakeFileExecutable(tempDir + "/Jackett/uninstall_jackett_macos");
                    }
                    else if (variant == Variants.JackettVariant.Mono)
                    {
                        var systemdPath = tempDir + "/Jackett/install_service_systemd_mono.sh";
                        filePermissionService.MakeFileExecutable(systemdPath);
                    }
                    else
                    {
                        var systemdPath = tempDir + "/Jackett/install_service_systemd.sh";
                        filePermissionService.MakeFileExecutable(systemdPath);

                        var launcherPath = tempDir + "/Jackett/jackett_launcher.sh";
                        filePermissionService.MakeFileExecutable(launcherPath);
                    }
                }
            }

            return tempDir;
        }

        private void StartUpdate(string updaterExePath, string installLocation, bool isWindows, bool noRestart, bool trayIsRunning)
        {
            var appType = "Console";

            if (isWindows && windowsService.ServiceExists() && windowsService.ServiceRunning())
                appType = "WindowsService";

            var args = string.Join(" ", Environment.GetCommandLineArgs().Skip(1).Select(a => a.Contains(" ") ? "\"" + a + "\"" : a)).Replace("\"", "\\\"");

            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Note: add a leading space to the --Args argument to avoid parsing as arguments
            if (variant == Variants.JackettVariant.Mono)
            {
                // Wrap mono
                args = Path.GetFileName(EnvironmentUtil.JackettExecutablePath()) + " " + args;

                startInfo.Arguments = $"{Path.Combine(updaterExePath)} --Path \"{installLocation}\" --Type \"{appType}\" --Args \" {args}\"";
                startInfo.FileName = "mono";
            }
            else
            {
                startInfo.Arguments = $"--Path \"{installLocation}\" --Type \"{appType}\" --Args \" {args}\"";
                startInfo.FileName = Path.Combine(updaterExePath);
            }

            try
            {
                var pid = Process.GetCurrentProcess().Id;
                startInfo.Arguments += $" --KillPids \"{pid}\"";
            }
            catch (Exception e)
            {
                logger.Error($"Unexpected error while retriving the PID.\n{e}");
            }

            if (noRestart)
                startInfo.Arguments += " --NoRestart";

            if (trayIsRunning && appType == "Console")
                startInfo.Arguments += " --StartTray";

            // create .lock file to detect errors in the update process
            var lockFilePath = Path.Combine(installLocation, ".lock");
            if (!File.Exists(lockFilePath))
                File.Create(lockFilePath).Dispose();

            logger.Info($"Starting updater: {startInfo.FileName} {startInfo.Arguments}");
            var procInfo = Process.Start(startInfo);
            logger.Info($"Updater started process id: {procInfo.Id}");

            if (!noRestart)
            {
                if (isWindows)
                {
                    logger.Info("Signal sent to lock service");
                    lockService.Signal();
                    Thread.Sleep(2000);
                }

                logger.Info("Exiting Jackett..");
                Environment.Exit(0);
            }
        }
    }
}
