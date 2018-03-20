using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Jackett.Common;

namespace Jackett.Utils
{
    public class JackettAuthorizedAttribute : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            // Skip authorisation on blank passwords
            if (string.IsNullOrEmpty(Engine.ServerConfig.AdminPassword))
            {
                return;
            }

            if (!Engine.SecurityService.CheckAuthorised(actionContext.Request))
            {
                if (actionContext.ControllerContext.ControllerDescriptor.ControllerType.GetCustomAttributes(true).Where(a => a.GetType() == typeof(AllowAnonymousAttribute)).Any())
                {
                    return;
                }

                if (actionContext.ControllerContext.ControllerDescriptor.ControllerType.GetMethod(actionContext.ActionDescriptor.ActionName).GetCustomAttributes(true).Where(a => a.GetType() == typeof(AllowAnonymousAttribute)).Any())
                {
                    return;
                }


                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode
                                                                                  .Unauthorized);
            }
        }
    }
}
