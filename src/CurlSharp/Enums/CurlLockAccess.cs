namespace CurlSharp.Enums
{
    /// <summary>
    ///     Values containing the type of shared access requested when libcurl
    ///     calls the <see cref="CurlShare.CurlShareLockCallback" /> delegate.
    /// </summary>
    public enum CurlLockAccess
    {
        /// <summary>
        ///     Unspecified action; the delegate should never receive this.
        /// </summary>
        None = 0,

        /// <summary>
        ///     The delegate receives this call when libcurl is requesting
        ///     read access to the shared resource.
        /// </summary>
        Shared = 1,

        /// <summary>
        ///     The delegate receives this call when libcurl is requesting
        ///     write access to the shared resource.
        /// </summary>
        Single = 2,

        /// <summary>
        ///     End-of-enumeration marker; do not use in application code.
        /// </summary>
        Last = 3
    };
}