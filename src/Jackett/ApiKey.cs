using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Jackett
{
    public class ApiKey
    {

        public static string CurrentKey;

        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";

        public static string Generate()
        {
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
