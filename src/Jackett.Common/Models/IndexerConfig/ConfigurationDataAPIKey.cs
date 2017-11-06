using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.IndexerConfig
{
    public class ConfigurationDataAPIKey : ConfigurationData
    {
        public ConfigurationData.StringItem Key { get; private set; }

        public ConfigurationDataAPIKey()
        {
            Key = new ConfigurationData.StringItem { Name = "APIKey", Value = string.Empty };
        }
    }
}
