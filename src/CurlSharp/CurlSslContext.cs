/***************************************************************************
 *
 * CurlS#arp
 *
 * Copyright (c) 2013 Dr. Masroor Ehsan (masroore@gmail.com)
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

namespace CurlSharp
{
    /// <summary>
    ///     An instance of this class is passed to the delegate
    ///     <see cref="CurlSslContextCallback" />, if it's implemented.
    ///     Within that delegate, the code will have to make native calls to
    ///     the <c>OpenSSL</c> library with the value returned from the
    ///     <see cref="CurlSslContext.Context" /> property cast to an
    ///     <c>SSL_CTX</c> pointer.
    /// </summary>
    public sealed class CurlSslContext
    {
        private readonly IntPtr _pvContext;

        internal CurlSslContext(IntPtr pvContext)
        {
            _pvContext = pvContext;
        }

        /// <summary>
        ///     Get the underlying OpenSSL context.
        /// </summary>
        public IntPtr Context
        {
            get { return _pvContext; }
        }
    }
}