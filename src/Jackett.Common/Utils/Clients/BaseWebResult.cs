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

        public bool IsRedirect => Status == HttpStatusCode.Redirect || Status == HttpStatusCode.RedirectKeepVerb ||
                                  Status == HttpStatusCode.RedirectMethod || Status == HttpStatusCode.Found ||
                                  Status == HttpStatusCode.MovedPermanently;
    }
}
