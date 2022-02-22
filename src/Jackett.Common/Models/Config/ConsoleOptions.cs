using System;
using CommandLine;

namespace Jackett.Common.Models.Config
{
    public class ConsoleOptions
    {
        [Option('i', "Install", HelpText = "Install Jackett windows service (Must be admin)")]
        public bool Install { get; set; }

        [Option('r', "ReserveUrls", HelpText = "(Re)Register windows port reservations (Required for listening on all interfaces).")]
        public bool ReserveUrls { get; set; }

        [Option('u', "Uninstall", HelpText = "Uninstall Jackett windows service (Must be admin).")]
        public bool Uninstall { get; set; }

        [Option('l', "Logging", HelpText = "Log all requests/responses to Jackett")]
        public bool Logging { get; set; }

        [Option('t', "Tracing", HelpText = "Enable tracing")]
        public bool Tracing { get; set; }

        [Option('c', "UseClient", HelpText = "Override web client selection. [automatic(Default)/httpclient/httpclient2]")]
        public string Client { get; set; }

        [Option('s', "Start", HelpText = "Start the Jacket Windows service (Must be admin)")]
        public bool StartService { get; set; }

        [Option('k', "Stop", HelpText = "Stop the Jacket Windows service (Must be admin)")]
        public bool StopService { get; set; }

        [Option('x', "ListenPublic", HelpText = "Listen publicly")]
        public bool ListenPublic { get; set; }

        [Option('z', "ListenPrivate", HelpText = "Only allow local access")]
        public bool ListenPrivate { get; set; }

        [Option('p', "Port", HelpText = "Web server port")]
        public int Port { get; set; }

        [Option('n', "IgnoreSslErrors", HelpText = "[true/false] Ignores invalid SSL certificates")]
        public bool? IgnoreSslErrors { get; set; }

        [Option('d', "DataFolder", HelpText = "Specify the location of the data folder (Must be admin on Windows) eg. --DataFolder=\"D:\\Your Data\\Jackett\\\". Don't use this on Unix (mono) systems. On Unix just adjust the HOME directory of the user to the datadir or set the XDG_CONFIG_HOME environment variable.")]
        public string DataFolder { get; set; }

        [Option("NoRestart", HelpText = "Don't restart after update")]
        public bool NoRestart { get; set; }

        [Option("PIDFile", HelpText = "Specify the location of PID file")]
        public string PIDFile { get; set; }

        [Option("NoUpdates", HelpText = "Disable automatic updates")]
        public bool NoUpdates { get; set; }

        public RuntimeSettings ToRunTimeSettings()
        {
            var options = this;
            var runtimeSettings = new RuntimeSettings();

            // Logging
            if (options.Logging)
                runtimeSettings.LogRequests = true;

            // Tracing
            if (options.Tracing)
                runtimeSettings.TracingEnabled = true;

            if (options.ListenPublic && options.ListenPrivate)
            {
                Console.WriteLine("You can only use listen private OR listen publicly.");
                Environment.Exit(1);
            }

            // Use curl
            if (options.Client != null)
                runtimeSettings.ClientOverride = options.Client.ToLowerInvariant();

            // Ignore SSL errors on Curl
            runtimeSettings.IgnoreSslErrors = options.IgnoreSslErrors;

            runtimeSettings.NoRestart = options.NoRestart;
            runtimeSettings.NoUpdates = options.NoUpdates;

            if (!string.IsNullOrWhiteSpace(options.DataFolder))
                runtimeSettings.CustomDataFolder = options.DataFolder;

            runtimeSettings.PIDFile = options.PIDFile;

            return runtimeSettings;
        }
    }
}
