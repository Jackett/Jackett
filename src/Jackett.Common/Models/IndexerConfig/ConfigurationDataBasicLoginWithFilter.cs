using System.Text.Json.Serialization;

namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataBasicLoginWithFilter : ConfigurationData
    {
        [JsonPropertyOrder(1)]
        public StringConfigurationItem Username { get; private set; }

        [JsonPropertyOrder(2)]
        public StringConfigurationItem Password { get; private set; }

        [JsonPropertyOrder(3)]
        public HiddenStringConfigurationItem LastLoggedInCheck { get; private set; }

        [JsonPropertyOrder(4)]
        public DisplayInfoConfigurationItem FilterExample { get; private set; }

        [JsonPropertyOrder(5)]
        public StringConfigurationItem FilterString { get; private set; }

        public ConfigurationDataBasicLoginWithFilter(string filterInstructions)
        {
            Username = new StringConfigurationItem("Username");
            Password = new StringConfigurationItem("Password");
            LastLoggedInCheck = new HiddenStringConfigurationItem("LastLoggedInCheck");
            FilterExample = new DisplayInfoConfigurationItem("", filterInstructions);
            FilterString = new StringConfigurationItem("Filters (optional)");
        }
    }
}
