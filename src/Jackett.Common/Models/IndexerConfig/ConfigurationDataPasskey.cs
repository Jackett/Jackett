using System.Text.Json.Serialization;

namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataPasskey : ConfigurationData
    {
        [JsonPropertyOrder(1)]
        public StringConfigurationItem Passkey { get; private set; }

        [JsonPropertyOrder(2)]
        public DisplayInfoConfigurationItem Instructions { get; private set; }

        public ConfigurationDataPasskey(string instructionMessageOptional = null)
        {
            Passkey = new StringConfigurationItem("Passkey");
            Instructions = new DisplayInfoConfigurationItem("", instructionMessageOptional);
        }
    }
}
