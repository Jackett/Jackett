/***************************************************************************
 *
 * CurlS#arp
 *
 * Copyright (c) 2013-2017 Dr. Masroor Ehsan (masroore@gmail.com)
 * Portions copyright (c) 2004, 2005 Jeff Phillips (jeff@jeffp.net)
 * Portions copyright (c) 2017 Katelyn Gigante (https://github.com/silasary)
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

//#define USE_LIBCURLSHIM

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CurlSharp
{
    /// <summary>
    ///     P/Invoke signatures.
    /// </summary>
    internal static unsafe class NativeMethods
    {
        private const string LIBCURL = "libcurl";

        private const string LIBCURLSHIM = "libcurlshim";

        private const string LIBC_LINUX = "libc";

        private const string WINSOCK_LIB = "ws2_32.dll";

        private const string LIB_DIR_WIN64 = "amd64";

        private const string LIB_DIR_WIN32 = "i386";

        static NativeMethods()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                switch (RuntimeInformation.OSArchitecture)
                {
                    case Architecture.X64:
                        SetDllDirectory(Path.Combine(AssemblyDirectory, LIB_DIR_WIN64));
                        break;
                    case Architecture.X86:
                        SetDllDirectory(Path.Combine(AssemblyDirectory, LIB_DIR_WIN32));
                        break;
                }
            }
#if USE_LIBCURLSHIM
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new InvalidOperationException("Can not run on other platform than Win NET");
#endif
        }
        
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetDllDirectory(string lpPathName);
        
        private static string AssemblyDirectory
        {
            get
            {
                var codeBase = typeof(NativeMethods).GetTypeInfo().Assembly.CodeBase;
                var uri = new UriBuilder(codeBase);
                var path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        #region curl_global_init

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CurlCode curl_global_init(int flags);

        #endregion

        #region curl_global_cleanup

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void curl_global_cleanup();

        #endregion

        #region curl_easy_escape

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr curl_easy_escape(IntPtr pEasy, string url, int length);

        #endregion

        #region curl_easy_unescape

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr curl_easy_unescape(IntPtr pEasy, string url, int inLength, out int outLength);

        #endregion

        #region curl_free

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void curl_free(IntPtr p);

        #endregion

        #region curl_version

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr curl_version();

        #endregion

        #region curl_version_info

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr curl_version_info(CurlVersion ver);

        #endregion

        #region curl_easy_init

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr curl_easy_init();

        #endregion

        #region curl_easy_cleanup

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void curl_easy_cleanup(IntPtr pCurl);

        #endregion

        #region curl_easy_setopt

        #region Delegates

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int _CurlGenericCallback(IntPtr ptr, int sz, int nmemb, IntPtr userdata);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int _CurlProgressCallback(
            IntPtr extraData,
            double dlTotal,
            double dlNow,
            double ulTotal,
            double ulNow);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int _CurlDebugCallback(
            IntPtr ptrCurl,
            CurlInfoType infoType,
            string message,
            int size,
            IntPtr ptrUserData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int _CurlSslCtxCallback(IntPtr ctx, IntPtr parm);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate CurlIoError _CurlIoctlCallback(CurlIoCommand cmd, IntPtr parm);

        #endregion

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CurlCode curl_easy_setopt(IntPtr pCurl, CurlOption opt, IntPtr parm);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CurlCode curl_easy_setopt(IntPtr pCurl, CurlOption opt, string parm);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CurlCode curl_easy_setopt(IntPtr pCurl, CurlOption opt, byte[] parm);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CurlCode curl_easy_setopt(IntPtr pCurl, CurlOption opt, long parm);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CurlCode curl_easy_setopt(IntPtr pCurl, CurlOption opt, bool parm);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CurlCode curl_easy_setopt(IntPtr pCurl, CurlOption opt, _CurlGenericCallback parm);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CurlCode curl_easy_setopt(IntPtr pCurl, CurlOption opt, _CurlProgressCallback parm);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CurlCode curl_easy_setopt(IntPtr pCurl, CurlOption opt, _CurlDebugCallback parm);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CurlCode curl_easy_setopt(IntPtr pCurl, CurlOption opt, _CurlSslCtxCallback parm);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CurlCode curl_easy_setopt(IntPtr pCurl, CurlOption opt, _CurlIoctlCallback parm);

        #endregion

        #region curl_easy_perform

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CurlCode curl_easy_perform(IntPtr pCurl);

        #endregion

        #region curl_easy_duphandle

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr curl_easy_duphandle(IntPtr pCurl);

        #endregion

        #region curl_easy_strerror

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr curl_easy_strerror(CurlCode err);

        #endregion

        #region curl_easy_getinfo

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CurlCode curl_easy_getinfo(IntPtr pCurl, CurlInfo info, ref IntPtr pInfo);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CurlCode curl_easy_getinfo(IntPtr pCurl, CurlInfo info, ref double dblVal);

        #endregion

        #region curl_easy_reset

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void curl_easy_reset(IntPtr pCurl);

        #endregion

        #region curl_multi_init

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr curl_multi_init();

        #endregion

        #region curl_multi_cleanup

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CurlMultiCode curl_multi_cleanup(IntPtr pmulti);

        #endregion

        #region curl_multi_add_handle

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CurlMultiCode curl_multi_add_handle(IntPtr pmulti, IntPtr peasy);

        #endregion

        #region curl_multi_remove_handle

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CurlMultiCode curl_multi_remove_handle(IntPtr pmulti, IntPtr peasy);

        #endregion

        #region curl_multi_setopt

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CurlMultiCode curl_multi_setopt(IntPtr pmulti, CurlMultiOption opt, bool parm);

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CurlMultiCode curl_multi_setopt(IntPtr pmulti, CurlMultiOption opt, long parm);

        #endregion

        #region curl_multi_strerror

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr curl_multi_strerror(CurlMultiCode errorNum);

        #endregion

        #region curl_multi_perform

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CurlMultiCode curl_multi_perform(IntPtr pmulti, ref int runningHandles);

        #endregion

#if !USE_LIBCURLSHIM

        #region curl_multi_fdset

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CurlMultiCode curl_multi_fdset(IntPtr pmulti,
            [In] [Out] ref fd_set read_fd_set,
            [In] [Out] ref fd_set write_fd_set,
            [In] [Out] ref fd_set exc_fd_set,
            [In] [Out] ref int max_fd);

        [StructLayout(LayoutKind.Sequential)]
        public struct fd_set
        {
            public uint fd_count;

            // [MarshalAs(UnmanagedType.ByValArray, SizeConst = FD_SETSIZE)] public IntPtr[] fd_array;
            public fixed uint fd_array[FD_SETSIZE];

            public const int FD_SETSIZE = 64;

            public void Cleanup()
            {
                // fd_array = null;
            }

            public static fd_set Create()
            {
                return new fd_set
                {
                    // fd_array = new IntPtr[FD_SETSIZE],
                    fd_count = 0
                };
            }

            public static fd_set Create(IntPtr socket)
            {
                var handle = Create();
                handle.fd_count = 1;
                handle.fd_array[0] = (uint) socket;
                return handle;
            }
        }

        public static void FD_ZERO(fd_set fds)
        {
            for (var i = 0; i < fd_set.FD_SETSIZE; i++)
            {
                fds.fd_array[i] = 0;
            }
            fds.fd_count = 0;
        }

        #endregion

        #region select

        [StructLayout(LayoutKind.Sequential)]
        public struct timeval
        {
            /// <summary>
            ///     Time interval, in seconds.
            /// </summary>
            public int tv_sec;

            /// <summary>
            ///     Time interval, in microseconds.
            /// </summary>
            public int tv_usec;

            public static timeval Create(int milliseconds)
            {
                return new timeval
                {
                    tv_sec = milliseconds / 1000,
                    tv_usec = milliseconds % 1000 * 1000
                };
            }
        }

        [DllImport(LIBC_LINUX, EntryPoint = "select")]
        private static extern int select_unix(
            int nfds, // number of sockets, (ignored in winsock)
            [In] [Out] ref fd_set readfds, // read sockets to watch
            [In] [Out] ref fd_set writefds, // write sockets to watch
            [In] [Out] ref fd_set exceptfds, // error sockets to watch
            ref timeval timeout);

        [DllImport(WINSOCK_LIB, EntryPoint = "select")]
        private static extern int select_win(
            int nfds, // number of sockets, (ignored in winsock)
            [In] [Out] ref fd_set readfds, // read sockets to watch
            [In] [Out] ref fd_set writefds, // write sockets to watch
            [In] [Out] ref fd_set exceptfds, // error sockets to watch
            ref timeval timeout);

        public static int select(
            int nfds, // number of sockets, (ignored in winsock)
            [In] [Out] ref fd_set readfds, // read sockets to watch
            [In] [Out] ref fd_set writefds, // write sockets to watch
            [In] [Out] ref fd_set exceptfds, // error sockets to watch
            ref timeval timeout)
        {
            int result;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                result = select_win(
                    nfds, // number of sockets, (ignored in winsock)
                    ref readfds, // read sockets to watch
                    ref writefds, // write sockets to watch
                    ref exceptfds, // error sockets to watch
                    ref timeout);
            }
            else
            {
                result = select_unix(
                    nfds, // number of sockets, (ignored in winsock)
                    ref readfds, // read sockets to watch
                    ref writefds, // write sockets to watch
                    ref exceptfds, // error sockets to watch
                    ref timeout);
            }

            return result;
        }

        #endregion

#endif

        #region curl_share_init

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr curl_share_init();

        #endregion

        #region curl_share_cleanup

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CurlShareCode curl_share_cleanup(IntPtr pShare);

        #endregion

        #region curl_share_strerror

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr curl_share_strerror(CurlShareCode errorCode);

        #endregion

        #region curl_share_setopt

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern CurlShareCode curl_share_setopt(
            IntPtr pShare,
            CurlShareOption optCode,
            IntPtr option);

        #endregion

        #region curl_formadd

#if !USE_LIBCURLSHIM

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int curl_formadd(ref IntPtr pHttppost, ref IntPtr pLastPost,
            int codeFirst, IntPtr bufFirst,
            int codeNext, IntPtr bufNext,
            int codeLast);

#endif

        #endregion

        #region curl_formfree

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void curl_formfree(IntPtr pForm);

        #endregion

        #region curl_slist_append

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr curl_slist_append(IntPtr slist, string data);

        #endregion

        #region curl_slist_free_all

        [DllImport(LIBCURL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void curl_slist_free_all(IntPtr pList);

        #endregion

#if USE_LIBCURLSHIM

        #region libcurlshim imports

        [DllImport(LIBCURLSHIM, CallingConvention = CallingConvention.Cdecl)]
        public static extern void curl_shim_initialize();

        [DllImport(LIBCURLSHIM, CallingConvention = CallingConvention.Cdecl)]
        public static extern void curl_shim_cleanup();

        [DllImport(LIBCURLSHIM, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr curl_shim_alloc_strings();

        [DllImport(LIBCURLSHIM, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr curl_shim_add_string_to_slist(IntPtr pStrings, string str);

        [DllImport(LIBCURLSHIM, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr curl_shim_get_string_from_slist(IntPtr pSlist, ref IntPtr pStr);

        [DllImport(LIBCURLSHIM, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr curl_shim_add_string(IntPtr pStrings, string str);

        [DllImport(LIBCURLSHIM, CallingConvention = CallingConvention.Cdecl)]
        public static extern void curl_shim_free_strings(IntPtr pStrings);

        [DllImport(LIBCURLSHIM, CallingConvention = CallingConvention.Cdecl)]
        public static extern int curl_shim_install_delegates(
            IntPtr pCurl,
            IntPtr pThis,
            _ShimWriteCallback pWrite,
            _ShimReadCallback pRead,
            _ShimProgressCallback pProgress,
            _ShimDebugCallback pDebug,
            _ShimHeaderCallback pHeader,
            _ShimSslCtxCallback pCtx,
            _ShimIoctlCallback pIoctl);

        [DllImport(LIBCURLSHIM, CallingConvention = CallingConvention.Cdecl)]
        public static extern void curl_shim_cleanup_delegates(IntPtr pThis);

        [DllImport(LIBCURLSHIM, CallingConvention = CallingConvention.Cdecl)]
        public static extern void curl_shim_get_file_time(
            int unixTime,
            ref int yy,
            ref int mm,
            ref int dd,
            ref int hh,
            ref int mn,
            ref int ss);

        [DllImport(LIBCURLSHIM, CallingConvention = CallingConvention.Cdecl)]
        public static extern void curl_shim_free_slist(IntPtr p);

        [DllImport(LIBCURLSHIM, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr curl_shim_alloc_fd_sets();

        [DllImport(LIBCURLSHIM, CallingConvention = CallingConvention.Cdecl)]
        public static extern void curl_shim_free_fd_sets(IntPtr fdsets);

        [DllImport(LIBCURLSHIM, CallingConvention = CallingConvention.Cdecl)]
        public static extern CurlMultiCode curl_shim_multi_fdset(IntPtr multi, IntPtr fdsets, ref int maxFD);

        [DllImport(LIBCURLSHIM, CallingConvention = CallingConvention.Cdecl)]
        public static extern int curl_shim_select(int maxFD, IntPtr fdsets, int milliseconds);

        [DllImport(LIBCURLSHIM, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr curl_shim_multi_info_read(IntPtr multi, ref int nMsgs);

        [DllImport(LIBCURLSHIM, CallingConvention = CallingConvention.Cdecl)]
        public static extern void curl_shim_multi_info_free(IntPtr multiInfo);

        [DllImport(LIBCURLSHIM, CallingConvention = CallingConvention.Cdecl)]
        public static extern int curl_shim_formadd(IntPtr[] ppForms, IntPtr[] pParams, int nParams);

        [DllImport(LIBCURLSHIM, CallingConvention = CallingConvention.Cdecl)]
        public static extern int curl_shim_install_share_delegates(
            IntPtr pShare,
            IntPtr pThis,
            _ShimLockCallback pLock,
            _ShimUnlockCallback pUnlock);

        [DllImport(LIBCURLSHIM, CallingConvention = CallingConvention.Cdecl)]
        public static extern void curl_shim_cleanup_share_delegates(IntPtr pShare);

        [DllImport(LIBCURLSHIM, CallingConvention = CallingConvention.Cdecl)]
        public static extern int curl_shim_get_version_int_value(IntPtr p, int offset);

        [DllImport(LIBCURLSHIM, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr curl_shim_get_version_char_ptr(IntPtr p, int offset);

        [DllImport(LIBCURLSHIM, CallingConvention = CallingConvention.Cdecl)]
        public static extern int curl_shim_get_number_of_protocols(IntPtr p, int offset);

        [DllImport(LIBCURLSHIM, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr curl_shim_get_protocol_string(IntPtr p, int offset, int index);

        public delegate void _ShimLockCallback(int data, int access, IntPtr userPtr);

        public delegate void _ShimUnlockCallback(int data, IntPtr userPtr);

        public delegate int _ShimDebugCallback(CurlInfoType infoType, IntPtr msgBuf, int msgBufSize, IntPtr parm);

        public delegate int _ShimHeaderCallback(IntPtr buf, int sz, int nmemb, IntPtr stream);

        public delegate CurlIoError _ShimIoctlCallback(CurlIoCommand cmd, IntPtr parm);

        public delegate int _ShimProgressCallback(
            IntPtr parm,
            double dlTotal,
            double dlNow,
            double ulTotal,
            double ulNow);

        public delegate int _ShimReadCallback(IntPtr buf, int sz, int nmemb, IntPtr parm);

        public delegate int _ShimSslCtxCallback(IntPtr ctx, IntPtr parm);

        public delegate int _ShimWriteCallback(IntPtr buf, int sz, int nmemb, IntPtr parm);

        #endregion

#endif
    }
}