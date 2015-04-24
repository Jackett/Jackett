/***************************************************************************
 *
 * CurlS#arp
 *
 * Copyright (c) 2014 Dr. Masroor Ehsan (masroore@gmail.com)
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

//#define USE_LIBCURLSHIM

using System;
using System.Runtime.InteropServices;

namespace CurlSharp
{
	/// <summary>
	///     P/Invoke signatures.
	/// </summary>
	internal static unsafe class NativeMethods
	{
		#if WIN64
        private const string CURL_LIB = "libcurl64.dll";





#if USE_LIBCURLSHIM
        private const string CURLSHIM_LIB = "libcurlshim64.dll";
#endif
		



#else
		#if LINUX
		private const string CURL_LIB = "libcurl";
		#else
        private const string CURL_LIB = "libcurl.dll";





#if USE_LIBCURLSHIM
        private const string CURLSHIM_LIB = "libcurlshim.dll";
#endif
		#endif
		#endif
		#if !USE_LIBCURLSHIM
		#if LINUX
		private const string WINSOCK_LIB = "libc";
		#else
        private const string WINSOCK_LIB = "ws2_32.dll";
#endif
		#endif

		// internal delegates from cURL

		// libcurl imports
		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern CurlCode curl_global_init (int flags);

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void curl_global_cleanup ();

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		internal static extern IntPtr curl_escape (String url, int length);

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		internal static extern IntPtr curl_unescape (String url, int length);

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void curl_free (IntPtr p);

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr curl_version ();

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr curl_easy_init ();

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void curl_easy_cleanup (IntPtr pCurl);

		[UnmanagedFunctionPointer (CallingConvention.Cdecl)]
        internal delegate int _CurlGenericCallback (IntPtr ptr, int sz, int nmemb, IntPtr userdata);

		[UnmanagedFunctionPointer (CallingConvention.Cdecl)]
        internal delegate int _CurlProgressCallback (
            IntPtr extraData, double dlTotal, double dlNow, double ulTotal, double ulNow);

		[UnmanagedFunctionPointer (CallingConvention.Cdecl)]
        internal delegate int _CurlDebugCallback (
            IntPtr ptrCurl, CurlInfoType infoType, string message, int size, IntPtr ptrUserData);

		[UnmanagedFunctionPointer (CallingConvention.Cdecl)]
        internal delegate int _CurlSslCtxCallback (IntPtr ctx, IntPtr parm);

		[UnmanagedFunctionPointer (CallingConvention.Cdecl)]
        internal delegate CurlIoError _CurlIoctlCallback (CurlIoCommand cmd, IntPtr parm);

		// curl_easy_setopt() overloads
		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern CurlCode curl_easy_setopt (IntPtr pCurl, CurlOption opt, IntPtr parm);

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern CurlCode curl_easy_setopt (IntPtr pCurl, CurlOption opt, string parm);

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern CurlCode curl_easy_setopt (IntPtr pCurl, CurlOption opt, byte[] parm);

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern CurlCode curl_easy_setopt (IntPtr pCurl, CurlOption opt, long parm);

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern CurlCode curl_easy_setopt (IntPtr pCurl, CurlOption opt, bool parm);

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl, EntryPoint = "curl_easy_setopt")]
		internal static extern CurlCode curl_easy_setopt_cb (IntPtr pCurl, CurlOption opt, _CurlGenericCallback parm);

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl, EntryPoint = "curl_easy_setopt")]
		internal static extern CurlCode curl_easy_setopt_cb (IntPtr pCurl, CurlOption opt, _CurlProgressCallback parm);

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl, EntryPoint = "curl_easy_setopt")]
		internal static extern CurlCode curl_easy_setopt_cb (IntPtr pCurl, CurlOption opt, _CurlDebugCallback parm);

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl, EntryPoint = "curl_easy_setopt")]
		internal static extern CurlCode curl_easy_setopt_cb (IntPtr pCurl, CurlOption opt, _CurlSslCtxCallback parm);

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl, EntryPoint = "curl_easy_setopt")]
		internal static extern CurlCode curl_easy_setopt_cb (IntPtr pCurl, CurlOption opt, _CurlIoctlCallback parm);

		#if !USE_LIBCURLSHIM
		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern CurlMultiCode curl_multi_fdset (IntPtr pmulti,
		                                                       [In, Out] ref fd_set read_fd_set,
		                                                       [In, Out] ref fd_set write_fd_set,
		                                                       [In, Out] ref fd_set exc_fd_set,
		                                                       [In, Out] ref int max_fd);

		[StructLayout (LayoutKind.Sequential)]
		internal struct fd_set
		{
			internal uint fd_count;
			//[MarshalAs(UnmanagedType.ByValArray, SizeConst = FD_SETSIZE)] internal IntPtr[] fd_array;
			internal fixed uint fd_array[FD_SETSIZE];

			internal const int FD_SETSIZE = 64;

			internal void Cleanup ()
			{
				//fd_array = null;
			}

			internal static fd_set Create ()
			{
				return new fd_set {
					//fd_array = new IntPtr[FD_SETSIZE],
					fd_count = 0
				};
			}

			internal static fd_set Create (IntPtr socket)
			{
				var handle = Create ();
				handle.fd_count = 1;
				handle.fd_array [0] = (uint)socket;
				return handle;
			}
		}

		internal static void FD_ZERO (fd_set fds)
		{
			for (var i = 0; i < fd_set.FD_SETSIZE; i++) {
				//fds.fd_array[i] = (IntPtr) 0;
				fds.fd_array [i] = 0;
			}
			fds.fd_count = 0;
		}

		[StructLayout (LayoutKind.Sequential)]
		internal struct timeval
		{
			/// <summary>
			///     Time interval, in seconds.
			/// </summary>
			internal int tv_sec;

			/// <summary>
			///     Time interval, in microseconds.
			/// </summary>
			internal int tv_usec;

			internal static timeval Create (int milliseconds)
			{
				return new timeval {
					tv_sec = milliseconds / 1000,
					tv_usec = (milliseconds % 1000) * 1000
				};
			}
		};

		[DllImport (WINSOCK_LIB, EntryPoint = "select")]
		internal static extern int select (
			int nfds, // number of sockets, (ignored in winsock)
			[In, Out] ref fd_set readfds, // read sockets to watch
			[In, Out] ref fd_set writefds, // write sockets to watch
			[In, Out] ref fd_set exceptfds, // error sockets to watch
			ref timeval timeout);

		//[DllImport(WINSOCK_LIB, EntryPoint = "select")]
		//internal static extern int select(int ndfs, fd_set* readfds, fd_set* writefds, fd_set* exceptfds, timeval* timeout);
		#endif

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern CurlCode curl_easy_perform (IntPtr pCurl);

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr curl_easy_duphandle (IntPtr pCurl);

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr curl_easy_strerror (CurlCode err);

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern CurlCode curl_easy_getinfo (IntPtr pCurl, CurlInfo info, ref IntPtr pInfo);

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern CurlCode curl_easy_getinfo (IntPtr pCurl, CurlInfo info, ref double dblVal);

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void curl_easy_reset (IntPtr pCurl);

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr curl_multi_init ();

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern CurlMultiCode curl_multi_cleanup (IntPtr pmulti);

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern CurlMultiCode curl_multi_add_handle (IntPtr pmulti, IntPtr peasy);

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern CurlMultiCode curl_multi_remove_handle (IntPtr pmulti, IntPtr peasy);

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr curl_multi_strerror (CurlMultiCode errorNum);

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern CurlMultiCode curl_multi_perform (IntPtr pmulti, ref int runningHandles);

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern void curl_formfree (IntPtr pForm);

		#if !USE_LIBCURLSHIM
		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern int curl_formadd (ref IntPtr pHttppost, ref IntPtr pLastPost,
		                                         int codeFirst, IntPtr bufFirst,
		                                         int codeNext, IntPtr bufNext,
		                                         int codeLast);
		#endif

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr curl_share_init ();

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern CurlShareCode curl_share_cleanup (IntPtr pShare);

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr curl_share_strerror (CurlShareCode errorCode);

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern CurlShareCode curl_share_setopt (IntPtr pShare, CurlShareOption optCode, IntPtr option);

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
		internal static extern IntPtr curl_slist_append (IntPtr slist, string data);

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern CurlShareCode curl_slist_free_all (IntPtr pList);

		[DllImport (CURL_LIB, CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr curl_version_info (CurlVersion ver);

		#if USE_LIBCURLSHIM
		
    // libcurlshim imports
        [DllImport(CURLSHIM_LIB, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void curl_shim_initialize();

        [DllImport(CURLSHIM_LIB, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void curl_shim_cleanup();

        [DllImport(CURLSHIM_LIB, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr curl_shim_alloc_strings();

        [DllImport(CURLSHIM_LIB, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        internal static extern IntPtr curl_shim_add_string_to_slist(
            IntPtr pStrings, String str);

        [DllImport(CURLSHIM_LIB, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        internal static extern IntPtr curl_shim_get_string_from_slist(
            IntPtr pSlist, ref IntPtr pStr);

        [DllImport(CURLSHIM_LIB, CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Ansi)]
        internal static extern IntPtr curl_shim_add_string(IntPtr pStrings, String str);

        [DllImport(CURLSHIM_LIB, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void curl_shim_free_strings(IntPtr pStrings);

        [DllImport(CURLSHIM_LIB, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int curl_shim_install_delegates(IntPtr pCurl, IntPtr pThis,
            _ShimWriteCallback pWrite, _ShimReadCallback pRead,
            _ShimProgressCallback pProgress, _ShimDebugCallback pDebug,
            _ShimHeaderCallback pHeader, _ShimSslCtxCallback pCtx,
            _ShimIoctlCallback pIoctl);

        [DllImport(CURLSHIM_LIB, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void curl_shim_cleanup_delegates(IntPtr pThis);

        [DllImport(CURLSHIM_LIB, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void curl_shim_get_file_time(int unixTime,
            ref int yy, ref int mm, ref int dd, ref int hh, ref int mn, ref int ss);

        [DllImport(CURLSHIM_LIB, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void curl_shim_free_slist(IntPtr p);

        [DllImport(CURLSHIM_LIB, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr curl_shim_alloc_fd_sets();

        [DllImport(CURLSHIM_LIB, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void curl_shim_free_fd_sets(IntPtr fdsets);

        [DllImport(CURLSHIM_LIB, CallingConvention = CallingConvention.Cdecl)]
        internal static extern CurlMultiCode curl_shim_multi_fdset(IntPtr multi,
            IntPtr fdsets, ref int maxFD);

        [DllImport(CURLSHIM_LIB, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int curl_shim_select(int maxFD, IntPtr fdsets,
            int milliseconds);

        [DllImport(CURLSHIM_LIB, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr curl_shim_multi_info_read(IntPtr multi,
            ref int nMsgs);

        [DllImport(CURLSHIM_LIB, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void curl_shim_multi_info_free(IntPtr multiInfo);

        [DllImport(CURLSHIM_LIB, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int curl_shim_formadd(IntPtr[] ppForms, IntPtr[] pParams, int nParams);

        [DllImport(CURLSHIM_LIB, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int curl_shim_install_share_delegates(IntPtr pShare,
            IntPtr pThis, _ShimLockCallback pLock, _ShimUnlockCallback pUnlock);

        [DllImport(CURLSHIM_LIB, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void curl_shim_cleanup_share_delegates(IntPtr pShare);

        [DllImport(CURLSHIM_LIB, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int curl_shim_get_version_int_value(IntPtr p, int offset);

        [DllImport(CURLSHIM_LIB, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr curl_shim_get_version_char_ptr(IntPtr p, int offset);

        [DllImport(CURLSHIM_LIB, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int curl_shim_get_number_of_protocols(IntPtr p, int offset);

        [DllImport(CURLSHIM_LIB, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr curl_shim_get_protocol_string(IntPtr p, int offset, int index);

        internal delegate void _ShimLockCallback(int data, int access, IntPtr userPtr);

        internal delegate void _ShimUnlockCallback(int data, IntPtr userPtr);

        internal delegate int _ShimDebugCallback(CurlInfoType infoType, IntPtr msgBuf, int msgBufSize, IntPtr parm);

        internal delegate int _ShimHeaderCallback(IntPtr buf, int sz, int nmemb, IntPtr stream);

        internal delegate CurlIoError _ShimIoctlCallback(CurlIoCommand cmd, IntPtr parm);

        internal delegate int _ShimProgressCallback(IntPtr parm, double dlTotal, double dlNow, double ulTotal, double ulNow);

        internal delegate int _ShimReadCallback(IntPtr buf, int sz, int nmemb, IntPtr parm);

        internal delegate int _ShimSslCtxCallback(IntPtr ctx, IntPtr parm);

        internal delegate int _ShimWriteCallback(IntPtr buf, int sz, int nmemb, IntPtr parm);
#endif
	}
}