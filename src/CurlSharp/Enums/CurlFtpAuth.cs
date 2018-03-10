namespace CurlSharp.Enums
{
    /// <summary>
    ///     This enumeration contains values used to specify the FTP Ssl
    ///     authorization level using the
    ///     <see cref="CurlOption.FtpSslAuth" /> option when calling
    ///     <see cref="CurlEasy.SetOpt" />
    /// </summary>
    public enum CurlFtpAuth
    {
        /// <summary>
        ///     Let <c>libcurl</c> decide on the authorization scheme.
        /// </summary>
        Default = 0,

        /// <summary>
        ///     Use "AUTH Ssl".
        /// </summary>
        SSL = 1,

        /// <summary>
        ///     Use "AUTH TLS".
        /// </summary>
        TLS = 2,

        /// <summary>
        ///     End-of-enumeration marker. Do not use in a client application.
        /// </summary>
        Last = 3
    };
}