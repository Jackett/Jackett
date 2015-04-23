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
        ///     Last entry in enumeration; do not use in application code.
        /// </summary>
        Last = 3
    };
}