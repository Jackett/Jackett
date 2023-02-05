using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataImmortalSeed : ConfigurationDataBasicLogin
    {
        public BoolConfigurationItem FreeleechOnly { get; private set; }

        public ConfigurationDataImmortalSeed()
        {
            FreeleechOnly = new BoolConfigurationItem("Search freeleech only") { Value = false };
        }
    }
}
