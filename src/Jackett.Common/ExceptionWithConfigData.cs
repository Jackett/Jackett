using System;
using Jackett.Common.Models.IndexerConfig;

namespace Jackett.Common
{

    public class ExceptionWithConfigData : Exception
    {
        public ConfigurationData ConfigData { get; private set; }
        public ExceptionWithConfigData(string message, ConfigurationData data)
            : base(message)
            => ConfigData = data;

    }
}
