namespace CurlSharp.Enums
{
    /// <summary>
    ///     Contains values used to initialize libcurl internally. One of
    ///     these is passed in the call to <see cref="Curl.GlobalInit" />.
    /// </summary>
    public enum CurlInitFlag
    {
        /// <summary>
        ///     Initialise nothing extra. This sets no bit.
        /// </summary>
        Nothing = 0,

        /// <summary>
        ///     Initialize Ssl.
        /// </summary>
        Ssl = 1,

        /// <summary>
        ///     Initialize the Win32 socket libraries.
        /// </summary>
        Win32 = 2,

        /// <summary>
        ///     Initialize everything possible. This sets all known bits.
        /// </summary>
        All = 3,

        /// <summary>
        ///     Equivalent to <c>All</c>.
        /// </summary>
        Default = All
    };
}