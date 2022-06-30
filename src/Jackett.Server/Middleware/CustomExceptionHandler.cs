using System;
using System.Threading.Tasks;
using Jackett.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Server.Middleware
{
    public class CustomExceptionHandler
    {
        private readonly RequestDelegate _next;
        private readonly Logger logger;

        public CustomExceptionHandler(RequestDelegate next, Logger l)
        {
            _next = next;
            logger = l;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception e)
            {
                try
                {
                    logger.Error(e);

                    var message = e.Message;
                    if (e.InnerException != null)
                        message += ": " + e.InnerException.Message;
                    var msg = message;

                    var json = new JObject();
                    if (e is ExceptionWithConfigData)
                        json["config"] = ((ExceptionWithConfigData)e).ConfigData.ToJson(null, false);

                    json["result"] = "error";
                    json["error"] = msg;
                    json["stacktrace"] = e.StackTrace;
                    if (e.InnerException != null)
                        json["innerstacktrace"] = e.InnerException.StackTrace;

                    httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    httpContext.Response.ContentType = "application/json";
                    await httpContext.Response.WriteAsync(json.ToString());
                    return;
                }
                catch (Exception e2)
                {
                    logger.Error($"An exception was thrown attempting to execute the custom exception error handler.\n{e2}");
                }

                await _next(httpContext);
            }
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class CustomExceptionHandlerExtensions
    {
        public static IApplicationBuilder UseCustomExceptionHandler(this IApplicationBuilder builder) => builder.UseMiddleware<CustomExceptionHandler>();
    }
}
