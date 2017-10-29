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
using System.Collections;
using System.Runtime.InteropServices;

namespace CurlSharp
{
    /// <summary>
    ///     Implements the <c>curl_multi_xxx</c> API.
    /// </summary>
    public class CurlMulti : IDisposable
    {
        // private members
        private readonly Hashtable _htEasy;
        private int _maxFd;
        private CurlMultiInfo[] _multiInfo;
        private bool _bGotMultiInfo;
#if USE_LIBCURLSHIM
        private IntPtr _fdSets;
#else
        private NativeMethods.fd_set _fd_read, _fd_write, _fd_except;
#endif
        private IntPtr _pMulti;
        private CurlPipelining _pipelining;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        ///     This is thrown
        ///     if <see cref="Curl" /> hasn't bee properly initialized.
        /// </exception>
        /// <exception cref="System.NullReferenceException">
        ///     This is thrown if the native <c>CurlMulti</c> handle wasn't
        ///     created successfully.
        /// </exception>
        public CurlMulti()
        {
            Curl.EnsureCurl();
            _pMulti = NativeMethods.curl_multi_init();
            ensureHandle();
            _maxFd = 0;
#if USE_LIBCURLSHIM
            _fdSets = IntPtr.Zero;
            _fdSets = NativeMethods.curl_shim_alloc_fd_sets();
#else
            _fd_read = NativeMethods.fd_set.Create();
            _fd_read = NativeMethods.fd_set.Create();
            _fd_write = NativeMethods.fd_set.Create();
            _fd_except = NativeMethods.fd_set.Create();
#endif
            _multiInfo = null;
            _bGotMultiInfo = false;
            _htEasy = new Hashtable();
        }

        /// <summary>
        ///     Max file descriptor
        /// </summary>
        public int MaxFd => _maxFd;

        /// <summary>
        ///     Cleanup unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Destructor
        /// </summary>
        ~CurlMulti()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            lock (this)
            {
                // if (disposing) // managed member cleanup
                // unmanaged cleanup
                if (_pMulti != IntPtr.Zero)
                {
                    NativeMethods.curl_multi_cleanup(_pMulti);
                    _pMulti = IntPtr.Zero;
                }

#if USE_LIBCURLSHIM
                if (_fdSets != IntPtr.Zero)
                {
                    NativeMethods.curl_shim_free_fd_sets(_fdSets);
                    _fdSets = IntPtr.Zero;
                }
#else
                _fd_read.Cleanup();
                _fd_write.Cleanup();
                _fd_except.Cleanup();
#endif
            }
        }

        private void ensureHandle()
        {
            if (_pMulti == IntPtr.Zero)
                throw new NullReferenceException("No internal multi handle");
        }

        /// <summary>
        ///     Add an CurlEasy object.
        /// </summary>
        /// <param name="curlEasy">
        ///     <see cref="CurlEasy" /> object to add.
        /// </param>
        /// <returns>
        ///     A <see cref="CurlMultiCode" />, hopefully <c>CurlMultiCode.Ok</c>
        /// </returns>
        /// <exception cref="System.NullReferenceException">
        ///     This is thrown if the native <c>CurlMulti</c> handle wasn't
        ///     created successfully.
        /// </exception>
        public CurlMultiCode AddHandle(CurlEasy curlEasy)
        {
            ensureHandle();
            var p = curlEasy.Handle;
            _htEasy.Add(p, curlEasy);
            return NativeMethods.curl_multi_add_handle(_pMulti, p);
        }

        public CurlPipelining Pipelining
        {
            get { return _pipelining; }
            set
            {
                ensureHandle();
                _pipelining = value;
                NativeMethods.curl_multi_setopt(_pMulti, CurlMultiOption.Pipelining, (long) value);
            }
        }

        /// <summary>
        ///     Remove an CurlEasy object.
        /// </summary>
        /// <param name="curlEasy">
        ///     <see cref="CurlEasy" /> object to remove.
        /// </param>
        /// <returns>
        ///     A <see cref="CurlMultiCode" />, hopefully <c>CurlMultiCode.Ok</c>
        /// </returns>
        /// <exception cref="System.NullReferenceException">
        ///     This is thrown if the native <c>CurlMulti</c> handle wasn't
        ///     created successfully.
        /// </exception>
        public CurlMultiCode RemoveHandle(CurlEasy curlEasy)
        {
            ensureHandle();
            var p = curlEasy.Handle;
            _htEasy.Remove(p);
            return NativeMethods.curl_multi_remove_handle(_pMulti, curlEasy.Handle);
        }

        /// <summary>
        ///     Get a string description of an error code.
        /// </summary>
        /// <param name="errorNum">
        ///     The <see cref="CurlMultiCode" /> for which to obtain the error
        ///     string description.
        /// </param>
        /// <returns>The string description.</returns>
        public string StrError(CurlMultiCode errorNum) => Marshal.PtrToStringAnsi(NativeMethods.curl_multi_strerror(errorNum));

        /// <summary>
        ///     Read/write data to/from each CurlEasy object.
        /// </summary>
        /// <param name="runningObjects">
        ///     The number of <see cref="CurlEasy" /> objects still in process is
        ///     written by this function to this reference parameter.
        /// </param>
        /// <returns>
        ///     A <see cref="CurlMultiCode" />, hopefully <c>CurlMultiCode.Ok</c>
        /// </returns>
        /// <exception cref="System.NullReferenceException">
        ///     This is thrown if the native <c>CurlMulti</c> handle wasn't
        ///     created successfully.
        /// </exception>
        public CurlMultiCode Perform(ref int runningObjects)
        {
            ensureHandle();
            return NativeMethods.curl_multi_perform(_pMulti, ref runningObjects);
        }

        /// <summary>
        ///     Set internal file desriptor information before calling Select.
        /// </summary>
        /// <returns>
        ///     A <see cref="CurlMultiCode" />, hopefully <c>CurlMultiCode.Ok</c>
        /// </returns>
        /// <exception cref="System.NullReferenceException">
        ///     This is thrown if the native <c>CurlMulti</c> handle wasn't
        ///     created successfully.
        /// </exception>
        public CurlMultiCode FdSet()
        {
            ensureHandle();
#if USE_LIBCURLSHIM
            return NativeMethods.curl_shim_multi_fdset(_pMulti, _fdSets, ref _maxFd);
#else
            NativeMethods.FD_ZERO(_fd_read);
            NativeMethods.FD_ZERO(_fd_write);
            NativeMethods.FD_ZERO(_fd_except);
            return NativeMethods.curl_multi_fdset(_pMulti, ref _fd_read, ref _fd_write, ref _fd_except, ref _maxFd);
#endif
        }

        /// <summary>
        ///     Call <c>select()</c> on the CurlEasy objects.
        /// </summary>
        /// <param name="timeoutMillis">
        ///     The timeout for the internal <c>select()</c> call,
        ///     in milliseconds.
        /// </param>
        /// <returns>
        ///     Number or <see cref="CurlEasy" /> objects with pending reads.
        /// </returns>
        /// <exception cref="System.NullReferenceException">
        ///     This is thrown if the native <c>CurlMulti</c> handle wasn't
        ///     created successfully.
        /// </exception>
        public int Select(int timeoutMillis)
        {
            ensureHandle();
#if USE_LIBCURLSHIM
            return NativeMethods.curl_shim_select(_maxFd + 1, _fdSets, timeoutMillis);
#else
            var timeout = NativeMethods.timeval.Create(timeoutMillis);
            return NativeMethods.select(_maxFd + 1, ref _fd_read, ref _fd_write, ref _fd_except, ref timeout);
            //return NativeMethods.select2(_maxFd + 1, _fd_read, _fd_write, _fd_except, timeout);
#endif
        }

        /// <summary>
        ///     Obtain status information for a CurlMulti transfer. Requires
        ///     CurlSharp be compiled with the libcurlshim helper.
        /// </summary>
        /// <returns>
        ///     An array of <see cref="CurlMultiInfo" /> objects, one for each
        ///     <see cref="CurlEasy" /> object child.
        /// </returns>
        /// <exception cref="System.NullReferenceException">
        ///     This is thrown if the native <c>CurlMulti</c> handle wasn't
        ///     created successfully.
        /// </exception>
        public CurlMultiInfo[] InfoRead()
        {
            if (_bGotMultiInfo)
                return _multiInfo;

#if USE_LIBCURLSHIM
            var nMsgs = 0;
            var pInfo = NativeMethods.curl_shim_multi_info_read(_pMulti, ref nMsgs);
            if (pInfo != IntPtr.Zero)
            {
                _multiInfo = new CurlMultiInfo[nMsgs];
                for (var i = 0; i < nMsgs; i++)
                {
                    var msg = (CurlMessage) Marshal.ReadInt32(pInfo, i*12);
                    var pEasy = Marshal.ReadIntPtr(pInfo, i*12 + 4);
                    var code = (CurlCode) Marshal.ReadInt32(pInfo, i*12 + 8);
                    _multiInfo[i] = new CurlMultiInfo(msg, (CurlEasy) _htEasy[pEasy], code);
                }
                NativeMethods.curl_shim_multi_info_free(pInfo);
            }
            _bGotMultiInfo = true;
#else
            _multiInfo = null;
            throw new NotImplementedException("CurlMulti.InfoRead()");
#endif
            return _multiInfo;
        }
    }
}