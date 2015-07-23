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
    class WindowsWebClient : IWebClient
    {
        private Logger logger;
      

        public WindowsWebClient(Logger l)
        {
            logger = l;
          
        }

        public async Task<WebClientByteResult> GetBytes(WebRequest request)
        {
            logger.Debug(string.Format("WindowsWebClient:GetBytes(Url:{0})", request.Url));

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
                    cookieBuilder.AppendFormat("{0} ", c.Substring(0, c.LastIndexOf(';')));
                }

                result.Cookies = cookieBuilder.ToString().TrimEnd();
            }

            return result;
        }

        public async Task<WebClientStringResult> GetString(WebRequest request)
        {
            logger.Debug(string.Format("WindowsWebClient:GetString(Url:{0})", request.Url));
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
            } else
            {
                response = await client.GetAsync(request.Url);
            }

            var result = new WebClientStringResult();
            result.Content = await response.Content.ReadAsStringAsync();

            // Compatiblity issue between the cookie format and httpclient
            // Pull it out manually ignoring the expiry date then set it manually
            // http://stackoverflow.com/questions/14681144/httpclient-not-storing-cookies-in-cookiecontainer
            IEnumerable<string> cookieHeaders;
            if (response.Headers.TryGetValues("set-cookie", out cookieHeaders))
            {
                var cookieBuilder = new StringBuilder();
                foreach (var c in cookieHeaders)
                {
                    if (cookieBuilder.Length > 0)
                    {
                        cookieBuilder.Append("; ");
                    }

                    cookieBuilder.Append( c.Substring(0, c.IndexOf(';')));
                }

                result.Cookies = cookieBuilder.ToString();
            }

            result.Status = response.StatusCode;
            if (null != response.Headers.Location)
            {
                result.RedirectingTo = response.Headers.Location.ToString();
            }
            return result;
        }
    }
}
