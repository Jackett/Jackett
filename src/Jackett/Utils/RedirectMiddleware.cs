using Microsoft.Owin;
using System;
using System.Threading.Tasks;
using Jackett.Common;

namespace Jackett.Utils
{
    public class WebApiRootRedirectMiddleware : OwinMiddleware
    {
        public WebApiRootRedirectMiddleware(OwinMiddleware next)
            : base(next)
        {
            //Ideally we'd dependency inject the server config into the middleware but AutoFac's Owin package has not been updated to support Autofac > 5
        }

        public async override Task Invoke(IOwinContext context)
        {
            if (context.Request.Path != null && context.Request.Path.HasValue && context.Request.Path.Value.StartsWith(Engine.ServerConfig.RuntimeSettings.BasePath, StringComparison.Ordinal))
            {
                context.Request.Path = new PathString(context.Request.Path.Value.Substring(Engine.ServerConfig.RuntimeSettings.BasePath.Length));
            }

            if (context.Request.Path == null || string.IsNullOrWhiteSpace(context.Request.Path.ToString()) || context.Request.Path.ToString() == "/")
            {
                // 301 is the status code of permanent redirect
                context.Response.StatusCode = 302;
                var redir = Engine.ServerConfig.RuntimeSettings.BasePath + "/UI/Dashboard";
                Engine.Logger.Info("redirecting to " + redir);
                context.Response.Headers.Set("Location", redir);
            }
            else
            {
                await Next.Invoke(context);
            }
        }
    }

    public class LegacyApiRedirectMiddleware : OwinMiddleware
    {
        public LegacyApiRedirectMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        public async override Task Invoke(IOwinContext context)
        {
            if (context.Request.Path == null || string.IsNullOrWhiteSpace(context.Request.Path.ToString()) || context.Request.Path.Value.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
            {
                // 301 is the status code of permanent redirect
                context.Response.StatusCode = 302;
                var redir = context.Request.Path.Value.Replace("/Admin", "/UI");
                Engine.Logger.Info("redirecting to " + redir);
                context.Response.Headers.Set("Location", redir);
            }
            else
            {
                await Next.Invoke(context);
            }
        }
    }
}