using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataAniDub : ConfigurationDataBasicLogin
    {
        public BoolConfigurationItem StripRussianTitle { get; private set; }

        public ConfigurationDataAniDub() : base()
        {
            StripRussianTitle = new BoolConfigurationItem("Strip Russian Title")
            {
                Value = true
            };
        }
    }
}
