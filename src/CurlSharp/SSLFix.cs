using System.Collections.ObjectModel;

namespace CurlSharp
{
    /// <summary>
    /// Our SSL FIX for CURL contain authorized Ciphers for SSL Communications
    /// </summary>
    public class SSLFix
    {
        // Our CiphersList
        private static readonly ReadOnlyCollection<string> Ciphers = new ReadOnlyCollection<string>( new[] {
            // Default supported ciphers by Jackett
            "rsa_aes_128_sha",
            "ecdhe_rsa_aes_256_sha",
            "ecdhe_ecdsa_aes_128_sha"
        });

        /// <summary>
        /// List of ciphers supported by Jackett
        /// </summary>
        /// <returns>Formatted string of ciphers</returns>
        public static string CiphersList()
        {
            // Comma-Separated list of ciphers
            return string.Join(",", Ciphers);
        }
    }
}
