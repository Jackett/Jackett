using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public class CustomException : Exception
    {
        public CustomException(string message)
            : base(message)
        { }
    }
}
