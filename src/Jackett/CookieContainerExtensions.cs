using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Jackett
{
    public static class CookieContainerExtensions
    {
        public static void FillFromJson(this CookieContainer cookies, Uri uri, JArray json)
        {
            foreach (var cookie in json)
            {
                var w = ((string)cookie).Split(':');
                cookies.Add(uri, new Cookie(w[0], w[1]));
            }
        }
    }
}
