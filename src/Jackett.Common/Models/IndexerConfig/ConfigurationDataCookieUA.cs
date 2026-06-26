using System.Text.Json.Serialization;

namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataCookieUA : ConfigurationData
    {
        [JsonPropertyOrder(1)]
        public StringConfigurationItem Cookie { get; private set; }

        [JsonPropertyOrder(2)]
        public DisplayInfoConfigurationItem CookieInstructions { get; private set; }

        [JsonPropertyOrder(3)]
        public StringConfigurationItem UserAgent { get; private set; }

        [JsonPropertyOrder(4)]
        public DisplayInfoConfigurationItem UserAgentInstructions { get; private set; }

        [JsonPropertyOrder(5)]
        public DisplayInfoConfigurationItem Instructions { get; private set; }

        public ConfigurationDataCookieUA(string instructionMessageOptional = null)
        {
            Cookie = new StringConfigurationItem("Cookie");
            CookieInstructions = new DisplayInfoConfigurationItem("Cookie Instructions",
            "Please enter the cookie for the site manually. <a href=\"https://github.com/Jackett/Jackett/wiki/Finding-cookies\" target=\"_blank\">See here</a> on how get the cookies." +
            "<br>Example cookie header (usually longer than this):<br><code>PHPSESSID=8rk27odm; ipsconnect_63ad9c=1; more_stuff=etc;</code>");
            UserAgent = new StringConfigurationItem("User-Agent");
            UserAgentInstructions = new DisplayInfoConfigurationItem("User Agent Instructions",
            "<ol><li>From the same place you fetched the cookie,<li>Find <b>'user-agent:'</b> in the <b>Request Headers</b> section<li><b>Select</b>" +
            "and <b>Copy</b> the whole user-agent string <i>(everything after 'user-agent: ')</i> and <b>Paste</b> here.</ol>");
            Instructions = new DisplayInfoConfigurationItem("", instructionMessageOptional);
        }
    }
}
