using System.Diagnostics.CodeAnalysis;

namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    [ExcludeFromCodeCoverage]
    internal class ConfigurationDataAniLibria : ConfigurationData
    {
        public StringConfigurationItem ApiLink { get; private set; }
        public StringConfigurationItem StaticLink { get; private set; }

        public ConfigurationDataAniLibria() : base()
        {
            ApiLink = new StringConfigurationItem("API Url")
            {
                Value = "https://api.anilibria.tv/v2/"
            };
            StaticLink = new StringConfigurationItem("Static Url")
            {
                Value = "https://static.anilibria.tv/"
            };
        }
    }
}
