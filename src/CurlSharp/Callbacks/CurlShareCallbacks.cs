using System;
using CurlSharp.Enums;

namespace CurlSharp.Callbacks
{
    /// <summary>
    ///     Called when <c>cURL</c> wants to lock a shared resource.
    /// </summary>
    /// <remarks>
    ///     For a usage example, refer to the <c>ShareDemo.cs</c> sample.
    ///     Arguments passed to your delegate implementation include:
    ///     <list type="table">
    ///         <listheader>
    ///             <term>Argument</term>
    ///             <term>Description</term>
    ///         </listheader>
    ///         <item>
    ///             <term>data</term>
    ///             <term>
    ///                 Type of data to lock; one of the values in the
    ///                 <see cref="CurlLockData" /> enumeration.
    ///             </term>
    ///         </item>
    ///         <item>
    ///             <term>access</term>
    ///             <term>
    ///                 Lock access requested; one of the values in the
    ///                 <see cref="CurlLockAccess" /> enumeration.
    ///             </term>
    ///         </item>
    ///         <item>
    ///             <term>userData</term>
    ///             <term>
    ///                 Client-provided data that is not touched internally by
    ///                 <c>cURL</c>. This is set via
    ///                 <see cref="CurlShareOption.UserData" /> when calling the
    ///                 <see cref="CurlShare.SetOpt" /> member of the <see cref="CurlShare" />
    ///                 class.
    ///             </term>
    ///         </item>
    ///     </list>
    /// </remarks>
    public delegate void CurlShareLockCallback(CurlLockData data, CurlLockAccess access, Object userData);

    /// <summary>
    ///     Called when <c>cURL</c> wants to unlock a shared resource.
    /// </summary>
    /// <remarks>
    ///     For a usage example, refer to the <c>ShareDemo.cs</c> sample.
    ///     Arguments passed to your delegate implementation include:
    ///     <list type="table">
    ///         <listheader>
    ///             <term>Argument</term>
    ///             <term>Description</term>
    ///         </listheader>
    ///         <item>
    ///             <term>data</term>
    ///             <term>
    ///                 Type of data to unlock; one of the values in the
    ///                 <see cref="CurlLockData" /> enumeration.
    ///             </term>
    ///         </item>
    ///         <item>
    ///             <term>userData</term>
    ///             <term>
    ///                 Client-provided data that is not touched internally by
    ///                 <c>cURL</c>. This is set via
    ///                 <see cref="CurlShareOption.UserData" /> when calling the
    ///                 <see cref="CurlShare.SetOpt" /> member of the <see cref="CurlShare" />
    ///                 class.
    ///             </term>
    ///         </item>
    ///     </list>
    /// </remarks>
    public delegate void CurlShareUnlockCallback(CurlLockData data, Object userData);
}