using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.Irc.DTO
{
    public class NetworkInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public List<ChannelInfo> Channels { get; } = new List<ChannelInfo>();
    }
}
