using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.IndexerConfig
{
    public class ConfigurationDataBasicLoginWithRSS : ConfigurationData
    {
        public StringItem Username { get; private set; }
        public StringItem Password { get; private set; }
        public HiddenItem RSSKey { get; private set; }

        public ConfigurationDataBasicLoginWithRSS()
        {
            Username = new StringItem { Name = "Username" };
            Password = new StringItem { Name = "Password" };
            RSSKey = new HiddenItem { Name = "RSSKey" };
        }
    }
}
