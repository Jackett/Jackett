using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.Irc
{
    public class ChannelInfoResult
    {
        public Network Network { get; set; }
        public Channel Channel { get; set; }
    }
}
