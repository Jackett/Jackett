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
        private const string Cookiename = "JACKETT";
        private readonly ServerConfig _serverConfig;

        public SecuityService(ServerConfig sc) => _serverConfig = sc;

        public string HashPassword(string input)
        {
            if (input == null)
                return null;
            // Append key as salt
            input += _serverConfig.APIKey;
            var ue = new UnicodeEncoding();
            byte[] hashValue;
            var message = ue.GetBytes(input);
            var hashString = new SHA512Managed();
            var hex = "";
            hashValue = hashString.ComputeHash(message);
            foreach (var x in hashValue)
                hex += string.Format("{0:x2}", x);
            return hex;
        }

        // Login
        public void Login(HttpResponseMessage response) => response.Headers.Add(
            "Set-Cookie", $"{Cookiename}={_serverConfig.AdminPassword}; path=/");

        // Logout
        public void Logout(HttpResponseMessage response) => response.Headers.Add("Set-Cookie", $"{Cookiename}=; path=/");

        public bool CheckAuthorised(HttpRequestMessage request)
        {
            if (string.IsNullOrEmpty(_serverConfig.AdminPassword))
                return true;
            try
            {
                var cookie = request.Headers.GetValues(Cookiename).FirstOrDefault();
                if (cookie != null)
                    return cookie == _serverConfig.AdminPassword;
            }
            catch { }

            return false;
        }
    }
}
