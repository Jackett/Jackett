namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataAPIKey : ConfigurationData
    {
        public StringItem Key { get; private set; }

        public ConfigurationDataAPIKey() => Key = new StringItem { Name = "APIKey", Value = string.Empty };
    }
}
