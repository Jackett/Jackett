using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.Config
{
    public class ServerConfig
    {
        public ServerConfig()
        {
            Port = 9117;
            AllowExternal = System.Environment.OSVersion.Platform == PlatformID.Unix;
        }

        public int Port { get; set; }
        public bool AllowExternal { get; set; }
        public string APIKey { get; set; }
        public string AdminPassword { get; set; }
        public string InstanceId { get; set; }
        public string BlackholeDir { get; set; }
        public bool UpdateDisabled { get; set; }
        public bool UpdatePrerelease { get; set; }
        public string BasePathOverride { get; set; }
        public string OmdbApiKey { get; set; }

        public string[] GetListenAddresses(bool? external = null)
        {
            if (external == null)
            {
                external = AllowExternal;
            }
            if (external.Value)
            {
                return new string[] { "http://*:" + Port + "/" };
            }
            else
            {
                return new string[] { 
                    "http://127.0.0.1:" + Port + "/",
                    "http://localhost:" + Port + "/",
                };
            }
        }
    }
}
