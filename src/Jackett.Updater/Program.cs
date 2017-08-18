using CommandLine;
using Jackett.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
/*
// no supported by appveyor, disabeling for now
#if __MonoCS__
using Mono.Unix.Native;
#endif
*/

namespace Jackett.Updater
{
    class Program
    {
        static void Main(string[] args)
        {
            new Program().Run(args);
        }

        private void Run(string[] args)
        {
            Engine.SetupLogging(null, "updater.txt");
            Engine.Logger.Info("Jackett Updater v" + GetCurrentVersion());
            Engine.Logger.Info("Options \"" + string.Join("\" \"", args) + "\"");
            try {
                var options = new UpdaterConsoleOptions();
                if (Parser.Default.ParseArguments(args, options))
                {
                    ProcessUpdate(options);
                }
                else
                {
                    Engine.Logger.Error("Failed to process update arguments!");
                    Console.ReadKey();
                }
            }
            catch (Exception e)
            {
                Engine.Logger.Error(e, "Exception applying update!");
            }
        }

        private string GetCurrentVersion()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
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
                    Engine.Logger.Info("Killing process " + proc.Id);
                    proc.Kill();
                    var exited = proc.WaitForExit(5000);
                    if (!exited)
                        Engine.Logger.Info("Process " + pid.ToString() + " didn't exit within 5 seconds");
/*
// no supported by appveyor, disabeling for now
#if __MonoCS__
                    Engine.Logger.Info("Sending SIGKILL to process " + pid.ToString());
                    Syscall.kill(proc.Id, Signum.SIGKILL);
#endif
*/
                }
                catch (ArgumentException)
                {
                    Engine.Logger.Info("Process " + pid.ToString() + " is already dead");
                }
                catch (Exception e)
                {
                    Engine.Logger.Info("Error killing process " + pid.ToString());
                    Engine.Logger.Info(e);
                }
            }
        }

        private void ProcessUpdate(UpdaterConsoleOptions options)
        {
            var updateLocation = GetUpdateLocation();
            if(!(updateLocation.EndsWith("\\") || updateLocation.EndsWith("/")))
            {
                updateLocation += Path.DirectorySeparatorChar;
            }

            var pids = new int[] { };
            if (options.KillPids != null)
            {
                var pidsStr = options.KillPids.Split(',').Where(pid => !string.IsNullOrWhiteSpace(pid)).ToArray();
                pids = Array.ConvertAll(pidsStr, pid => int.Parse(pid));
            }

            var isWindows = System.Environment.OSVersion.Platform != PlatformID.Unix;
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
                            Engine.Logger.Info("Killing tray process " + proc.Id);
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
            Engine.Logger.Info("Finding files in: " + updateLocation);
            var files = Directory.GetFiles(updateLocation, "*.*", SearchOption.AllDirectories);
            foreach(var file in files)
            {
                var fileName = Path.GetFileName(file).ToLowerInvariant();

                if (fileName.EndsWith(".zip") ||
                    fileName.EndsWith(".tar") ||
                    fileName.EndsWith(".gz"))
                {
                    continue;
                }
                try {
                    Engine.Logger.Info("Copying " + fileName);
                    var dest = Path.Combine(options.Path, file.Substring(updateLocation.Length));
                    var destDir = Path.GetDirectoryName(dest);
                    if (!Directory.Exists(destDir))
                    {
                        Engine.Logger.Info("Creating directory " + destDir);
                        Directory.CreateDirectory(destDir);
                    }
                    File.Copy(file, dest, true);
                }
                catch(Exception e)
                {
                    Engine.Logger.Error(e);
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
                        Engine.Logger.Info("Deleting directory " + deleteDir);
                        Directory.Delete(deleteDir, true);
                    }
                }
                catch (Exception e)
                {
                    Engine.Logger.Error(e);
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
            };

            foreach (var oldFIle in oldFiles)
            {
                try
                {
                    var deleteFile = Path.Combine(options.Path, oldFIle);
                    if (File.Exists(deleteFile))
                    {
                        Engine.Logger.Info("Deleting file " + deleteFile);
                        File.Delete(deleteFile);
                    }
                }
                catch (Exception e)
                {
                    Engine.Logger.Error(e);
                }
            }

            // kill pids after the update on UNIX
            if (!isWindows)
                KillPids(pids);

            if (options.NoRestart == false)
            {
                if (trayRunning)
                {
                    var startInfo = new ProcessStartInfo()
                    {
                        Arguments = options.Args,
                        FileName = Path.Combine(options.Path, "JackettTray.exe"),
                        UseShellExecute = true
                    };

                    Process.Start(startInfo);
                }

                if(string.Equals(options.Type, "JackettService.exe", StringComparison.InvariantCultureIgnoreCase))
                {
                    var serviceHelper = new ServiceConfigService(null, null);
                    if (serviceHelper.ServiceExists())
                    {
                        serviceHelper.Start();
                    }
                } else
                {
                    var startInfo = new ProcessStartInfo()
                    {
                        Arguments = options.Args,
                        FileName = Path.Combine(options.Path, "JackettConsole.exe"),
                        UseShellExecute = true
                    };

                    if (!isWindows)
                    {
                        startInfo.Arguments = startInfo.FileName + " " + startInfo.Arguments;
                        startInfo.FileName = "mono";
                    }

                    Engine.Logger.Info("Starting Jackett: " + startInfo.FileName + " " + startInfo.Arguments);
                    Process.Start(startInfo);
                }
            }
        }

        private string GetUpdateLocation()
        {
            var location = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase);
            return new FileInfo(HttpUtility.UrlDecode(location.AbsolutePath)).DirectoryName;
        }
    }
}
