using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jackett.Common;

namespace Jackett.Utils
{
    class WebAPIRequestLogger : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            //logging request body
            string requestBody = await request.Content.ReadAsStringAsync();
            Trace.WriteLine(requestBody);
            Engine.Logger.Debug(request.Method +  ": " + request.RequestUri);
            Engine.Logger.Debug("Body: " + requestBody);

            //let other handlers process the request
            return await base.SendAsync(request, cancellationToken)
                .ContinueWith(task =>
                {
                    if (null != task.Result.Content)
                    {
                        //once response is ready, log it
                        var responseBody = task.Result.Content.ReadAsStringAsync().Result;
                        Trace.WriteLine(responseBody);
                        Engine.Logger.Debug("Response: " + responseBody);
                    }
                    return task.Result;
                });
        }
    }
}
