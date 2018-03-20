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

namespace Jackett.Services
{

    public class ProtectionService : IProtectionService
    {
        DataProtectionScope PROTECTION_SCOPE = DataProtectionScope.LocalMachine;
        private const string JACKETT_KEY = "JACKETT_KEY";
        const string APPLICATION_KEY = "Dvz66r3n8vhTGip2/quiw5ISyM37f7L2iOdupzdKmzkvXGhAgQiWK+6F+4qpxjPVNks1qO7LdWuVqRlzgLzeW8mChC6JnBMUS1Fin4N2nS9lh4XPuCZ1che75xO92Nk2vyXUo9KSFG1hvEszAuLfG2Mcg1r0sVyVXd2gQDU/TbY=";
        private byte[] _instanceKey;

        public ProtectionService(ServerConfig config)
        {
            if (System.Environment.OSVersion.Platform == PlatformID.Unix)
            {
                // We should not be running as root and will only have access to the local store.
                PROTECTION_SCOPE = DataProtectionScope.CurrentUser;
            }
            _instanceKey = Encoding.UTF8.GetBytes(config.InstanceId);
        }

        public string Protect(string plainText)
        {
            var jackettKey = Environment.GetEnvironmentVariable(JACKETT_KEY);

            if (jackettKey == null)
            {
                return ProtectDefaultMethod(plainText);
            }
            else
            {
                return ProtectUsingKey(plainText, jackettKey);
            }
        }

        public string UnProtect(string plainText)
        {
            var jackettKey = Environment.GetEnvironmentVariable(JACKETT_KEY);

            if (jackettKey == null)
            {
                return UnProtectDefaultMethod(plainText);
            }
            else
            {
                return UnProtectUsingKey(plainText, jackettKey);
            }
        }

        private string ProtectDefaultMethod(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var appKey = Convert.FromBase64String(APPLICATION_KEY);
            var instanceKey = _instanceKey;
            var entropy = new byte[appKey.Length + instanceKey.Length];
            Buffer.BlockCopy(instanceKey, 0, entropy, 0, instanceKey.Length);
            Buffer.BlockCopy(appKey, 0, entropy, instanceKey.Length, appKey.Length);

            var protectedBytes = ProtectedData.Protect(plainBytes, entropy, PROTECTION_SCOPE);

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(instanceKey, instanceKey.Reverse().ToArray(), 64);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC;

                    using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(protectedBytes, 0, protectedBytes.Length);
                        cs.Close();
                    }
                    protectedBytes = ms.ToArray();
                }
            }

            return Convert.ToBase64String(protectedBytes);
        }

        private string UnProtectDefaultMethod(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            var protectedBytes = Convert.FromBase64String(plainText);
            var instanceKey = _instanceKey;

            using (MemoryStream ms = new MemoryStream())
            {
                using (RijndaelManaged AES = new RijndaelManaged())
                {
                    AES.KeySize = 256;
                    AES.BlockSize = 128;

                    var key = new Rfc2898DeriveBytes(instanceKey, instanceKey.Reverse().ToArray(), 64);
                    AES.Key = key.GetBytes(AES.KeySize / 8);
                    AES.IV = key.GetBytes(AES.BlockSize / 8);

                    AES.Mode = CipherMode.CBC;

                    using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(protectedBytes, 0, protectedBytes.Length);
                        cs.Close();
                    }
                    protectedBytes = ms.ToArray();
                }
            }

            var appKey = Convert.FromBase64String(APPLICATION_KEY);
            var entropy = new byte[appKey.Length + instanceKey.Length];
            Buffer.BlockCopy(instanceKey, 0, entropy, 0, instanceKey.Length);
            Buffer.BlockCopy(appKey, 0, entropy, instanceKey.Length, appKey.Length);

            var unprotectedBytes = ProtectedData.Unprotect(protectedBytes, entropy, PROTECTION_SCOPE);
            return Encoding.UTF8.GetString(unprotectedBytes);
        }

        private string ProtectUsingKey(string plainText, string key)
        {
            return StringCipher.Encrypt(plainText, key);
        }

        private string UnProtectUsingKey(string plainText, string key)
        {
            return StringCipher.Decrypt(plainText, key);
        }

        public void Protect<T>(T obj)
        {
            var type = obj.GetType();

            foreach (var property in type.GetProperties(BindingFlags.SetProperty | BindingFlags.GetProperty | BindingFlags.Public))
            {
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
        }

        public void UnProtect<T>(T obj)
        {
            var type = obj.GetType();

            foreach (var property in type.GetProperties(BindingFlags.SetProperty | BindingFlags.GetProperty | BindingFlags.Public))
            {
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
}
