namespace CurlSharp.Enums
{
    /// <summary>
    ///     This enumeration contains values used to specify the proxy type when
    ///     using the <see cref="CurlOption.Proxy" /> option when calling
    ///     <see cref="CurlEasy.SetOpt" />
    /// </summary>
    public enum CurlProxyType
    {
        /// <summary>
        ///     Ordinary HTTP proxy.
        /// </summary>
        Http = 0,

        /// <summary>
        ///     Use if the proxy supports SOCKS4 user authentication. If you're
        ///     unfamiliar with this, consult your network administrator.
        /// </summary>
        Socks4 = 4,

        /// <summary>
        ///     Use if the proxy supports SOCKS5 user authentication. If you're
        ///     unfamiliar with this, consult your network administrator.
        /// </summary>
        Socks5 = 5
    };
}