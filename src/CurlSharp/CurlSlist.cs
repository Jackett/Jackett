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
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CurlSharp
{
    /// <summary>
    ///     This class wraps a linked list of strings used in <c>cURL</c>. Use it
    ///     to build string lists where they're required, such as when calling
    ///     <see cref="CurlEasy.SetOpt" /> with <see cref="CurlOption.Quote" />
    ///     as the option.
    /// </summary>
    public class CurlSlist : IDisposable
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        ///     This is thrown
        ///     if <see cref="Curl" /> hasn't bee properly initialized.
        /// </exception>
        public CurlSlist()
        {
            Curl.EnsureCurl();
            Handle = IntPtr.Zero;
        }

        public CurlSlist(IntPtr handle)
        {
            Handle = handle;
        }

        /// <summary>
        ///     Read-only copy of the strings stored in the SList
        /// </summary>
        public List<string> Strings
        {
            get
            {
                if (Handle == IntPtr.Zero)
                    return null;
                var strings = new List<string>();

#if !USE_LIBCURLSHIM
                var slist = new curl_slist();
                Marshal.PtrToStructure(Handle, slist);

                while (true)
                {
                    strings.Add(slist.data);
                    if (slist.next != IntPtr.Zero)
                        Marshal.PtrToStructure(slist.next, slist);
                    else
                        break;
                }
#endif
                return strings;
            }
        }

        internal IntPtr Handle { get; private set; }

        /// <summary>
        ///     Free all internal strings.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        /// <summary>
        ///     Destructor
        /// </summary>
        ~CurlSlist()
        {
            Dispose(false);
        }

        /// <summary>
        ///     Append a string to the list.
        /// </summary>
        /// <param name="str">The <c>string</c> to append.</param>
        public void Append(string str)
        {
#if USE_LIBCURLSHIM
            Handle = NativeMethods.curl_shim_add_string_to_slist(Handle, str);
#else
            Handle = NativeMethods.curl_slist_append(Handle, str);
#endif
        }

        private void Dispose(bool disposing)
        {
            lock (this)
            {
                if (Handle != IntPtr.Zero)
                {
#if USE_LIBCURLSHIM
                    NativeMethods.curl_shim_free_slist(Handle);
#else
                    NativeMethods.curl_slist_free_all(Handle);
#endif
                    Handle = IntPtr.Zero;
                }
            }
        }

#if !USE_LIBCURLSHIM
        [StructLayout(LayoutKind.Sequential)]
        private class curl_slist
        {
            /// char*
            [MarshalAs(UnmanagedType.LPStr)] public string data;

            /// curl_slist*
            public IntPtr next;
        }
#endif
    }
}