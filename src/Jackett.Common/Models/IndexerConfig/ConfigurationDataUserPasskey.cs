namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataUserPasskey : ConfigurationData
    {
        public StringConfigurationItem Username { get; private set; }
        public StringConfigurationItem Passkey { get; private set; }
        public DisplayInfoConfigurationItem Instructions { get; protected set; }

        public ConfigurationDataUserPasskey(string instructionMessageOptional = null)
        {
            Username = new StringConfigurationItem("Username");
            Passkey = new StringConfigurationItem("Passkey");
            Instructions = new DisplayInfoConfigurationItem("", instructionMessageOptional);
        }
    }
}
