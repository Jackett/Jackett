using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataXSpeeds : ConfigurationDataBasicLoginWithRSSAndDisplay
    {
        public BoolConfigurationItem FreeleechOnly { get; private set; }

        public ConfigurationDataXSpeeds()
        {
            FreeleechOnly = new BoolConfigurationItem("Search freeleech only") { Value = false };
        }
    }
}
