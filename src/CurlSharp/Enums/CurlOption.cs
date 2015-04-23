/***************************************************************************
 *
 * CurlS#arp
 *
 * Copyright (c) 2014 Dr. Masroor Ehsan (masroore@gmail.com)
 * Portions copyright (c) 2004, 2005 Jeff Phillips (jeff@jeffp.net)
 *
 * This software is licensed as described in the file LICENSE, which you
 * should have received as part of this distribution.
 *
 * You may opt to use, copy, modify, merge, publish, distribute and/or sell
 * copies of this Software, and permit persons to whom the Software is
 * furnished to do so, under the terms of the LICENSE file.
 *
 * This software is distributed on an "AS IS" basis, WITHOUT WARRANTY OF
 * ANY KIND, either express or implied.
 *
 **************************************************************************/

namespace CurlSharp
{
    /// <summary>
    ///     One of these is passed as the first parameter to
    ///     <see cref="CurlEasy.SetOpt" />. The <c>Description</c> column of
    ///     the table describes the value that should be passed as the second parameter.
    /// </summary>
    public enum CurlOption
    {
        /// <summary>
        ///     Pass a <c>true</c> parameter to enable this. When enabled, libcurl
        ///     will automatically set the Referer: field in requests where it follows
        ///     a Location: redirect.
        /// </summary>
        AutoReferer = 58,

        /// <summary>
        ///     Pass an <c>int</c> specifying your preferred size for the receive buffer
        ///     in libcurl. The main point of this would be that the write callback gets
        ///     called more often and with smaller chunks. This is just treated as a
        ///     request, not an order. You cannot be guaranteed to actually get the
        ///     requested size. (Added in 7.10)
        /// </summary>
        BufferSize = 98,

        /// <summary>
        ///     Pass a <c>string</c> naming a file holding one or more certificates
        ///     to verify the peer with. This only makes sense when used in combination
        ///     with the <c>SslVerifyPeer</c> option.
        /// </summary>
        CaInfo = 10065,

        /// <summary>
        ///     Pass a <c>string</c> naming a directory holding multiple CA certificates
        ///     to verify the peer with. The certificate directory must be prepared
        ///     using the openssl c_rehash utility. This only makes sense when used in
        ///     combination with the <c>SslVerifyPeer</c> option. The
        ///     <c>CaPath</c> function apparently does not work in Windows due
        ///     to some limitation in openssl. (Added in 7.9.8)
        /// </summary>
        CaPath = 10097,

        /// <summary>
        ///     Pass an <c>int</c>. This option sets what policy libcurl should use when
        ///     the connection cache is filled and one of the open connections has to be
        ///     closed to make room for a new connection. This must be one of the
        ///     <see cref="CurlClosePolicy" /> members. Use
        ///     <see cref="CurlClosePolicy.LeastRecentlyUsed" /> to make
        ///     libcurl close the connection that was least recently used, that connection
        ///     is also least likely to be capable of re-use. Use
        ///     <see cref="CurlClosePolicy.Oldest" /> to make libcurl close
        ///     the oldest connection, the one that was created first among the ones in
        ///     the connection cache. The other close policies are not supported yet.
        /// </summary>
        ClosePolicy = 72,

        /// <summary>
        ///     Time-out connect operations after this amount of seconds, if connects
        ///     are OK within this time, then fine... This only aborts the connect
        ///     phase. [Only works on unix-style/SIGALRM operating systems]
        /// </summary>
        ConnectTimeout = 78,

        /// <summary>
        ///     Pass a <c>string</c> as parameter. It will be used to set a cookie
        ///     in the http request. The format of the string should be NAME=CONTENTS,
        ///     where NAME is the cookie name and CONTENTS is what the cookie should contain.
        ///     <para>
        ///         If you need to set multiple cookies, you need to set them all using a
        ///         single option and thus you need to concatenate them all in one single
        ///         string. Set multiple cookies in one string like this:
        ///         "name1=content1; name2=content2;" etc.
        ///     </para>
        ///     <para>
        ///         Using this option multiple times will only make the latest string override
        ///         the previously ones.
        ///     </para>
        /// </summary>
        Cookie = 10022,

        /// <summary>
        ///     Pass a <c>string</c> as parameter. It should contain the name of your
        ///     file holding cookie data to read. The cookie data may be in Netscape /
        ///     Mozilla cookie data format or just regular HTTP-style headers dumped
        ///     to a file.
        ///     <para>
        ///         Given an empty or non-existing file, this option will enable cookies
        ///         for this CurlEasy object, making it understand and parse received cookies
        ///         and then use matching cookies in future request.
        ///     </para>
        /// </summary>
        CookieFile = 10031,

        /// <summary>
        ///     Pass a file name as <c>string</c>. This will make libcurl write all
        ///     internally known cookies to the specified file when
        ///     <see cref="CurlEasy.Dispose" /> is called. If no cookies are known, no file
        ///     will be created. Using this option also enables cookies for this
        ///     session, so if you for example follow a location it will make matching
        ///     cookies get sent accordingly.
        ///     <note>
        ///         If the cookie jar file can't be created or written to
        ///         (when <see cref="CurlEasy.Dispose" /> is called), libcurl will not and
        ///         cannot report an error for this. Using <c>Verbose</c> or
        ///         <c>CurlDebugCallback</c> will get a warning to display, but that
        ///         is the only visible feedback you get about this possibly lethal situation.
        ///     </note>
        /// </summary>
        CookieJar = 10082,

        /// <summary>
        ///     Pass a <c>bool</c> set to <c>true</c> to mark this as a new cookie
        ///     "session". It will force libcurl to ignore all cookies it is about to
        ///     load that are "session cookies" from the previous session. By default,
        ///     libcurl always stores and loads all cookies, independent of whether they are
        ///     session cookies. Session cookies are cookies without expiry date and they
        ///     are meant to be alive and existing for this "session" only.
        /// </summary>
        CookieSession = 96,

        /// <summary>
        ///     Convert Unix newlines to CRLF newlines on transfers.
        /// </summary>
        CRLF = 27,

        /// <summary>
        ///     Pass a <c>string</c> as parameter. It will be used instead of GET or
        ///     HEAD when doing an HTTP request, or instead of LIST or NLST when
        ///     doing an ftp directory listing. This is useful for doing DELETE or
        ///     other more or less obscure HTTP requests. Don't do this at will,
        ///     make sure your server supports the command first.
        ///     <para>
        ///         Restore to the internal default by setting this to <c>null</c>.
        ///     </para>
        ///     <note>
        ///         Many people have wrongly used this option to replace the entire
        ///         request with their own, including multiple headers and POST contents.
        ///         While that might work in many cases, it will cause libcurl to send
        ///         invalid requests and it could possibly confuse the remote server badly.
        ///         Use <c>Post</c> and <c>PostFields</c> to set POST data.
        ///         Use <c>HttpHeader</c> to replace or extend the set of headers
        ///         sent by libcurl. Use <c>HttpVersion</c> to change HTTP version.
        ///     </note>
        /// </summary>
        CustomRequest = 10036,

        /// <summary>
        ///     Pass an <c>object</c> referene to whatever you want passed to your
        ///     <see cref="CurlEasy.CurlDebugCallback" /> delegate's <c>extraData</c> argument.
        ///     This reference is not used internally by libcurl, it is only passed to
        ///     the delegate.
        /// </summary>
        DebugData = 10095,

        /// <summary>
        ///     Pass a reference to an <see cref="CurlEasy.CurlDebugCallback" /> delegate.
        ///     <c>Verbose</c> must be in effect. This delegate receives debug
        ///     information, as specified with the <see cref="CurlInfoType" /> argument.
        ///     This function must return 0.
        /// </summary>
        DebugFunction = 20094,

        /// <summary>
        ///     Pass an <c>int</c>, specifying the timeout in seconds. Name resolves
        ///     will be kept in memory for this number of seconds. Set to zero (0)
        ///     to completely disable caching, or set to -1 to make the cached
        ///     entries remain forever. By default, libcurl caches this info for 60
        ///     seconds.
        /// </summary>
        DnsCacheTimeout = 92,

        /// <summary>
        ///     Not supported.
        /// </summary>
        DnsUseGlobalCache = 91,

        /// <summary>
        ///     Pass a <c>string</c> containing the path name to the Entropy Gathering
        ///     Daemon socket. It will be used to seed the random engine for Ssl.
        /// </summary>
        EgdSocket = 10077,

        /// <summary>
        ///     Set this option to the file name of your .netrc file you want libcurl to parse (using the CURLOPT_NETRC option). If
        ///     not set, libcurl will do a poor attempt to find the user's home directory and check for a .netrc file in there.
        /// </summary>
        NetRcFile = 118,

        /// <summary>
        ///     Sets the contents of the Accept-Encoding: header sent in an HTTP request,
        ///     and enables decoding of a response when a Content-Encoding: header is
        ///     received. Three encodings are supported: <c>identity</c>, which does
        ///     nothing, <c>deflate</c> which requests the server to compress its
        ///     response using the zlib algorithm, and <c>gzip</c> which requests the
        ///     gzip algorithm. If a zero-length string is set, then an Accept-Encoding:
        ///     header containing all supported encodings is sent.
        /// </summary>
        Encoding = 10102,

        /// <summary>
        ///     Not supported.
        /// </summary>
        ErrorBuffer = 10010,

        /// <summary>
        ///     A <c>true</c> parameter tells the library to fail silently if the
        ///     HTTP code returned is equal to or larger than 300. The default
        ///     action would be to return the page normally, ignoring that code.
        /// </summary>
        FailOnError = 45,

        /// <summary>
        ///     Pass a <c>bool</c>. If it is <c>true</c>, libcurl will attempt to get
        ///     the modification date of the remote document in this operation. This
        ///     requires that the remote server sends the time or replies to a time
        ///     querying command. The CurlEasy.GetInfo function with the
        ///     <see cref="CurlInfo.Filetime" /> argument can be used after a
        ///     transfer to extract the received time (if any).
        /// </summary>
        Filetime = 69,

        /// <summary>
        ///     A <c>true</c> parameter tells the library to follow any Location:
        ///     header that the server sends as part of an HTTP header.
        ///     <note>
        ///         this means that the library will re-send the same request on the
        ///         new location and follow new Location: headers all the way until no
        ///         more such headers are returned. <c>MaxRedirs</c> can be used
        ///         to limit the number of redirects libcurl will follow.
        ///     </note>
        /// </summary>
        FollowLocation = 52,

        /// <summary>
        ///     Pass a <c>bool</c>. Set to <c>true</c> to make the next transfer
        ///     explicitly close the connection when done. Normally, libcurl keeps all
        ///     connections alive when done with one transfer in case there comes a
        ///     succeeding one that can re-use them. This option should be used with
        ///     caution and only if you understand what it does. Set to <c>false</c>
        ///     to have libcurl keep the connection open for possibly later re-use
        ///     (default behavior).
        /// </summary>
        ForbidReuse = 75,

        /// <summary>
        ///     Pass a <c>bool</c>. Set to <c>true</c> to make the next transfer use a
        ///     new (fresh) connection by force. If the connection cache is full before
        ///     this connection, one of the existing connections will be closed as
        ///     according to the selected or default policy. This option should be used
        ///     with caution and only if you understand what it does. Set this to
        ///     <c>false</c> to have libcurl attempt re-using an existing connection
        ///     (default behavior).
        /// </summary>
        FreshConnect = 74,

        /// <summary>
        ///     String that will be passed to the FTP server when it requests
        ///     account info.
        /// </summary>
        FtpAccount = 10134,

        /// <summary>
        ///     A <c>true</c> parameter tells the library to append to the remote
        ///     file instead of overwrite it. This is only useful when uploading
        ///     to an ftp site.
        /// </summary>
        FtpAppend = 50,

        /// <summary>
        ///     A <c>true</c> parameter tells the library to just list the names of
        ///     an ftp directory, instead of doing a full directory listing that
        ///     would include file sizes, dates etc.
        ///     <para>
        ///         This causes an FTP NLST command to be sent. Beware that some FTP
        ///         servers list only files in their response to NLST; they might not
        ///         include subdirectories and symbolic links.
        ///     </para>
        /// </summary>
        FtpListOnly = 48,

        /// <summary>
        ///     Pass a <c>string</c> as parameter. It will be used to get the IP
        ///     address to use for the ftp PORT instruction. The PORT instruction
        ///     tells the remote server to connect to our specified IP address.
        ///     The string may be a plain IP address, a host name, an network
        ///     interface name (under Unix) or just a '-' letter to let the library
        ///     use your systems default IP address. Default FTP operations are
        ///     passive, and thus won't use PORT.
        ///     <para>
        ///         You disable PORT again and go back to using the passive version
        ///         by setting this option to NULL.
        ///     </para>
        /// </summary>
        FtpPort = 10017,

        /// <summary>
        ///     When FTP over Ssl/TLS is selected (with <c>FtpSsl</c>),
        ///     this option can be used to change libcurl's default action which
        ///     is to first try "AUTH Ssl" and then "AUTH TLS" in this order,
        ///     and proceed when a OK response has been received.
        ///     <para>
        ///         Pass a member of the <see cref="CurlFtpAuth" /> enumeration.
        ///     </para>
        /// </summary>
        FtpSslAuth = 129,

        /// <summary>
        ///     Pass a <c>bool</c>. If the value is <c>true</c>, cURL will attempt to
        ///     create any remote directory that it fails to CWD into. CWD is the
        ///     command that changes working directory. (Added in 7.10.7)
        /// </summary>
        FtpCreateMissingDirs = 110,

        /// <summary>
        ///     Pass an <c>int</c>. Causes libcurl to set a timeout period (in seconds)
        ///     on the amount of time that the server is allowed to take in order to
        ///     generate a response message for a command before the session is
        ///     considered hung. Note that while libcurl is waiting for a response, this
        ///     value overrides <c>Timeout</c>. It is recommended that if used in
        ///     conjunction with <c>Timeout</c>, you set
        ///     <c>FtpResponseTimeout</c> to a value smaller than
        ///     <c>Timeout</c>. (Added in 7.10.8)
        /// </summary>
        FtpResponseTimeout = 112,

        /// <summary>
        ///     Pass a member of the <see cref="CurlFtpSsl" /> enumeration.
        /// </summary>
        FtpSsl = 119,

        /// <summary>
        ///     Pass a <c>bool</c>. If the value is <c>true</c>, it tells curl to use
        ///     the EPRT (and LPRT) command when doing active FTP downloads (which is
        ///     enabled by FtpPort). Using EPRT means that it will first attempt
        ///     to use EPRT and then LPRT before using PORT, but if you pass <c>false</c>
        ///     to this option, it will not try using EPRT or LPRT, only plain PORT.
        ///     (Added in 7.10.5)
        /// </summary>
        FtpUseEprt = 106,

        /// <summary>
        ///     Pass a <c>bool</c>. If the value is <c>true</c>, it tells curl to use
        ///     the EPSV command when doing passive FTP downloads (which it always does
        ///     by default). Using EPSV means that it will first attempt to use EPSV
        ///     before using PASV, but if you pass <c>false</c> to this option, it will
        ///     not try using EPSV, only plain PASV.
        /// </summary>
        FtpUseEpsv = 85,

        /// <summary>
        ///     A <c>true</c> parameter tells the library to include the header in
        ///     the body output. This is only relevant for protocols that actually
        ///     have headers preceding the data (like HTTP).
        /// </summary>
        Header = 42,

        /// <summary>
        ///     Object reference to pass to the <see cref="CurlEasy.CurlHeaderCallback" />
        ///     delegate. Note that if you specify the <c>CurlHeaderCallback</c>,
        ///     this is the reference you'll get as the <c>extraData</c> parameter.
        /// </summary>
        HeaderData = 10029,

        /// <summary>
        ///     Provide an <see cref="CurlEasy.CurlHeaderCallback" /> delegate reference.
        ///     This delegate gets called by libcurl as soon as there is received
        ///     header data that needs to be written down. The headers are guaranteed
        ///     to be written one-by-one and only complete lines are written. Parsing
        ///     headers should be easy enough using this. The size of the data contained
        ///     in <c>buf</c> is <c>size</c> multiplied with <c>nmemb</c>.
        ///     Return the number of bytes actually written or return -1 to signal
        ///     error to the library (it will cause it to abort the transfer with a
        ///     <see cref="CurlCode.WriteError" /> return code).
        /// </summary>
        HeaderFunction = 20079,

        /// <summary>
        ///     Pass an <see cref="CurlSlist" /> of aliases to be treated as valid HTTP
        ///     200 responses. Some servers respond with a custom header response line.
        ///     For example, IceCast servers respond with "ICY 200 OK". By including
        ///     this string in your list of aliases, the response will be treated as a
        ///     valid HTTP header line such as "HTTP/1.0 200 OK". (Added in 7.10.3)
        ///     <note>
        ///         The alias itself is not parsed for any version strings. So if your alias
        ///         is "MYHTTP/9.9", libcurl will not treat the server as responding with
        ///         HTTP version 9.9. Instead libcurl will use the value set by option
        ///         <c>HttpVersion</c>.
        ///     </note>
        /// </summary>
        Http200Aliases = 10104,

        /// <summary>
        ///     Pass an <c>int</c> as parameter, which is set to a bitmask
        ///     of <see cref="CurlHttpAuth" />, to tell libcurl what authentication
        ///     method(s) you want it to use. If more than one bit is set, libcurl will
        ///     first query the site to see what authentication methods it supports and
        ///     then pick the best one you allow it to use. Note that for some methods,
        ///     this will induce an extra network round-trip. Set the actual name and
        ///     password with the <c>UserPwd</c> option. (Added in 7.10.6)
        /// </summary>
        HttpAuth = 107,

        /// <summary>
        ///     Pass a <c>bool</c>. If it's <c>true</c>, this forces the HTTP request
        ///     to get back to GET. Usable if a POST, HEAD, PUT or a custom request
        ///     has been used previously using the same <see cref="CurlEasy" /> object.
        /// </summary>
        HttpGet = 80,

        /// <summary>
        ///     Pass an <see cref="CurlSlist" /> reference containing HTTP headers to pass to
        ///     the server in your HTTP request. If you add a header that is otherwise
        ///     generated and used by libcurl internally, your added one will be used
        ///     instead. If you add a header with no contents as in 'Accept:' (no data
        ///     on the right side of the colon), the internally used header will get
        ///     disabled. Thus, using this option you can add new headers, replace
        ///     internal headers and remove internal headers. The headers included in the
        ///     <c>CurlSlist</c> must not be CRLF-terminated, because curl adds CRLF after
        ///     each header item. Failure to comply with this will result in strange bugs
        ///     because the server will most likely ignore part of the headers you specified.
        ///     <para>
        ///         The first line in a request (usually containing a GET or POST) is not
        ///         a header and cannot be replaced using this option. Only the lines
        ///         following the request-line are headers.
        ///     </para>
        ///     <para>
        ///         Pass a <c>null</c> to this to reset back to no custom headers.
        ///     </para>
        ///     <note>
        ///         The most commonly replaced headers have "shortcuts" in the options
        ///         <c>Cookie</c>, <c>UserAgent</c> and <c>Referer</c>.
        ///     </note>
        /// </summary>
        HttpHeader = 10023,

        /// <summary>
        ///     Tells libcurl you want a multipart/formdata HTTP POST to be made and you
        ///     instruct what data to pass on to the server. Pass a reference to a
        ///     <see cref="CurlHttpMultiPartForm" /> object as parameter.
        ///     The best and most elegant way to do this, is to use
        ///     <see cref="CurlHttpMultiPartForm.AddSection" /> as documented.
        ///     <para>
        ///         Using POST with HTTP 1.1 implies the use of a "Expect: 100-continue"
        ///         header. You can disable this header with <c>HttpHeader</c> as usual.
        ///     </para>
        /// </summary>
        HttpPost = 10024,

        /// <summary>
        ///     Set the parameter to <c>true</c> to get the library to tunnel all
        ///     operations through a given HTTP proxy. Note that there is a big
        ///     difference between using a proxy and tunneling through it. If you
        ///     don't know what this means, you probably don't want this tunneling option.
        /// </summary>
        HttpProxyTunnel = 61,

        /// <summary>
        ///     Pass a member of the <see cref="CurlHttpVersion" /> enumeration. These
        ///     values force libcurl to use the specific HTTP versions. This is not
        ///     sensible to do unless you have a good reason.
        /// </summary>
        HttpVersion = 84,

        /// <summary>
        ///     Provide an <see cref="CurlEasy.CurlIoctlCallback" /> delegate reference.
        ///     This delegate gets called by libcurl when an IOCTL operation,
        ///     such as a rewind of a file being sent via FTP, is required on
        ///     the client side.
        /// </summary>
        IoctlFunction = 20130,

        /// <summary>
        ///     Provide an object, such as a <c>FileStream</c>, upon which
        ///     you may need to perform an IOCTL operation. Right now, only
        ///     rewind is supported.
        /// </summary>
        IoctlData = 10131,

        /// <summary>
        ///     When uploading a file to a remote site, this option should be used to
        ///     tell libcurl what the expected size of the infile is. This value should
        ///     be passed as an <c>int</c>.
        /// </summary>
        InfileSize = 14,

        /// <summary>
        ///     When uploading a file to a remote site, this option should be used to
        ///     tell libcurl what the expected size of the infile is. This value should
        ///     be passed as a <c>long</c>. (Added in 7.11.0)
        /// </summary>
        InFileSizeLarge = 30115,

        /// <summary>
        ///     Pass a <c>string</c> as parameter. This sets the interface name to use
        ///     as the outgoing network interface. The name can be an interface name,
        ///     an IP address or a host name.
        /// </summary>
        Interface = 10062,

        /// <summary>
        ///     Pass one of the members of the <see cref="CurlIpResolve" /> enumeration.
        /// </summary>
        IpResolve = 113,

        /// <summary>
        ///     Pass a <c>string</c> as parameter. Set the kerberos4 security level;
        ///     this also enables kerberos4 awareness. This is a string, 'clear', 'safe',
        ///     'confidential' or 'private'. If the string is set but doesn't match
        ///     one of these, 'private' will be used. Set the string to <c>null</c>
        ///     to disable kerberos4. The kerberos support only works for FTP.
        /// </summary>
        Krb4Level = 10063,

        /// <summary>
        ///     Pass an <c>int</c> as parameter. It contains the transfer speed in bytes
        ///     per second that the transfer should be below during
        ///     <c>LowSpeedTime</c> seconds for the library to consider it
        ///     too slow and abort.
        /// </summary>
        LowSpeedLimit = 19,

        /// <summary>
        ///     Pass an <c>int</c> as parameter. It contains the time in seconds that
        ///     the transfer should be below the <c>LowSpeedLimit</c> for the
        ///     library to consider it too slow and abort.
        /// </summary>
        LowSpeedTime = 20,

        /// <summary>
        ///     Pass an <c>int</c>. The set number will be the persistent connection
        ///     cache size. The set amount will be the maximum amount of simultaneously
        ///     open connections that libcurl may cache. Default is 5, and there isn't
        ///     much point in changing this value unless you are perfectly aware of how
        ///     this works and changes libcurl's behaviour. This concerns connections
        ///     using any of the protocols that support persistent connections.
        ///     <para>
        ///         When reaching the maximum limit, cURL uses the <c>ClosePolicy</c>
        ///         to figure out which of the existing connections to close to prevent the
        ///         number of open connections to increase.
        ///     </para>
        ///     <note>
        ///         if you already have performed transfers with this CurlEasy object, setting a
        ///         smaller <c>MaxConnects</c> than before may cause open connections
        ///         to get closed unnecessarily.
        ///     </note>
        /// </summary>
        MaxConnects = 71,

        /// <summary>
        ///     Pass an <c>int</c> as parameter. This allows you to specify the maximum
        ///     size (in bytes) of a file to download. If the file requested is larger
        ///     than this value, the transfer will not start and
        ///     <see cref="CurlCode.FilesizeExceeded" /> will be returned.
        ///     <note>
        ///         The file size is not always known prior to download, and for such files
        ///         this option has no effect even if the file transfer ends up being larger
        ///         than this given limit. This concerns both FTP and HTTP transfers.
        ///     </note>
        /// </summary>
        MaxFileSize = 114,

        /// <summary>
        ///     Pass a <c>long</c> as parameter. This allows you to specify the
        ///     maximum size (in bytes) of a file to download. If the file requested
        ///     is larger than this value, the transfer will not start and
        ///     <see cref="CurlCode.FilesizeExceeded" /> will be returned.
        ///     (Added in 7.11.0)
        ///     <note>
        ///         The file size is not always known prior to download, and for such files
        ///         this option has no effect even if the file transfer ends up being larger
        ///         than this given limit. This concerns both FTP and HTTP transfers.
        ///     </note>
        /// </summary>
        MaxFileSizeLarge = 30117,

        /// <summary>
        ///     Pass an <c>int</c>. The set number will be the redirection limit. If
        ///     that many redirections have been followed, the next redirect will cause
        ///     an error (<c>TooManyRedirects</c>). This option only makes sense
        ///     if the <c>FollowLocation</c> is used at the same time.
        /// </summary>
        MaxRedirs = 68,

        /// <summary>
        ///     This parameter controls the preference of libcurl between using
        ///     user names and passwords from your <c>~/.netrc</c> file, relative to
        ///     user names and passwords in the URL supplied with <c>Url</c>.
        ///     <note>
        ///         libcurl uses a user name (and supplied or prompted password)
        ///         supplied with <c>UserPwd</c> in preference to any of the
        ///         options controlled by this parameter.
        ///     </note>
        ///     <para>
        ///         Pass a member of the <see cref="CurlNetrcOption" /> enumeration.
        ///     </para>
        ///     <para>
        ///         Only machine name, user name and password are taken into account
        ///         (init macros and similar things aren't supported).
        ///     </para>
        ///     <note>
        ///         libcurl does not verify that the file has the correct properties
        ///         set (as the standard Unix ftp client does). It should only be
        ///         readable by user.
        ///     </note>
        /// </summary>
        Netrc = 51,

        /// <summary>
        ///     Pass a <c>string</c> as parameter, containing the full path name to the
        ///     file you want libcurl to use as .netrc file. If this option is omitted,
        ///     and <c>Netrc</c> is set, libcurl will attempt to find the a
        ///     .netrc file in the current user's home directory. (Added in 7.10.9)
        /// </summary>
        NetrcFile = 10118,

        /// <summary>
        ///     A <c>true</c> parameter tells the library to not include the
        ///     body-part in the output. This is only relevant for protocols that
        ///     have separate header and body parts. On HTTP(S) servers, this
        ///     will make libcurl do a HEAD request.
        ///     <para>
        ///         To change back to GET, you should use <c>HttpGet</c>. To
        ///         change back to POST, you should use <c>Post</c>. Setting
        ///         <c>NoBody</c> to <c>false</c> has no effect.
        ///     </para>
        /// </summary>
        NoBody = 44,

        /// <summary>
        ///     A <c>true</c> parameter tells the library to shut off progress
        ///     reporting.
        /// </summary>
        NoProgress = 43,

        /// <summary>
        ///     Pass a <c>bool</c>. If it is <c>true</c>, libcurl will not use any
        ///     functions that install signal handlers or any functions that cause
        ///     signals to be sent to the process. This option is mainly here to allow
        ///     multi-threaded unix applications to still set/use all timeout options
        ///     etc, without risking getting signals. (Added in 7.10)
        ///     <para>
        ///         Consider using libcurl with ares built-in to enable asynchronous Dns
        ///         lookups. It enables nice timeouts for name resolves without signals.
        ///     </para>
        /// </summary>
        NoSignal = 99,

        /// <summary>
        ///     Not supported.
        /// </summary>
        PasvHost = 126,

        /// <summary>
        ///     Pass an <c>int</c> specifying what remote port number to connect to,
        ///     instead of the one specified in the URL or the default port for the
        ///     used protocol.
        /// </summary>
        Port = 3,

        /// <summary>
        ///     A <c>true</c> parameter tells the library to do a regular HTTP post.
        ///     This will also make the library use the a "Content-Type:
        ///     application/x-www-form-urlencoded" header. (This is by far the most
        ///     commonly used POST method).
        ///     <para>
        ///         Use the <c>PostFields</c> option to specify what data to post
        ///         and <c>PostFieldSize</c> to set the data size. Optionally,
        ///         you can provide data to POST using the <c>CurlReadCallback</c> and
        ///         <c>ReadData</c> options.
        ///     </para>
        ///     <para>
        ///         You can override the default POST Content-Type: header by setting
        ///         your own with <c>HttpHeader</c>.
        ///     </para>
        ///     <para>
        ///         Using POST with HTTP 1.1 implies the use of a "Expect: 100-continue"
        ///         header. You can disable this header with <c>HttpHeader</c> as usual.
        ///     </para>
        ///     <para>
        ///         If you use POST to a HTTP 1.1 server, you can send data without knowing
        ///         the size before starting the POST if you use chunked encoding. You
        ///         enable this by adding a header like "Transfer-Encoding: chunked" with
        ///         <c>HttpHeader</c>. With HTTP 1.0 or without chunked transfer,
        ///         you must specify the size in the request.
        ///     </para>
        ///     <note>
        ///         if you have issued a POST request and want to make a HEAD or GET instead,
        ///         you must explictly pick the new request type using <c>NoBody</c>
        ///         or <c>HttpGet</c> or similar.
        ///     </note>
        /// </summary>
        Post = 47,

        /// <summary>
        ///     Pass a <c>string</c> as parameter, which should be the full data to post
        ///     in an HTTP POST operation. You must make sure that the data is formatted
        ///     the way you want the server to receive it. libcurl will not convert or
        ///     encode it for you. Most web servers will assume this data to be
        ///     url-encoded. Take note.
        ///     <para>
        ///         This POST is a normal application/x-www-form-urlencoded kind (and
        ///         libcurl will set that Content-Type by default when this option is used),
        ///         which is the most commonly used one by HTML forms. See also the
        ///         <c>Post</c>. Using <c>PostFields</c> implies
        ///         <c>Post</c>.
        ///     </para>
        ///     <para>
        ///         Using POST with HTTP 1.1 implies the use of a "Expect: 100-continue"
        ///         header. You can disable this header with <c>HttpHeader</c> as usual.
        ///     </para>
        ///     <note>
        ///         to make multipart/formdata posts (aka rfc1867-posts), check out the
        ///         <c>HttpPost</c> option.
        ///     </note>
        /// </summary>
        PostFields = 10015,

        /// <summary>
        ///     If you want to post data to the server without letting libcurl do a
        ///     <c>strlen()</c> to measure the data size, this option must be used. When
        ///     this option is used you can post fully binary data, which otherwise
        ///     is likely to fail. If this size is set to zero, the library will use
        ///     <c>strlen()</c> to get the size.
        /// </summary>
        PostFieldSize = 60,

        /// <summary>
        ///     Pass a <c>long</c> as parameter. Use this to set the size of the
        ///     <c>PostFields</c> data to prevent libcurl from doing
        ///     <c>strlen()</c> on the data to figure out the size. This is the large
        ///     file version of the <c>PostFieldSize</c> option. (Added in 7.11.1)
        /// </summary>
        PostFieldSizeLarge = 30120,

        /// <summary>
        ///     Pass an <see cref="CurlSlist" /> of FTP commands to pass to the server after
        ///     your ftp transfer request. Disable this operation again by setting this
        ///     option to <c>null</c>.
        /// </summary>
        Postquote = 10039,

        /// <summary>
        ///     Pass an <see cref="CurlSlist" /> containing the FTP commands to pass to
        ///     the server after the transfer type is set. Disable this operation
        ///     again by setting a <c>null</c> to this option.
        /// </summary>
        Prequote = 10093,

        /// <summary>
        ///     Pass an <c>object</c> as parameter, referencing data that should be
        ///     associated with this <see cref="CurlEasy" /> object. The object can
        ///     subsequently be retrieved using CurlEasy.GetInfo with the
        ///     <see cref="CurlInfo.Private" /> option. libcurl itself does
        ///     nothing with this data. (Added in 7.10.3)
        /// </summary>
        Private = 10103,

        /// <summary>
        ///     Pass an <c>object</c> reference that will be untouched by libcurl
        ///     and passed as the first argument in the progress delegate set with
        ///     <c>CurlProgressCallback</c>.
        /// </summary>
        ProgressData = 10057,

        /// <summary>
        ///     Pass an <see cref="CurlEasy.CurlProgressCallback" /> delegate reference. This
        ///     delegate gets called by libcurl at a frequent interval during data
        ///     transfer. Unknown/unused argument values will be set to zero (like if
        ///     you only download data, the upload size will remain 0). Returning a
        ///     non-zero value from this delegate will cause libcurl to abort the
        ///     transfer and return <see cref="CurlCode.AbortedByCallback" />.
        ///     <note>
        ///         <c>NoProgress</c> must be set to <c>false</c> to make this
        ///         function actually get called.
        ///     </note>
        /// </summary>
        ProgressFunction = 20056,

        /// <summary>
        ///     Set HTTP proxy to use. The parameter should be a <c>string</c> holding
        ///     the host name or dotted IP address. To specify port number in this
        ///     string, append <c>:[port]</c> to the end of the host name. The proxy
        ///     string may be prefixed with <c>[protocol]://</c> since any such prefix
        ///     will be ignored. The proxy's port number may optionally be specified
        ///     with the separate option <c>ProxyPort</c>.
        ///     <para>
        ///         NOTE: when you tell the library to use an HTTP proxy, libcurl will
        ///         transparently convert operations to HTTP even if you specify an FTP
        ///         URL etc. This may have an impact on what other features of the library
        ///         you can use, such as <c>Quote</c> and similar FTP specifics
        ///         that don't work unless you tunnel through the HTTP proxy. Such tunneling
        ///         is activated with <c>HttpProxyTunnel</c>.
        ///     </para>
        /// </summary>
        Proxy = 10004,

        /// <summary>
        ///     Pass a bitmask of <see cref="CurlHttpAuth" /> as the paramter, to tell
        ///     libcurl what authentication method(s) you want it to use for your proxy
        ///     authentication. If more than one bit is set, libcurl will first query the
        ///     site to see what authentication methods it supports and then pick the best
        ///     one you allow it to use. Note that for some methods, this will induce an
        ///     extra network round-trip. Set the actual name and password with the
        ///     <c>ProxyUserPwd</c> option. The bitmask can be constructed by
        ///     or'ing together the <see cref="CurlHttpAuth" /> bits. As of this writing,
        ///     only <see cref="CurlHttpAuth.Basic" /> and
        ///     <see cref="CurlHttpAuth.Ntlm" /> work. (Added in 7.10.7)
        /// </summary>
        ProxyAuth = 111,

        /// <summary>
        ///     Pass an <c>int</c> with this option to set the proxy port to connect
        ///     to unless it is specified in the proxy string <c>Proxy</c>.
        /// </summary>
        ProxyPort = 59,

        /// <summary>
        ///     Pass a <see cref="CurlProxyType" /> to set type of the proxy.
        /// </summary>
        ProxyType = 101,

        /// <summary>
        ///     Pass a <c>string</c> as parameter, which should be
        ///     <c>[user name]:[password]</c> to use for the connection to the
        ///     HTTP proxy. Use <c>ProxyAuth</c> to decide authentication method.
        /// </summary>
        ProxyUserPwd = 10006,

        /// <summary>
        ///     A <c>true</c> parameter tells the library to use HTTP PUT to transfer
        ///     data. The data should be set with <c>ReadData</c> and
        ///     <c>InfileSize</c>.
        ///     <para>
        ///         This option is deprecated and starting with version 7.12.1 you should
        ///         instead use <c>Upload</c>.
        ///     </para>
        /// </summary>
        Put = 54,

        /// <summary>
        ///     Pass a reference to an <see cref="CurlSlist" /> containing FTP commands to
        ///     pass to the server prior to your ftp request. This will be done before
        ///     any other FTP commands are issued (even before the CWD command).
        ///     Disable this operation again by setting a null to this option.
        /// </summary>
        Quote = 10028,

        /// <summary>
        ///     Pass a <c>string</c> containing the file name. The file will be used
        ///     to read from to seed the random engine for Ssl. The more random the
        ///     specified file is, the more secure the Ssl connection will become.
        /// </summary>
        RandomFile = 10076,

        /// <summary>
        ///     Pass a <c>string</c> as parameter, which should contain the
        ///     specified range you want. It should be in the format <c>X-Y</c>, where X
        ///     or Y may be left out. HTTP transfers also support several intervals,
        ///     separated with commas as in <c>X-Y,N-M</c>. Using this kind of multiple
        ///     intervals will cause the HTTP server to send the response document
        ///     in pieces (using standard MIME separation techniques). Pass a
        ///     <c>null</c> to this option to disable the use of ranges.
        /// </summary>
        Range = 10007,

        /// <summary>
        ///     Object reference to pass to the <see cref="CurlEasy.CurlReadCallback" />
        ///     delegate. Note that if you specify the <c>CurlReadCallback</c>,
        ///     this is the reference you'll get as input.
        /// </summary>
        ReadData = 10009,

        /// <summary>
        ///     Pass a reference to an <see cref="CurlEasy.CurlReadCallback" /> delegate.
        ///     This delegate gets called by libcurl as soon as it needs to read data
        ///     in order to send it to the peer. The data area referenced by the
        ///     <c>buf</c> may be filled with at most <c>size</c> multiplied with
        ///     <c>nmemb</c> number of bytes. Your function must return the actual
        ///     number of bytes that you stored in that byte array. Returning 0 will
        ///     signal end-of-file to the library and cause it to stop the current transfer.
        ///     <para>
        ///         If you stop the current transfer by returning 0 "pre-maturely"
        ///         (i.e before the server expected it, like when you've told you will
        ///         upload N bytes and you upload less than N bytes), you may experience that
        ///         the server "hangs" waiting for the rest of the data that won't come.
        ///     </para>
        /// </summary>
        ReadFunction = 20012,

        /// <summary>
        ///     Pass a <c>string</c> as parameter. It will be used to set the Referer:
        ///     header in the http request sent to the remote server. This can be used
        ///     to fool servers or scripts. You can also set any custom header with
        ///     <c>HttpHeader</c>.
        /// </summary>
        Referer = 10016,

        /// <summary>
        ///     Pass an <c>int</c> as parameter. It contains the offset in number of
        ///     bytes that you want the transfer to start from. Set this option to 0
        ///     to make the transfer start from the beginning (effectively disabling resume).
        /// </summary>
        ResumeFrom = 21,

        /// <summary>
        ///     Pass a <c>long</c> as parameter. It contains the offset in number of
        ///     bytes that you want the transfer to start from. (Added in 7.11.0)
        /// </summary>
        ResumeFromLarge = 30116,

        /// <summary>
        ///     Pass an initialized <see cref="CurlShare" /> reference as a parameter.
        ///     Setting this option will make this <see cref="CurlEasy" /> object use the
        ///     data from the CurlShare object instead of keeping the data to itself. This
        ///     enables several CurlEasy objects to share data. If the CurlEasy objects are used
        ///     simultaneously, you MUST use the CurlShare object's locking methods.
        ///     See <see cref="CurlShare.SetOpt" /> for details.
        /// </summary>
        Share = 10100,

        /// <summary>
        ///     Not supported.
        /// </summary>
        SourceHost = 10122,

        /// <summary>
        ///     Not supported.
        /// </summary>
        SourcePath = 10124,

        /// <summary>
        ///     Not supported.
        /// </summary>
        SourcePort = 125,

        /// <summary>
        ///     When doing a third-party transfer, set the source post-quote list,
        ///     as an <see cref="CurlSlist" />.
        /// </summary>
        SourcePostquote = 10128,

        /// <summary>
        ///     When doing a third-party transfer, set the source pre-quote list,
        ///     as an <see cref="CurlSlist" />.
        /// </summary>
        SourcePrequote = 10127,

        /// <summary>
        ///     When doing a third-party transfer, set a quote list,
        ///     as an <see cref="CurlSlist" />.
        /// </summary>
        SourceQuote = 10133,

        /// <summary>
        ///     Set the source URL for a third-party transfer.
        /// </summary>
        SourceUrl = 10132,

        /// <summary>
        ///     When doing 3rd party transfer, set the source user and password, as
        ///     a <c>string</c> with format <c>user:password</c>.
        /// </summary>
        SourceUserpwd = 10123,

        /// <summary>
        ///     Pass a <c>string</c> as parameter. The string should be the file name
        ///     of your certificate. The default format is "PEM" and can be changed
        ///     with <c>SslCertType</c>.
        /// </summary>
        SslCert = 10025,

        /// <summary>
        ///     Pass a <c>string</c> as parameter. It will be used as the password
        ///     required to use the <c>SslCert</c> certificate.
        ///     <para>
        ///         This option is replaced by <c>SslKeyPasswd</c> and should only
        ///         be used for backward compatibility. You never needed a pass phrase to
        ///         load a certificate but you need one to load your private key.
        ///     </para>
        /// </summary>
        SslCertPasswd = 10026,

        /// <summary>
        ///     Pass a <c>string</c> as parameter. The string should be the format of
        ///     your certificate. Supported formats are "PEM" and "DER". (Added in 7.9.3)
        /// </summary>
        SslCertType = 10086,

        /// <summary>
        ///     Pass a <c>string</c> as parameter. It will be used as the identifier
        ///     for the crypto engine you want to use for your private key.
        ///     <note>
        ///         If the crypto device cannot be loaded,
        ///         <see cref="CurlCode.SslEngineNotFound" /> is returned.
        ///     </note>
        /// </summary>
        SslEngine = 10089,

        /// <summary>
        ///     Sets the actual crypto engine as the default for (asymmetric)
        ///     crypto operations.
        ///     <note>
        ///         If the crypto device cannot be set,
        ///         <see cref="CurlCode.SslEngineSetFailed" /> is returned.
        ///     </note>
        /// </summary>
        SslEngineDefault = 90,

        /// <summary>
        ///     Pass a <c>string</c> as parameter. The string should be the file name
        ///     of your private key. The default format is "PEM" and can be changed
        ///     with <c>SslKeyType</c>.
        /// </summary>
        SslKey = 10087,

        /// <summary>
        ///     Pass a <c>string</c> as parameter. It will be used as the password
        ///     required to use the <c>SslKey</c> private key.
        /// </summary>
        SslKeyPasswd = 10026,

        /// <summary>
        ///     Pass a <c>string</c> as parameter. The string should be the format of
        ///     your private key. Supported formats are "PEM", "DER" and "ENG".
        ///     <note>
        ///         The format "ENG" enables you to load the private key from a crypto
        ///         engine. In this case <c>SslKey</c> is used as an identifier
        ///         passed to the engine. You have to set the crypto engine with
        ///         <c>SslEngine</c>. "DER" format key file currently does not
        ///         work because of a bug in OpenSSL.
        ///     </note>
        /// </summary>
        SslKeyType = 10088,

        /// <summary>
        ///     Pass a member of the <see cref="CurlSslVersion" /> enumeration as the
        ///     parameter to set the Ssl version to use. By default
        ///     the Ssl library will try to solve this by itself although some servers
        ///     servers make this difficult why you at times may have to use this
        ///     option.
        /// </summary>
        SslVersion = 32,

        /// <summary>
        ///     Pass a <c>string</c> holding the list of ciphers to use for the Ssl
        ///     connection. The list must be syntactically correct, it consists of
        ///     one or more cipher strings separated by colons. Commas or spaces are
        ///     also acceptable separators but colons are normally used, !, - and +
        ///     can be used as operators. Valid examples of cipher lists include
        ///     'RC4-SHA', ´SHA1+DES´, 'Tlsv1' and 'DEFAULT'. The default list is
        ///     normally set when you compile OpenSSL.
        ///     <para>
        ///         You'll find more details about cipher lists on this URL:
        ///         http://www.openssl.org/docs/apps/ciphers.html
        ///     </para>
        /// </summary>
        SslCipherList = 10083,

        /// <summary>
        ///     Object reference to pass to the ssl context delegate set by the option
        ///     <c>SslCtxFunction</c>, this is the pointer you'll get as the
        ///     second parameter, otherwise <c>null</c>. (Added in 7.11.0)
        /// </summary>
        SslCtxData = 10109,

        /// <summary>
        ///     Reference to an <see cref="CurlEasy.CurlSslContextCallback" /> delegate.
        ///     This delegate gets called by libcurl just before the initialization of
        ///     an Ssl connection after having processed all other Ssl related options
        ///     to give a last chance to an application to modify the behaviour of
        ///     openssl's ssl initialization. The <see cref="CurlSslContext" /> parameter
        ///     wraps a pointer to an openssl SSL_CTX. If an error is returned no attempt
        ///     to establish a connection is made and the perform operation will return
        ///     the error code from this callback function. Set the parm argument with
        ///     the <c>SslCtxData</c> option. This option was introduced
        ///     in 7.11.0.
        ///     <note>
        ///         To use this properly, a non-trivial amount of knowledge of the openssl
        ///         libraries is necessary. Using this function allows for example to use
        ///         openssl callbacks to add additional validation code for certificates,
        ///         and even to change the actual URI of an HTTPS request.
        ///     </note>
        /// </summary>
        SslCtxFunction = 20108,

        /// <summary>
        ///     Pass an <c>int</c>. Set if we should verify the common name from the
        ///     peer certificate in the Ssl handshake, set 1 to check existence, 2 to
        ///     ensure that it matches the provided hostname. This is by default set
        ///     to 2. (default changed in 7.10)
        /// </summary>
        SslVerifyhost = 81,

        /// <summary>
        ///     Pass a <c>bool</c> that is set to <c>false</c> to stop curl from
        ///     verifying the peer's certificate (7.10 starting setting this option
        ///     to non-zero by default). Alternate certificates to verify against
        ///     can be specified with the <c>CaInfo</c> option or a
        ///     certificate directory can be specified with the <c>CaPath</c>
        ///     option. As of 7.10, curl installs a default bundle.
        ///     <c>SslVerifyhost</c> may also need to be set to 1
        ///     or 0 if <c>SslVerifyPeer</c> is disabled (it defaults to 2).
        /// </summary>
        SslVerifyPeer = 64,

        /// <summary>
        ///     Not supported.
        /// </summary>
        Stderr = 10037,

        /// <summary>
        ///     Pass a <c>bool</c> specifying whether the TCP_NODELAY option should be
        ///     set or cleared (<c>true</c> = set, <c>false</c> = clear). The option is
        ///     cleared by default. This will have no effect after the connection has
        ///     been established.
        ///     <para>
        ///         Setting this option will disable TCP's Nagle algorithm. The purpose of
        ///         this algorithm is to try to minimize the number of small packets on the
        ///         network (where "small packets" means TCP segments less than the Maximum
        ///         Segment Size (MSS) for the network).
        ///     </para>
        ///     <para>
        ///         Maximizing the amount of data sent per TCP segment is good because it
        ///         amortizes the overhead of the send. However, in some cases (most notably
        ///         telnet or rlogin) small segments may need to be sent without delay. This
        ///         is less efficient than sending larger amounts of data at a time, and can
        ///         contribute to congestion on the network if overdone.
        ///     </para>
        /// </summary>
        TcpNoDelay = 121,

        /// <summary>
        ///     Pass a <c>bool</c> specifying whether to ignore Content-Length
        /// </summary>
        IgnoreContentLength = 136,

        /// <summary>
        ///     Set to true to skip the IP address received in a 227 PASV FTP server response. Typically used for FTP-SSL purposes
        ///     but is not restricted to that. libcurl will then instead use the same IP address it used for the control
        ///     connection.
        /// </summary>
        FtpSkipPasvIp = 137,

        /// <summary>
        ///     Provide an <see cref="CurlSlist" /> with variables to pass to the telnet
        ///     negotiations. The variables should be in the format "option=value".
        ///     libcurl supports the options 'TTYPE', 'XDISPLOC' and 'NEW_ENV'. See
        ///     the TELNET standard for details.
        /// </summary>
        TelnetOptions = 10070,

        /// <summary>
        ///     Pass a member of the <see cref="CurlTimeCond" /> enumeration as
        ///     parameter. This defines how the <c>TimeValue</c> time
        ///     value is treated. This feature applies to HTTP and FTP.
        ///     <note>
        ///         The last modification time of a file is not always known and in such
        ///         instances this feature will have no effect even if the given time
        ///         condition would have not been met.
        ///     </note>
        /// </summary>
        TimeCondition = 33,

        /// <summary>
        ///     Pass a <c>int</c> as parameter containing the maximum time in seconds
        ///     that you allow the libcurl transfer operation to take. Normally, name
        ///     lookups can take a considerable time and limiting operations to less
        ///     than a few minutes risk aborting perfectly normal operations. This
        ///     option will cause curl to use the SIGALRM to enable time-outing
        ///     system calls.
        ///     <note>
        ///         this is not recommended to use in unix multi-threaded programs,
        ///         as it uses signals unless <c>NoSignal</c> (see above) is set.
        ///     </note>
        /// </summary>
        Timeout = 13,

        /// <summary>
        ///     Pass a <see cref="System.DateTime" /> as parameter. This time will be
        ///     used in a condition as specified with <c>TimeCondition</c>.
        /// </summary>
        TimeValue = 34,

        /// <summary>
        ///     A <c>true</c> parameter tells the library to use ASCII mode for ftp
        ///     transfers, instead of the default binary transfer. For LDAP transfers
        ///     it gets the data in plain text instead of HTML and for win32 systems
        ///     it does not set the stdout to binary mode. This option can be usable
        ///     when transferring text data between systems with different views on
        ///     certain characters, such as newlines or similar.
        /// </summary>
        TransferText = 53,

        /// <summary>
        ///     A <c>true</c> parameter tells the library it can continue to send
        ///     authentication (user+password) when following locations, even when
        ///     hostname changed. Note that this is meaningful only when setting
        ///     <c>FollowLocation</c>.
        /// </summary>
        UnrestrictedAuth = 105,

        /// <summary>
        ///     A <c>true</c> parameter tells the library to prepare for an
        ///     upload. The <c>ReadData</c> and <c>InfileSize</c>
        ///     or <c>InFileSizeLarge</c> are also interesting for uploads.
        ///     If the protocol is HTTP, uploading means using the PUT request
        ///     unless you tell libcurl otherwise.
        ///     <para>
        ///         Using PUT with HTTP 1.1 implies the use of a "Expect: 100-continue"
        ///         header. You can disable this header with <c>HttpHeader</c> as usual.
        ///     </para>
        ///     <para>
        ///         If you use PUT to a HTTP 1.1 server, you can upload data without
        ///         knowing the size before starting the transfer if you use chunked
        ///         encoding. You enable this by adding a header like
        ///         "Transfer-Encoding: chunked" with <c>HttpHeader</c>. With
        ///         HTTP 1.0 or without chunked transfer, you must specify the size.
        ///     </para>
        /// </summary>
        Upload = 46,

        /// <summary>
        ///     The actual URL to deal with. The parameter should be a <c>string</c>.
        ///     If the given URL lacks the protocol part ("http://" or "ftp://" etc), it
        ///     will attempt to guess which protocol to use based on the given host name.
        ///     <para>
        ///         If the given protocol of the set URL is not supported, libcurl will return
        ///         an error <c>CurlCode.</c>(<see cref="CurlCode.UnsupportedProtocol" />)
        ///         when you call CurlEasy's <see cref="CurlEasy.Perform" /> or
        ///         CurlMulti's <see cref="CurlMulti.Perform" />.
        ///     </para>
        ///     <para>
        ///         Use <see cref="Curl.GetVersionInfo" /> for detailed info
        ///         on which protocols that are supported.
        ///     </para>
        /// </summary>
        Url = 10002,

        /// <summary>
        ///     Pass a <c>string</c> as parameter. It will be used to set the
        ///     User-Agent: header in the http request sent to the remote server.
        ///     This can be used to fool servers or scripts. You can also set any
        ///     custom header with <c>HttpHeader</c>.
        /// </summary>
        UserAgent = 10018,

        /// <summary>
        ///     Pass a <c>string</c> as parameter, which should be
        ///     <c>[user name]:[password]</c> to use for the connection. Use
        ///     <c>HttpAuth</c> to decide authentication method.
        ///     <para>
        ///         When using HTTP and <c>FollowLocation</c>, libcurl might
        ///         perform several requests to possibly different hosts. libcurl will
        ///         only send this user and password information to hosts using the
        ///         initial host name (unless <c>UnrestrictedAuth</c> is set),
        ///         so if libcurl follows locations to other hosts it will not send the
        ///         user and password to those. This is enforced to prevent accidental
        ///         information leakage.
        ///     </para>
        /// </summary>
        UserPwd = 10005,

        /// <summary>
        ///     Set the parameter to <c>true</c> to get the library to display a lot
        ///     of verbose information about its operations. Very useful for libcurl
        ///     and/or protocol debugging and understanding. The verbose information
        ///     will be sent to the <see cref="CurlDebugCallback" /> delegate, if it's
        ///     implemented. You hardly ever want this set in production use, you will
        ///     almost always want this when you debug/report problems.
        /// </summary>
        Verbose = 41,

        /// <summary>
        ///     Object reference to pass to the <see cref="CurlWriteCallback" />
        ///     delegate. Note that if you specify the <c>CurlWriteCallback</c>,
        ///     this is the object you'll get as input.
        /// </summary>
        WriteData = 10001,

        /// <summary>
        ///     Pass a reference to an <see cref="CurlWriteCallback" /> delegate.
        ///     The delegate gets called by libcurl as soon as there is data received
        ///     that needs to be saved. The size of the data referenced by <c>buf</c>
        ///     is <c>size</c> multiplied with <c>nmemb</c>, it will not be zero
        ///     terminated. Return the number of bytes actually taken care of. If
        ///     that amount differs from the amount passed to your function, it'll
        ///     signal an error to the library and it will abort the transfer and
        ///     return <c>CurlCode.</c><see cref="CurlCode.WriteError" />.
        ///     <note>
        ///         This function may be called with zero bytes data if the
        ///         transfered file is empty.
        ///     </note>
        /// </summary>
        WriteFunction = 20011,

        /// <summary>
        ///     Pass a <c>string</c> of the output using full variable-replacement
        ///     as described elsewhere.
        /// </summary>
        WriteInfo = 10040,


        /// <summary>
        ///     Set to true to enable the "TE:" header in HTTP requests to ask for compressed transfer-encoded responses. Set to 0
        ///     to disable the use of TE: in outgoing requests. The current default is false, but it might change in a future
        ///     libcurl release. libcurl will ask for the compressed methods it knows of, and if that isn't any, it will not ask
        ///     for transfer-encoding at all even if this option is set to true.
        /// </summary>
        TransferEncoding = 207,

        /// <summary>
        ///     Callback function for closing socket (instead of close(2)). The callback should have type curl_closesocket_callback
        /// </summary>
        CloseSocketFunction = 20208,
        CloseSocketData = 10209,


        /// <summary>
        ///     feed cookies into cookie engine
        /// </summary>
        CookieList = 10135,

        /// <summary>
        ///     Select "file method" to use when doing FTP, see the curl_ftpmethod above.
        /// </summary>
        FtpFileMethod = 138,

        /// <summary>
        ///     Local port number to bind the socket to
        /// </summary>
        LocalPort = 139,

        /// <summary>
        ///     Number of ports to try, including the first one set with LocalPort. Thus, setting it to 1 will make no additional
        ///     attempts but the first.
        /// </summary>
        LocalPortRange = 140,

        /// <summary>
        ///     No transfer, set up connection and let application use the socket by extracting it with CURLINFO_LASTSOCKET
        /// </summary>
        ConnectOnly = 141,

        /// <summary>
        ///     if the connection proceeds too quickly then need to slow it down limit-rate: maximum number of bytes per second to
        ///     send or receive
        /// </summary>
        MaxSendSpeedLarge = 30145,
        MaxRecvSpeedLarge = 30146,

        /// <summary>
        ///     Pointer to command string to send if USER/PASS fails.
        /// </summary>
        FtpAlternativeToUser = 10147,

        /// <summary>
        ///     Callback function for setting socket options
        /// </summary>
        SockoptFunction = 20148,
        SockoptData = 149,


        /// <summary>
        ///     Set to false to disable session ID re-use for this transfer, default is enabled (== true)
        /// </summary>
        SslSessionidCache = 150,

        /// <summary>
        ///     Allowed SSH authentication methods
        /// </summary>
        SshAuthTypes = 151,

        /// <summary>
        ///     Used by scp/sftp to do public/private key authentication
        /// </summary>
        SshPublicKeyfile = 10152,
        SshPrivateKeyfile = 10152,

        /// <summary>
        ///     Same as Timeout and ConnectTimeout, but with ms resolution
        /// </summary>
        TimeoutMs = 155,
        ConnectTimeoutMs = 156,

        /// <summary>
        ///     Set to false to disable the libcurl's decoding and thus pass the raw body data to the application even when it is
        ///     encoded/compressed
        /// </summary>
        HttpTransferDecoding = 157,
        HttpContentDecoding = 158,

        /// <summary>
        ///     Permission used when creating new files and directories on the remote server for protocols that support it,
        ///     SFTP/SCP/FILE
        /// </summary>
        NewFilePerms = 159,
        NewDirectoryPerms = 160,

        /// <summary>
        ///     Set the behaviour of POST when redirecting. Values must be set to one of CURL_REDIR* defines below. This used to be
        ///     called CURLOPT_POST301
        /// </summary>
        PostRedir = 161,

        /// <summary>
        ///     Used by scp/sftp to verify the host's public key
        /// </summary>
        SshHostPublicKeyMd5 = 10162,


        /// <summary>
        ///     Callback function for opening socket (instead of socket(2)). Optionally, callback is able change the address or
        ///     refuse to connect returning CURL_SOCKET_BAD.  The callback should have type curl_opensocket_callback
        /// </summary>
        OpenSocketFunction = 20163,
        OpenSocketData = 10164,

        /// <summary>
        ///     POST volatile input fields.
        /// </summary>
        CopyPostFields = 10165,

        /// <summary>
        ///     set transfer mode (;type=<a| i>) when doing FTP via an HTTP proxy
        /// </summary>
        ProxyTransferMode = 166,

        /// <summary>
        ///     Callback function for seeking in the input stream
        /// </summary>
        SeekFunction = 20167,
        SeekData = 10168,

        /// <summary>
        ///     CRL file
        /// </summary>
        CrlFile = 10169,

        /// <summary>
        ///     Issuer certificate
        /// </summary>
        IssuerCert = 10170,

        /// <summary>
        ///     (IPv6) Address scope
        /// </summary>
        AddressScope = 171,

        /// <summary>
        ///     Collect certificate chain info and allow it to get retrievable with CURLINFO_CERTINFO after the transfer is
        ///     complete.
        /// </summary>
        CertInfo = 172,

        /// <summary>
        ///     "name" and "pwd" to use when fetching.
        /// </summary>
        Username = 10173,
        Password = 10174,

        /// <summary>
        ///     "name" and "pwd" to use with Proxy when fetching.
        /// </summary>
        ProxyUsername = 10175,
        ProxyPassword = 10176,

        /// <summary>
        ///     Comma separated list of hostnames defining no-proxy zones. These should match both hostnames directly, and
        ///     hostnames within a domain. For example, local.com will match local.com and www.local.com, but NOT notlocal.com or
        ///     www.notlocal.com. For compatibility with other implementations of this, .local.com will be considered to be the
        ///     same as local.com. A single * is the only valid wildcard, and effectively disables the use of proxy.
        /// </summary>
        NoProxy = 10177,


        /// <summary>
        ///     block size for TFTP transfers
        /// </summary>
        TftpBlksize = 178,

        /// <summary>
        ///     Socks service
        /// </summary>
        Socks5GssApiService = 10179,
        Socks5GssApiNec = 180,

        /// <summary>
        ///     set the bitmask for the protocols that are allowed to be used for the transfer, which thus helps the app which
        ///     takes URLs from users or other external inputs and want to restrict what protocol(s) to deal with. Defaults to
        ///     CURLPROTO_ALL.
        /// </summary>
        Protocols = 181,

        /// <summary>
        ///     set the bitmask for the protocols that libcurl is allowed to follow to, as a subset of the CURLOPT_PROTOCOLS ones.
        ///     That means the protocol needs to be set in both bitmasks to be allowed to get redirected to. Defaults to all
        ///     protocols except FILE and SCP.
        /// </summary>
        RedirProtocols = 182,
        
        /// <summary>
        ///     Set the SSH knownhost file name to use
        /// </summary>
        SshKnownHosts = 10183,

        /// <summary>
        ///     set the SSH host key callback, must point to a curl_sshkeycallback function
        /// </summary>
        SshKeyFunction = 20184,

        /// <summary>
        ///     Set the SSH host key callback custom pointer
        /// </summary>
        SshKeydata = 10185,

        /// <summary>
        ///     Set the SMTP mail originator
        /// </summary>
        MailFrom = 10186,

        /// <summary>
        ///     Set the SMTP mail receiver(s)
        /// </summary>
        MailRcpt = 10187,

        /// <summary>
        ///     FTP: send PRET before PASV
        /// </summary>
        FtpUsePret = 188,

        /// <summary>
        ///     Set the interface string to use as outgoing network interface for DNS requests. Only supported by the c-ares DNS
        ///     backend */
        /// </summary>
        DnsInterface = 10221,

        /// <summary>
        ///     Set the local IPv4 address to use for outgoing DNS requests. Only supported by the c-ares DNS backend
        /// </summary>
        DnsLocalIp4 = 10222,

        /// <summary>
        ///     Set the local IPv6 address to use for outgoing DNS requests.  Only supported by the c-ares DNS backend
        /// </summary>
        DnsLocalIp6 = 10223,

        /// <summary>
        ///     Set authentication options directly
        /// </summary>
        LoginOptions = 10224,

        /// <summary>
        ///     Enable/disable TLS NPN extension (http2 over ssl might fail without)
        /// </summary>
        SslEnableNpn = 225,

        /// <summary>
        ///     Enable/disable TLS ALPN extension (http2 over ssl might fail without)
        /// </summary>
        SslEnableAlpn = 226,

        /// <summary>
        ///     Time to wait for a response to a HTTP request containing an Expect: 100-continue header before sending the data
        ///     anyway.
        /// </summary>
        Expect100TimeoutMs = 227,

        /// <summary>
        ///     This points to a linked list of headers used for proxy requests only, struct curl_slist kind
        /// </summary>
        ProxyHeader = 228,

        /// <summary>
        ///     Pass in a bitmask of "header options"
        /// </summary>
        HeaderOpt = 229,

        /// <summary>
        ///     Last numeric entry in the enumeration. Don't use this in your
        ///     application code.
        /// </summary>
        LastEntry = 230,
    };
}