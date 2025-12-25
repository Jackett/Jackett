using System.Text.Json.Serialization;

namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataBasicLoginWith2FA : ConfigurationData
    {
        [JsonPropertyOrder(1)]
        public StringConfigurationItem Username { get; private set; }

        [JsonPropertyOrder(2)]
        public StringConfigurationItem Password { get; private set; }

        [JsonPropertyOrder(3)]
        public StringConfigurationItem TwoFactorAuth { get; private set; }

        [JsonPropertyOrder(4)]
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
