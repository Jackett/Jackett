namespace CurlSharp.Enums
{
    /// <summary>
    ///     Members of this enumeration should be passed to
    ///     <see cref="CurlShare.SetOpt" /> when it is called with the
    ///     <c>CurlShare</c> or <c>Unshare</c> options
    ///     provided in the <see cref="CurlShareOption" /> enumeration.
    /// </summary>
    public enum CurlLockData
    {
        /// <summary>
        ///     Not used.
        /// </summary>
        None = 0,

        /// <summary>
        ///     Used internally by libcurl.
        /// </summary>
        Share = 1,

        /// <summary>
        ///     Cookie data will be shared across the <see cref="CurlEasy" /> objects
        ///     using this shared object.
        /// </summary>
        Cookie = 2,

        /// <summary>
        ///     Cached Dns hosts will be shared across the <see cref="CurlEasy" />
        ///     objects using this shared object.
        /// </summary>
        Dns = 3,

        /// <summary>
        ///     Not supported yet.
        /// </summary>
        SslSession = 4,

        /// <summary>
        ///     Not supported yet.
        /// </summary>
        Connect = 5,

        /// <summary>
        ///     End-of-enumeration marker; do not use in application code.
        /// </summary>
        Last = 6
    };
}