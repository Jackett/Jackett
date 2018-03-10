namespace CurlSharp.Enums
{
    /// <summary>
    ///     The status code associated with an <see cref="CurlEasy" /> object in a
    ///     <see cref="CurlMulti" /> operation. One of these is returned in response
    ///     to reading the <see cref="CurlMultiInfo.Msg" /> property.
    /// </summary>
    public enum CurlMessage
    {
        /// <summary>
        ///     First entry in the enumeration, not used.
        /// </summary>
        None = 0,

        /// <summary>
        ///     The associated <see cref="CurlEasy" /> object completed.
        /// </summary>
        Done = 1,

        /// <summary>
        ///     End-of-enumeration marker, not used.
        /// </summary>
        Last = 2
    };
}