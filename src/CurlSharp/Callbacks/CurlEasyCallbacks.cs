using System;
using System.Runtime.InteropServices;

namespace CurlSharp
{
    /// <summary>
    ///     Called when cURL has debug information for the client.
    /// </summary>
    /// <remarks>
    ///     For usage, see the sample <c>Upload.cs</c>.
    ///     Arguments passed to the recipient include:
    ///     <list type="table">
    ///         <listheader>
    ///             <term>Argument</term>
    ///             <description>Description</description>
    ///         </listheader>
    ///         <item>
    ///             <term>infoType</term>
    ///             <description>
    ///                 Type of debug information, see
    ///                 <see cref="CurlInfoType" />.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>message</term>
    ///             <description>Debug information as a string.</description>
    ///         </item>
    ///         <item>
    ///             <term>size</term>
    ///             <description>The size in bytes.</description>
    ///         </item>
    ///         <item>
    ///             <term>extraData</term>
    ///             <description>Client-provided extra data.</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public delegate void CurlDebugCallback(CurlInfoType infoType, String message, int size, Object extraData);

    /// <summary>
    ///     Called when cURL has header data for the client.
    /// </summary>
    /// <remarks>
    ///     For usage, see the sample <c>Headers.cs</c>.
    ///     Arguments passed to the recipient include:
    ///     <list type="table">
    ///         <listheader>
    ///             <term>Argument</term>
    ///             <description>Description</description>
    ///         </listheader>
    ///         <item>
    ///             <term>buf</term>
    ///             <description>Header data from cURL to the client.</description>
    ///         </item>
    ///         <item>
    ///             <term>size</term>
    ///             <description>Size of a character, in bytes.</description>
    ///         </item>
    ///         <item>
    ///             <term>nmemb</term>
    ///             <description>Number of characters.</description>
    ///         </item>
    ///         <item>
    ///             <term>extraData</term>
    ///             <description>Client-provided extra data.</description>
    ///         </item>
    ///     </list>
    ///     Your implementation should return the number of bytes (not
    ///     characters) processed. Usually this is <c>size * nmemb</c>.
    ///     Return -1 to abort the transfer.
    /// </remarks>
    public delegate int CurlHeaderCallback(byte[] buf, int size, int nmemb, Object extraData);

    /// <summary>
    ///     Called when cURL needs for the client to perform an
    ///     IOCTL operation. An example might be when an FTP
    ///     upload requires rewinding of the input file to deal
    ///     with a resend occasioned by an error.
    /// </summary>
    /// <remarks>
    ///     <list type="table">
    ///         <listheader>
    ///             <term>Argument</term>
    ///             <description>Description</description>
    ///         </listheader>
    ///         <item>
    ///             <term>cmd</term>
    ///             <description>
    ///                 A <see cref="CurlIoCommand" />; for now, only
    ///                 <c>RestartRead</c> should be passed.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>extraData</term>
    ///             <description>
    ///                 Client-provided extra data; in the
    ///                 case of an FTP upload, it might be a
    ///                 <c>FileStream</c> object.
    ///             </description>
    ///         </item>
    ///     </list>
    ///     Your implementation should return a <see cref="CurlIoError" />,
    ///     which should be <see cref="CurlIoError.Ok" /> if everything
    ///     is okay.
    /// </remarks>
    public delegate CurlIoError CurlIoctlCallback(CurlIoCommand cmd, Object extraData);

    /// <summary>
    ///     Called when cURL wants to report progress.
    /// </summary>
    /// <remarks>
    ///     For usage, see the sample <c>Upload.cs</c>.
    ///     Arguments passed to the recipient include:
    ///     <list type="table">
    ///         <listheader>
    ///             <term>Argument</term>
    ///             <description>Description</description>
    ///         </listheader>
    ///         <item>
    ///             <term>extraData</term>
    ///             <description>Client-provided extra data.</description>
    ///         </item>
    ///         <item>
    ///             <term>dlTotal</term>
    ///             <description>Number of bytes to download.</description>
    ///         </item>
    ///         <item>
    ///             <term>dlNow</term>
    ///             <description>Number of bytes downloaded so far.</description>
    ///         </item>
    ///         <item>
    ///             <term>ulTotal</term>
    ///             <description>Number of bytes to upload.</description>
    ///         </item>
    ///         <item>
    ///             <term>ulNow</term>
    ///             <description>Number of bytes uploaded so far.</description>
    ///         </item>
    ///     </list>
    ///     Your implementation should return 0 to continue, or a non-zero
    ///     value to abort the transfer.
    /// </remarks>
    public delegate int CurlProgressCallback(Object extraData, double dlTotal, double dlNow,
        double ulTotal, double ulNow);

    /// <summary>
    ///     Called when cURL wants to read data from the client.
    /// </summary>
    /// <remarks>
    ///     For usage, see the sample <c>Upload.cs</c>.
    ///     Arguments passed to the recipient include:
    ///     <list type="table">
    ///         <listheader>
    ///             <term>Argument</term>
    ///             <description>Description</description>
    ///         </listheader>
    ///         <item>
    ///             <term>buf</term>
    ///             <description>
    ///                 Buffer into which your client should write data
    ///                 for cURL.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>size</term>
    ///             <description>Size of a character, usually 1.</description>
    ///         </item>
    ///         <item>
    ///             <term>nmemb</term>
    ///             <description>Number of characters.</description>
    ///         </item>
    ///         <item>
    ///             <term>extraData</term>
    ///             <description>Client-provided extra data.</description>
    ///         </item>
    ///     </list>
    ///     Your implementation should return the number of bytes (not
    ///     characters) written to <c>buf</c>. Return 0 to abort the transfer.
    /// </remarks>
    public delegate int CurlReadCallback([Out] byte[] buf, int size, int nmemb, Object extraData);

    /// <summary>
    ///     Called when cURL wants to report an Ssl event.
    /// </summary>
    /// <remarks>
    ///     For usage, see the sample <c>SSLGet.cs</c>.
    ///     Arguments passed to the recipient include:
    ///     <list type="table">
    ///         <listheader>
    ///             <term>Argument</term>
    ///             <description>Description</description>
    ///         </listheader>
    ///         <item>
    ///             <term>ctx</term>
    ///             <description>
    ///                 An <see cref="CurlSslContext" /> object that wraps an
    ///                 OpenSSL <c>SSL_CTX</c> pointer.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <term>extraData</term>
    ///             <description>Client-provided extra data.</description>
    ///         </item>
    ///     </list>
    ///     Your implementation should return a <see cref="CurlCode" />,
    ///     which should be <see cref="CurlCode.Ok" /> if everything
    ///     is okay.
    /// </remarks>
    public delegate CurlCode CurlSslContextCallback(CurlSslContext ctx, Object extraData);

    /// <summary>
    ///     Called when cURL has data for the client.
    /// </summary>
    /// <remarks>
    ///     For usage, see the example <c>EasyGet.cs</c>.
    ///     Arguments passed to the delegate implementation include:
    ///     <list type="table">
    ///         <listheader>
    ///             <term>Argument</term>
    ///             <description>Description</description>
    ///         </listheader>
    ///         <item>
    ///             <term>buf</term>
    ///             <description>Data cURL is providing to the client.</description>
    ///         </item>
    ///         <item>
    ///             <term>size</term>
    ///             <description>Size of a character, usually 1.</description>
    ///         </item>
    ///         <item>
    ///             <term>nmemb</term>
    ///             <description>Number of characters.</description>
    ///         </item>
    ///         <item>
    ///             <term>extraData</term>
    ///             <description>Client-provided extra data.</description>
    ///         </item>
    ///     </list>
    ///     Your implementation should return the number of bytes (not
    ///     characters) processed. Return 0 to abort the transfer.
    /// </remarks>
    public delegate int CurlWriteCallback(byte[] buf, int size, int nmemb, Object extraData);
}