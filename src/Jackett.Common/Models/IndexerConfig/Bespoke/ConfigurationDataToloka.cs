using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataToloka : ConfigurationDataBasicLogin
    {
        public BoolConfigurationItem FreeleechOnly { get; private set; }
        public BoolConfigurationItem StripCyrillicLetters { get; private set; }

        public ConfigurationDataToloka()
        {
            FreeleechOnly = new BoolConfigurationItem("Show freeleech only") { Value = false };
            StripCyrillicLetters = new BoolConfigurationItem("Strip Cyrillic Letters") { Value = true };
        }
    }
}
