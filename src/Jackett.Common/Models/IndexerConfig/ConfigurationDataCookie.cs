namespace Jackett.Common.Models.IndexerConfig
{

    public class ConfigurationDataCookie : ConfigurationData
    {
        public StringItem Cookie { get; private set; }
        public DisplayItem CookieHint { get; private set; }
        public DisplayItem CookieExample { get; private set; }

        public ConfigurationDataCookie()
        {
            Cookie = new StringItem { Name = "Cookie" };
            CookieHint = new DisplayItem(
            "<ol><li>Login to this tracker in your browser <li>Open the developer console, go the network tab <li>Find 'cookie' in the request headers <li>Copy & paste it to here</ol>")
            {
                Name = "CookieHint"
            };
            CookieExample = new DisplayItem(
            "Example cookie header (usually longer than this):<br><code>PHPSESSID=8rk27odm; ipsconnect_63ad9c=1; more_stuff=etc;</code>")
            {
                Name = "CookieExample"
            };
        }
    }

}
