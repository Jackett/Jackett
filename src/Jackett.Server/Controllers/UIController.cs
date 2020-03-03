using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace Jackett.Server.Controllers
{
    [Route("UI/[action]")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class WebUIController : Controller
    {
        private readonly IConfigurationService config;
        private readonly ServerConfig serverConfig;
        private readonly ISecuityService securityService;
        private readonly Logger logger;

        public WebUIController(IConfigurationService config, ISecuityService ss, ServerConfig s, Logger l)
        {
            this.config = config;
            serverConfig = s;
            securityService = ss;
            logger = l;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login()
        {
            if (string.IsNullOrEmpty(serverConfig.AdminPassword))
            {
                await MakeUserAuthenticated();
            }

            if (User.Identity.IsAuthenticated)
            {
                return Redirect("Dashboard");
            }

            return new PhysicalFileResult(config.GetContentFolder() + "/login.html", "text/html");
            ;
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
            if (password != null && securityService.HashPassword(password) == serverConfig.AdminPassword)
            {
                await MakeUserAuthenticated();
            }

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

            return new PhysicalFileResult(config.GetContentFolder() + "/index.html", "text/html");
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
