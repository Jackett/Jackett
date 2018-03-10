namespace CurlSharp.Enums
{
    /// <summary>
    ///     A member of this enumeration is passed as the first parameter to the
    ///     <see cref="CurlEasy.CurlDebugCallback" /> delegate to which libcurl passes
    ///     debug messages.
    /// </summary>
    public enum CurlInfoType
    {
        /// <summary>
        ///     The data is informational text.
        /// </summary>
        Text = 0,

        /// <summary>
        ///     The data is header (or header-like) data received from the peer.
        /// </summary>
        HeaderIn = 1,

        /// <summary>
        ///     The data is header (or header-like) data sent to the peer.
        /// </summary>
        HeaderOut = 2,

        /// <summary>
        ///     The data is protocol data received from the peer.
        /// </summary>
        DataIn = 3,

        /// <summary>
        ///     The data is protocol data sent to the peer.
        /// </summary>
        DataOut = 4,

        /// <summary>
        ///     The data is Ssl-related data sent to the peer.
        /// </summary>
        SslDataIn = 5,

        /// <summary>
        ///     The data is Ssl-related data received from the peer.
        /// </summary>
        SslDataOut = 6,

        /// <summary>
        ///     End of enumeration marker, don't use in a client application.
        /// </summary>
        End = 7
    };
}