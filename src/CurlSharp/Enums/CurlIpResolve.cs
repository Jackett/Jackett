namespace CurlSharp.Enums
{
    /// <summary>
    ///     This enumeration contains values used to specify the IP resolution
    ///     method when using the <see cref="CurlOption.IpResolve" />
    ///     option in a call to <see cref="CurlEasy.SetOpt" />
    /// </summary>
    public enum CurlIpResolve
    {
        /// <summary>
        ///     Default, resolves addresses to all IP versions that your system
        ///     allows.
        /// </summary>
        Whatever = 0,

        /// <summary>
        ///     Resolve to ipv4 addresses.
        /// </summary>
        V4 = 1,

        /// <summary>
        ///     Resolve to ipv6 addresses.
        /// </summary>
        V6 = 2
    };
}