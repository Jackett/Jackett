namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataBasicLoginWithRSS : ConfigurationData
    {
        public StringConfigurationItem Username { get; private set; }
        public StringConfigurationItem Password { get; private set; }
        public HiddenStringConfigurationItem RSSKey { get; private set; }

        public ConfigurationDataBasicLoginWithRSS()
        {
            Username = new StringConfigurationItem("Username");
            Password = new StringConfigurationItem("Password");
            RSSKey = new HiddenStringConfigurationItem("RSSKey");
        }
    }
}
