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
    ///     This class wraps a <c>curl_version_info_data</c> struct. An instance is
    ///     obtained by calling <see cref="Curl.GetVersionInfo" />.
    /// </summary>
    public sealed class CurlVersionInfoData
    {
        private const int OFFSET_AGE = 0;
        private const int OFFSET_VERSION = 4;
        private const int OFFSET_VERSION_NUM = 8;
        private const int OFFSET_HOST = 12;
        private const int OFFSET_FEATURES = 16;
        private const int OFFSET_SSL_VERSION = 20;
        private const int OFFSET_SSL_VERSION_NUM = 24;
        private const int OFFSET_LIBZ_VERSION = 28;
        private const int OFFSET_PROTOCOLS = 32;
        private const int OFFSET_ARES_VERSION = 36;
        private const int OFFSET_ARES_VERSION_NUM = 40;
        private const int OFFSET_LIBIDN_VERSION = 44;

        private readonly IntPtr m_pVersionInfoData;

        internal CurlVersionInfoData(CurlVersion ver)
        {
            m_pVersionInfoData = NativeMethods.curl_version_info(ver);
        }
#if USE_LIBCURLSHIM
        /// <summary>
        ///     Age of this struct, depending on how recent the linked-in
        ///     <c>libcurl</c> is, as a <see cref="CurlVersion" />.
        /// </summary>
        public CurlVersion Age
        {
            get { return (CurlVersion) NativeMethods.curl_shim_get_version_int_value(m_pVersionInfoData, OFFSET_AGE); }
        }

        /// <summary>
        ///     Get the internal cURL version, as a <c>string</c>.
        /// </summary>
        public string Version
        {
            get
            {
                return Marshal.PtrToStringAnsi(
                    NativeMethods.curl_shim_get_version_char_ptr(m_pVersionInfoData, OFFSET_VERSION));
            }
        }

        /// <summary>
        ///     Get the internal cURL version number, a A 24-bit number created
        ///     like this: [8 bits major number] | [8 bits minor number] | [8
        ///     bits patch number]. For example, Version 7.12.2 is <c>0x070C02</c>.
        /// </summary>
        public int VersionNum
        {
            get { return NativeMethods.curl_shim_get_version_int_value(m_pVersionInfoData, OFFSET_VERSION_NUM); }
        }

        /// <summary>
        ///     Get the host information on which the underlying cURL was built.
        /// </summary>
        public string Host
        {
            get
            {
                return
                    Marshal.PtrToStringAnsi(NativeMethods.curl_shim_get_version_char_ptr(m_pVersionInfoData, OFFSET_HOST));
            }
        }

        /// <summary>
        ///     Get a bitmask of features, containing bits or'd from the
        ///     <see cref="CurlVersionFeatureBitmask" /> enumeration.
        /// </summary>
        public int Features
        {
            get { return NativeMethods.curl_shim_get_version_int_value(m_pVersionInfoData, OFFSET_FEATURES); }
        }

        /// <summary>
        ///     Get the Ssl version, if it's linked in.
        /// </summary>
        public string SslVersion
        {
            get
            {
                return
                    Marshal.PtrToStringAnsi(NativeMethods.curl_shim_get_version_char_ptr(m_pVersionInfoData,
                        OFFSET_SSL_VERSION));
            }
        }

        /// <summary>
        ///     Get the Ssl version number, if Ssl is linked in.
        /// </summary>
        public int SSLVersionNum
        {
            get { return NativeMethods.curl_shim_get_version_int_value(m_pVersionInfoData, OFFSET_SSL_VERSION_NUM); }
        }

        /// <summary>
        ///     Get the libz version, if libz is linked in.
        /// </summary>
        public string LibZVersion
        {
            get
            {
                return
                    Marshal.PtrToStringAnsi(NativeMethods.curl_shim_get_version_char_ptr(m_pVersionInfoData,
                        OFFSET_LIBZ_VERSION));
            }
        }

        /// <summary>
        ///     Get the names of the supported protocols.
        /// </summary>
        public string[] Protocols
        {
            get
            {
                var nProts = NativeMethods.curl_shim_get_number_of_protocols(
                    m_pVersionInfoData, OFFSET_PROTOCOLS);
                var aProts = new String[nProts];
                for (var i = 0; i < nProts; i++)
                {
                    aProts[i] =
                        Marshal.PtrToStringAnsi(NativeMethods.curl_shim_get_protocol_string(m_pVersionInfoData,
                            OFFSET_PROTOCOLS, i));
                }
                return aProts;
            }
        }

        /// <summary>
        ///     Get the ARes version, if ARes is linked in.
        /// </summary>
        public string ARes
        {
            get
            {
                if (Age > CurlVersion.First)
                {
                    return
                        Marshal.PtrToStringAnsi(NativeMethods.curl_shim_get_version_char_ptr(m_pVersionInfoData,
                            OFFSET_ARES_VERSION));
                }
                return "n.a.";
            }
        }

        /// <summary>
        ///     Get the ARes version number, if ARes is linked in.
        /// </summary>
        public int AResNum
        {
            get
            {
                if (Age > CurlVersion.First)
                {
                    return NativeMethods.curl_shim_get_version_int_value(m_pVersionInfoData, OFFSET_ARES_VERSION_NUM);
                }
                return 0;
            }
        }

        /// <summary>
        ///     Get the libidn version, if libidn is linked in.
        /// </summary>
        public string LibIdn
        {
            get
            {
                if (Age > CurlVersion.Second)
                {
                    return
                        Marshal.PtrToStringAnsi(NativeMethods.curl_shim_get_version_char_ptr(m_pVersionInfoData,
                            OFFSET_LIBIDN_VERSION));
                }
                return "n.a.";
            }
        }
#endif
    }
}