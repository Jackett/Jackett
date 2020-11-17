using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataToloka : ConfigurationDataBasicLogin
    {
        public BoolItem StripCyrillicLetters { get; private set; }

        public ConfigurationDataToloka()
            => StripCyrillicLetters = new BoolItem() { Name = "Strip Cyrillic Letters", Value = true };
    }
}
