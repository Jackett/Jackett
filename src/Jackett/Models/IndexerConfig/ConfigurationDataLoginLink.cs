using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.IndexerConfig
{
    public class ConfigurationDataLoginLink : ConfigurationData
    {
        public StringItem LoginLink { get; private set; }
        public HiddenItem RSSKey { get; private set; }
        public DisplayItem DisplayText { get; private set; }

        public ConfigurationDataLoginLink()
        {
            LoginLink = new StringItem { Name = "Login Link" };
            RSSKey = new HiddenItem { Name = "RSSKey" };
            DisplayText = new DisplayItem(""){ Name = "" };
        }
    }
}
