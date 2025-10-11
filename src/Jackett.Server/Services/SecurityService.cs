using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;

namespace Jackett.Server.Services
{
    internal class SecurityService : ISecurityService
    {
        private readonly ServerConfig _serverConfig;

        public SecurityService(ServerConfig sc) => _serverConfig = sc;

        public bool CheckAuthorised(string password)
        {
            if (string.IsNullOrEmpty(_serverConfig.AdminPassword))
                return true;

            if (!string.IsNullOrEmpty(password) && HashPassword(password) == _serverConfig.AdminPassword)
                return true;

            return false;
        }

        public string HashPassword(string input)
        {
            if (input == null)
                return null;

            var ue = new UnicodeEncoding();

            // Append key as salt
            input += _serverConfig.APIKey;
            var message = ue.GetBytes(input);
            var hashValue = SHA512.HashData(message);
            return hashValue.Aggregate("", (current, x) => current + $"{x:x2}");
        }
    }
}
