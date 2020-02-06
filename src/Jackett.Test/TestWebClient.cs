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
        private readonly Dictionary<WebRequest, Func<WebRequest, WebClientByteResult>> _byteCallbacks =
            new Dictionary<WebRequest, Func<WebRequest, WebClientByteResult>>();

        private readonly Dictionary<WebRequest, Func<WebRequest, WebClientStringResult>> _stringCallbacks =
            new Dictionary<WebRequest, Func<WebRequest, WebClientStringResult>>();

        public TestWebClient(IProcessService p, Logger l, IConfigurationService c, ServerConfig sc) : base(p, l, c, sc)
        {
        }

        public void RegisterByteCall(WebRequest req, Func<WebRequest, WebClientByteResult> f) => _byteCallbacks.Add(req, f);

        public void RegisterStringCall(WebRequest req, Func<WebRequest, WebClientStringResult> f) =>
            _stringCallbacks.Add(req, f);

        public override Task<WebClientByteResult> GetBytesAsync(WebRequest request) => Task.FromResult(
            _byteCallbacks.Where(r => r.Key.Equals(request)).First().Value.Invoke(request));

        public override Task<WebClientStringResult> GetStringAsync(WebRequest request) => Task.FromResult(
            _stringCallbacks.Where(r => r.Key.Equals(request)).First().Value.Invoke(request));

        public override void Init()
        {
        }
    }
}
