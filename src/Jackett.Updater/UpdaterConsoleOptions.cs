using CommandLine;

namespace Jackett.Updater
{
    public class UpdaterConsoleOptions
    {
        [Option('p', "Path", HelpText = "Install location", Required = true)]
        public string Path { get; set; }

        [Option('t', "Type", HelpText = "Install type")]
        public string Type { get; set; }

        [Option('a', "Args", HelpText = "Launch arguments")]
        public string Args { get; set; }

        [Option("NoRestart", HelpText = "Don't restart after update")]
        public bool NoRestart { get; set; }

        [Option("KillPids", HelpText = "PIDs which will be killed before (Windows) or after (Unix) the update")]
        public string KillPids { get; set; }

        [Option("StartTray", HelpText = "Indicates that the updater should start the tray icon")]
        public bool StartTray { get; set; }
    }
}
