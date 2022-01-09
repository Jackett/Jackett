using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace Jackett.Server.Controllers
{
    [AllowAnonymous]
    [Route("health")]
    public class HealthcheckController : Controller
    {
        [HttpGet]
        [HttpHead]
        public Task<IActionResult> Health()
        {
            var jsonReply = new JObject
            {
                ["status"] = "OK"
            };
            return Task.FromResult<IActionResult>(Json(jsonReply));
        }
    }
}
