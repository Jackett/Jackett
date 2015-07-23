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
        CookieContainer cookies;

        public WindowsWebClient(Logger l)
        {
            logger = l;
            cookies = new CookieContainer();
        }

        public async Task<WebClientByteResult> GetBytes(WebRequest request)
        {
            logger.Debug(string.Format("WindowsWebClient:GetBytes(Url:{0})", request.Url));


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
                AllowAutoRedirect = false,
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
            result.Cookies = cookies.GetCookieHeader(new Uri(request.Url));
            result.Status = response.StatusCode;
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
                AllowAutoRedirect = request.AutoRedirect,
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
            result.Cookies = cookies.GetCookieHeader(new Uri(request.Url));
            result.Status = response.StatusCode;
            return result;
        }
    }
}
