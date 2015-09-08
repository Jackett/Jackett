using IrcDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.Irc
{
    public class Channel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<Message> Messages { get; } = new List<Message>();
        public List<User> Users { get; } = new List<User>();
        public bool Joined { get; set; }
    }
}
