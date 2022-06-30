namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataBasicLogin : ConfigurationData
    {
        public StringConfigurationItem Username { get; private set; }
        public StringConfigurationItem Password { get; private set; }
        public DisplayInfoConfigurationItem Instructions { get; private set; }

        public ConfigurationDataBasicLogin(string instructionMessageOptional = null)
        {
            Username = new StringConfigurationItem("Username");
            Password = new StringConfigurationItem("Password");
            Instructions = new DisplayInfoConfigurationItem("", instructionMessageOptional);
        }
    }
}
