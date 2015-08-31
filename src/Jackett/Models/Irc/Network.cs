using Jackett.Models.Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.Irc
{
    public class Network : SyncObjectBase
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public List<Message> Messages => new List<Message>();
    }
}
