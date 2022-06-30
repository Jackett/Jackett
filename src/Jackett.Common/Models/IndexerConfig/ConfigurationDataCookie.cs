namespace Jackett.Common.Models.IndexerConfig
{
    public class ConfigurationDataCookie : ConfigurationData
    {
        public StringConfigurationItem Cookie { get; private set; }
        public DisplayInfoConfigurationItem CookieInstructions { get; private set; }
        public DisplayInfoConfigurationItem Instructions { get; private set; }

        public ConfigurationDataCookie(string instructionMessageOptional = null)
        {
            Cookie = new StringConfigurationItem("Cookie");
            CookieInstructions = new DisplayInfoConfigurationItem("Cookie Instructions",
            "Please enter the cookie for the site manually. <a href=\"https://github.com/Jackett/Jackett/wiki/Finding-cookies\" target=\"_blank\">See here</a> on how get the cookies." +
            "<br>Example cookie header (usually longer than this):<br><code>PHPSESSID=8rk27odm; ipsconnect_63ad9c=1; more_stuff=etc;</code>");
            Instructions = new DisplayInfoConfigurationItem("", instructionMessageOptional);
        }
    }
}
