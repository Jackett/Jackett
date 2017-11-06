using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Utils.Clients
{
    public class WebClientByteResult : BaseWebResult
    {
        public byte[] Content { get; set; }
    }
}
