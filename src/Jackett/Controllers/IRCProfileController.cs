using Jackett.Models.AutoDL;
using Jackett.Models.Irc;
using Jackett.Services;
using Jackett.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Jackett.Controllers
{
    [JackettAuthorized]
    [JackettAPINoCache]
    public class IRCProfileController : ApiController
    {
        IIRCProfileService profileService;
        IAutoDLProfileService autodlService;

        public IRCProfileController(IIRCProfileService p, IAutoDLProfileService a)
        {
            profileService = p;
            autodlService = a;
        }

        [HttpGet]
        public List<IRCProfile> Index()
        {
            return profileService.All;
        }

        public IHttpActionResult Get(string name)
        {
            var item = profileService.Get(name);
            if (item == null)
                return NotFound();
            return Content(System.Net.HttpStatusCode.OK, item);
        }

        public IHttpActionResult Put([FromBody]IRCProfile profile)
        {
            profileService.Set(profile);
            return Ok();
        }

        [HttpGet]
        public List<NetworkSummary> AutoDLProfiles()
        {
            return autodlService.GetNetworks();
        }
    }
}
