using AutoMapper;
using Jackett.Models;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Utils.Clients
{
    class HttpWebClient : IWebClient
    {
        private Logger logger;

        public HttpWebClient(Logger l)
        {
            logger = l;
        }


        public void Init()
        {
        }

        public async Task<WebClientByteResult> GetBytes(WebRequest request)
        {
            logger.Debug(string.Format("WindowsWebClient:GetBytes(Url:{0})", request.Url));
            var result = await Run(request);
            logger.Debug(string.Format("WindowsWebClient: Returning {0} => {1} bytes", result.Status, (result.Content == null ? "<NULL>" : result.Content.Length.ToString())));
            return result;
        }

        public async Task<WebClientStringResult> GetString(WebRequest request)
        {
            logger.Debug(string.Format("WindowsWebClient:GetString(Url:{0})", request.Url));
            var result = await Run(request);
            logger.Debug(string.Format("WindowsWebClient: Returning {0} => {1}", result.Status, (result.Content == null ? "<NULL>" : Encoding.UTF8.GetString(result.Content))));
            return Mapper.Map<WebClientStringResult>(result);
        }

        private async Task<WebClientByteResult> Run(WebRequest request)
        {
            var cookies = new CookieContainer();
            if (!string.IsNullOrEmpty(request.Cookies))
            {
                var uri = new Uri(request.Url);
                foreach (var c in request.Cookies.Split(';'))
                {
                    try
                    {
                        cookies.SetCookies(uri, c);
                    }
                    catch (CookieException ex)
                    {
                        logger.Info("(Non-critical) Problem loading cookie {0}, {1}, {2}", uri, c, ex.Message);
                    }
                }
            }

            var client = new HttpClient(new HttpClientHandler
            {
                CookieContainer = cookies,
                AllowAutoRedirect = false, // Do not use this - Bugs ahoy! Lost cookies and more.
                UseCookies = true,
            });

            client.DefaultRequestHeaders.Add("User-Agent", BrowserUtil.ChromeUserAgent);
            HttpResponseMessage response = null;
           
            if (request.Type == RequestType.POST)
            {
                var content = new FormUrlEncodedContent(request.PostData);
                response = await client.PostAsync(request.Url, content);
            }
            else
            {
                response = await client.GetAsync(request.Url);
            }

            var result = new WebClientByteResult();
            result.Content = await response.Content.ReadAsByteArrayAsync();
            if (response.Headers.Location != null)
            {
                result.RedirectingTo = response.Headers.Location.ToString();
            }
            result.Status = response.StatusCode;

            // Compatiblity issue between the cookie format and httpclient
            // Pull it out manually ignoring the expiry date then set it manually
            // http://stackoverflow.com/questions/14681144/httpclient-not-storing-cookies-in-cookiecontainer
            IEnumerable<string> cookieHeaders;
            if (response.Headers.TryGetValues("set-cookie", out cookieHeaders))
            {
                var cookieBuilder = new StringBuilder();
                foreach (var c in cookieHeaders)
                {
                    cookieBuilder.AppendFormat("{0} ", c.Substring(0, c.IndexOf(';')+1));
                }

                result.Cookies = cookieBuilder.ToString().TrimEnd();
            }

            ServerUtil.ResureRedirectIsFullyQualified(request, result);
            return result;
        }
    }
}
