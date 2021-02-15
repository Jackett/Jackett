namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataBasicLoginWith2FA : ConfigurationData
    {
        public StringConfigurationItem Username { get; private set; }
        public StringConfigurationItem Password { get; private set; }
        public StringConfigurationItem TwoFactorAuth { get; private set; }
        public DisplayInfoConfigurationItem Instructions { get; private set; }

        public ConfigurationDataBasicLoginWith2FA(string instructionMessageOptional = null)
        {
            Username = new StringConfigurationItem("Username");
            Password = new StringConfigurationItem("Password");
            TwoFactorAuth = new StringConfigurationItem("Two-Factor Auth");
            Instructions = new DisplayInfoConfigurationItem("", instructionMessageOptional);
        }
    }
}
