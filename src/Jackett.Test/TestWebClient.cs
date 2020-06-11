using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Test
{

    // Currently not used in any Unit tests. Leaving it for potential future testing purposes.
    public class TestWebClient : WebClient
    {
        private readonly Dictionary<WebRequest, Func<WebRequest, WebResult>> _requestCallbacks = new Dictionary<WebRequest, Func<WebRequest, WebResult>>();

        public TestWebClient(IProcessService p, Logger l, IConfigurationService c, ServerConfig sc)
            : base(p, l, c, sc)
        {
        }

        public void RegisterRequestCallback(WebRequest req, Func<WebRequest, WebResult> f) => _requestCallbacks.Add(req, f);

        public override Task<WebResult> GetResultAsync(WebRequest request) => Task.FromResult(_requestCallbacks.First(r => r.Key.Equals(request)).Value.Invoke(request));

        public override void Init()
        {

        }
    }
}
