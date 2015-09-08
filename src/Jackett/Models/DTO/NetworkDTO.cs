using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.DTO
{
    public class NetworkDTO
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public List<ChannelDTO> Channels { get; set; } = new List<ChannelDTO>();
    }
}
