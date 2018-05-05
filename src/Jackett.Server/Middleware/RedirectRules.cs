using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Net.Http.Headers;

namespace Jackett.Server.Middleware
{
    public class RedirectRules
    {
        public static void RedirectToDashboard(RewriteContext context)
        {
            var request = context.HttpContext.Request;

            if (request.Path == null || string.IsNullOrWhiteSpace(request.Path.ToString()) || request.Path.ToString() == "/")
            {
                // 301 is the status code of permanent redirect
                var redir = Initialisation.ServerService.BasePath() + "/UI/Dashboard";
                var response = context.HttpContext.Response;
                response.StatusCode = StatusCodes.Status301MovedPermanently;
                context.Result = RuleResult.EndResponse;
                response.Headers[HeaderNames.Location] = redir;
            }
        }
    }
}
