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
    [Route("UI/[action]"), ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class WebUIController : Controller
    {
        private readonly IConfigurationService _config;
        private readonly ServerConfig _serverConfig;
        private readonly ISecuityService _securityService;
        private readonly Logger _logger;

        public WebUIController(IConfigurationService config, ISecuityService ss, ServerConfig s, Logger l)
        {
            _config = config;
            _serverConfig = s;
            _securityService = ss;
            _logger = l;
        }

        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> LoginAsync()
        {
            if (string.IsNullOrEmpty(_serverConfig.AdminPassword))
                await MakeUserAuthenticatedAsync();
            return User.Identity.IsAuthenticated
                ? Redirect("Dashboard")
                : (IActionResult)new PhysicalFileResult($"{_config.GetContentFolder()}/login.html", "text/html");
            ;
        }

        [HttpGet, AllowAnonymous]
        public async Task<IActionResult> LogoutAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("Login");
        }

        [HttpPost, AllowAnonymous]
        public async Task<IActionResult> DashboardAsync([FromForm] string password)
        {
            if (password != null && _securityService.HashPassword(password) == _serverConfig.AdminPassword)
                await MakeUserAuthenticatedAsync();
            return Redirect("Dashboard");
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            var logout = HttpContext.Request.Query.Where(
                x => string.Equals(x.Key, "logout", StringComparison.OrdinalIgnoreCase) &&
                     string.Equals(x.Value, "true", StringComparison.OrdinalIgnoreCase)).Any();
            return logout ? Redirect("Logout") : (IActionResult)new PhysicalFileResult($"{_config.GetContentFolder()}/index.html", "text/html");
        }

        //TODO: Move this to security service once off Mono
        private async Task MakeUserAuthenticatedAsync()
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, "Jackett", ClaimValueTypes.String) };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties
                {
                    ExpiresUtc = DateTime.UtcNow.AddDays(14), //Cookie expires at end of session
                    IsPersistent = true,
                    AllowRefresh = true
                });
        }
    }
}
