/***************************************************************************
 *
 * CurlS#arp
 *
 * Copyright (c) 2013-2017 Dr. Masroor Ehsan (masroore@gmail.com)
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

using System;
using System.Runtime.InteropServices;

namespace CurlSharp
{
    /// <summary>
    ///     Top-level class for initialization and cleanup.
    /// </summary>
    /// <remarks>
    ///     It also implements static methods for capabilities that don't
    ///     logically belong in a class.
    /// </remarks>
    public static class Curl
    {
        // for state management
        private static CurlCode _initCode;

        /// <summary>
        ///     Class constructor - initialize global status.
        /// </summary>
        static Curl()
        {
            _initCode = CurlCode.FailedInit;
        }

        // hidden instance stuff

        /// <summary>
        ///     Get the underlying cURL version as a string, example "7.12.2".
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        ///     Thrown if cURL isn't properly initialized.
        /// </exception>
        public static string Version
        {
            get
            {
                EnsureCurl();
                return Marshal.PtrToStringAnsi(NativeMethods.curl_version());
            }
        }

        /// <summary>
        ///     Process-wide initialization -- call only once per process.
        /// </summary>
        /// <param name="flags">
        ///     An or'd combination of
        ///     <see cref="CurlInitFlag" /> members.
        /// </param>
        /// <returns>
        ///     A <see cref="CurlCode" />, hopefully
        ///     <c>CurlCode.Ok</c>.
        /// </returns>
        public static CurlCode GlobalInit(CurlInitFlag flags)
        {
            _initCode = NativeMethods.curl_global_init((int) flags);
#if USE_LIBCURLSHIM
            if (_initCode == CurlCode.Ok)
                NativeMethods.curl_shim_initialize();
#endif
            return _initCode;
        }

        /// <summary>
        ///     Process-wide cleanup -- call just before exiting process.
        /// </summary>
        /// <remarks>
        ///     While it's not necessary that your program call this method
        ///     before exiting, doing so will prevent leaks of native cURL resources.
        /// </remarks>
        public static void GlobalCleanup()
        {
            if (_initCode == CurlCode.Ok)
            {
#if USE_LIBCURLSHIM
                NativeMethods.curl_shim_cleanup();
#endif
                NativeMethods.curl_global_cleanup();
                _initCode = CurlCode.FailedInit;
            }
        }

        /// <summary>
        ///     Get a <see cref="CurlVersionInfoData" /> object.
        /// </summary>
        /// <param name="ver">
        ///     Specify a <see cref="CurlVersion" />, such as
        ///     <c>CurlVersion.Now</c>.
        /// </param>
        /// <returns>A <see cref="CurlVersionInfoData" /> object.</returns>
        /// <exception cref="System.InvalidOperationException">
        ///     Thrown if cURL isn't properly initialized.
        /// </exception>
        public static CurlVersionInfoData GetVersionInfo(CurlVersion ver)
        {
            EnsureCurl();
            return new CurlVersionInfoData(ver);
        }

        /// <summary>
        ///     Called by other classes to ensure valid cURL state.
        /// </summary>
        internal static void EnsureCurl()
        {
            if (_initCode != CurlCode.Ok)
                throw new InvalidOperationException("cURL not initialized");
        }
    }
}