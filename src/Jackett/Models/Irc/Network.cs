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
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public List<Channel> Channels { get; } = new List<Channel>();
        public List<Message> Messages { get; } = new List<Message>();
        public StandardIrcClient Client = null;
        public bool UseSSL { set; get; } = false;
    }
}
