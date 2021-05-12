using Jackett.Common.Services.Interfaces;

namespace Jackett.Performance.Services
{
    public class PerformanceProtectionService : IProtectionService
    {
        public string Protect(string plainText) => plainText;

        public string UnProtect(string plainText) => plainText;
    }
}
