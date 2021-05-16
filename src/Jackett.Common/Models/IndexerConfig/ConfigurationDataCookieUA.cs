namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataCookieUA : ConfigurationData
    {
        public StringConfigurationItem Cookie { get; private set; }
        public DisplayInfoConfigurationItem CookieInstructions { get; private set; }
        public DisplayInfoConfigurationItem Instructions { get; private set; }
        public StringConfigurationItem UserAgent { get; private set; }
        public DisplayInfoConfigurationItem UserAgentInstructions { get; private set; }

        public ConfigurationDataCookieUA(string instructionMessageOptional = null)
        {
            Cookie = new StringConfigurationItem("Cookie");
            CookieInstructions = new DisplayInfoConfigurationItem("Cookie Instructions",
            "Please enter the cookie for the site manually. <a href=\"https://github.com/Jackett/Jackett/wiki/Finding-cookies\" target=\"_blank\">See here</a> on how get the cookies." +
            "<br>Example cookie header (usually longer than this):<br><code>PHPSESSID=8rk27odm; ipsconnect_63ad9c=1; more_stuff=etc;</code>");
            Instructions = new DisplayInfoConfigurationItem("", instructionMessageOptional);
            UserAgent = new StringConfigurationItem("User-Agent");
            UserAgentInstructions = new DisplayInfoConfigurationItem("User-Agent Instructions",
            "From the same place you fetched the cookie, find 'user-agent:' in the Request Headers section, select and copy the whole user-agent string and paste here.");
        }
    }
}
