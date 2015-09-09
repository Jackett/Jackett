using Jackett.Models.DTO;
using Jackett.Models.Irc;
using Jackett.Services;
using Jackett.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Jackett.Controllers
{
    [JackettAuthorized]
    [JackettAPINoCache]
    public class IRCChannelController : ApiController
    {
        private IIRCService irc;

        public IRCChannelController(IIRCService i)
        {
            irc = i;
        }

        [HttpGet]
        public List<Message> Messages(string network)
        {
            return irc.GetMessages(network, null);
        }

        [HttpGet]
        public List<Message> Messages(string network, string room)
        {
            return irc.GetMessages(network, room);
        }

        [HttpGet]
        public List<User> Users(string network, string room)
        {
            return irc.GetUser(network, room);
        }

        [HttpPost]
        public IHttpActionResult Command([FromBody]IRCommandDTO command)
        {
            irc.ProcessCommand(command.NetworkId, command.ChannelId, command.Text);
            return Ok();
        }
    }
}
