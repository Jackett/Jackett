using System;
using Jackett.Common.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace Jackett.Server.Services
{
    public class ProtectionService : IProtectionService
    {
        private const string JackettKey = "JACKETT_KEY";
        private const string ApplicationKey = "Dvz66r3n8vhTGip2/quiw5ISyM37f7L2iOdupzdKmzkvXGhAgQiWK+6F+4qpxjPVNks1qO7LdWuVqRlzgLzeW8mChC6JnBMUS1Fin4N2nS9lh4XPuCZ1che75xO92Nk2vyXUo9KSFG1hvEszAuLfG2Mcg1r0sVyVXd2gQDU/TbY=";
        private readonly IDataProtector _protector;

        public ProtectionService(IDataProtectionProvider provider = null)
        {
            if (provider != null)
            {
                var jackettKey = Environment.GetEnvironmentVariable(JackettKey);
                var purpose = string.IsNullOrEmpty(jackettKey) ? ApplicationKey : jackettKey;

                _protector = provider.CreateProtector(purpose);
            }
        }

        public string Protect(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            return _protector.Protect(plainText);
        }

        public string UnProtect(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            return _protector.Unprotect(plainText);
        }
    }
}
