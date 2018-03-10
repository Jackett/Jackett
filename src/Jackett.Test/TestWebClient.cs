using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jackett.Test
{
    public class TestWebClient : WebClient
    {
        private Dictionary<WebRequest, Func<WebRequest, WebClientByteResult>> byteCallbacks = new Dictionary<WebRequest, Func<WebRequest, WebClientByteResult>>();
        private Dictionary<WebRequest, Func<WebRequest, WebClientStringResult>> stringCallbacks = new Dictionary<WebRequest, Func<WebRequest, WebClientStringResult>>();

        public TestWebClient(IProcessService p, Logger l, IConfigurationService c, ServerConfig sc)
            : base(p: p,
                   l: l,
                   c: c,
                   sc: sc)
        {
        }

        public void RegisterByteCall(WebRequest req, Func<WebRequest, WebClientByteResult> f)
        {
            byteCallbacks.Add(req, f);
        }

        public void RegisterStringCall(WebRequest req, Func<WebRequest, WebClientStringResult> f)
        {
            stringCallbacks.Add(req, f);
        }

        override public Task<WebClientByteResult> GetBytes(WebRequest request)
        {
           return Task.FromResult< WebClientByteResult>(byteCallbacks.Where(r => r.Key.Equals(request)).First().Value.Invoke(request));
        }

        override public Task<WebClientStringResult> GetString(WebRequest request)
        {
            return Task.FromResult<WebClientStringResult>(stringCallbacks.Where(r => r.Key.Equals(request)).First().Value.Invoke(request));
        }

        override public void Init()
        {
          
        }
    }
}
