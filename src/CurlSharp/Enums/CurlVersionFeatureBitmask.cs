namespace CurlSharp.Enums
{
    /// <summary>
    ///     A bitmask of libcurl features OR'd together as the value of the
    ///     property <see cref="CurlVersionInfoData.Features" />. The feature
    ///     bits are summarized in the table below.
    /// </summary>
    public enum CurlVersionFeatureBitmask
    {
        /// <summary>
        ///     Supports Ipv6.
        /// </summary>
        Ipv6 = 0x01,

        /// <summary>
        ///     Supports kerberos4 (when using FTP).
        /// </summary>
        Kerberos64 = 0x02,

        /// <summary>
        ///     Supports Ssl (HTTPS/FTPS).
        /// </summary>
        Ssl = 0x04,

        /// <summary>
        ///     Supports HTTP deflate using libz.
        /// </summary>
        LibZ = 0x08,

        /// <summary>
        ///     Supports HTTP Ntlm (added in 7.10.6).
        /// </summary>
        Ntlm = 0x10,

        /// <summary>
        ///     Supports HTTP GSS-Negotiate (added in 7.10.6).
        /// </summary>
        GssNegotiate = 0x20,

        /// <summary>
        ///     libcurl was built with extra debug capabilities built-in. This
        ///     is mainly of interest for libcurl hackers. (added in 7.10.6)
        /// </summary>
        Debug = 0x40,

        /// <summary>
        ///     libcurl was built with support for asynchronous name lookups,
        ///     which allows more exact timeouts (even on Windows) and less
        ///     blocking when using the multi interface. (added in 7.10.7)
        /// </summary>
        AsynchDns = 0x80,

        /// <summary>
        ///     libcurl was built with support for Spnego authentication
        ///     (Simple and Protected GSS-API Negotiation Mechanism, defined
        ///     in RFC 2478.) (added in 7.10.8)
        /// </summary>
        Spnego = 0x100,

        /// <summary>
        ///     libcurl was built with support for large files.
        /// </summary>
        LargeFile = 0x200,

        /// <summary>
        ///     libcurl was built with support for IDNA, domain names with
        ///     international letters.
        /// </summary>
        Idn = 0x400
    };
}