namespace CurlSharp.Enums
{
    /// <summary>
    ///     Your handler for the <see cref="CurlEasy.CurlIoctlCallback" />
    ///     delegate is passed one of these values as its first parameter.
    ///     Right now, the only supported value is
    ///     <code>RestartRead</code>.
    /// </summary>
    public enum CurlIoCommand
    {
        /// <summary>
        ///     No IOCTL operation; we should never see this.
        /// </summary>
        Nop = 0,

        /// <summary>
        ///     When this is sent, your callback may need to, for example,
        ///     rewind a local file that is being sent via FTP.
        /// </summary>
        RestartRead = 1,

        /// <summary>
        ///     End of enumeration marker, don't use in a client application.
        /// </summary>
        Last = 2
    }
}