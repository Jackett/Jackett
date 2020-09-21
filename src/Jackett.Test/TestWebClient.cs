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
    public class TestWebClient : WebClient
    {
        private readonly Dictionary<WebRequest, Func<WebRequest, WebClientByteResult>> byteCallbacks = new Dictionary<WebRequest, Func<WebRequest, WebClientByteResult>>();
        private readonly Dictionary<WebRequest, Func<WebRequest, WebClientStringResult>> stringCallbacks = new Dictionary<WebRequest, Func<WebRequest, WebClientStringResult>>();

        public TestWebClient(IProcessService p, Logger l, IConfigurationService c, ServerConfig sc)
            : base(p: p,
                   l: l,
                   c: c,
                   sc: sc)
        {
        }

        public void RegisterByteCall(WebRequest req, Func<WebRequest, WebClientByteResult> f) => byteCallbacks.Add(req, f);

        public void RegisterStringCall(WebRequest req, Func<WebRequest, WebClientStringResult> f) => stringCallbacks.Add(req, f);

        public override Task<WebClientByteResult> GetBytes(WebRequest request) => Task.FromResult<WebClientByteResult>(byteCallbacks.Where(r => r.Key.Equals(request)).First().Value.Invoke(request));

        public override Task<WebClientStringResult> GetString(WebRequest request) => Task.FromResult<WebClientStringResult>(stringCallbacks.Where(r => r.Key.Equals(request)).First().Value.Invoke(request));

        public override void Init()
        {

        }
    }
}
