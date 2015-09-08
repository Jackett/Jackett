using Jackett.Models.DTO;
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
    public class IRCStateController : ApiController
    {
        private IIRCService irc;

        public IRCStateController(IIRCService i)
        {
            irc = i;
        }

        [HttpGet]
        public List<NetworkDTO> Get()
        {
            return irc.GetSummary();
        }
    }
}
