using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;

namespace Jackett.Server.Services
{
    internal class SecuityService : ISecuityService
    {
        private const string COOKIENAME = "JACKETT";
        private readonly ServerConfig _serverConfig;

        public SecuityService(ServerConfig sc) => _serverConfig = sc;

        public string HashPassword(string input)
        {
            if (input == null)
                return null;
            // Append key as salt
            input += _serverConfig.APIKey;

            var UE = new UnicodeEncoding();
            byte[] hashValue;
            var message = UE.GetBytes(input);

            var hashString = new SHA512Managed();
            var hex = "";

            hashValue = hashString.ComputeHash(message);
            foreach (var x in hashValue)
            {
                hex += string.Format("{0:x2}", x);
            }
            return hex;
        }

        public void Login(HttpResponseMessage response) => response.Headers.Add("Set-Cookie", COOKIENAME + "=" + _serverConfig.AdminPassword + "; path=/");

        public void Logout(HttpResponseMessage response) => response.Headers.Add("Set-Cookie", COOKIENAME + "=; path=/");

        public bool CheckAuthorised(HttpRequestMessage request)
        {
            if (string.IsNullOrEmpty(_serverConfig.AdminPassword))
                return true;

            try
            {
                var cookie = request.Headers.GetValues(COOKIENAME).FirstOrDefault();
                if (cookie != null)
                {
                    return cookie == _serverConfig.AdminPassword;
                }
            }
            catch { }

            return false;
        }
    }
}
