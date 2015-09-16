using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.Commands.IRC
{
    public class IRCMessageEvent : INotification
    {
        public string Network { get; set; }
        public string Channel { get; set;}
        public string Profile { get; set; }
        public string Message { get; set; }
        public string From { get; set; }
    }
}
