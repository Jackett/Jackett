namespace CurlSharp.Enums
{
    /// <summary>
    ///     Status code returned from <see cref="CurlEasy" /> functions.
    /// </summary>
    public enum CurlCode
    {
        /// <summary>
        ///     All fine. Proceed as usual.
        /// </summary>
        Ok = 0,

        /// <summary>
        ///     Aborted by callback. An internal callback returned "abort"
        ///     to libcurl.
        /// </summary>
        AbortedByCallback = 42,

        /// <summary>
        ///     Internal error. A function was called in a bad order.
        /// </summary>
        BadCallingOrder = 44,

        /// <summary>
        ///     Unrecognized transfer encoding.
        /// </summary>
        BadContentEncoding = 61,

        /// <summary>
        ///     Attempting FTP resume beyond file size.
        /// </summary>
        BadDownloadResume = 36,

        /// <summary>
        ///     Internal error. A function was called with a bad parameter.
        /// </summary>
        BadFunctionArgument = 43,

        /// <summary>
        ///     Bad password entered. An error was signaled when the password was
        ///     entered. This can also be the result of a "bad password" returned
        ///     from a specified password callback.
        /// </summary>
        BadPasswordEntered = 46,

        /// <summary>
        ///     Failed to connect to host or proxy.
        /// </summary>
        CouldntConnect = 7,

        /// <summary>
        ///     Couldn't resolve host. The given remote host was not resolved.
        /// </summary>
        CouldntResolveHost = 6,

        /// <summary>
        ///     Couldn't resolve proxy. The given proxy host could not be resolved.
        /// </summary>
        CouldntResolveProxy = 5,

        /// <summary>
        ///     Very early initialization code failed. This is likely to be an
        ///     internal error or problem.
        /// </summary>
        FailedInit = 2,

        /// <summary>
        ///     Maximum file size exceeded.
        /// </summary>
        FilesizeExceeded = 63,

        /// <summary>
        ///     A file given with FILE:// couldn't be opened. Most likely
        ///     because the file path doesn't identify an existing file. Did
        ///     you check file permissions?
        /// </summary>
        FileCouldntReadFile = 37,

        /// <summary>
        ///     We were denied access when trying to login to an FTP server or
        ///     when trying to change working directory to the one given in the URL.
        /// </summary>
        FtpAccessDenied = 9,

        /// <summary>
        ///     An internal failure to lookup the host used for the new
        ///     connection.
        /// </summary>
        FtpCantGetHost = 15,

        /// <summary>
        ///     A bad return code on either PASV or EPSV was sent by the FTP
        ///     server, preventing libcurl from being able to continue.
        /// </summary>
        FtpCantReconnect = 16,

        /// <summary>
        ///     The FTP SIZE command returned error. SIZE is not a kosher FTP
        ///     command, it is an extension and not all servers support it. This
        ///     is not a surprising error.
        /// </summary>
        FtpCouldntGetSize = 32,

        /// <summary>
        ///     This was either a weird reply to a 'RETR' command or a zero byte
        ///     transfer complete.
        /// </summary>
        FtpCouldntRetrFile = 19,

        /// <summary>
        ///     libcurl failed to set ASCII transfer type (TYPE A).
        /// </summary>
        FtpCouldntSetAscii = 29,

        /// <summary>
        ///     Received an error when trying to set the transfer mode to binary.
        /// </summary>
        FtpCouldntSetBinary = 17,

        /// <summary>
        ///     FTP couldn't STOR file. The server denied the STOR operation.
        ///     The error buffer usually contains the server's explanation to this.
        /// </summary>
        FtpCouldntStorFile = 25,

        /// <summary>
        ///     The FTP REST command returned error. This should never happen
        ///     if the server is sane.
        /// </summary>
        FtpCouldntUseRest = 31,

        /// <summary>
        ///     The FTP PORT command returned error. This mostly happen when
        ///     you haven't specified a good enough address for libcurl to use.
        ///     See <see cref="CurlOption.FtpPort" />.
        /// </summary>
        FtpPortFailed = 30,

        /// <summary>
        ///     When sending custom "QUOTE" commands to the remote server, one
        ///     of the commands returned an error code that was 400 or higher.
        /// </summary>
        FtpQuoteError = 21,

        /// <summary>
        ///     Requested FTP Ssl level failed.
        /// </summary>
        FtpSslFailed = 64,

        /// <summary>
        ///     The FTP server rejected access to the server after the password
        ///     was sent to it. It might be because the username and/or the
        ///     password were incorrect or just that the server is not allowing
        ///     you access for the moment etc.
        /// </summary>
        FtpUserPasswordIncorrect = 10,

        /// <summary>
        ///     FTP servers return a 227-line as a response to a PASV command.
        ///     If libcurl fails to parse that line, this return code is
        ///     passed back.
        /// </summary>
        FtpWeird227Format = 14,

        /// <summary>
        ///     After having sent the FTP password to the server, libcurl expects
        ///     a proper reply. This error code indicates that an unexpected code
        ///     was returned.
        /// </summary>
        FtpWeirdPassReply = 11,

        /// <summary>
        ///     libcurl failed to get a sensible result back from the server as
        ///     a response to either a PASV or a EPSV command. The server is flawed.
        /// </summary>
        FtpWeirdPasvReply = 13,

        /// <summary>
        ///     After connecting to an FTP server, libcurl expects to get a
        ///     certain reply back. This error code implies that it got a strange
        ///     or bad reply. The given remote server is probably not an
        ///     OK FTP server.
        /// </summary>
        FtpWeirdServerReply = 8,

        /// <summary>
        ///     After having sent user name to the FTP server, libcurl expects a
        ///     proper reply. This error code indicates that an unexpected code
        ///     was returned.
        /// </summary>
        FtpWeirdUserReply = 12,

        /// <summary>
        ///     After a completed file transfer, the FTP server did not respond a
        ///     proper "transfer successful" code.
        /// </summary>
        FtpWriteError = 20,

        /// <summary>
        ///     Function not found. A required LDAP function was not found.
        /// </summary>
        FunctionNotFound = 41,

        /// <summary>
        ///     Nothing was returned from the server, and under the circumstances,
        ///     getting nothing is considered an error.
        /// </summary>
        GotNothing = 52,

        /// <summary>
        ///     This is an odd error that mainly occurs due to internal confusion.
        /// </summary>
        HttpPostError = 34,

        /// <summary>
        ///     The HTTP server does not support or accept range requests.
        /// </summary>
        HttpRangeError = 33,

        /// <summary>
        ///     This is returned if <see cref="CurlOption.FailOnError" />
        ///     is set TRUE and the HTTP server returns an error code that
        ///     is >= 400.
        /// </summary>
        HttpReturnedError = 22,

        /// <summary>
        ///     Interface error. A specified outgoing interface could not be
        ///     used. Set which interface to use for outgoing connections'
        ///     source IP address with <see cref="CurlOption.Interface" />.
        /// </summary>
        InterfaceFailed = 45,

        /// <summary>
        ///     End-of-enumeration marker; do not use in client applications.
        /// </summary>
        Last = 67,

        /// <summary>
        ///     LDAP cannot bind. LDAP bind operation failed.
        /// </summary>
        LdapCannotBind = 38,

        /// <summary>
        ///     Invalid LDAP URL.
        /// </summary>
        LdapInvalidUrl = 62,

        /// <summary>
        ///     LDAP search failed.
        /// </summary>
        LdapSearchFailed = 39,

        /// <summary>
        ///     Library not found. The LDAP library was not found.
        /// </summary>
        LibraryNotFound = 40,

        /// <summary>
        ///     Malformat user. User name badly specified. *Not currently used*
        /// </summary>
        MalformatUser = 24,

        /// <summary>
        ///     This is not an error. This used to be another error code in an
        ///     old libcurl version and is currently unused.
        /// </summary>
        Obsolete = 50,

        /// <summary>
        ///     Operation timeout. The specified time-out period was reached
        ///     according to the conditions.
        /// </summary>
        OperationTimeouted = 28,

        /// <summary>
        ///     Out of memory. A memory allocation request failed. This is serious
        ///     badness and things are severely messed up if this ever occurs.
        /// </summary>
        OutOfMemory = 27,

        /// <summary>
        ///     A file transfer was shorter or larger than expected. This
        ///     happens when the server first reports an expected transfer size,
        ///     and then delivers data that doesn't match the previously
        ///     given size.
        /// </summary>
        PartialFile = 18,

        /// <summary>
        ///     There was a problem reading a local file or an error returned by
        ///     the read callback.
        /// </summary>
        ReadError = 26,

        /// <summary>
        ///     Failure with receiving network data.
        /// </summary>
        RecvError = 56,

        /// <summary>
        ///     Failed sending network data.
        /// </summary>
        SendError = 55,

        /// <summary>
        ///     Sending the data requires a rewind that failed.
        /// </summary>
        SendFailRewind = 65,

        /// <summary>
        ///     CurlShare is in use.
        /// </summary>
        ShareInUse = 57,

        /// <summary>
        ///     Problem with the CA cert (path? access rights?)
        /// </summary>
        SslCaCert = 60,

        /// <summary>
        ///     There's a problem with the local client certificate.
        /// </summary>
        SslCertProblem = 58,

        /// <summary>
        ///     Couldn't use specified cipher.
        /// </summary>
        SslCipher = 59,

        /// <summary>
        ///     A problem occurred somewhere in the Ssl/TLS handshake. You really
        ///     want to use the <see cref="CurlEasy.CurlDebugCallback" /> delegate and read
        ///     the message there as it pinpoints the problem slightly more. It
        ///     could be certificates (file formats, paths, permissions),
        ///     passwords, and others.
        /// </summary>
        SslConnectError = 35,

        /// <summary>
        ///     Failed to initialize Ssl engine.
        /// </summary>
        SslEngineInitFailed = 66,

        /// <summary>
        ///     The specified crypto engine wasn't found.
        /// </summary>
        SslEngineNotFound = 53,

        /// <summary>
        ///     Failed setting the selected Ssl crypto engine as default!
        /// </summary>
        SslEngineSetFailed = 54,

        /// <summary>
        ///     The remote server's Ssl certificate was deemed not OK.
        /// </summary>
        SslPeerCertificate = 51,

        /// <summary>
        ///     A telnet option string was improperly formatted.
        /// </summary>
        TelnetOptionSyntax = 49,

        /// <summary>
        ///     Too many redirects. When following redirects, libcurl hit the
        ///     maximum amount. Set your limit with
        ///     <see cref="CurlOption.MaxRedirs" />.
        /// </summary>
        TooManyRedirects = 47,

        /// <summary>
        ///     An option set with <see cref="CurlOption.TelnetOptions" />
        ///     was not recognized/known. Refer to the appropriate documentation.
        /// </summary>
        UnknownTelnetOption = 48,

        /// <summary>
        ///     The URL you passed to libcurl used a protocol that this libcurl
        ///     does not support. The support might be a compile-time option that
        ///     wasn't used, it can be a misspelled protocol string or just a
        ///     protocol libcurl has no code for.
        /// </summary>
        UnsupportedProtocol = 1,

        /// <summary>
        ///     The URL was not properly formatted.
        /// </summary>
        UrlMalformat = 3,

        /// <summary>
        ///     URL user malformatted. The user-part of the URL syntax was not
        ///     correct.
        /// </summary>
        UrlMalformatUser = 4,

        /// <summary>
        ///     An error occurred when writing received data to a local file,
        ///     or an error was returned to libcurl from a write callback.
        /// </summary>
        WriteError = 23,
    };
}