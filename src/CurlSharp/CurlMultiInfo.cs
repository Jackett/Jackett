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

namespace CurlSharp
{
    /// <summary>
    ///     Wraps the <c>cURL</c> struct <c>CURLMsg</c>. This class provides
    ///     status information following a <see cref="CurlMulti" /> transfer.
    /// </summary>
    public sealed class CurlMultiInfo
    {
        // private members
        private readonly CurlEasy _mCurlEasy;
        private readonly CurlMessage _msg;
        private readonly CurlCode _result;

        internal CurlMultiInfo(CurlMessage msg, CurlEasy curlEasy, CurlCode result)
        {
            _msg = msg;
            _mCurlEasy = curlEasy;
            _result = result;
        }

        /// <summary>
        ///     Get the status code from the <see cref="CurlMessage" /> enumeration.
        /// </summary>
        public CurlMessage Msg
        {
            get { return _msg; }
        }

        /// <summary>
        ///     Get the <see cref="CurlEasy" /> object for this child.
        /// </summary>
        public CurlEasy CurlEasyHandle
        {
            get { return _mCurlEasy; }
        }

        /// <summary>
        ///     Get the return code for the transfer, as a
        ///     <see cref="CurlCode" />.
        /// </summary>
        public CurlCode Result
        {
            get { return _result; }
        }
    }
}