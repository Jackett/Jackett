using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;

namespace Jackett.Services
{

    class SecuityService : ISecuityService
    {
        private const string COOKIENAME = "JACKETT";
        private ServerConfig _serverConfig;

        public SecuityService(ServerConfig sc)
        {
            _serverConfig = sc;
        }

        public string HashPassword(string input)
        {
            if (input == null)
                return null;
            // Append key as salt
            input += _serverConfig.APIKey;

            UnicodeEncoding UE = new UnicodeEncoding();
            byte[] hashValue;
            byte[] message = UE.GetBytes(input);

            SHA512Managed hashString = new SHA512Managed();
            string hex = "";

            hashValue = hashString.ComputeHash(message);
            foreach (byte x in hashValue)
            {
                hex += String.Format("{0:x2}", x);
            }
            return hex;
        }

        public void Login(HttpResponseMessage response)
        {
            // Login
            response.Headers.Add("Set-Cookie", COOKIENAME + "=" + _serverConfig.AdminPassword + "; path=/");
        }

        public void Logout(HttpResponseMessage response)
        {
            // Logout
            response.Headers.Add("Set-Cookie", COOKIENAME + "=; path=/");
        }

        public bool CheckAuthorised(HttpRequestMessage request)
        {
            if (string.IsNullOrEmpty(_serverConfig.AdminPassword))
                return true;

            try
            {
                var cookie = request.Headers.GetCookies(COOKIENAME).FirstOrDefault();
                if (cookie != null)
                {
                    return cookie[COOKIENAME].Value == _serverConfig.AdminPassword;
                }
            }
            catch { }

            return false;
        }
    }
}
