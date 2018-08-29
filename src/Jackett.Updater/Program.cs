using CommandLine;
using CommandLine.Text;
using Jackett.Common.Models.Config;
using Jackett.Common.Services;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;

namespace Jackett.Updater
{
    public class Program
    {
        private IProcessService processService;
        private IServiceConfigService windowsService;
        private Logger logger;

        public static void Main(string[] args)
        {
            new Program().Run(args);
        }

        private void Run(string[] args)
        {
            RuntimeSettings runtimeSettings = new RuntimeSettings()
            {
                CustomLogFileName = "updater.txt"
            };

            LogManager.Configuration = LoggingSetup.GetLoggingConfiguration(runtimeSettings);
            logger = LogManager.GetCurrentClassLogger();

            logger.Info("Jackett Updater v" + GetCurrentVersion());
            logger.Info("Options \"" + string.Join("\" \"", args) + "\"");

            bool isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
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
                    proc.Kill();
                    var exited = proc.WaitForExit(5000);
                    if (!exited)
                        logger.Info("Process " + pid.ToString() + " didn't exit within 5 seconds");
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
                if (trayProcesses.Count() > 0)
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
            logger.Info("Finding files in: " + updateLocation);
            var files = Directory.GetFiles(updateLocation, "*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file).ToLowerInvariant();

                if (fileName.EndsWith(".zip") ||
                    fileName.EndsWith(".tar") ||
                    fileName.EndsWith(".gz"))
                {
                    continue;
                }
                try
                {
                    logger.Info("Copying " + fileName);
                    var dest = Path.Combine(options.Path, file.Substring(updateLocation.Length));
                    var destDir = Path.GetDirectoryName(dest);
                    if (!Directory.Exists(destDir))
                    {
                        logger.Info("Creating directory " + destDir);
                        Directory.CreateDirectory(destDir);
                    }
                    File.Copy(file, dest, true);
                }
                catch (Exception e)
                {
                    logger.Error(e);
                }
            }

            // delete old dirs
            string[] oldDirs = new string[] { "Content/logos" };

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
            string[] oldFiles = new string[] {
                "Content/css/jquery.dataTables.css",
                "Content/css/jquery.dataTables_themeroller.css",
                "Definitions/tspate.yml",
                "Definitions/freakstrackingsystem.yml",
                "Definitions/rarbg.yml",
                "Definitions/t411.yml",
                "Definitions/hdbc.yml", // renamed to hdbitscom
                "Definitions/maniatorrent.yml",
                "Definitions/nyaa.yml",
                "Definitions/nachtwerk.yml",
                "Definitions/leparadisdunet.yml",
                "Definitions/qctorrent.yml",
                "Definitions/dragonworld.yml",
                "Definitions/hdclub.yml",
                "Definitions/polishtracker.yml",
                "Definitions/zetorrents.yml",
                "Definitions/rapidetracker.yml",
                "Definitions/isohunt.yml",
                "Definitions/t411v2.yml",
                "Definitions/bithq.yml",
                "Definitions/blubits.yml",
                "Definitions/torrentproject.yml",
                "Definitions/torrentvault.yml",
                "Definitions/apollo.yml", // migrated to C# gazelle base tracker
                "Definitions/secretcinema.yml", // migrated to C# gazelle base tracker
                "Definitions/utorrents.yml", // same as SzeneFZ now
                "Definitions/ultrahdclub.yml",
                "Definitions/infinityt.yml",
                "Definitions/hachede-c.yml",
                "Definitions/hd4Free.yml",
                "Definitions/skytorrents.yml",
                "Definitions/gormogon.yml",
                "Definitions/czteam.yml",
                "Definitions/rockhardlossless.yml",
                "Definitions/oxtorrent.yml",
                "Definitions/tehconnection.yml",
                "Definitions/torrentwtf.yml",
                "Definitions/eotforum.yml",
                "Definitions/nexttorrent.yml",
                "appsettings.Development.json",
                "CurlSharp.dll",
                "CurlSharp.pdb",
                "Jackett.dll",
                "Jackett.dll.config",
                "Jackett.pdb",
                "Autofac.Integration.WebApi.dll",
                "Microsoft.Owin.dll",
                "Microsoft.Owin.FileSystems.dll",
                "Microsoft.Owin.Host.HttpListener.dll",
                "Microsoft.Owin.Hosting.dll",
                "Microsoft.Owin.StaticFiles.dll",
                "Owin.dll",
                "System.Web.Http.dll",
                "System.Web.Http.Owin.dll",
                "System.Web.Http.Tracing.dll",
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

            if (options.NoRestart == false)
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
                        FileName = Path.Combine(options.Path, "JackettConsole.exe"),
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
                    else
                    {
                        startInfo.Arguments = startInfo.FileName + " " + startInfo.Arguments;
                        startInfo.FileName = "mono";
                    }

                    logger.Info("Starting Jackett: " + startInfo.FileName + " " + startInfo.Arguments);
                    Process.Start(startInfo);
                }
            }
        }

        private string GetUpdateLocation()
        {
            var location = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase);
            return new FileInfo(WebUtility.UrlDecode(location.AbsolutePath)).DirectoryName;
        }
    }
}
