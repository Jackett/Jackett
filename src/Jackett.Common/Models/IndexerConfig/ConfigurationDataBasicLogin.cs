using System.Text.Json.Serialization;

namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataBasicLogin : ConfigurationData
    {
        [JsonPropertyOrder(1)]
        public StringConfigurationItem Username { get; private set; }

        [JsonPropertyOrder(2)]
        public StringConfigurationItem Password { get; private set; }

        [JsonPropertyOrder(3)]
        public DisplayInfoConfigurationItem Instructions { get; private set; }

        public ConfigurationDataBasicLogin(string instructionMessageOptional = null)
        {
            Username = new StringConfigurationItem("Username");
            Password = new StringConfigurationItem("Password");
            Instructions = new DisplayInfoConfigurationItem("", instructionMessageOptional);
        }
    }
}
