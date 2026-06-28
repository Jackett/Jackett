using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
            return Task.FromResult<IActionResult>(Content("OK", "text/plain", System.Text.Encoding.UTF8));
        }
    }
}
