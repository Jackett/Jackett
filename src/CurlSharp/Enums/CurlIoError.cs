namespace CurlSharp.Enums
{
    /// <summary>
    ///     Your handler for the <see cref="CurlEasy.CurlIoctlCallback" /> delegate
    ///     should return a member of this enumeration.
    /// </summary>
    public enum CurlIoError
    {
        /// <summary>
        ///     Indicate that the callback processed everything okay.
        /// </summary>
        Ok = 0,

        /// <summary>
        ///     Unknown command sent to callback. Right now, only
        ///     <code>RestartRead</code> is supported.
        /// </summary>
        UnknownCommand = 1,

        /// <summary>
        ///     Indicate to libcurl that a restart failed.
        /// </summary>
        FailRestart = 2,

        /// <summary>
        ///     End of enumeration marker, don't use in a client application.
        /// </summary>
        Last = 3
    }
}