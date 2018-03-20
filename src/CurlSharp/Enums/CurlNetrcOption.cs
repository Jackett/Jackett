namespace CurlSharp.Enums
{
    /// <summary>
    ///     Contains values used to specify the preference of libcurl between
    ///     using user names and passwords from your ~/.netrc file, relative to
    ///     user names and passwords in the URL supplied with
    ///     <see cref="CurlOption.Url" />. This is passed when using
    ///     the <see cref="CurlOption.Netrc" /> option in a call
    ///     to <see cref="CurlEasy.SetOpt" />
    /// </summary>
    public enum CurlNetrcOption
    {
        /// <summary>
        ///     The library will ignore the file and use only the information
        ///     in the URL. This is the default.
        /// </summary>
        Ignored = 0,

        /// <summary>
        ///     The use of your ~/.netrc file is optional, and information in the
        ///     URL is to be preferred. The file will be scanned with the host
        ///     and user name (to find the password only) or with the host only,
        ///     to find the first user name and password after that machine,
        ///     which ever information is not specified in the URL.
        ///     <para>
        ///         Undefined values of the option will have this effect.
        ///     </para>
        /// </summary>
        Optional = 1,

        /// <summary>
        ///     This value tells the library that use of the file is required,
        ///     to ignore the information in the URL, and to search the file
        ///     with the host only.
        /// </summary>
        Required = 2,

        /// <summary>
        ///     Last entry in enumeration; do not use in application code.
        /// </summary>
        Last = 3
    };
}