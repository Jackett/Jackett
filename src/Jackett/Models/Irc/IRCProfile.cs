using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.Irc
{
    public class IRCProfile
    {
        public string Name { get; set; }
        public List<string> Servers { get; set; } = new List<string>();
        public string Username { get; set; }
    }
}
