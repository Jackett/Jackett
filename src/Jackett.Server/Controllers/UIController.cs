using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using MimeMapping;
using NLog;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Jackett.Server.Controllers
{
    [Route("UI/[action]")]
    //[JackettAuthorized]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class WebUIController : Controller
    {
        private IConfigurationService config;
        private ServerConfig serverConfig;
        private ISecuityService securityService;
        private Logger logger;

        public WebUIController(IConfigurationService config, ISecuityService ss, ServerConfig s, Logger l)
        {
            this.config = config;
            serverConfig = s;
            securityService = ss;
            logger = l;
        }

        private HttpResponseMessage GetFile(string path)
        {
            var result = new HttpResponseMessage(HttpStatusCode.OK);
            var mappedPath = Path.Combine(config.GetContentFolder(), path);
            var stream = new FileStream(mappedPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            result.Content = new StreamContent(stream);
            result.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeUtility.GetMimeMapping(mappedPath));

            return result;
        }

        [HttpGet]
        //[AllowAnonymous]
        public IActionResult Logout()
        {
            var ctx = Request.HttpContext;
            //TODO
            //var authManager = ctx.Authentication;
            //authManager.SignOut("ApplicationCookie");
            return Redirect("Dashboard");
        }

        [HttpGet]
        [HttpPost]
        //[AllowAnonymous]
        public async Task<HttpResponseMessage> Dashboard()
        {
            if (Request.Path != null && Request.Path.ToString().Contains("logout"))
            {
                var file = GetFile("login.html");
                securityService.Logout(file);
                return file;
            }

            //TODO

            //if (securityService.CheckAuthorised(Request))
            //{
            return GetFile("index.html");

            //}
            //else
            //{
            //    var formData = await Request.ReadFormAsync();

            //    if (formData != null && securityService.HashPassword(formData["password"]) == serverConfig.AdminPassword)
            //    {
            //        var file = GetFile("index.html");
            //        securityService.Login(file);
            //        return file;
            //    }
            //    else
            //    {
            //        return GetFile("login.html");
            //    }
            //}
        }
    }
}
