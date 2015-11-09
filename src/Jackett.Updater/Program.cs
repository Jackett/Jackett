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
            Console.WriteLine("Jackett Updater v" + GetCurrentVersion());
            Console.WriteLine("Waiting for Jackett to close..");
            Thread.Sleep(2000);

            try {
                var options = new UpdaterConsoleOptions();
                if (Parser.Default.ParseArguments(args, options))
                {
                    ProcessUpdate(options);
                }
                else
                {
                    Console.WriteLine("Failed to process update arguments!: " + string.Join(" ", args));
                    Console.ReadKey();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception applying update: " + e.Message);
                Console.ReadKey();
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
            var trayProcesses = Process.GetProcessesByName("JackettTray.exe");
            if (isWindows)
            {
                if (trayProcesses.Count() > 0)
                {  
                    foreach (var proc in trayProcesses)
                    {
                        try
                        {
                            Console.WriteLine("Killing tray process " + proc.Id);
                            proc.Kill();
                            trayRunning = true;
                        }
                        catch { }
                    }
                }
            }

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

                Console.WriteLine("Copying " + fileName);
                var dest = Path.Combine(options.Path, file.Substring(updateLocation.Length));
                File.Copy(file, dest, true);
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
