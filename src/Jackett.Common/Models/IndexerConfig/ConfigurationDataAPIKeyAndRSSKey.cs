using System.Text.Json.Serialization;

namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataAPIKeyAndRSSKey : ConfigurationData
    {
        [JsonPropertyOrder(1)]
        public StringConfigurationItem ApiKey { get; private set; }

        [JsonPropertyOrder(2)]
        public StringConfigurationItem RSSKey { get; private set; }

        [JsonPropertyOrder(3)]
        public DisplayInfoConfigurationItem Instructions { get; private set; }

        public ConfigurationDataAPIKeyAndRSSKey(string instructionMessageOptional = null)
        {
            ApiKey = new StringConfigurationItem("API Key");
            RSSKey = new StringConfigurationItem("RSS Key");
            Instructions = new DisplayInfoConfigurationItem("", instructionMessageOptional);
        }
    }
}
