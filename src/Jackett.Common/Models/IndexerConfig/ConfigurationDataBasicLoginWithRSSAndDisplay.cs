namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataBasicLoginWithRSSAndDisplay : ConfigurationData
    {
        public StringConfigurationItem Username { get; private set; }
        public StringConfigurationItem Password { get; private set; }
        public HiddenStringConfigurationItem RSSKey { get; private set; }
        public DisplayInfoConfigurationItem Instructions { get; private set; }

        public ConfigurationDataBasicLoginWithRSSAndDisplay(string instructionMessageOptional = null)
        {
            Username = new StringConfigurationItem("Username");
            Password = new StringConfigurationItem("Password");
            RSSKey = new HiddenStringConfigurationItem("RSSKey");
            Instructions = new DisplayInfoConfigurationItem("", instructionMessageOptional);
        }
    }
}
