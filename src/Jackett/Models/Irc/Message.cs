using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.Irc
{
    public class Message
    {
        public string Text { get; set; }
        public DateTime When { get; set; } = DateTime.Now;
        public string From { get; set; }
        public MessageType Type { get; set; } = MessageType.Message;
    }
}
