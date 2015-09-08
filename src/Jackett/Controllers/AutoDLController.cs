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
        IAutoDLProfileService autodlService;

        public AutoDLController(IAutoDLProfileService a)
        {
            autodlService = a;
        }

        [HttpGet]
        public List<NetworkSummary> Summary()
        {
            return autodlService.GetNetworks();
        }
    }
}
