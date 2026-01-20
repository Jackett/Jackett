using System.Text.Json.Serialization;

namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataAPIKey : ConfigurationData
    {
        [JsonPropertyOrder(1)]
        public StringConfigurationItem Key { get; private set; }

        public ConfigurationDataAPIKey()
        {
            Key = new StringConfigurationItem("APIKey") { Value = string.Empty };
        }
    }
}
