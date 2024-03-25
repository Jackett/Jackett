using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataMTeamTp : ConfigurationData
    {
        public StringConfigurationItem ApiKey { get; private set; }
        public BoolConfigurationItem FreeleechOnly { get; private set; }

        public ConfigurationDataMTeamTp()
        {
            ApiKey = new StringConfigurationItem("API Key");
            FreeleechOnly = new BoolConfigurationItem("Search freeleech only") { Value = false };
        }
    }
}
