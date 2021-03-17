namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataPasskey : ConfigurationData
    {
        public StringConfigurationItem Passkey { get; private set; }
        public DisplayInfoConfigurationItem Instructions { get; private set; }

        public ConfigurationDataPasskey(string instructionMessageOptional = null)
        {
            Passkey = new StringConfigurationItem("Passkey");
            Instructions = new DisplayInfoConfigurationItem("", instructionMessageOptional);
        }
    }
}
