using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.AutoDL
{
    public class ServerInfo
    {
        public string Network { get; set; }
        public List<string> Servers { get; set; } = new List<string>();
        public List<string> Channels { get; set; } = new List<string>();
        public List<string> Announcers { get; set; } = new List<string>();
    }
}
