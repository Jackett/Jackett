using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.Sync
{
    public class Change
    {
        public Change()
        {
            Properties = new Dictionary<string, object>();
        }

        public uint Id { get; set; }
        public Dictionary<string, object> Properties { get; set; }
    }
}
