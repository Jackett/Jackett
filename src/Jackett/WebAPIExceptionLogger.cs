using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.ExceptionHandling;
using Jackett.Utils;

namespace Jackett
{
    class WebAPIExceptionLogger : IExceptionLogger
    {
        public async Task LogAsync(ExceptionLoggerContext context, CancellationToken cancellationToken)
        {
            Engine.Logger.Error("Unhandled exception: " + context.Exception.GetExceptionDetails());
            var request = await context.Request.Content.ReadAsStringAsync();
            Engine.Logger.Error("Unhandled exception url: " + context.Request.RequestUri);
            Engine.Logger.Error("Unhandled exception request body: " + request);
        }
    }
}
