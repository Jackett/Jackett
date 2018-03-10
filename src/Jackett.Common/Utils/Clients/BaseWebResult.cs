using System.Collections.Generic;
using System.Net;

namespace Jackett.Common.Utils.Clients
{
    public abstract class BaseWebResult
    {
        public HttpStatusCode Status { get; set; }
        public string Cookies { get; set; }
        public string RedirectingTo { get; set; }
        public WebRequest Request { get; set; }
        public Dictionary<string, string[]> Headers = new Dictionary<string, string[]>();

        public bool IsRedirect
        {
          get
            {
             return  Status == System.Net.HttpStatusCode.Redirect ||
                     Status == System.Net.HttpStatusCode.RedirectKeepVerb ||
                     Status == System.Net.HttpStatusCode.RedirectMethod ||
                     Status == System.Net.HttpStatusCode.Found ||
                     Status == System.Net.HttpStatusCode.MovedPermanently;
            }
        }
    }
}
