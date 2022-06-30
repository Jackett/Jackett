using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataToloka : ConfigurationDataBasicLogin
    {
        public BoolConfigurationItem StripCyrillicLetters { get; private set; }

        public ConfigurationDataToloka()
            => StripCyrillicLetters = new BoolConfigurationItem("Strip Cyrillic Letters") { Value = true };
    }
}
