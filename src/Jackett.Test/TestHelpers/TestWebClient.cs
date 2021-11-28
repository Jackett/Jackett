using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;
using WebClient = Jackett.Common.Utils.Clients.WebClient;
using WebRequest = Jackett.Common.Utils.Clients.WebRequest;

namespace Jackett.Test.TestHelpers
{
    public class TestWebClient : WebClient
    {
        private readonly Dictionary<string, WebResult> _requestCallbacks = new Dictionary<string, WebResult>();

        public TestWebClient() : base(null, null, null, new ServerConfig(null))
        {
        }

        public TestWebClient(IProcessService p, Logger l, IConfigurationService c, ServerConfig sc)
            : base(p, l, c, sc)
        {
        }

        public void RegisterRequestCallback(string requestUrl, string responseFileName)
        {
            var contentString = TestUtil.LoadTestFile(responseFileName);
            var webResult = new WebResult
            {
                ContentBytes = Encoding.UTF8.GetBytes(contentString),
                Status = HttpStatusCode.OK
            };
            _requestCallbacks.Add(requestUrl, webResult);
        }

        public override Task<WebResult> GetResultAsync(WebRequest request)
        {
            if (_requestCallbacks.ContainsKey(request.Url))
                return Task.Factory.StartNew(
                    () =>
                    {
                        var response = _requestCallbacks[request.Url];
                        response.Request = request;
                        return response;
                    });
            throw new Exception($"You have to mock the URL {request.Url} with RegisterRequestCallback");
        }

        public override void Init()
        {
        }
    }
}
