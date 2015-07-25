using Jackett.Models;
using Jackett.Services;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static Jackett.CurlHelper;

namespace Jackett.Utils.Clients
{
    public class UnixLibCurlWebClient : IWebClient
    {
        private Logger logger;

        public UnixLibCurlWebClient(Logger l)
        {
            logger = l;
        }

        public async Task<WebClientByteResult> GetBytes(WebRequest request)
        {
            CurlResponse response;

            logger.Debug(string.Format("UnixLibCurlWebClient:GetBytes(Url:{0})", request.Url));

            if (request.Type == RequestType.GET)
            {
                response = await CurlHelper.GetAsync(request.Url, request.Cookies, request.Referer);
            }
            else
            {
                response = await CurlHelper.PostAsync(request.Url, request.PostData, request.Cookies, request.Referer);
            }

            var result = new WebClientByteResult()
            {
                Content = response.Content,
                Cookies = response.CookieHeader,
                Status = response.Status
            };

            if (response.Headers != null)
            {
                foreach(var header in response.Headers)
                {
                    if(string.Equals(header.Key, "location", StringComparison.InvariantCultureIgnoreCase) && header.Value !=null)
                    {
                        result.RedirectingTo = header.Value;
                    }
                }
            }

            logger.Debug(string.Format("UnixLibCurlWebClient: Returning", result.Status));
            return result;
        }

        public async Task<WebClientStringResult> GetString(WebRequest request)
        {
            logger.Debug(string.Format("UnixLibCurlWebClient:GetString(Url:{0})", request.Url));
            var result = await GetBytes(request);

            var sresult = new WebClientStringResult()
            {
                Content = Encoding.UTF8.GetString(result.Content),
                Cookies = result.Cookies,
                Status = result.Status
            };

            return sresult;
        }
    }
}
