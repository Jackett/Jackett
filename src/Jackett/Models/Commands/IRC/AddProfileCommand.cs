using Jackett.Models.Irc;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.Commands.IRC
{
    public class AddProfileCommand : INotification
    {
        public IRCProfile Profile { get; set; }
    }
}
