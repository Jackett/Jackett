using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.IndexerConfig
{
    class ConfigurationDataBasicLoginFrenchTorrentDb : ConfigurationData
    {
        public StringItem Cookie { get; private set; }

        public ConfigurationDataBasicLoginFrenchTorrentDb()
        {
            Cookie = new StringItem { Name = "Cookie" };
        }

        public override Item[] GetItems()
        {
            return new Item[] { Cookie };
        }
    }
}
