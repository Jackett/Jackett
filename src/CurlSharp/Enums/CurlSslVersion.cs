namespace CurlSharp.Enums
{
    /// <summary>
    ///     Contains values used to specify the Ssl version level when using
    ///     the <see cref="CurlOption.SslVersion" /> option in a call
    ///     to <see cref="CurlEasy.SetOpt" />
    /// </summary>
    public enum CurlSslVersion
    {
        /// <summary>
        ///     Use whatever version the Ssl library selects.
        /// </summary>
        Default = 0,

        /// <summary>
        ///     Use TLS version 1.
        /// </summary>
        Tlsv1 = 1,

        /// <summary>
        ///     Use Ssl version 2. This is not a good option unless it's the
        ///     only version supported by the remote server.
        /// </summary>
        Sslv2 = 2,

        /// <summary>
        ///     Use Ssl version 3. This is a preferred option.
        /// </summary>
        Sslv3 = 3,

        /// <summary>
        ///     Last entry in enumeration; do not use in application code.
        /// </summary>
        Last = 4
    };
}