using Jackett.Services;
using Jackett.Utils.Clients;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JackettTest
{
    public class TestWebClient : IWebClient
    {
        private Dictionary<WebRequest, Func<WebRequest, WebClientByteResult>> byteCallbacks = new Dictionary<WebRequest, Func<WebRequest, WebClientByteResult>>();
        private Dictionary<WebRequest, Func<WebRequest, WebClientStringResult>> stringCallbacks = new Dictionary<WebRequest, Func<WebRequest, WebClientStringResult>>();

        public TestWebClient(IProcessService p, Logger l, IConfigurationService c)
            : base(p: p,
                   l: l,
                   c: c)
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
