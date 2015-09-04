using IrcDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.Irc
{
    public class Network 
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public List<Message> Messages { get; } = new List<Message>();
        public StandardIrcClient Client = null;
    }
}
