namespace CurlSharp.Enums
{
    /// <summary>
    ///     Contains return codes for many of the functions in the
    ///     <see cref="CurlMulti" /> class.
    /// </summary>
    public enum CurlMultiCode
    {
        /// <summary>
        ///     You should call <see cref="CurlMulti.Perform" /> again before calling
        ///     <see cref="CurlMulti.Select" />.
        /// </summary>
        CallMultiPerform = -1,

        /// <summary>
        ///     The function succeded.
        /// </summary>
        Ok = 0,

        /// <summary>
        ///     The internal <see cref="CurlMulti" /> is bad.
        /// </summary>
        BadHandle = 1,

        /// <summary>
        ///     One of the <see cref="CurlEasy" /> handles associated with the
        ///     <see cref="CurlMulti" /> object is bad.
        /// </summary>
        BadEasyHandle = 2,

        /// <summary>
        ///     Out of memory. This is a severe problem.
        /// </summary>
        OutOfMemory = 3,

        /// <summary>
        ///     Internal error deep within the libcurl library.
        /// </summary>
        InternalError = 4,

        /// <summary>
        ///     End-of-enumeration marker, not used.
        /// </summary>
        Last = 5
    };
}