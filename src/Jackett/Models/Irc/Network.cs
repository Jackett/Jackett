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
        public string Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public List<string> Address { get; set; } = new List<string>();
        public int AddressIndex { set; get; }
        public List<Channel> Channels { get; } = new List<Channel>();
        public List<Message> Messages { get; } = new List<Message>();
        public StandardIrcClient Client = null;
        public bool UseSSL { set; get; } = false;
    }
}
