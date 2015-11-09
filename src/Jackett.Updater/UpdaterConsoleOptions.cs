using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Updater
{
    public class UpdaterConsoleOptions
    {
        [Option('p', "Path", HelpText = "Install location")]
        public string Path { get; set; }

        [Option('t', "Type", HelpText = "Install type")]
        public string Type { get; set; }

        [Option('a', "Args", HelpText = "Launch arguments")]
        public string Args { get; set; }
    }
}
