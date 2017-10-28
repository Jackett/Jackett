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
using System.Runtime.InteropServices;

namespace CurlSharp
{
    /// <summary>
    ///     This class provides an infrastructure for serializing access to data
    ///     shared by multiple <see cref="CurlEasy" /> objects, including cookie data
    ///     and Dns hosts. It implements the <c>curl_share_xxx</c> API.
    /// </summary>
    public class CurlShare : IDisposable
    {
        // private members
        private GCHandle _hThis; // for handle extraction
#if USE_LIBCURLSHIM
        private NativeMethods._ShimLockCallback _pDelLock; // lock delegate
        private NativeMethods._ShimUnlockCallback _pDelUnlock; // unlock delegate
#endif
        private IntPtr _pShare; // share handle
        private IntPtr _ptrThis; // numeric handle

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        ///     This is thrown
        ///     if <see cref="Curl" /> hasn't bee properly initialized.
        /// </exception>
        /// <exception cref="System.NullReferenceException">
        ///     This is thrown if
        ///     the native <c>share</c> handle wasn't created successfully.
        /// </exception>
        public CurlShare()
        {
            Curl.EnsureCurl();
            _pShare = NativeMethods.curl_share_init();
            EnsureHandle();
            LockFunction = null;
            UnlockFunction = null;
            UserData = null;
            installDelegates();
        }

        public object UserData { get; set; }

        public CurlShareUnlockCallback UnlockFunction { get; set; }

        public CurlShareLockCallback LockFunction { get; set; }

        public CurlLockData Share
        {
            set { setShareOption(CurlShareOption.Share, value); }
        }

        public CurlLockData Unshare
        {
            set { setShareOption(CurlShareOption.Unshare, value); }
        }

        public CurlShareCode LastErrorCode { get; private set; }

        public string LastErrorDescription { get; private set; }

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
        ~CurlShare()
        {
            Dispose(false);
        }

        /// <summary>
        ///     Set options for this object.
        /// </summary>
        /// <param name="option">
        ///     One of the values in the <see cref="CurlShareOption" />
        ///     enumeration.
        /// </param>
        /// <param name="parameter">
        ///     An appropriate object based on the value passed in the
        ///     <c>option</c> argument. See <see cref="CurlShareOption" />
        ///     for more information about the appropriate parameter type.
        /// </param>
        /// <returns>
        ///     A <see cref="CurlShareCode" />, hopefully
        ///     <c>CurlShareCode.Ok</c>.
        /// </returns>
        /// <exception cref="System.NullReferenceException">
        ///     This is thrown if
        ///     the native <c>share</c> handle wasn't created successfully.
        /// </exception>
        public CurlShareCode SetOpt(CurlShareOption option, object parameter)
        {
            EnsureHandle();
            var retCode = CurlShareCode.Ok;

            switch (option)
            {
                case CurlShareOption.LockFunction:
                    var lf = parameter as CurlShareLockCallback;
                    if (lf == null)
                        return CurlShareCode.BadOption;
                    LockFunction = lf;
                    break;

                case CurlShareOption.UnlockFunction:
                    var ulf = parameter as CurlShareUnlockCallback;
                    if (ulf == null)
                        return CurlShareCode.BadOption;
                    UnlockFunction = ulf;
                    break;

                case CurlShareOption.Share:
                case CurlShareOption.Unshare:
                {
                    var opt = (CurlLockData) Convert.ToInt32(parameter);
                    retCode = setShareOption(option, opt);
                    break;
                }

                case CurlShareOption.UserData:
                    UserData = parameter;
                    break;

                default:
                    retCode = CurlShareCode.BadOption;
                    break;
            }
            return retCode;
        }

        private void setLastError(CurlShareCode code, CurlShareOption opt)
        {
            if ((LastErrorCode == CurlShareCode.Ok) && (code != CurlShareCode.Ok))
            {
                LastErrorCode = code;
                LastErrorDescription = $"Error: {StrError(code)} setting option {opt}";
            }
        }

        private CurlShareCode setShareOption(CurlShareOption option, CurlLockData value)
        {
            var retCode = (value != CurlLockData.Cookie) && (value != CurlLockData.Dns)
                ? CurlShareCode.BadOption
                : NativeMethods.curl_share_setopt(_pShare, option, (IntPtr) value);
            setLastError(retCode, option);
            return retCode;
        }

        /// <summary>
        ///     Return a String description of an error code.
        /// </summary>
        /// <param name="errorNum">
        ///     The <see cref="CurlShareCode" /> for which to obtain the error
        ///     string description.
        /// </param>
        /// <returns>The string description.</returns>
        public string StrError(CurlShareCode errorNum)
            => Marshal.PtrToStringAnsi(NativeMethods.curl_share_strerror(errorNum));

        private void Dispose(bool disposing)
        {
            lock (this)
            {
                // if (disposing) cleanup managed objects
                if (_pShare != IntPtr.Zero)
                {
#if USE_LIBCURLSHIM
                    NativeMethods.curl_shim_cleanup_share_delegates(_pShare);
#endif
                    NativeMethods.curl_share_cleanup(_pShare);
                    _hThis.Free();
                    _ptrThis = IntPtr.Zero;
                    _pShare = IntPtr.Zero;
                }
            }
        }

        internal IntPtr GetHandle() => _pShare;

        private void EnsureHandle()
        {
            if (_pShare == IntPtr.Zero)
                throw new NullReferenceException("No internal share handle");
        }

        private void installDelegates()
        {
            _hThis = GCHandle.Alloc(this);
            _ptrThis = (IntPtr) _hThis;
#if USE_LIBCURLSHIM
            _pDelLock = LockDelegate;
            _pDelUnlock = UnlockDelegate;
            NativeMethods.curl_shim_install_share_delegates(_pShare, _ptrThis, _pDelLock, _pDelUnlock);
#endif
        }

        internal static void LockDelegate(int data, int access, IntPtr userPtr)
        {
            var gch = (GCHandle) userPtr;
            var share = (CurlShare) gch.Target;
            share?.LockFunction?.Invoke((CurlLockData) data, (CurlLockAccess) access, share.UserData);
        }

        internal static void UnlockDelegate(int data, IntPtr userPtr)
        {
            var gch = (GCHandle) userPtr;
            var share = (CurlShare) gch.Target;
            share?.UnlockFunction?.Invoke((CurlLockData) data, share.UserData);
        }
    }
}