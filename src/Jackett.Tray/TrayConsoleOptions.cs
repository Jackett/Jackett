using CommandLine;

namespace Jackett.Tray
{
    public class TrayConsoleOptions
    {
        [Option("UpdatedVersion", HelpText = "Indicates the new version that Jackett just updated to so that user understands why they are getting a prompt to start Windows service")]
        public string UpdatedVersion { get; set; }
    }
}
