using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataMTeamTp : ConfigurationData
    {
        public StringConfigurationItem ApiKey { get; private set; }
        public DisplayInfoConfigurationItem ApiKeyInfo { get; private set; }
        public BoolConfigurationItem FreeleechOnly { get; private set; }

        public ConfigurationDataMTeamTp()
        {
            ApiKey = new StringConfigurationItem("API Key");
            ApiKeyInfo = new DisplayInfoConfigurationItem("ApiKey Info", "The API key can be obtained by accessing your M-Team-TP User Control Panel > Security > Laboratory.");
            FreeleechOnly = new BoolConfigurationItem("Search freeleech only") { Value = false };
        }
    }
}
