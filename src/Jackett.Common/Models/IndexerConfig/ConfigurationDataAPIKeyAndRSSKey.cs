namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataAPIKeyAndRSSKey : ConfigurationData
    {
        public StringConfigurationItem ApiKey { get; private set; }
        public StringConfigurationItem RSSKey { get; private set; }

        public ConfigurationDataAPIKeyAndRSSKey()
        {
            ApiKey = new StringConfigurationItem("API Key");
            RSSKey = new StringConfigurationItem("RSS Key");
        }
    }
}
