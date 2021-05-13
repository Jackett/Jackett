using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataRarBG : ConfigurationData
    {
        public StringConfigurationItem ApiLink { get; private set; }
        public StringConfigurationItem StaticLink { get; private set; }

        public ConfigurationDataRarBG() : base()
        {
            ApiLink = new StringConfigurationItem("API Url")
            {
                Value = "https://torrentapi.org/pubapi_v2.php"
            };
        }
    }
}
