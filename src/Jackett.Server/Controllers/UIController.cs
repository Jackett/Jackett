using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Jackett.Common.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jackett.Server.Controllers
{
    [Route("UI/[action]")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class WebUIController : Controller
    {
        private readonly IConfigurationService _config;
        private readonly ISecurityService _securityService;

        public WebUIController(IConfigurationService config, ISecurityService ss)
        {
            _config = config;
            _securityService = ss;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromQuery] string cookiesChecked)
        {
            if (string.IsNullOrEmpty(cookiesChecked))
            {
                HttpContext.Response.Cookies.Append("TestCookie", "1");
                return Redirect("TestCookie");
            }

            if (_securityService.CheckAuthorised(string.Empty))
            {
                await MakeUserAuthenticated();
                return Redirect("Dashboard");
            }

            return User.Identity.IsAuthenticated
                ? Redirect("Dashboard")
                : (IActionResult)new PhysicalFileResult(_config.GetContentFolder() + "/login.html", "text/html");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult TestCookie()
        {
            if (HttpContext.Request.Cookies.Any(x => x.Key == "TestCookie"))
            {
                return Redirect("Login?cookiesChecked=1");
            }
            return BadRequest("Cookies required");
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("Login");
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Dashboard([FromForm] string password)
        {
            if (_securityService.CheckAuthorised(password))
                await MakeUserAuthenticated();

            return Redirect("Dashboard");
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            var logout = HttpContext.Request.Query.Where(x => string.Equals(x.Key, "logout", StringComparison.OrdinalIgnoreCase)
                                                            && string.Equals(x.Value, "true", StringComparison.OrdinalIgnoreCase)).Any();

            if (logout)
            {
                return Redirect("Logout");
            }

            return new PhysicalFileResult(_config.GetContentFolder() + "/index.html", "text/html");
        }

        //TODO: Move this to security service once off Mono
        private async Task MakeUserAuthenticated()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "Jackett", ClaimValueTypes.String)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties
                {
                    ExpiresUtc = DateTime.UtcNow.AddDays(14), //Cookie expires at end of session
                    IsPersistent = true,
                    AllowRefresh = true
                });
        }
    }
}
