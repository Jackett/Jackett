namespace CurlSharp.Enums
{
    /// <summary>
    ///     This enumeration is used to extract information associated with an
    ///     <see cref="CurlEasy" /> transfer. Specifically, a member of this
    ///     enumeration is passed as the first argument to
    ///     CurlEasy.GetInfo specifying the item to retrieve in the
    ///     second argument, which is a reference to an <c>int</c>, a
    ///     <c>double</c>, a <c>string</c>, a <c>DateTime</c> or an <c>object</c>.
    /// </summary>
    public enum CurlInfo
    {
        /// <summary>
        ///     The second argument receives the elapsed time, as a <c>double</c>,
        ///     in seconds, from the start until the connect to the remote host
        ///     (or proxy) was completed.
        /// </summary>
        ConnectTime = 0x300005,

        /// <summary>
        ///     The second argument receives, as a <c>double</c>, the content-length
        ///     of the download. This is the value read from the Content-Length: field.
        /// </summary>
        ContentLengthDownload = 0x30000F,

        /// <summary>
        ///     The second argument receives, as a <c>double</c>, the specified size
        ///     of the upload.
        /// </summary>
        ContentLengthUpload = 0x300010,

        /// <summary>
        ///     The second argument receives, as a <c>string</c>, the content-type of
        ///     the downloaded object. This is the value read from the Content-Type:
        ///     field. If you get <c>null</c>, it means that the server didn't
        ///     send a valid Content-Type header or that the protocol used
        ///     doesn't support this.
        /// </summary>
        ContentType = 0x100012,

        /// <summary>
        ///     The second argument receives, as a <c>string</c>, the last
        ///     used effective URL.
        /// </summary>
        EffectiveUrl = 0x100001,

        /// <summary>
        ///     The second argument receives, as a <c>long</c>, the remote time
        ///     of the retrieved document. You should construct a <c>DateTime</c>
        ///     from this value, as shown in the <c>InfoDemo</c> sample. If you
        ///     get a date in the distant
        ///     past, it can be because of many reasons (unknown, the server
        ///     hides it or the server doesn't support the command that tells
        ///     document time etc) and the time of the document is unknown. Note
        ///     that you must tell the server to collect this information before
        ///     the transfer is made, by using the
        ///     <see cref="CurlOption.Filetime" /> option to
        ///     <see cref="CurlEasy.SetOpt" />. (Added in 7.5)
        /// </summary>
        Filetime = 0x20000E,

        /// <summary>
        ///     The second argument receives an <c>int</c> specifying the total size
        ///     of all the headers received.
        /// </summary>
        HeaderSize = 0x20000B,

        /// <summary>
        ///     The second argument receives, as an <c>int</c>, a bitmask indicating
        ///     the authentication method(s) available. The meaning of the bits is
        ///     explained in the documentation of
        ///     <see cref="CurlOption.HttpAuth" />. (Added in 7.10.8)
        /// </summary>
        HttpAuthAvail = 0x200017,

        /// <summary>
        ///     The second argument receives an <c>int</c> indicating the numeric
        ///     connect code for the HTTP request.
        /// </summary>
        HttpConnectCode = 0x200016,

        /// <summary>
        ///     End-of-enumeration marker; do not use in client applications.
        /// </summary>
        LastOne = 0x1C,

        /// <summary>
        ///     The second argument receives, as a <c>double</c>, the time, in
        ///     seconds it took from the start until the name resolving was
        ///     completed.
        /// </summary>
        NameLookupTime = 0x300004,

        /// <summary>
        ///     Never used.
        /// </summary>
        None = 0x0,

        /// <summary>
        ///     The second argument receives an <c>int</c> indicating the
        ///     number of current connections. (Added in 7.13.0)
        /// </summary>
        NumConnects = 0x20001A,

        /// <summary>
        ///     The second argument receives an <c>int</c> indicating the operating
        ///     system error number: <c>_errro</c> or <c>GetLastError()</c>,
        ///     depending on the platform. (Added in 7.12.2)
        /// </summary>
        OsErrno = 0x200019,

        /// <summary>
        ///     The second argument receives, as a <c>double</c>, the time, in
        ///     seconds, it took from the start until the file transfer is just about
        ///     to begin. This includes all pre-transfer commands and negotiations
        ///     that are specific to the particular protocol(s) involved.
        /// </summary>
        PreTransferTime = 0x300006,

        /// <summary>
        ///     The second argument receives a reference to the private data
        ///     associated with the <see cref="CurlEasy" /> object (set with the
        ///     <see cref="CurlOption.Private" /> option to
        ///     <see cref="CurlEasy.SetOpt" />. (Added in 7.10.3)
        /// </summary>
        Private = 0x100015,

        /// <summary>
        ///     The second argument receives, as an <c>int</c>, a bitmask
        ///     indicating the authentication method(s) available for your
        ///     proxy authentication. This will be a bitmask of
        ///     <see cref="CurlHttpAuth" /> enumeration constants.
        ///     (Added in 7.10.8)
        /// </summary>
        ProxyAuthAvail = 0x200018,

        /// <summary>
        ///     The second argument receives an <c>int</c> indicating the total
        ///     number of redirections that were actually followed. (Added in 7.9.7)
        /// </summary>
        RedirectCount = 0x200014,

        /// <summary>
        ///     The second argument receives, as a <c>double</c>, the total time, in
        ///     seconds, for all redirection steps include name lookup, connect,
        ///     pretransfer and transfer before final transaction was started.
        ///     <c>RedirectTime</c> contains the complete execution
        ///     time for multiple redirections. (Added in 7.9.7)
        /// </summary>
        RedirectTime = 0x300013,

        /// <summary>
        ///     The second argument receives an <c>int</c> containing the total size
        ///     of the issued requests. This is so far only for HTTP requests. Note
        ///     that this may be more than one request if
        ///     <see cref="CurlOption.FollowLocation" /> is <c>true</c>.
        /// </summary>
        RequestSize = 0x20000C,

        /// <summary>
        ///     The second argument receives an <c>int</c> with the last received HTTP
        ///     or FTP code. This option was known as <c>CURLINFO_HTTP_CODE</c> in
        ///     libcurl 7.10.7 and earlier.
        /// </summary>
        ResponseCode = 0x200002,

        /// <summary>
        ///     The second argument receives a <c>double</c> with the total amount of
        ///     bytes that were downloaded. The amount is only for the latest transfer
        ///     and will be reset again for each new transfer.
        /// </summary>
        SizeDownload = 0x300008,

        /// <summary>
        ///     The second argument receives a <c>double</c> with the total amount
        ///     of bytes that were uploaded.
        /// </summary>
        SizeUpload = 0x300007,

        /// <summary>
        ///     The second argument receives a <c>double</c> with the average
        ///     download speed that cURL measured for the complete download.
        /// </summary>
        SpeedDownload = 0x300009,

        /// <summary>
        ///     The second argument receives a <c>double</c> with the average
        ///     upload speed that libcurl measured for the complete upload.
        /// </summary>
        SpeedUpload = 0x30000A,

        /// <summary>
        ///     The second argument receives an <see cref="CurlSlist" /> containing
        ///     the names of the available Ssl engines.
        /// </summary>
        SslEngines = 0x40001B,

        /// <summary>
        ///     The second argument receives an <c>int</c> with the result of
        ///     the certificate verification that was requested (using the
        ///     <see cref="CurlOption.SslVerifyPeer" /> option in
        ///     <see cref="CurlEasy.SetOpt" />.
        /// </summary>
        SslVerifyResult = 0x20000D,

        /// <summary>
        ///     The second argument receives a <c>double</c> specifying the time,
        ///     in seconds, from the start until the first byte is just about to be
        ///     transferred. This includes <c>PreTransferTime</c> and
        ///     also the time the server needs to calculate the result.
        /// </summary>
        StartTransferTime = 0x300011,

        /// <summary>
        ///     The second argument receives a <c>double</c> indicating the total transaction
        ///     time in seconds for the previous transfer. This time does not include
        ///     the connect time, so if you want the complete operation time,
        ///     you should add the <c>ConnectTime</c>.
        /// </summary>
        TotalTime = 0x300003,
    };
}