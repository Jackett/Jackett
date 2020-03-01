using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using CommandLine;
using CommandLine.Text;
using Jackett.Common.Models.Config;
using Jackett.Common.Services;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using NLog;

namespace Jackett.Updater
{
    public class Program
    {
        private IProcessService processService;
        private IServiceConfigService windowsService;
        public static Logger logger;
        private Variants.JackettVariant variant = Variants.JackettVariant.NotFound;

        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
            new Program().Run(args);
        }

        private void Run(string[] args)
        {
            var runtimeSettings = new RuntimeSettings()
            {
                CustomLogFileName = "updater.txt"
            };

            LogManager.Configuration = LoggingSetup.GetLoggingConfiguration(runtimeSettings);
            logger = LogManager.GetCurrentClassLogger();

            logger.Info("Jackett Updater v" + GetCurrentVersion());
            logger.Info("Options \"" + string.Join("\" \"", args) + "\"");

            var variants = new Variants();
            variant = variants.GetVariant();
            logger.Info("Jackett variant: " + variant.ToString());

            var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
            if (isWindows)
            {
                //The updater starts before Jackett closes
                logger.Info("Pausing for 3 seconds to give Jackett & tray time to shutdown");
                System.Threading.Thread.Sleep(3000);
            }

            processService = new ProcessService(logger);
            windowsService = new WindowsServiceConfigService(processService, logger);

            var commandLineParser = new Parser(settings => settings.CaseSensitive = false);

            try
            {
                var optionsResult = commandLineParser.ParseArguments<UpdaterConsoleOptions>(args);
                optionsResult.WithParsed(options =>
                {
                    ProcessUpdate(options);
                }
                );
                optionsResult.WithNotParsed(errors =>
                {
                    logger.Error(HelpText.AutoBuild(optionsResult));
                    logger.Error("Failed to process update arguments!");
                    logger.Error(errors.ToString());
                    Console.ReadKey();
                });
            }
            catch (Exception e)
            {
                logger.Error(e, "Exception applying update!");
            }
        }

        private string GetCurrentVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            return fvi.FileVersion;
        }

        private void KillPids(int[] pids)
        {
            foreach (var pid in pids)
            {
                try
                {
                    var proc = Process.GetProcessById(pid);
                    logger.Info("Killing process " + proc.Id);

                    // try to kill gracefully (on unix) first, see #3692
                    var exited = false;
                    if (Environment.OSVersion.Platform == PlatformID.Unix)
                    {
                        try
                        {
                            var startInfo = new ProcessStartInfo
                            {
                                Arguments = "-15 " + pid,
                                FileName = "kill"
                            };
                            Process.Start(startInfo);
                            System.Threading.Thread.Sleep(1000); // just sleep, WaitForExit() doesn't seem to work on mono/linux (returns immediantly), https://bugzilla.xamarin.com/show_bug.cgi?id=51742
                            exited = proc.WaitForExit(2000);
                        }
                        catch (Exception e)
                        {
                            logger.Error(e, "Error while sending SIGTERM to " + pid.ToString());
                        }
                        if (!exited)
                            logger.Info("Process " + pid.ToString() + " didn't exit within 2 seconds after a SIGTERM");
                    }
                    if (!exited)
                    {
                        proc.Kill(); // send SIGKILL
                    }
                    exited = proc.WaitForExit(5000);
                    if (!exited)
                        logger.Info("Process " + pid.ToString() + " didn't exit within 5 seconds after a SIGKILL");
                }
                catch (ArgumentException)
                {
                    logger.Info("Process " + pid.ToString() + " is already dead");
                }
                catch (Exception e)
                {
                    logger.Info("Error killing process " + pid.ToString());
                    logger.Info(e);
                }
            }
        }

        private void ProcessUpdate(UpdaterConsoleOptions options)
        {
            var updateLocation = GetUpdateLocation();
            if (!(updateLocation.EndsWith("\\") || updateLocation.EndsWith("/")))
            {
                updateLocation += Path.DirectorySeparatorChar;
            }

            var pids = new int[] { };
            if (options.KillPids != null)
            {
                var pidsStr = options.KillPids.Split(',').Where(pid => !string.IsNullOrWhiteSpace(pid)).ToArray();
                pids = Array.ConvertAll(pidsStr, pid => int.Parse(pid));
            }

            var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
            var trayRunning = false;
            var trayProcesses = Process.GetProcessesByName("JackettTray");
            if (isWindows)
            {
                if (trayProcesses.Length > 0)
                {
                    foreach (var proc in trayProcesses)
                    {
                        try
                        {
                            logger.Info("Killing tray process " + proc.Id);
                            proc.Kill();
                            trayRunning = true;
                        }
                        catch { }
                    }
                }

                // on unix we don't have to wait (we can overwrite files which are in use)
                // On unix we kill the PIDs after the update so e.g. systemd can automatically restart the process
                KillPids(pids);
            }

            var variants = new Variants();
            if (variants.IsNonWindowsDotNetCoreVariant(variant))
            {
                // On Linux you can't modify an executable while it is executing
                // https://github.com/Jackett/Jackett/issues/5022
                // https://stackoverflow.com/questions/16764946/what-generates-the-text-file-busy-message-in-unix#comment32135232_16764967
                // Delete the ./jackett executable
                // pdb files are also problematic https://github.com/Jackett/Jackett/issues/5167#issuecomment-489301150

                var jackettExecutable = options.Path.TrimEnd('/') + "/jackett";
                var pdbFiles = Directory.EnumerateFiles(options.Path, "*.pdb", SearchOption.AllDirectories).ToList();
                var removeList = pdbFiles;
                removeList.Add(jackettExecutable);

                foreach (var fileForDelete in removeList)
                {
                    try
                    {
                        logger.Info("Attempting to remove: " + fileForDelete);

                        if (File.Exists(fileForDelete))
                        {
                            File.Delete(fileForDelete);
                            logger.Info("Deleted " + fileForDelete);
                        }
                        else
                        {
                            logger.Info("File for deleting not found: " + fileForDelete);
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Error(e);
                    }
                }
            }

            logger.Info("Finding files in: " + updateLocation);
            var files = Directory.GetFiles(updateLocation, "*.*", SearchOption.AllDirectories).OrderBy(x => x).ToList();
            logger.Info($"{files.Count()} update files found");

            try
            {
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file).ToLowerInvariant();

                    if (fileName.EndsWith(".zip") || fileName.EndsWith(".tar") || fileName.EndsWith(".gz"))
                    {
                        continue;
                    }

                    var fileCopySuccess = CopyUpdateFile(options.Path, file, updateLocation, false);

                    if (!fileCopySuccess)
                    {
                        //Perform second attempt, this time removing the target file first
                        CopyUpdateFile(options.Path, file, updateLocation, true);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }

            logger.Info("File copying complete");

            // delete old dirs
            var oldDirs = new string[] { "Content/logos" };

            foreach (var oldDir in oldDirs)
            {
                try
                {
                    var deleteDir = Path.Combine(options.Path, oldDir);
                    if (Directory.Exists(deleteDir))
                    {
                        logger.Info("Deleting directory " + deleteDir);
                        Directory.Delete(deleteDir, true);
                    }
                }
                catch (Exception e)
                {
                    logger.Error(e);
                }
            }

            // delete old files
            var oldFiles = new string[] {
                "appsettings.Development.json",
                "Autofac.Integration.WebApi.dll",
                "Content/congruent_outline.png",
                "Content/crissXcross.png",
                "Content/css/jquery.dataTables.css",
                "Content/css/jquery.dataTables_themeroller.css",
                "CsQuery.dll",
                "CurlSharp.dll",
                "CurlSharp.pdb",
                "Definitions/420files.yml",
                "Definitions/anidex.yml", // migrated to C#
                "Definitions/aox.yml",
                "Definitions/apollo.yml", // migrated to C# gazelle base tracker
                "Definitions/archetorrent.yml",
                "Definitions/asiandvdclub.yml",
                "Definitions/avg.yml",
                "Definitions/b2s-share.yml",
                "Definitions/bithq.yml",
                "Definitions/bitme.yml",
                "Definitions/blubits.yml",
                "Definitions/bt-scene.yml",
                "Definitions/btbit.yml",
                "Definitions/btkitty.yml",
                "Definitions/btstornet.yml",
                "Definitions/btxpress.yml",
                "Definitions/cinefilhd.yml",
                "Definitions/czteam.yml",
                "Definitions/dark-shadow.yml",
                "Definitions/digbt.yml",
                "Definitions/dragonworld.yml",
                "Definitions/dreamteam.yml",
                "Definitions/elitehd.yml",
                "Definitions/elittracker.yml",
                "Definitions/eotforum.yml",
                "Definitions/evolutionpalace.yml",
                "Definitions/extratorrent-ag.yml",
                "Definitions/extratorrentclone.yml",
                "Definitions/freakstrackingsystem.yml",
                "Definitions/freedomhd.yml",
                "Definitions/gdf76.yml",
                "Definitions/gfxnews.yml",
                "Definitions/gods.yml",
                "Definitions/gormogon.yml",
                "Definitions/hachede-c.yml",
                "Definitions/hd4free.yml",
                "Definitions/hdbc.yml", // renamed to hdbitscom
                "Definitions/hdclub.yml",
                "Definitions/hdplus.yml",
                "Definitions/hon3yhd-net.yml",
                "Definitions/horriblesubs.yml",
                "Definitions/hyperay.yml",
                "Definitions/idopeclone.yml",
                "Definitions/iloveclassics.yml",
                "Definitions/infinityt.yml",
                "Definitions/isohunt.yml",
                "Definitions/katcrs.yml",
                "Definitions/kikibt.yml",
                "Definitions/lapausetorrents.yml",
                "Definitions/lechaudron.yml",
                "Definitions/lemencili.yml",
                "Definitions/leparadisdunet.yml",
                "Definitions/maniatorrent.yml",
                "Definitions/manicomioshare.yml",
                "Definitions/megabliz.yml",
                "Definitions/mkvcage.yml",
                "Definitions/music-master.yml",
                "Definitions/nachtwerk.yml",
                "Definitions/nexttorrent.yml",
                "Definitions/nordichd.yml",
                "Definitions/nyaa.yml",
                "Definitions/nyoo.yml",
                "Definitions/passionetorrent.yml",
                "Definitions/polishtracker.yml",
                "Definitions/qctorrent.yml",
                "Definitions/qxr.yml",
                "Definitions/rapidetracker.yml",
                "Definitions/rarbg.yml",
                "Definitions/redtopia.yml",
                "Definitions/rgu.yml",
                "Definitions/rockethd.yml",
                "Definitions/rockhardlossless.yml",
                "Definitions/scenehd.yml", // migrated to C# (use JSON API)
                "Definitions/scenereactor.yml",
                "Definitions/scenexpress.yml",
                "Definitions/secretcinema.yml", // migrated to C# gazelle base tracker
                "Definitions/sharingue.yml",
                "Definitions/skytorrents.yml",
                "Definitions/solidtorrents.yml", // migrated to C#
                "Definitions/speed-share.yml",
                "Definitions/t411.yml",
                "Definitions/t411v2.yml",
                "Definitions/tazmaniaden.yml",
                "Definitions/tbplus.yml",
                "Definitions/tehconnection.yml",
                "Definitions/themoviecave.yml",
                "Definitions/theresurrection.yml",
                "Definitions/thetorrents.yml",
                "Definitions/tigers-dl.yml",
                "Definitions/tntvillage.yml",
                "Definitions/torrentcouch.yml",
                "Definitions/torrentkim.yml",
                "Definitions/torrentproject.yml",
                "Definitions/torrentsmd.yml",
                "Definitions/torrentvault.yml",
                "Definitions/torrentwtf.yml",
                "Definitions/torrof.yml",
                "Definitions/torviet.yml",
                "Definitions/tspate.yml",
                "Definitions/ultimategamerclub.yml",
                "Definitions/ultrahdclub.yml",
                "Definitions/utorrents.yml", // same as SzeneFZ now
                "Definitions/waffles.yml",
                "Definitions/worldofp2p.yml",
                "Definitions/worldwidetorrents.yml",
                "Definitions/xktorrent.yml",
                "Definitions/zetorrents.yml",
                "Microsoft.Owin.dll",
                "Microsoft.Owin.FileSystems.dll",
                "Microsoft.Owin.Host.HttpListener.dll",
                "Microsoft.Owin.Hosting.dll",
                "Microsoft.Owin.StaticFiles.dll",
                "Owin.dll",
                "System.ServiceModel.dll",
                "System.Web.Http.dll",
                "System.Web.Http.Owin.dll",
                "System.Web.Http.Tracing.dll",
                "System.Xml.XPath.XmlDocument.dll"
            };

            foreach (var oldFile in oldFiles)
            {
                try
                {
                    var deleteFile = Path.Combine(options.Path, oldFile);
                    if (File.Exists(deleteFile))
                    {
                        logger.Info("Deleting file " + deleteFile);
                        File.Delete(deleteFile);
                    }
                }
                catch (Exception e)
                {
                    logger.Error(e);
                }
            }

            // kill pids after the update on UNIX
            if (!isWindows)
                KillPids(pids);

            if (!options.NoRestart)
            {
                if (isWindows && (trayRunning || options.StartTray) && !string.Equals(options.Type, "WindowsService", StringComparison.OrdinalIgnoreCase))
                {
                    var startInfo = new ProcessStartInfo()
                    {
                        Arguments = $"--UpdatedVersion \" {EnvironmentUtil.JackettVersion}\"",
                        FileName = Path.Combine(options.Path, "JackettTray.exe"),
                        UseShellExecute = true
                    };

                    logger.Info("Starting Tray: " + startInfo.FileName + " " + startInfo.Arguments);
                    Process.Start(startInfo);

                    if (!windowsService.ServiceExists())
                    {
                        //User was running the tray icon, but not using the Windows service, starting Tray icon will start JackettConsole as well
                        return;
                    }
                }

                if (string.Equals(options.Type, "WindowsService", StringComparison.OrdinalIgnoreCase))
                {
                    logger.Info("Starting Windows service");

                    if (ServerUtil.IsUserAdministrator())
                    {
                        windowsService.Start();
                    }
                    else
                    {
                        try
                        {
                            var consolePath = Path.Combine(options.Path, "JackettConsole.exe");
                            processService.StartProcessAndLog(consolePath, "--Start", true);
                        }
                        catch
                        {
                            logger.Error("Failed to get admin rights to start the service.");
                        }
                    }
                }
                else
                {
                    var startInfo = new ProcessStartInfo()
                    {
                        Arguments = options.Args,
                        FileName = GetJackettConsolePath(options.Path),
                        UseShellExecute = true
                    };

                    if (isWindows)
                    {
                        //User didn't initiate the update from Windows service and wasn't running Jackett via the tray, must have started from the console
                        startInfo.Arguments = $"/K {startInfo.FileName} {startInfo.Arguments}";
                        startInfo.FileName = "cmd.exe";
                        startInfo.CreateNoWindow = false;
                        startInfo.WindowStyle = ProcessWindowStyle.Normal;
                    }

                    if (variant == Variants.JackettVariant.Mono)
                    {
                        startInfo.Arguments = startInfo.FileName + " " + startInfo.Arguments;
                        startInfo.FileName = "mono";
                    }

                    if (variant == Variants.JackettVariant.CoreMacOs || variant == Variants.JackettVariant.CoreLinuxAmdx64
                    || variant == Variants.JackettVariant.CoreLinuxArm32 || variant == Variants.JackettVariant.CoreLinuxArm64)
                    {
                        startInfo.UseShellExecute = false;
                        startInfo.CreateNoWindow = true;
                    }

                    logger.Info("Starting Jackett: " + startInfo.FileName + " " + startInfo.Arguments);
                    Process.Start(startInfo);
                }
            }
        }

        private bool CopyUpdateFile(string jackettDestinationDirectory, string fullSourceFilePath, string updateSourceDirectory, bool previousAttemptFailed)
        {
            var success = false;

            string fileName;
            string fullDestinationFilePath;
            string fileDestinationDirectory;

            try
            {
                fileName = Path.GetFileName(fullSourceFilePath);
                fullDestinationFilePath = Path.Combine(jackettDestinationDirectory, fullSourceFilePath.Substring(updateSourceDirectory.Length));
                fileDestinationDirectory = Path.GetDirectoryName(fullDestinationFilePath);
            }
            catch (Exception e)
            {
                logger.Error(e);
                return false;
            }

            logger.Info($"Attempting to copy {fileName} from source: {fullSourceFilePath} to destination: {fullDestinationFilePath}");

            if (previousAttemptFailed)
            {
                logger.Info("The first attempt copying file: " + fileName + "failed. Retrying and will delete old file first");

                try
                {
                    if (File.Exists(fullDestinationFilePath))
                    {
                        logger.Info(fullDestinationFilePath + " was found");
                        System.Threading.Thread.Sleep(1000);
                        File.Delete(fullDestinationFilePath);
                        logger.Info("Deleted " + fullDestinationFilePath);
                        System.Threading.Thread.Sleep(1000);
                    }
                    else
                    {
                        logger.Info(fullDestinationFilePath + " was NOT found");
                    }
                }
                catch (Exception e)
                {
                    logger.Error(e);
                }
            }

            try
            {
                if (!Directory.Exists(fileDestinationDirectory))
                {
                    logger.Info("Creating directory " + fileDestinationDirectory);
                    Directory.CreateDirectory(fileDestinationDirectory);
                }

                File.Copy(fullSourceFilePath, fullDestinationFilePath, true);
                logger.Info("Copied " + fileName);
                success = true;
            }
            catch (Exception e)
            {
                logger.Error(e);
            }

            return success;
        }

        private string GetUpdateLocation()
        {
            // Use EscapedCodeBase to avoid Uri reserved characters from causing bugs
            // https://stackoverflow.com/questions/896572
            var location = new Uri(Assembly.GetEntryAssembly().GetName().EscapedCodeBase);
            // Use LocalPath instead of AbsolutePath to avoid needing to unescape Uri format.
            return new FileInfo(location.LocalPath).DirectoryName;
        }

        private string GetJackettConsolePath(string directoryPath)
        {
            var variants = new Variants();
            if (variants.IsNonWindowsDotNetCoreVariant(variant))
            {
                return Path.Combine(directoryPath, "jackett");
            }
            else
            {
                return Path.Combine(directoryPath, "JackettConsole.exe");
            }
        }

        private static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            Console.WriteLine(e.ExceptionObject.ToString());
            logger.Error(e.ExceptionObject.ToString());
            Environment.Exit(1);
        }
    }
}
