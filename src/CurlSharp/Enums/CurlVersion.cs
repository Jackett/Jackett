namespace CurlSharp.Enums
{
    /// <summary>
    ///     A member of this enumeration is passed to the function
    ///     <see cref="Curl.GetVersionInfo" />
    /// </summary>
    public enum CurlVersion
    {
        /// <summary>
        ///     Capabilities associated with the initial version of libcurl.
        /// </summary>
        First = 0,

        /// <summary>
        ///     Capabilities associated with the second version of libcurl.
        /// </summary>
        Second = 1,

        /// <summary>
        ///     Capabilities associated with the third version of libcurl.
        /// </summary>
        Third = 2,

        /// <summary>
        ///     Same as <c>Third</c>.
        /// </summary>
        Now = Third,

        /// <summary>
        ///     End-of-enumeration marker; do not use in application code.
        /// </summary>
        Last = 3
    };
}