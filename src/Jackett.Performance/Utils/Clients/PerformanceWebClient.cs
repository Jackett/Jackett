using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;

using NLog;

namespace Jackett.Performance.Utils.Clients
{
    public class PerformanceWebClient : WebClient
    {
        private readonly WebClient webClient;
        private readonly ConcurrentDictionary<WebRequest, Task<WebResult>> _cache = new ConcurrentDictionary<WebRequest, Task<WebResult>>(WebRequestEqualityComparer.Default);

        public PerformanceWebClient(IProcessService p, Logger l, IConfigurationService c, ServerConfig sc) : base(p, l, c, sc)
        {
            webClient = new HttpWebClient2(p, l, c, sc);
        }

        public override void Init() => webClient.Init();

        public override void AddTrustedCertificate(string host, string hash) => webClient.AddTrustedCertificate(host, hash);

        public override async Task<WebResult> GetResultAsync(WebRequest request)
        {
            return await _cache.GetOrAdd(request, x =>
            {
                Console.WriteLine($"Caching WebRequest result: {x.Type} {x.Url}");
                return webClient.GetResultAsync(x);
            });
        }

        protected override void OnConfigChange() => throw new NotImplementedException();

        protected override void PrepareRequest(WebRequest request) => throw new NotImplementedException();

        protected override Task<WebResult> Run(WebRequest webRequest) => throw new NotImplementedException();

        public override void OnCompleted() => throw new NotImplementedException();

        public override void OnError(Exception error) => throw new NotImplementedException();

        public override void OnNext(ServerConfig value) => throw new NotImplementedException();
    }
}
