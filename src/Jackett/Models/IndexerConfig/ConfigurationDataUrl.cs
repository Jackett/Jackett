using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.IndexerConfig
{
    public class ConfigurationDataUrl : ConfigurationData
    {
        public StringItem Url { get; private set; }

        public ConfigurationDataUrl(Uri defaultUrl)
        {
            Url = new StringItem { Name = "Url", Value = defaultUrl.ToString() };
        }

        public ConfigurationDataUrl(string defaultUrl)
        {
            Url = new StringItem { Name = "Url", Value = defaultUrl };
        }
    }
}
