namespace CurlSharp.Enums
{
    /// <summary>
    ///     This enumeration contains values used to specify the HTTP authentication
    ///     when using the <see cref="CurlOption.HttpAuth" /> option when
    ///     calling <see cref="CurlEasy.SetOpt" />
    /// </summary>
    public enum CurlHttpAuth
    {
        /// <summary>
        ///     No authentication.
        /// </summary>
        None = 0,

        /// <summary>
        ///     HTTP Basic authentication. This is the default choice, and the
        ///     only method that is in wide-spread use and supported virtually
        ///     everywhere. This is sending the user name and password over the
        ///     network in plain text, easily captured by others.
        /// </summary>
        Basic = 1,

        /// <summary>
        ///     HTTP Digest authentication. Digest authentication is defined
        ///     in RFC2617 and is a more secure way to do authentication over
        ///     public networks than the regular old-fashioned Basic method.
        /// </summary>
        Digest = 2,

        /// <summary>
        ///     HTTP GSS-Negotiate authentication. The GSS-Negotiate (also known
        ///     as plain "Negotiate") method was designed by Microsoft and is
        ///     used in their web applications. It is primarily meant as a
        ///     support for Kerberos5 authentication but may be also used along
        ///     with another authentication methods. For more information see IETF
        ///     draft draft-brezak-spnego-http-04.txt.
        ///     <note>
        ///         You need to use a version of libcurl.NET built with a suitable
        ///         GSS-API library for this to work. This is not currently standard.
        ///     </note>
        /// </summary>
        GssNegotiate = 4,

        /// <summary>
        ///     HTTP Ntlm authentication. A proprietary protocol invented and
        ///     used by Microsoft. It uses a challenge-response and hash concept
        ///     similar to Digest, to prevent the password from being eavesdropped.
        /// </summary>
        Ntlm = 8,

        /// <summary>
        ///     This is a convenience macro that sets all bits and thus makes
        ///     libcurl pick any it finds suitable. libcurl will automatically
        ///     select the one it finds most secure.
        /// </summary>
        Any = 15, // ~0

        /// <summary>
        ///     This is a convenience macro that sets all bits except Basic
        ///     and thus makes libcurl pick any it finds suitable. libcurl
        ///     will automatically select the one it finds most secure.
        /// </summary>
        AnySafe = 14 // ~Basic
    };
}