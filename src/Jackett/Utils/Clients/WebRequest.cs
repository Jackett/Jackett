using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Utils.Clients
{
    public class WebRequest
    {
        public WebRequest()
        {
            PostData = new Dictionary<string, string>();
            Type = RequestType.GET;
        }

        public string Url { get; set; }
        public Dictionary<string, string> PostData { get; set; }
        public string Cookies { get; set; }
        public string Referer { get; set; }
        public RequestType Type { get; set; }
        public bool AutoRedirect { get; set; }
    }

    public enum RequestType
    {
        GET,
        POST
    }
}
