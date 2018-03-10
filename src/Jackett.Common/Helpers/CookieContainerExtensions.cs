using System;
using System.Net;
using Newtonsoft.Json.Linq;
using NLog;

namespace Jackett.Common.Helpers
{
    public static class CookieContainerExtensions
    {
        public static void FillFromJson(this CookieContainer cookies, Uri uri, JToken json, Logger logger)
        {
            if (json["cookies"] != null)
            {
                var cookieArray = (JArray)json["cookies"];
                foreach (string cookie in cookieArray)
                {
                    var w = cookie.Split('=');
                    if (w.Length == 1)
                    {
                        cookies.Add(uri, new Cookie { Name = cookie.Trim() });
                    }
                    else
                    {
                        cookies.Add(uri, new Cookie(w[0].Trim(), w[1].Trim()));
                    }
                }
            }

            if (json["cookie_header"] != null)
            {
                var cfh = (string)json["cookie_header"];
                var cookieHeaders = ((string)json["cookie_header"]).Split(';');
                foreach (var c in cookieHeaders)
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
        }

        public static void DumpToJson(this CookieContainer cookies, string uri, JToken json)
        {
            DumpToJson(cookies, new Uri(uri), json);
        }

        public static void DumpToJson(this CookieContainer cookies, Uri uri, JToken json)
        {
            json["cookie_header"] = cookies.GetCookieHeader(uri);
        }
    }
}
