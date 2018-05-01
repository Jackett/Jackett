using CommandLine;
using CommandLine.Text;
using Jackett.Common.Models.Config;
using Jackett.Common.Utils;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jackett.Server
{
    public class Program
    {
        public static IConfiguration Configuration { get; set; }

        public static void Main(string[] args)
        {
            var optionsResult = Parser.Default.ParseArguments<ConsoleOptions>(args);
            optionsResult.WithNotParsed(errors =>
            {
                var text = HelpText.AutoBuild(optionsResult);
                text.Copyright = " ";
                text.Heading = "Jackett v" + EnvironmentUtil.JackettVersion + " options:";
                Console.WriteLine(text);
                Environment.ExitCode = 1;
                return;
            });

            var runtimeDictionary = new Dictionary<string, string>();

            optionsResult.WithParsed(options =>
            {
                RuntimeSettings r = options.ToRunTimeSettings();
                runtimeDictionary = GetValues(r);
            });

            var builder = new ConfigurationBuilder();
            builder.AddInMemoryCollection(runtimeDictionary);

            Configuration = builder.Build();

            BuildWebHost().Run();
        }

        public static Dictionary<string, string> GetValues(object obj)
        {
            return obj
                    .GetType()
                    .GetProperties()
                    .ToDictionary(p => "RuntimeSettings:" + p.Name, p => p.GetValue(obj) == null ? null : p.GetValue(obj).ToString());
        }

        public static IWebHost BuildWebHost() =>
            WebHost.CreateDefaultBuilder()
            .UseConfiguration(Configuration)
                .UseStartup<Startup>()
                .Build();
    }
}
