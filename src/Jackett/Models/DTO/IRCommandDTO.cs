using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.DTO
{
    public class IRCommandDTO
    {
        public string Text { get; set; }
        public string NetworkId { get; set; }
        public string ChannelId { get; set; }
    }
}
