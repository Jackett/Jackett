namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataAPIKey : ConfigurationData
    {
        public StringConfigurationItem Key { get; private set; }

        public ConfigurationDataAPIKey() => Key = new StringConfigurationItem("APIKey") { Value = string.Empty };
    }
}
