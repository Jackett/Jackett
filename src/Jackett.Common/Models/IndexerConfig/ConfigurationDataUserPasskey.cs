using System.Text.Json.Serialization;

namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataUserPasskey : ConfigurationData
    {
        [JsonPropertyOrder(1)]
        public StringConfigurationItem Username { get; private set; }

        [JsonPropertyOrder(2)]
        public StringConfigurationItem Passkey { get; private set; }

        [JsonPropertyOrder(3)]
        public DisplayInfoConfigurationItem Instructions { get; protected set; }

        public ConfigurationDataUserPasskey(string instructionMessageOptional = null)
        {
            Username = new StringConfigurationItem("Username");
            Passkey = new StringConfigurationItem("Passkey");
            Instructions = new DisplayInfoConfigurationItem("", instructionMessageOptional);
        }
    }
}
