using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett
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
            "<ol><li>Login to BeyondHD in your browser <li>Open the developer console, go the network tab <li>Find 'cookie' in the request headers <li>Copy & paste it to here</ol>")
            {
                Name = "CookieHint"
            };
            CookieExample = new DisplayItem(
            "Example cookie header (usually longer than this):<br><code>PHPSESSID=8rk27odm; ipsconnect_63ad9c=1; more_stuff=etc;</code>")
            {
                Name = "CookieExample"
            };
        }

        public override Item[] GetItems()
        {
            return new Item[] { Cookie, CookieHint, CookieExample };
        }

        public string CookieHeader
        {
            get
            {
                return Cookie.Value.Trim().TrimStart(new char[] { '"' }).TrimEnd(new char[] { '"' });
            }
        }
    }

}
