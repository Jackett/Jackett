using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Utils.Clients
{
    public class WebClientStringResult
    {
        public HttpStatusCode Status { get; set; }
        public string Cookies { get; set; }
        public string Content { get; set; }
    }
}
