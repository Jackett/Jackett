using System.Security.Cryptography;
using System.Text;

namespace Jackett.Common.Extensions
{
    public static class HashingExtensions
    {
        public static string SHA1Hash(this string input)
        {
            using var hash = SHA1.Create();

            return GetHash(hash.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }

        public static string SHA256Hash(this string input)
        {
            using var hash = SHA256.Create();

            return GetHash(hash.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }

        private static string GetHash(byte[] bytes)
        {
            var stringBuilder = new StringBuilder();

            foreach (var b in bytes)
            {
                stringBuilder.Append(b.ToString("x2"));
            }

            return stringBuilder.ToString();
        }
    }
}
