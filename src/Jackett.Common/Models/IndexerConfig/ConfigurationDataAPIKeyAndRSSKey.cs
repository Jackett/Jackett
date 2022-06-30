namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataAPIKeyAndRSSKey : ConfigurationData
    {
        public StringConfigurationItem ApiKey { get; private set; }
        public StringConfigurationItem RSSKey { get; private set; }
        public DisplayInfoConfigurationItem Instructions { get; private set; }

        public ConfigurationDataAPIKeyAndRSSKey(string instructionMessageOptional = null)
        {
            ApiKey = new StringConfigurationItem("API Key");
            RSSKey = new StringConfigurationItem("RSS Key");
            Instructions = new DisplayInfoConfigurationItem("", instructionMessageOptional);
        }
    }
}
