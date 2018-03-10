namespace CurlSharp.Enums
{
    /// <summary>
    ///     Contains values used to specify the order in which cached connections
    ///     are closed. One of these is passed as the
    ///     <see cref="CurlOption.ClosePolicy" /> option in a call
    ///     to <see cref="CurlEasy.SetOpt" />
    /// </summary>
    public enum CurlClosePolicy
    {
        /// <summary>
        ///     No close policy. Never use this.
        /// </summary>
        None = 0,

        /// <summary>
        ///     Close the oldest cached connections first.
        /// </summary>
        Oldest = 1,

        /// <summary>
        ///     Close the least recently used connections first.
        /// </summary>
        LeastRecentlyUsed = 2,

        /// <summary>
        ///     Close the connections with the least traffic first.
        /// </summary>
        LeastTraffic = 3,

        /// <summary>
        ///     Close the slowest connections first.
        /// </summary>
        Slowest = 4,

        /// <summary>
        ///     Currently unimplemented.
        /// </summary>
        Callback = 5,

        /// <summary>
        ///     End-of-enumeration marker; do not use in application code.
        /// </summary>
        Last = 6
    };
}