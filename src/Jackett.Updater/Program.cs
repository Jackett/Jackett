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
            Engine.Logger.Info("Options " + string.Join(" ", args));
            try {
                var options = new UpdaterConsoleOptions();
                if (Parser.Default.ParseArguments(args, options))
                {
                    ProcessUpdate(options);
                }
                else
                {
                    Engine.Logger.Error("Failed to process update arguments!: " + string.Join(" ", args));
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

        private void ProcessUpdate(UpdaterConsoleOptions options)
        {
            var updateLocation = GetUpdateLocation();
            if(!(updateLocation.EndsWith("\\") || updateLocation.EndsWith("/")))
            {
                updateLocation += Path.DirectorySeparatorChar;
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
            }

            Engine.Logger.Info("Waiting for Jackett to close..");
            Thread.Sleep(2000);
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

            // delete old files
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

        private string GetUpdateLocation()
        {
            var location = new Uri(Assembly.GetEntryAssembly().GetName().CodeBase);
            return new FileInfo(location.AbsolutePath).DirectoryName;
        }
    }
}
