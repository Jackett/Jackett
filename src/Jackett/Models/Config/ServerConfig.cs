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
        }

        public int Port { get; set; }
        public bool AllowExternal { get; set; }
        public string APIKey { get; set; }
        public string AdminPassword { get; set; }

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

        public string GenerateApi()
        {
            var chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            var randBytes = new byte[32];
            var rngCsp = new RNGCryptoServiceProvider();
            rngCsp.GetBytes(randBytes);
            var key = "";
            foreach (var b in randBytes)
            {
                key += chars[b % chars.Length];
            }
            return key;
        }
    }
}
