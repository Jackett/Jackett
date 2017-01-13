using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.ExceptionHandling;
using Jackett.Utils;
using System.Net.Http;
using System.Net;
using System.Web.Http;
using System.Net.Sockets;

namespace Jackett
{
    class WebAPIExceptionHandler : IExceptionHandler
    {
        public virtual Task HandleAsync(ExceptionHandlerContext context,
                                   CancellationToken cancellationToken)
        {
            if (!ShouldHandle(context))
            {
                return Task.FromResult(0);
            }

            return HandleAsyncCore(context, cancellationToken);
        }

        public virtual Task HandleAsyncCore(ExceptionHandlerContext context,
                                           CancellationToken cancellationToken)
        {
            HandleCore(context);
            return Task.FromResult(0);
        }

        public virtual void HandleCore(ExceptionHandlerContext context)
        {
            // attempt to fix #930
            if (context.Exception is SocketException)
            {
                Engine.Logger.Error("Ignoring unhandled SocketException: " + context.Exception.GetExceptionDetails());
                return;
            }

            Engine.Logger.Error("HandleCore(): unhandled exception: " + context.Exception.GetExceptionDetails());

            var resp = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(context.Exception.Message),
                ReasonPhrase = "Jackett_InternalServerError"
            };
            throw new HttpResponseException(resp);
        }

        public virtual bool ShouldHandle(ExceptionHandlerContext context)
        {
            return context.ExceptionContext.CatchBlock.IsTopLevel;
        }
    }
}
