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
            catch (Exception ex)
            {
                try
                {
                    var msg = "";
                    var json = new JObject();

                    logger.Error(ex);

                    var message = ex.Message;
                    if (ex.InnerException != null)
                    {
                        message += ": " + ex.InnerException.Message;
                    }

                    msg = message;

                    if (ex is ExceptionWithConfigData)
                    {
                        json["config"] = ((ExceptionWithConfigData)ex).ConfigData.ToJson(null, false);
                    }

                    json["result"] = "error";
                    json["error"] = msg;
                    json["stacktrace"] = ex.StackTrace;
                    if (ex.InnerException != null)
                    {
                        json["innerstacktrace"] = ex.InnerException.StackTrace;
                    }

                    httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    httpContext.Response.ContentType = "application/json";
                    await httpContext.Response.WriteAsync(json.ToString());
                    return;
                }
                catch (Exception ex2)
                {
                    logger.Error(ex2, "An exception was thrown attempting to execute the custom exception error handler.");
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
