using Jackett.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Jackett.Models.IndexerConfig;

namespace Jackett
{

    public class ExceptionWithConfigData : Exception
    {
        public ConfigurationData ConfigData { get; private set; }
        public ExceptionWithConfigData(string message, ConfigurationData data)
            : base(message)
        {
            ConfigData = data;
        }

    }
}
