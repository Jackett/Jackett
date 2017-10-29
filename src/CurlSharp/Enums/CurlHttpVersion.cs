namespace CurlSharp
{
    /// <summary>
    ///     Contains values used to specify the HTTP version level when using
    ///     the <see cref="CurlOption.HttpVersion" /> option in a call
    ///     to <see cref="CurlEasy.SetOpt" />
    /// </summary>
    public enum CurlHttpVersion
    {
        /// <summary>
        ///     We don't care about what version the library uses. libcurl will
        ///     use whatever it thinks fit.
        /// </summary>
        None = 0,

        /// <summary>
        ///     Enforce HTTP 1.0 requests.
        /// </summary>
        Http1_0 = 1,

        /// <summary>
        ///     Enforce HTTP 1.1 requests.
        /// </summary>
        Http1_1 = 2,

        /// <summary>
        ///     Enforce HTTP 2 requests.
        /// </summary>
        Http2_0 = 3,

        /// <summary>
        ///     Enforce version 2 for HTTPS, version 1.1 for HTTP.
        /// </summary>
        Http2_Tls = 4,

        /// <summary>
        ///     Enforce HTTP 2 without HTTP/1.1 upgrade.
        /// </summary>
        Http2_PriorKnowledge = 5,

        /// <summary>
        ///     Last entry in enumeration; do not use in application code.
        /// </summary>
        Last = 6
    }
}