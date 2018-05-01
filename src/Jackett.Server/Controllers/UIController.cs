using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using Jackett.Utils;
using MimeMapping;
using NLog;

namespace Jackett.Controllers
{
    [RoutePrefix("UI")]
    [JackettAuthorized]
    [JackettAPINoCache]
    public class WebUIController : ApiController
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
        [AllowAnonymous]
        public IHttpActionResult Logout()
        {
            var ctx = Request.GetOwinContext();
            var authManager = ctx.Authentication;
            authManager.SignOut("ApplicationCookie");
            return Redirect("UI/Dashboard");
        }

        [HttpGet]
        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> Dashboard()
        {
            if (Request.RequestUri.Query != null && Request.RequestUri.Query.Contains("logout"))
            {
                var file = GetFile("login.html");
                securityService.Logout(file);
                return file;
            }


            if (securityService.CheckAuthorised(Request))
            {
                return GetFile("index.html");

            }
            else
            {
                var formData = await Request.Content.ReadAsFormDataAsync();

                if (formData != null && securityService.HashPassword(formData["password"]) == serverConfig.AdminPassword)
                {
                    var file = GetFile("index.html");
                    securityService.Login(file);
                    return file;
                }
                else
                {
                    return GetFile("login.html");
                }
            }
        }

      
    }
}
