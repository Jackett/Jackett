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

        public IRCProfileController(IIRCProfileService p)
        {
            profileService = p;
        }

        [HttpGet]
        public List<IRCProfile> Index()
        {
            return profileService.All;
        }

        [HttpGet]
        public IHttpActionResult Get(string id)
        {
            var item = profileService.Get(id);
            if (item == null)
                return NotFound();
            return Content(System.Net.HttpStatusCode.OK, item);
        }

        public IHttpActionResult Put([FromBody]IRCProfile profile)
        {
            profileService.Set(profile);
            return Ok();
        }


        public IHttpActionResult Delete(string id)
        {
            profileService.Delete(id);
            return Ok();
        }
    }
}
