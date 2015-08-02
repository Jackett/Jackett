using Jackett.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Utils.Clients
{
    public interface IWebClient
    {
        Task<WebClientStringResult> GetString(WebRequest request);
        Task<WebClientByteResult> GetBytes(WebRequest request);
        void Init();
    }
}
