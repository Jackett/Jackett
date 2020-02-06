using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Jackett.Common;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils;
using Microsoft.AspNetCore.DataProtection;

namespace Jackett.Server.Services
{
    public class ProtectionService : IProtectionService
    {
        private readonly DataProtectionScope _protectionScope = DataProtectionScope.LocalMachine;
        private const string JackettKey = "JACKETT_KEY";

        private const string ApplicationKey =
            "Dvz66r3n8vhTGip2/quiw5ISyM37f7L2iOdupzdKmzkvXGhAgQiWK+6F+4qpxjPVNks1qO7LdWuVqRlzgLzeW8mChC6JnBMUS1Fin4N2nS9lh4XPuCZ1che75xO92Nk2vyXUo9KSFG1hvEszAuLfG2Mcg1r0sVyVXd2gQDU/TbY=";

        private readonly byte[] _instanceKey;
        private readonly IDataProtector _protector;

        public ProtectionService(ServerConfig config, IDataProtectionProvider provider = null)
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
                // We should not be running as root and will only have access to the local store.
                _protectionScope = DataProtectionScope.CurrentUser;
            _instanceKey = Encoding.UTF8.GetBytes(config.InstanceId);
            if (provider != null)
            {
                var jackettKey = Environment.GetEnvironmentVariable(JackettKey);
                var purpose = string.IsNullOrEmpty(jackettKey) ? ApplicationKey : jackettKey;
                _protector = provider.CreateProtector(purpose);
            }
        }

        public string Protect(string plainText) => string.IsNullOrEmpty(plainText) ? string.Empty : _protector.Protect(plainText);

        public string UnProtect(string plainText) => string.IsNullOrEmpty(plainText) ? string.Empty : _protector.Unprotect(plainText);

        public string LegacyProtect(string plainText)
        {
            var jackettKey = Environment.GetEnvironmentVariable(JackettKey);
            return jackettKey == null ? ProtectDefaultMethod(plainText) : ProtectUsingKey(plainText, jackettKey);
        }

        public string LegacyUnProtect(string plainText)
        {
            var jackettKey = Environment.GetEnvironmentVariable(JackettKey);
            return jackettKey == null ? UnProtectDefaultMethod(plainText) : UnProtectUsingKey(plainText, jackettKey);
        }

        private string ProtectDefaultMethod(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var appKey = Convert.FromBase64String(ApplicationKey);
            var instanceKey = _instanceKey;
            var entropy = new byte[appKey.Length + instanceKey.Length];
            Buffer.BlockCopy(instanceKey, 0, entropy, 0, instanceKey.Length);
            Buffer.BlockCopy(appKey, 0, entropy, instanceKey.Length, appKey.Length);
            var protectedBytes = ProtectedData.Protect(plainBytes, entropy, _protectionScope);
            using (var ms = new MemoryStream())
            using (var aes = new RijndaelManaged())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;
                var key = new Rfc2898DeriveBytes(instanceKey, instanceKey.Reverse().ToArray(), 64);
                aes.Key = key.GetBytes(aes.KeySize / 8);
                aes.IV = key.GetBytes(aes.BlockSize / 8);
                aes.Mode = CipherMode.CBC;
                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(protectedBytes, 0, protectedBytes.Length);
                    cs.Close();
                }

                protectedBytes = ms.ToArray();
            }

            return Convert.ToBase64String(protectedBytes);
        }

        private string UnProtectDefaultMethod(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;
            var protectedBytes = Convert.FromBase64String(plainText);
            var instanceKey = _instanceKey;
            using (var ms = new MemoryStream())
            using (var aes = new RijndaelManaged())
            {
                aes.KeySize = 256;
                aes.BlockSize = 128;
                var key = new Rfc2898DeriveBytes(instanceKey, instanceKey.Reverse().ToArray(), 64);
                aes.Key = key.GetBytes(aes.KeySize / 8);
                aes.IV = key.GetBytes(aes.BlockSize / 8);
                aes.Mode = CipherMode.CBC;
                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(protectedBytes, 0, protectedBytes.Length);
                    cs.Close();
                }

                protectedBytes = ms.ToArray();
            }

            var appKey = Convert.FromBase64String(ApplicationKey);
            var entropy = new byte[appKey.Length + instanceKey.Length];
            Buffer.BlockCopy(instanceKey, 0, entropy, 0, instanceKey.Length);
            Buffer.BlockCopy(appKey, 0, entropy, instanceKey.Length, appKey.Length);
            var unprotectedBytes = ProtectedData.Unprotect(protectedBytes, entropy, _protectionScope);
            return Encoding.UTF8.GetString(unprotectedBytes);
        }

        private string ProtectUsingKey(string plainText, string key) => StringCipher.Encrypt(plainText, key);

        private string UnProtectUsingKey(string plainText, string key) => StringCipher.Decrypt(plainText, key);

        public void Protect<T>(T obj)
        {
            var type = obj.GetType();
            foreach (var property in type.GetProperties(
                BindingFlags.SetProperty | BindingFlags.GetProperty | BindingFlags.Public))
                if (property.GetCustomAttributes(typeof(JackettProtectedAttribute), false).Count() > 0)
                {
                    var value = property.GetValue(obj);
                    if (value is string)
                    {
                        var protectedString = Protect(value as string);
                        property.SetValue(obj, protectedString);
                    }
                }
        }

        public void UnProtect<T>(T obj)
        {
            var type = obj.GetType();
            foreach (var property in type.GetProperties(
                BindingFlags.SetProperty | BindingFlags.GetProperty | BindingFlags.Public))
                if (property.GetCustomAttributes(typeof(JackettProtectedAttribute), false).Count() > 0)
                {
                    var value = property.GetValue(obj);
                    if (value is string)
                    {
                        var unprotectedString = UnProtect(value as string);
                        property.SetValue(obj, unprotectedString);
                    }
                }
        }
    }
}
