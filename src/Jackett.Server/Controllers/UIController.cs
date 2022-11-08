using System;
using System.Collections.Generic;
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
    [Route("[action]")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class WebUIController : Controller
    {
        private readonly IConfigurationService config;
        private readonly ServerConfig serverConfig;
        private readonly ISecurityService securityService;
        private readonly Logger logger;

        public WebUIController(IConfigurationService config, ISecurityService ss, ServerConfig s, Logger l)
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

            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return Redirect("indexers");
            }

            return new PhysicalFileResult(config.GetContentFolder() + "/login.html", "text/html");
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromForm] string password)
        {
            if (password != null && securityService.HashPassword(password) == serverConfig.AdminPassword)
            {
                await MakeUserAuthenticated();
            }

            return Redirect("indexers");
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("login");
        }

        [HttpGet]
        public IActionResult Indexers() => new PhysicalFileResult(config.GetContentFolder() + "/indexers.html", "text/html");

        [HttpGet]
        public IActionResult Cache() => new PhysicalFileResult(config.GetContentFolder() + "/cache.html", "text/html");

        [HttpGet]
        public IActionResult Configure() => new PhysicalFileResult(config.GetContentFolder() + "/configure.html", "text/html");

        [HttpGet]
        public IActionResult Search() => new PhysicalFileResult(config.GetContentFolder() + "/search.html", "text/html");

        [HttpGet]
        public IActionResult Logs() => new PhysicalFileResult(config.GetContentFolder() + "/logs.html", "text/html");

        [HttpGet]
        public IActionResult Settings() => new PhysicalFileResult(config.GetContentFolder() + "/settings.html", "text/html");

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
