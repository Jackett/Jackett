namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataAPIKey : ConfigurationData
    {
        public ConfigurationData.StringItem Key { get; private set; }

        public ConfigurationDataAPIKey()
        {
            Key = new ConfigurationData.StringItem { Name = "APIKey", Value = string.Empty };
        }
    }
}
