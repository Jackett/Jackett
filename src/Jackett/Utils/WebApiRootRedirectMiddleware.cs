using Jackett.Services;
using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Utils
{
    public class WebApiRootRedirectMiddleware : OwinMiddleware
    {
        public WebApiRootRedirectMiddleware(OwinMiddleware next)
            : base(next)
        { 
        }

        public async override Task Invoke(IOwinContext context)
        {
            var url = context.Request.Uri;
            if (string.IsNullOrWhiteSpace(url.AbsolutePath) || url.AbsolutePath == "/")
            {
                // 301 is the status code of permanent redirect
                context.Response.StatusCode = 301;
                context.Response.Headers.Set("Location", ServerService.BasePath(context.Request.Uri.AbsolutePath) + "Admin/Dashboard");
            }
            else
            {
                await Next.Invoke(context);
            }
        }
    }
}