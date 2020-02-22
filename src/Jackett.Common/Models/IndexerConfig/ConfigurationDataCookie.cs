namespace Jackett.Common.Models.IndexerConfig
{

    public class ConfigurationDataCookie : ConfigurationData
    {
        public StringItem Cookie { get; private set; }
        public DisplayItem CookieInstructions { get; private set; }

        public ConfigurationDataCookie()
        {
            Cookie = new StringItem { Name = "Cookie" };
            CookieInstructions = new DisplayItem(
            "Please enter the cookie for the site manually. <a href=\"https://github.com/Jackett/Jackett/wiki/Finding-cookies\" target=\"_blank\">See here</a> on how get the cookies." +
            "<br>Example cookie header (usually longer than this):<br><code>PHPSESSID=8rk27odm; ipsconnect_63ad9c=1; more_stuff=etc;</code>")
            {
                Name = "Cookie Instructions"
            };
        }
    }

}
