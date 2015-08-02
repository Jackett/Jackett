using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.IndexerConfig
{
    class ConfigurationDataBasicLoginAnimeBytes : ConfigurationDataBasicLogin
    {
        public BoolItem IncludeRaw { get; private set; }
        public DisplayItem DateWarning { get; private set; }

        public ConfigurationDataBasicLoginAnimeBytes()
            : base()
        {
            IncludeRaw = new BoolItem() { Name = "IncludeRaw", Value = false };
            DateWarning = new DisplayItem("This tracker does not supply upload dates so they are based off year of release.") { Name = "DateWarning" };
        }

        public override Item[] GetItems()
        {
            return new Item[] { Username, Password, IncludeRaw, DateWarning };
        }
    }
}
