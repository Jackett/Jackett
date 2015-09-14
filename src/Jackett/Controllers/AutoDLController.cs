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
    public class AutoDLController : ApiController
    {
        IAutoDLProfileervice autodlService;

        public AutoDLController(IAutoDLProfileervice a)
        {
            autodlService = a;
        }

        [HttpGet]
        [Route("AutoDL/Summary")]
        public List<NetworkSummary> Summary()
        {
            return autodlService.GetNetworks();
        }

        [HttpGet]
        public List<AutoDLProfileSummary> Index()
        {
            return autodlService.GetProfiles();
        }

        [HttpPut]
        public IHttpActionResult Put([FromBody]AutoDLProfileSummary profile)
        {
            autodlService.Set(profile);
            return Ok();
        }
    }
}
