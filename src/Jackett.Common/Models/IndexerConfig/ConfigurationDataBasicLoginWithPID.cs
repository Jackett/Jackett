using System.Text.Json.Serialization;

namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataBasicLoginWithPID : ConfigurationData
    {
        [JsonPropertyOrder(1)]
        public StringConfigurationItem Username { get; private set; }

        [JsonPropertyOrder(2)]
        public StringConfigurationItem Password { get; private set; }

        [JsonPropertyOrder(3)]
        public StringConfigurationItem Pid { get; private set; }

        [JsonPropertyOrder(4)]
        public DisplayInfoConfigurationItem Instructions { get; private set; }

        public ConfigurationDataBasicLoginWithPID(string instructionMessageOptional = null)
        {
            Username = new StringConfigurationItem("Username");
            Password = new StringConfigurationItem("Password");
            Pid = new StringConfigurationItem("Pid");
            Instructions = new DisplayInfoConfigurationItem("", instructionMessageOptional);
        }
    }
}
