using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.IndexerConfig
{
    class PretomeConfiguration : ConfigurationDataBasicLogin
    {
        public StringItem Pin { get; private set; }

        public PretomeConfiguration() : base()
        {
            Pin = new StringItem { Name = "Login Pin Number" };
        }

        public override Item[] GetItems()
        {
            return new Item[] { Pin, Username, Password };
        }
    }
}
