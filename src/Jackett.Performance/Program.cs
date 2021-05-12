using System.Globalization;
using System.Linq;
using AutoMapper;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Running;
using Jackett.Common.Models;
using Jackett.Common.Utils.Clients;

namespace Jackett.Performance
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkSwitcher
                .FromAssembly(typeof(Program).Assembly)
                .Run(args, GetGlobalConfig());
        }

        private static IConfig GetGlobalConfig()
        {
            return DefaultConfig.Instance
                    .WithCultureInfo(new CultureInfo("en-US"))
                    .AddExporter(RPlotExporter.Default);
        }
    }
}
