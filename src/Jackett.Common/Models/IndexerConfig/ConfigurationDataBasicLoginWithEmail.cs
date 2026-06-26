using System.Text.Json.Serialization;

namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataBasicLoginWithEmail : ConfigurationData
    {
        [JsonPropertyOrder(1)]
        public StringConfigurationItem Email { get; private set; }

        [JsonPropertyOrder(2)]
        public StringConfigurationItem Password { get; private set; }

        [JsonPropertyOrder(3)]
        public DisplayInfoConfigurationItem Instructions { get; private set; }

        public ConfigurationDataBasicLoginWithEmail(string instructionMessageOptional = null)
        {
            Email = new StringConfigurationItem("Email");
            Password = new StringConfigurationItem("Password");
            Instructions = new DisplayInfoConfigurationItem("", instructionMessageOptional);
        }
    }
}
