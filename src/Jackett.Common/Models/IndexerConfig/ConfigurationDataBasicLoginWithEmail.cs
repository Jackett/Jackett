namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataBasicLoginWithEmail : ConfigurationData
    {
        public StringConfigurationItem Email { get; private set; }
        public StringConfigurationItem Password { get; private set; }
        public DisplayInfoConfigurationItem Instructions { get; private set; }

        public ConfigurationDataBasicLoginWithEmail(string instructionMessageOptional = null)
        {
            Email = new StringConfigurationItem("Email");
            Password = new StringConfigurationItem("Password");
            Instructions = new DisplayInfoConfigurationItem("", instructionMessageOptional);
        }
    }
}
