namespace CurlSharp.Enums
{
    /// <summary>
    ///     This enumeration contains values used to specify the FTP Ssl level
    ///     using the <see cref="CurlOption.FtpSsl" /> option when calling
    ///     <see cref="CurlEasy.SetOpt" />
    /// </summary>
    public enum CurlFtpSsl
    {
        /// <summary>
        ///     Don't attempt to use Ssl.
        /// </summary>
        None = 0,

        /// <summary>
        ///     Try using Ssl, proceed as normal otherwise.
        /// </summary>
        Try = 1,

        /// <summary>
        ///     Require Ssl for the control connection or fail with
        ///     <see cref="CurlCode.FtpSslFailed" />.
        /// </summary>
        Control = 2,

        /// <summary>
        ///     Require Ssl for all communication or fail with
        ///     <see cref="CurlCode.FtpSslFailed" />.
        /// </summary>
        All = 3,

        /// <summary>
        ///     End-of-enumeration marker. Do not use in a client application.
        /// </summary>
        Last = 4
    };
}