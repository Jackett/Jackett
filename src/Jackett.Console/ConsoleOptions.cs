using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Console
{
    public class ConsoleOptions
    {
        [Option('i', "Install", HelpText = "Install Jackett windows service (Must be admin)")]
        public bool Install { get; set; }

        [Option('r', "ReserveUrls",  HelpText = "(Re)Register windows port reservations (Required for listening on all interfaces).")]
        public bool ReserveUrls { get; set; }

        [Option('u', "Uninstall", HelpText = "Uninstall Jackett windows service (Must be admin).")]
        public bool Uninstall { get; set; }

        [Option('l', "Logging",  HelpText = "Log all requests/responses to Jackett")]
        public bool Logging { get; set; }

        [Option('t', "Tracing", HelpText = "Enable tracing")]
        public bool Tracing { get; set; }

        [Option('c', "UseClient",  HelpText = "Override web client selection. [automatic(Default)/libcurl/safecurl/httpclient/httpclient2]")]
        public string Client { get; set; }

        [Option('j', "ProxyConnection", HelpText = "use proxy - e.g. 127.0.0.1:8888")]
        public string ProxyConnection { get; set; }

        [Option('s', "Start",  HelpText = "Start the Jacket Windows service (Must be admin)")]
        public bool StartService { get; set; }

        [Option('k', "Stop", HelpText = "Stop the Jacket Windows service (Must be admin)")]
        public bool StopService { get; set; }

        [Option('x',  "ListenPublic",  HelpText = "Listen publicly")]
        public bool ListenPublic { get; set; }

        [Option('z', "ListenPrivate", HelpText = "Only allow local access")]
        public bool ListenPrivate { get; set; }

        [Option('h', "Help",  HelpText = "Show Help")]
        public bool ShowHelp { get; set; }

        [Option('v', "Version",  HelpText = "Show Version")]
        public bool ShowVersion { get; set; }

        [Option('p', "Port", HelpText = "Web server port")]
        public int Port { get; set; }

        [Option('m', "MigrateSettings", HelpText = "Migrate settings manually (Must be admin on Windows)")]
        public bool MigrateSettings { get; set; }

        [Option('f', "SSLFix", HelpText = "[true/false] Linux Libcurl NSS Missing ECC Ciphers workaround (Use if you can't access some trackers) .")]
        public bool? SSLFix { get; set; }

        [Option('n', "IgnoreSslErrors", HelpText = "[true/false] Ignores invalid SSL certificates")]
        public bool? IgnoreSslErrors { get; set; }

        [Option('d', "DataFolder", HelpText = "Specify the location of the data folder (Must be admin on Windows) eg. --DataFolder=\"D:\\Your Data\\Jackett\\\"")]
        public string DataFolder { get; set; }

        [Option(HelpText = "Don't restart after update")]
        public bool NoRestart { get; set; }

        [ParserState]
        public IParserState LastParserState { get; set; }
    }
}
