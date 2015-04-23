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

using System;
using System.Runtime.InteropServices;

namespace CurlSharp
{
    /// <summary>
    ///     Implements the <c>curl_easy_xxx</c> API.
    /// </summary>
    /// <remarks>
    ///     This is the most important class in <c>libcurl.NET</c>. It wraps a
    ///     <c>CURL*</c> handle and provides delegates through which callbacks
    ///     (such as <c>CurlWriteCallback</c> and <c>CurlReadCallback</c>)
    ///     are implemented.
    /// </remarks>
    public class CurlEasy : IDisposable
    {
        // constants (used internally only)
        private const int CURLOPTTYPE_OBJECTPOINT = 10000;
        private const int CURLOPTTYPE_FUNCTIONPOINT = 20000;
        private const int CURLOPTTYPE_OFF_T = 30000;
        private const int CURLINFO_STRING = 0x100000;
        private const int CURLINFO_LONG = 0x200000;
        private const int CURLINFO_DOUBLE = 0x300000;
        private const int CURLINFO_SLIST = 0x400000;
#if USE_LIBCURLSHIM
        private readonly IntPtr _pMyStrings;
#endif
        private bool _autoReferer;
        private int _bufferSize;
        private string _caInfo;
        private string _caPath;
        private CurlClosePolicy _closePolicy;
        private int _connectTimeout;
        private string _cookie;
        private string _cookieFile;
        private string _cookieJar;
        private bool _cookieSession;
        private CurlShare _curlShare;
        private string _customRequest;
        private Object _debugData;
        private int _dnsCacheTimeout;
        private bool _dnsUseGlobalCache;
        private string _egdSocket;
        private string _encoding;
        private string _errorBuffer;
        private bool _failOnError;
        private bool _filetime;
        private bool _followLocation;
        private bool _forbidReuse;
        private bool _freshConnect;
        private string _ftpAccount;
        private bool _ftpAppend;
        private CurlFtpAuth _ftpAuth;
        private bool _ftpCreateMissingDirs;
        private bool _ftpListOnly;
        private string _ftpPort;
        private int _ftpResponseTimeout;
        private bool _ftpSkipPasvIp;
        private CurlFtpSsl _ftpSsl;
        private bool _ftpUseEprt;
        private bool _ftpUseEpsv;
        private GCHandle _hThis;
        private Object _headerData;
        private CurlHttpAuth _httpAuth;
        private bool _httpGet;
        private CurlHttpMultiPartForm _httpMultiPartForm;
        private bool _httpProxyTunnel;
        private CurlHttpVersion _httpVersion;
        private bool _ignoreContentLength;
        private long _infileSize;
        private string _interface;
        private Object _ioctlData;
        private string _krb4Level;
        private CurlCode _lastErrorCode;
        private string _lastErrorDescription;
        private int _lowSpeedLimit;
        private int _lowSpeedTime;
        private int _maxConnects;
        private long _maxFileSize;
        private int _maxRedirs;
        private string _netRcFile;
        private bool _noBody;
        private bool _noProgress;
        private bool _noSignal;
        private IntPtr _pCurl;
#if USE_LIBCURLSHIM
        private NativeMethods._ShimDebugCallback _pcbDebug;
        private NativeMethods._ShimHeaderCallback _pcbHeader;
        private NativeMethods._ShimIoctlCallback _pcbIoctl;
        private NativeMethods._ShimProgressCallback _pcbProgress;
        private NativeMethods._ShimReadCallback _pcbRead;
        private NativeMethods._ShimSslCtxCallback _pcbSslCtx;
        private NativeMethods._ShimWriteCallback _pcbWrite;
        private IntPtr _ptrThis;
#else
        private NativeMethods._CurlGenericCallback _pcbWrite;
        private NativeMethods._CurlGenericCallback _pcbRead;
        private NativeMethods._CurlGenericCallback _pcbHeader;
        private NativeMethods._CurlDebugCallback _pcbDebug;
        private NativeMethods._CurlIoctlCallback _pcbIoctl;
        private NativeMethods._CurlProgressCallback _pcbProgress;
        private NativeMethods._CurlSslCtxCallback _pcbSslCtx;
#endif
        private CurlDebugCallback _pfCurlDebug;
        private CurlHeaderCallback _pfCurlHeader;
        private CurlIoctlCallback _pfCurlIoctl;
        private CurlProgressCallback _pfCurlProgress;
        private CurlReadCallback _pfCurlRead;
        private CurlSslContextCallback _pfCurlSslContext;
        private CurlWriteCallback _pfCurlWrite;
        private int _port;
        private bool _post;
        private int _postFieldSize;
        private string _postFields;
        private Object _privateData;
        private Object _progressData;
        private string _proxy;
        private int _proxyPort;
        private string _proxyUserPwd;
        private bool _put;
        private string _randomFile;
        private string _range;
        private Object _readData;
        private string _referer;
        private int _resumeFrom;
        private string _sourceUrl;
        private string _sslCert;
        private string _sslCertPasswd;
        private string _sslCipherList;
        private Object _sslContextData;
        private string _sslEngine;
        private bool _sslEngineDefault;
        private string _sslKey;
        private string _sslKeyPasswd;
        private bool _sslVerifyPeer;
        private bool _tcpNoDelay;
        private int _timeValue;
        private int _timeout;
        private bool _transferText;
        private bool _unrestrictedAuth;
        private bool _upload;
        private string _url;
        private string _userAgent;
        private string _userPwd;
        private bool _verbose;
        private Object _writeData;
        private string _writeInfo;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        ///     This is thrown
        ///     if <see cref="Curl" /> hasn't bee properly initialized.
        /// </exception>
        /// <exception cref="System.NullReferenceException">
        ///     This is thrown if
        ///     the native <c>CURL*</c> handle wasn't created successfully.
        /// </exception>
        public CurlEasy()
        {
            Curl.EnsureCurl();
            _pCurl = NativeMethods.curl_easy_init();
            ensureHandle();
            NativeMethods.curl_easy_setopt(_pCurl, CurlOption.NoProgress, IntPtr.Zero);
#if USE_LIBCURLSHIM
            _pMyStrings = NativeMethods.curl_shim_alloc_strings();
#endif
            resetPrivateVariables();
            installDelegates();
        }

        private CurlEasy(CurlEasy from)
        {
            _pCurl = NativeMethods.curl_easy_duphandle(from._pCurl);
            ensureHandle();
#if USE_LIBCURLSHIM
            _pMyStrings = NativeMethods.curl_shim_alloc_strings();
#endif
            resetPrivateVariables();
            installDelegates();
        }

        public object Private
        {
            get { return _privateData; }
            set { _privateData = value; }
        }

        public object WriteData
        {
            get { return _writeData; }
            set
            {
                _writeData = value;
#if !USE_LIBCURLSHIM
                setWriteData(value);
#endif
            }
        }

#if !USE_LIBCURLSHIM
        private IntPtr _curlWriteData = IntPtr.Zero;

        /// <summary>
        ///     Object to pass to OnWriteCallback.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private CurlCode setWriteData(object data)
        {
            _curlWriteData = getHandle(data);
            return setCurlOpt(_curlWriteData, CurlOption.WriteData);
        }

        private CurlCode setCurlOpt(IntPtr data, CurlOption opt)
        {
            var retCode = NativeMethods.curl_easy_setopt(_pCurl, opt, data);
            setLastError(retCode, opt);
            return retCode;
        }

        private IntPtr _curlReadData = IntPtr.Zero;

        /// <summary>
        ///     Object to pass to OnReadCallback.
        ///     Use <see cref="getObject" /> to convert the passed IntPtr back into the object, then cast.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private CurlCode setReadData(object data)
        {
            _curlReadData = getHandle(data);
            return setCurlOpt(_curlReadData, CurlOption.ReadData);
        }

        private IntPtr _curlProgressData = IntPtr.Zero;

        /// <summary>
        ///     Object to pass to OnProgressCallback.
        ///     Use <see cref="getObject" /> to convert the passed IntPtr back into the object, then cast.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private CurlCode setProgressData(object data)
        {
            _curlProgressData = getHandle(data);
            return setCurlOpt(_curlProgressData, CurlOption.ProgressData);
        }

        private IntPtr _curlHeaderData = IntPtr.Zero;

        /// <summary>
        ///     Object to pass to OnHeaderCallback.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private CurlCode setHeaderData(object data)
        {
            _curlHeaderData = getHandle(data);
            return setCurlOpt(_curlHeaderData, CurlOption.HeaderData);
        }

        private IntPtr _curlDebugData = IntPtr.Zero;

        /// <summary>
        ///     Object to pass to OnDebugCallback.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private CurlCode setDebugData(object data)
        {
            _curlDebugData = getHandle(data);
            return setCurlOpt(_curlDebugData, CurlOption.DebugData);
        }

        private IntPtr _curlSslCtxData = IntPtr.Zero;

        /// <summary>
        ///     Object to pass to OnSslCtxCallback.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private CurlCode setSslCtxData(object data)
        {
            _curlSslCtxData = getHandle(data);
            return setCurlOpt(_curlSslCtxData, CurlOption.SslCtxData);
        }

        private IntPtr _curlIoctlData = IntPtr.Zero;

        /// <summary>
        ///     Object to pass to OnIoctlCallback.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private CurlCode setIoctlData(object data)
        {
            _curlIoctlData = getHandle(data);
            return setCurlOpt(_curlIoctlData, CurlOption.IoctlData);
        }
#endif

        public object ReadData
        {
            get { return _readData; }
            set
            {
                _readData = value;
#if !USE_LIBCURLSHIM
                setReadData(value);
#endif
            }
        }

        public object ProgressData
        {
            get { return _progressData; }
            set
            {
                _progressData = value;
#if !USE_LIBCURLSHIM
                setProgressData(value);
#endif
            }
        }

        public object DebugData
        {
            get { return _debugData; }
            set
            {
                _debugData = value;
#if !USE_LIBCURLSHIM
                setDebugData(value);
#endif
            }
        }

        public object HeaderData
        {
            get { return _headerData; }
            set
            {
                _headerData = value;
#if !USE_LIBCURLSHIM
                setHeaderData(value);
#endif
            }
        }

        public object SslCtxData
        {
            get { return _sslContextData; }
            set
            {
                _sslContextData = value;
#if !USE_LIBCURLSHIM
                setSslCtxData(value);
#endif
            }
        }

        public object IoctlData
        {
            get { return _ioctlData; }
            set
            {
                _ioctlData = value;
#if !USE_LIBCURLSHIM
                setIoctlData(value);
#endif
            }
        }

        public CurlShare Share
        {
            get { return _curlShare; }
            set
            {
                _curlShare = value;
                setShareObject();
            }
        }

        public CurlHttpMultiPartForm HttpPost
        {
            get { return _httpMultiPartForm; }
            set
            {
                _httpMultiPartForm = value;
                setMultiPartFormObject();
            }
        }

        public CurlSlist HttpHeader
        {
            set { setSlistObject(CurlOption.HttpHeader, value); }
        }

        public CurlSlist Prequote
        {
            set { setSlistObject(CurlOption.Prequote, value); }
        }

        public CurlSlist Quote
        {
            set { setSlistObject(CurlOption.Quote, value); }
        }

        public CurlSlist Postquote
        {
            set { setSlistObject(CurlOption.Postquote, value); }
        }

        public CurlSlist SourceQuote
        {
            set { setSlistObject(CurlOption.SourceQuote, value); }
        }

        public CurlSlist Http200Aliases
        {
            set { setSlistObject(CurlOption.Http200Aliases, value); }
        }

        public CurlFtpAuth FtpAuth
        {
            get { return _ftpAuth; }
            set
            {
                _ftpAuth = value;
                var l = Convert.ToInt32(value);
                setLastError(NativeMethods.curl_easy_setopt(_pCurl, CurlOption.FtpSslAuth, (IntPtr) l),
                             CurlOption.FtpSslAuth);
            }
        }

        public CurlHttpVersion HttpVersion
        {
            get { return _httpVersion; }
            set
            {
                _httpVersion = value;
                var l = Convert.ToInt32(value);
                setLastError(NativeMethods.curl_easy_setopt(_pCurl, CurlOption.HttpVersion, (IntPtr) l),
                             CurlOption.HttpVersion);
            }
        }

        public CurlHttpAuth HttpAuth
        {
            get { return _httpAuth; }
            set
            {
                _httpAuth = value;
                var l = Convert.ToInt32(value);
                setLastError(NativeMethods.curl_easy_setopt(_pCurl, CurlOption.HttpAuth, (IntPtr) l),
                             CurlOption.HttpAuth);
            }
        }

        public CurlFtpSsl FtpSsl
        {
            get { return _ftpSsl; }
            set
            {
                _ftpSsl = value;
                var l = Convert.ToInt32(value);
                setLastError(NativeMethods.curl_easy_setopt(_pCurl, CurlOption.FtpSsl, (IntPtr) l),
                             CurlOption.FtpSsl);
            }
        }

        public CurlClosePolicy ClosePolicy
        {
            get { return _closePolicy; }
            set
            {
                _closePolicy = value;
                var l = Convert.ToInt32(value);
                setLastError(NativeMethods.curl_easy_setopt(_pCurl, CurlOption.ClosePolicy, (IntPtr) l),
                             CurlOption.ClosePolicy);
            }
        }

        public CurlWriteCallback WriteFunction
        {
            get { return _pfCurlWrite; }
            set { setFunctionOptions(CurlOption.WriteFunction, value); }
        }

        public CurlReadCallback ReadFunction
        {
            get { return _pfCurlRead; }
            set { setFunctionOptions(CurlOption.ReadFunction, value); }
        }

        public CurlHeaderCallback HeaderFunction
        {
            get { return _pfCurlHeader; }
            set { setFunctionOptions(CurlOption.HeaderFunction, value); }
        }

        public CurlDebugCallback DebugFunction
        {
            get { return _pfCurlDebug; }
            set { setFunctionOptions(CurlOption.DebugFunction, value); }
        }

        public CurlProgressCallback ProgressFunction
        {
            get { return _pfCurlProgress; }
            set { setFunctionOptions(CurlOption.ProgressFunction, value); }
        }

        public CurlIoctlCallback IoctlFunction
        {
            get { return _pfCurlIoctl; }
            set { setFunctionOptions(CurlOption.IoctlFunction, value); }
        }

        public CurlSslContextCallback SslContextFunction
        {
            get { return _pfCurlSslContext; }
            set { setFunctionOptions(CurlOption.SslCtxFunction, value); }
        }

        public string LastErrorDescription
        {
            get { return _lastErrorDescription; }
        }

        public bool NoProgress
        {
            get { return _noProgress; }
            set { setBoolOption(CurlOption.NoProgress, ref _noProgress, value); }
        }

        public bool NoBody
        {
            get { return _noBody; }
            set { setBoolOption(CurlOption.NoBody, ref _noBody, value); }
        }

        public bool FailOnError
        {
            get { return _failOnError; }
            set { setBoolOption(CurlOption.FailOnError, ref _failOnError, value); }
        }

        public bool Upload
        {
            get { return _upload; }
            set { setBoolOption(CurlOption.Upload, ref _upload, value); }
        }

        public bool Post
        {
            get { return _post; }
            set { setBoolOption(CurlOption.Post, ref _post, value); }
        }

        public bool FtpListOnly
        {
            get { return _ftpListOnly; }
            set { setBoolOption(CurlOption.FtpListOnly, ref _ftpListOnly, value); }
        }

        public bool FtpAppend
        {
            get { return _ftpAppend; }
            set { setBoolOption(CurlOption.FtpAppend, ref _ftpAppend, value); }
        }

        public bool FollowLocation
        {
            get { return _followLocation; }
            set { setBoolOption(CurlOption.FollowLocation, ref _followLocation, value); }
        }

        public bool TransferText
        {
            get { return _transferText; }
            set { setBoolOption(CurlOption.TransferText, ref _transferText, value); }
        }

        public bool Put
        {
            get { return _put; }
            set { setBoolOption(CurlOption.Put, ref _put, value); }
        }

        public bool HttpProxyTunnel
        {
            get { return _httpProxyTunnel; }
            set { setBoolOption(CurlOption.HttpProxyTunnel, ref _httpProxyTunnel, value); }
        }

        public bool SslVerifyPeer
        {
            get { return _sslVerifyPeer; }
            set { setBoolOption(CurlOption.SslVerifyPeer, ref _sslVerifyPeer, value); }
        }

        public bool FreshConnect
        {
            get { return _freshConnect; }
            set { setBoolOption(CurlOption.FreshConnect, ref _freshConnect, value); }
        }

        public bool ForbidReuse
        {
            get { return _forbidReuse; }
            set { setBoolOption(CurlOption.ForbidReuse, ref _forbidReuse, value); }
        }

        public bool HttpGet
        {
            get { return _httpGet; }
            set { setBoolOption(CurlOption.HttpGet, ref _httpGet, value); }
        }

        public bool FtpUseEpsv
        {
            get { return _ftpUseEpsv; }
            set { setBoolOption(CurlOption.FtpUseEpsv, ref _ftpUseEpsv, value); }
        }

        public bool Filetime
        {
            get { return _filetime; }
            set { setBoolOption(CurlOption.Filetime, ref _filetime, value); }
        }

        public bool UnrestrictedAuth
        {
            get { return _unrestrictedAuth; }
            set { setBoolOption(CurlOption.UnrestrictedAuth, ref _unrestrictedAuth, value); }
        }

        public bool FtpUseEprt
        {
            get { return _ftpUseEprt; }
            set { setBoolOption(CurlOption.FtpUseEprt, ref _ftpUseEprt, value); }
        }

        public bool AutoReferer
        {
            get { return _autoReferer; }
            set { setBoolOption(CurlOption.AutoReferer, ref _autoReferer, value); }
        }

        public bool CookieSession
        {
            get { return _cookieSession; }
            set { setBoolOption(CurlOption.CookieSession, ref _cookieSession, value); }
        }

        public bool SslEngineDefault
        {
            get { return _sslEngineDefault; }
            set { setBoolOption(CurlOption.SslEngineDefault, ref _sslEngineDefault, value); }
        }

        public bool DnsUseGlobalCache
        {
            get { return _dnsUseGlobalCache; }
            set { setBoolOption(CurlOption.DnsUseGlobalCache, ref _dnsUseGlobalCache, value); }
        }

        public bool NoSignal
        {
            get { return _noSignal; }
            set { setBoolOption(CurlOption.NoSignal, ref _noSignal, value); }
        }

        public bool FtpCreateMissingDirs
        {
            get { return _ftpCreateMissingDirs; }
            set { setBoolOption(CurlOption.FtpCreateMissingDirs, ref _ftpCreateMissingDirs, value); }
        }

        public bool TcpNoDelay
        {
            get { return _tcpNoDelay; }
            set { setBoolOption(CurlOption.TcpNoDelay, ref _tcpNoDelay, value); }
        }

        public bool IgnoreContentLength
        {
            get { return _ignoreContentLength; }
            set { setBoolOption(CurlOption.IgnoreContentLength, ref _ignoreContentLength, value); }
        }

        public bool FtpSkipPasvIp
        {
            get { return _ftpSkipPasvIp; }
            set { setBoolOption(CurlOption.FtpSkipPasvIp, ref _ftpSkipPasvIp, value); }
        }

        public int Port
        {
            get { return _port; }
            set { setIntOption(CurlOption.Port, ref _port, value); }
        }

        public int Timeout
        {
            get { return _timeout; }
            set { setIntOption(CurlOption.Timeout, ref _timeout, value); }
        }

        public int LowSpeedLimit
        {
            get { return _lowSpeedLimit; }
            set { setIntOption(CurlOption.LowSpeedLimit, ref _lowSpeedLimit, value); }
        }

        public int LowSpeedTime
        {
            get { return _lowSpeedTime; }
            set { setIntOption(CurlOption.LowSpeedTime, ref _lowSpeedTime, value); }
        }

        public int ResumeFrom
        {
            get { return _resumeFrom; }
            set { setIntOption(CurlOption.ResumeFrom, ref _resumeFrom, value); }
        }

        public int TimeValue
        {
            get { return _timeValue; }
            set { setIntOption(CurlOption.TimeValue, ref _timeValue, value); }
        }

        public int ProxyPort
        {
            get { return _proxyPort; }
            set { setIntOption(CurlOption.ProxyPort, ref _proxyPort, value); }
        }

        public int PostFieldSize
        {
            get { return _postFieldSize; }
            set { setIntOption(CurlOption.PostFieldSize, ref _postFieldSize, value); }
        }

        public int MaxRedirs
        {
            get { return _maxRedirs; }
            set { setIntOption(CurlOption.MaxRedirs, ref _maxRedirs, value); }
        }

        public int MaxConnects
        {
            get { return _maxConnects; }
            set { setIntOption(CurlOption.MaxConnects, ref _maxConnects, value); }
        }

        public int ConnectTimeout
        {
            get { return _connectTimeout; }
            set { setIntOption(CurlOption.ConnectTimeout, ref _connectTimeout, value); }
        }

        public int BufferSize
        {
            get { return _bufferSize; }
            set { setIntOption(CurlOption.BufferSize, ref _bufferSize, value); }
        }

        public int DnsCacheTimeout
        {
            get { return _dnsCacheTimeout; }
            set { setIntOption(CurlOption.DnsCacheTimeout, ref _dnsCacheTimeout, value); }
        }

        public int FtpResponseTimeout
        {
            get { return _ftpResponseTimeout; }
            set { setIntOption(CurlOption.FtpResponseTimeout, ref _ftpResponseTimeout, value); }
        }

        public long InfileSize
        {
            get { return _infileSize; }
            set { setLongOption(CurlOption.InfileSize, ref _infileSize, value); }
        }

        public long MaxFileSize
        {
            get { return _maxFileSize; }
            set { setLongOption(CurlOption.MaxFileSize, ref _maxFileSize, value); }
        }

        public string Url
        {
            get { return _url; }
            set { setStringOption(CurlOption.Url, out _url, value); }
        }

        public string Proxy
        {
            get { return _proxy; }
            set { setStringOption(CurlOption.Proxy, out _proxy, value); }
        }

        public string UserPwd
        {
            get { return _userPwd; }
            set { setStringOption(CurlOption.UserPwd, out _userPwd, value); }
        }

        public string ProxyUserPwd
        {
            get { return _proxyUserPwd; }
            set { setStringOption(CurlOption.ProxyUserPwd, out _proxyUserPwd, value); }
        }

        public string Range
        {
            get { return _range; }
            set { setStringOption(CurlOption.Range, out _range, value); }
        }

        public string PostFields
        {
            get { return _postFields; }
            set { setStringOption(CurlOption.PostFields, out _postFields, value); }
        }

        public string Referer
        {
            get { return _referer; }
            set { setStringOption(CurlOption.Referer, out _referer, value); }
        }

        public string FtpPort
        {
            get { return _ftpPort; }
            set { setStringOption(CurlOption.FtpPort, out _ftpPort, value); }
        }

        public string UserAgent
        {
            get { return _userAgent; }
            set { setStringOption(CurlOption.UserAgent, out _userAgent, value); }
        }

        public string Cookie
        {
            get { return _cookie; }
            set { setStringOption(CurlOption.Cookie, out _cookie, value); }
        }

        public string SslCert
        {
            get { return _sslCert; }
            set { setStringOption(CurlOption.SslCert, out _sslCert, value); }
        }

        public string SslCertPasswd
        {
            get { return _sslCertPasswd; }
            set { setStringOption(CurlOption.SslCertPasswd, out _sslCertPasswd, value); }
        }

        public string CustomRequest
        {
            get { return _customRequest; }
            set { setStringOption(CurlOption.CustomRequest, out _customRequest, value); }
        }

        public string Interface
        {
            get { return _interface; }
            set { setStringOption(CurlOption.Interface, out _interface, value); }
        }

        public string Encoding
        {
            get { return _encoding; }
            set { setStringOption(CurlOption.Encoding, out _encoding, value); }
        }

        public string Krb4Level
        {
            get { return _krb4Level; }
            set { setStringOption(CurlOption.Krb4Level, out _krb4Level, value); }
        }

        public string CaInfo
        {
            get { return _caInfo; }
            set { setStringOption(CurlOption.CaInfo, out _caInfo, value); }
        }

        public string RandomFile
        {
            get { return _randomFile; }
            set { setStringOption(CurlOption.RandomFile, out _randomFile, value); }
        }

        public string EgdSocket
        {
            get { return _egdSocket; }
            set { setStringOption(CurlOption.EgdSocket, out _egdSocket, value); }
        }

        public string CookieJar
        {
            get { return _cookieJar; }
            set { setStringOption(CurlOption.CookieJar, out _cookieJar, value); }
        }

        public string CookieFile
        {
            get { return _cookieFile; }
            set { setStringOption(CurlOption.CookieFile, out _cookieFile, value); }
        }

        public string SslCipherList
        {
            get { return _sslCipherList; }
            set { setStringOption(CurlOption.SslCipherList, out _sslCipherList, value); }
        }

        public string WriteInfo
        {
            get { return _writeInfo; }
            set { setStringOption(CurlOption.WriteInfo, out _writeInfo, value); }
        }

        public string CaPath
        {
            get { return _caPath; }
            set { setStringOption(CurlOption.CaPath, out _caPath, value); }
        }

        public string SslKey
        {
            get { return _sslKey; }
            set { setStringOption(CurlOption.SslKey, out _sslKey, value); }
        }

        public string SslEngine
        {
            get { return _sslEngine; }
            set { setStringOption(CurlOption.SslEngine, out _sslEngine, value); }
        }

        public string SslKeyPasswd
        {
            get { return _sslKeyPasswd; }
            set { setStringOption(CurlOption.SslKeyPasswd, out _sslKeyPasswd, value); }
        }

        public string ErrorBuffer
        {
            get { return _errorBuffer; }
            set { setStringOption(CurlOption.ErrorBuffer, out _errorBuffer, value); }
        }

        public string NetRcFile
        {
            get { return _netRcFile; }
            set { setStringOption(CurlOption.NetRcFile, out _netRcFile, value); }
        }

        public string FtpAccount
        {
            get { return _ftpAccount; }
            set { setStringOption(CurlOption.FtpAccount, out _ftpAccount, value); }
        }

        public string SourceUrl
        {
            get { return _sourceUrl; }
            set { setStringOption(CurlOption.SourceUrl, out _sourceUrl, value); }
        }

        public string EffectiveUrl
        {
            get { return getStringInfo(CurlInfo.EffectiveUrl); }
        }

        public string ContentType
        {
            get { return getStringInfo(CurlInfo.ContentType); }
        }

        public int ResponseCode
        {
            get { return getIntInfo(CurlInfo.ResponseCode); }
        }

        public int HeaderSize
        {
            get { return getIntInfo(CurlInfo.HeaderSize); }
        }

        public int RequestSize
        {
            get { return getIntInfo(CurlInfo.RequestSize); }
        }

        public bool Verbose
        {
            get { return _verbose; }
            set { setBoolOption(CurlOption.Verbose, ref _verbose, value); }
        }

        public int HttpAuthAvail
        {
            get { return getIntInfo(CurlInfo.HttpAuthAvail); }
        }

        public int SslVerifyResult
        {
            get { return getIntInfo(CurlInfo.SslVerifyResult); }
        }

        public int RedirectCount
        {
            get { return getIntInfo(CurlInfo.RedirectCount); }
        }

        public int ProxyAuthAvail
        {
            get { return getIntInfo(CurlInfo.ProxyAuthAvail); }
        }

        public int OsErrno
        {
            get { return getIntInfo(CurlInfo.OsErrno); }
        }

        public int NumConnects
        {
            get { return getIntInfo(CurlInfo.NumConnects); }
        }

        public int HttpConnectCode
        {
            get { return getIntInfo(CurlInfo.HttpConnectCode); }
        }

        public DateTime FileTime
        {
            get { return getDateTimeInfo(CurlInfo.Filetime); }
        }

        public double TotalTime
        {
            get { return getDoubleInfo(CurlInfo.TotalTime); }
        }

        public double NameLookupTime
        {
            get { return getDoubleInfo(CurlInfo.NameLookupTime); }
        }

        public double ConnectTime
        {
            get { return getDoubleInfo(CurlInfo.ConnectTime); }
        }

        public double PreTransferTime
        {
            get { return getDoubleInfo(CurlInfo.PreTransferTime); }
        }

        public double SizeUpload
        {
            get { return getDoubleInfo(CurlInfo.SizeUpload); }
        }

        public double SizeDownload
        {
            get { return getDoubleInfo(CurlInfo.SizeDownload); }
        }

        public double SpeedDownload
        {
            get { return getDoubleInfo(CurlInfo.SpeedDownload); }
        }

        public double SpeedUpload
        {
            get { return getDoubleInfo(CurlInfo.SpeedUpload); }
        }

        public double StartTransferTime
        {
            get { return getDoubleInfo(CurlInfo.StartTransferTime); }
        }

        public double ContentLengthDownload
        {
            get { return getDoubleInfo(CurlInfo.ContentLengthDownload); }
        }

        public double ContentLengthUpload
        {
            get { return getDoubleInfo(CurlInfo.ContentLengthUpload); }
        }

        public double RedirectTime
        {
            get { return getDoubleInfo(CurlInfo.RedirectTime); }
        }

        public CurlSlist SslEngines
        {
            get { return getSlistInfo(CurlInfo.SslEngines); }
        }

        public CurlCode LastErrorCode
        {
            get { return _lastErrorCode; }
        }

        /// <summary>
        ///     Cleanup unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void resetPrivateVariables()
        {
            _privateData = null;

            _pfCurlWrite = null;
            _writeData = null;

            _pfCurlRead = null;
            _readData = null;

            _pfCurlProgress = null;
            _progressData = null;

            _pfCurlDebug = null;
            _debugData = null;

            _pfCurlHeader = null;
            _headerData = null;

            _pfCurlSslContext = null;
            _sslContextData = null;

            _pfCurlIoctl = null;
            _ioctlData = null;
        }

        private CurlCode setMultiPartFormObject()
        {
            ensureHandle();
            var retCode = _httpMultiPartForm == null
                ? CurlCode.BadFunctionArgument
                : NativeMethods.curl_easy_setopt(_pCurl, CurlOption.HttpPost, _httpMultiPartForm.GetHandle());
            setLastError(retCode, CurlOption.HttpPost);
            return retCode;
        }

        private CurlCode setSlistObject(CurlOption option, CurlSlist slist)
        {
            ensureHandle();
            var retCode = NativeMethods.curl_easy_setopt(_pCurl, option, slist == null ? IntPtr.Zero : slist.Handle);
            setLastError(retCode, option);
            return retCode;
        }

        private CurlCode setShareObject()
        {
            ensureHandle();
            var retCode = _curlShare == null
                ? CurlCode.BadFunctionArgument
                : NativeMethods.curl_easy_setopt(_pCurl, CurlOption.Share, _curlShare.GetHandle());
            setLastError(retCode, CurlOption.Share);
            return retCode;
        }

        /// <summary>
        ///     Handles return code from all calls to 'curl_easy_xxx' functions.
        /// </summary>
        private void setLastError(CurlCode code, CurlOption opt)
        {
            if (LastErrorCode == CurlCode.Ok && code != CurlCode.Ok)
            {
                _lastErrorCode = code;
                _lastErrorDescription = string.Format("Error: {0} setting option {1}", StrError(code), opt);
            }
        }

        /// <summary>
        ///     Handles return code from all calls to 'curl_easy_xxx' functions.
        /// </summary>
        private void setLastError(CurlCode code, CurlInfo info)
        {
            if (LastErrorCode == CurlCode.Ok && code != CurlCode.Ok)
            {
                _lastErrorCode = code;
                _lastErrorDescription = string.Format("Error: {0} getting info {1}", StrError(code), info);
            }
        }

        /// <summary>
        ///     Destructor
        /// </summary>
        ~CurlEasy()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            lock (this)
            {
                if (disposing)
                {
                    // free managed resources
                    //if (managedResource != null)
                    //{
                    // managedResource.Dispose();
                    // managedResource = null;
                    //}
                }

                // free native resources if there are any.
                if (_pCurl != IntPtr.Zero)
                {
#if USE_LIBCURLSHIM
                    NativeMethods.curl_shim_cleanup_delegates(_ptrThis);
                    NativeMethods.curl_shim_free_strings(_pMyStrings);
#else
                    freeHandle(ref _curlWriteData);
                    freeHandle(ref _curlReadData);
                    freeHandle(ref _curlDebugData);
                    freeHandle(ref _curlProgressData);
                    freeHandle(ref _curlHeaderData);
                    freeHandle(ref _curlIoctlData);
                    freeHandle(ref _curlSslCtxData);
#endif
                    NativeMethods.curl_easy_cleanup(_pCurl);

                    _hThis.Free();
                    _pCurl = IntPtr.Zero;
                }
            }
        }

        private void ensureHandle()
        {
            if (_pCurl == IntPtr.Zero)
                throw new NullReferenceException("No internal easy handle");
        }

        internal IntPtr Handle
        {
            get
            {
                ensureHandle();
                return _pCurl;
            }
        }

        /// <summary>
        ///     Reset the internal cURL handle.
        /// </summary>
        /// <exception cref="System.NullReferenceException">
        ///     This is thrown if
        ///     the native <c>CURL*</c> handle wasn't created successfully.
        /// </exception>
        public void Reset()
        {
            ensureHandle();
            NativeMethods.curl_easy_reset(_pCurl);
        }

        private void setBoolOption(CurlOption option, ref bool field, bool value)
        {
            ensureHandle();
            field = value;
            setLastError(NativeMethods.curl_easy_setopt(_pCurl, option, (IntPtr) Convert.ToInt32(value)), option);
        }

        private void setIntOption(CurlOption option, ref int field, int value)
        {
            ensureHandle();
            field = value;
            setLastError(NativeMethods.curl_easy_setopt(_pCurl, option, (IntPtr) value), option);
        }

        private void setLongOption(CurlOption option, ref long field, long value)
        {
            ensureHandle();
            field = value;
            setLastError(NativeMethods.curl_easy_setopt(_pCurl, option, (IntPtr) value), option);
        }

        private void setStringOption(CurlOption option, out string field, string value)
        {
            setStringOption(option, value);
            field = value;
        }

        private void setStringOption(CurlOption option, string value)
        {
            ensureHandle();
            if (string.IsNullOrEmpty(value))
            {
                setLastError(NativeMethods.curl_easy_setopt(_pCurl, option, IntPtr.Zero), option);
            }
            else
            {
#if USE_LIBCURLSHIM
                var pCurlStr = NativeMethods.curl_shim_add_string(_pMyStrings, value);
                if (pCurlStr != IntPtr.Zero)
                    setLastError(NativeMethods.curl_easy_setopt(_pCurl, option, pCurlStr), option);
#else
                // convert the string to a null-terminated one
                var buffer = System.Text.Encoding.UTF8.GetBytes(value + "\0");
                setLastError(NativeMethods.curl_easy_setopt(_pCurl, option, buffer), option);
#endif
            }
        }

        /// <summary>
        ///     Set options for this object. See the <c>EasyGet</c> sample for
        ///     basic usage.
        /// </summary>
        /// <param name="option">This should be a valid <see cref="CurlOption" />.</param>
        /// <param name="parameter">
        ///     This should be a parameter of a varying
        ///     type based on the value of the <c>option</c> parameter.
        /// </param>
        /// <exception cref="System.NullReferenceException">
        ///     This is thrown if
        ///     the native <c>CURL*</c> handle wasn't created successfully.
        /// </exception>
        /// <returns>
        ///     A <see cref="CurlCode" />, typically obtained from
        ///     <c>cURL</c> internally, but sometimes a
        ///     <see cref="CurlCode.BadFunctionArgument" />
        ///     will be returned if the type of value of <c>parameter</c> is invalid.
        /// </returns>
        public CurlCode SetOpt(CurlOption option, Object parameter)
        {
            ensureHandle();
            var retCode = CurlCode.Ok;

            // numeric cases
            if ((int) option < CURLOPTTYPE_OBJECTPOINT)
            {
                var i = 0;
                if (option == CurlOption.DnsUseGlobalCache || option == CurlOption.SourcePort)
                {
                    return CurlCode.BadFunctionArgument;
                }

                if (option == CurlOption.TimeValue)
                {
                    // unboxing may throw class cast exception
                    var d = (DateTime) parameter;
                    var startTime = new DateTime(1970, 1, 1);
                    var currTime = new TimeSpan(DateTime.Now.Ticks - startTime.Ticks);
                    i = Convert.ToInt32(currTime.TotalSeconds);
                }
                else
                    i = Convert.ToInt32(parameter);

                retCode = NativeMethods.curl_easy_setopt(_pCurl, option, (IntPtr) i);
            }

                // object cases: the majority
            else if ((int) option < CURLOPTTYPE_FUNCTIONPOINT)
            {
                return setObjectOptions(option, parameter);
            }
                // FUNCTIONPOINT args, for delegates
            else if ((int) option < CURLOPTTYPE_OFF_T)
            {
                return setFunctionOptions(option, parameter);
            }
                // otherwise, it's one of those 64-bit off_t guys!
            else
            {
                var i = Convert.ToInt64(parameter);
                retCode = NativeMethods.curl_easy_setopt(_pCurl, option, i);
            }

            return retCode;
        }

        private CurlCode setObjectOptions(CurlOption option, object parameter)
        {
            var retCode = CurlCode.Ok;
            switch (option)
            {
                    // various data items
                case CurlOption.Private:
                    _privateData = parameter;
                    break;
                case CurlOption.WriteData:
                    _writeData = parameter;
                    break;
                case CurlOption.ReadData:
                    _readData = parameter;
                    break;
                case CurlOption.ProgressData:
                    _progressData = parameter;
                    break;
                case CurlOption.DebugData:
                    _debugData = parameter;
                    break;
                case CurlOption.HeaderData:
                    _headerData = parameter;
                    break;
                case CurlOption.SslCtxData:
                    _sslContextData = parameter;
                    break;
                case CurlOption.IoctlData:
                    _ioctlData = parameter;
                    break;

                    // items that can't be set externally or
                    // obsolete items
                case CurlOption.ErrorBuffer:
                case CurlOption.Stderr:
                case CurlOption.SourceHost:
                case CurlOption.SourcePath:
                case CurlOption.PasvHost:
                    return CurlCode.BadFunctionArgument;

                    // singular case for share
                case CurlOption.Share:
                {
                    _curlShare = parameter as CurlShare;
                    retCode = setShareObject();
                    break;
                }

                    // multipart HTTP post
                case CurlOption.HttpPost:
                {
                    _httpMultiPartForm = parameter as CurlHttpMultiPartForm;
                    retCode = setMultiPartFormObject();
                    break;
                }

                    // items curl wants as a curl_slist
                case CurlOption.HttpHeader:
                case CurlOption.Prequote:
                case CurlOption.Quote:
                case CurlOption.Postquote:
                case CurlOption.SourceQuote:
                case CurlOption.TelnetOptions:
                case CurlOption.Http200Aliases:
                {
                    var slist = parameter as CurlSlist;
                    retCode = setSlistObject(option, slist);
                    break;
                }

                    // string items
                default:
                {
                    var s = parameter as string;
                    setStringOption(option, s);
                    retCode = _lastErrorCode;
                    break;
                }
            }
            return retCode;
        }

#if !USE_LIBCURLSHIM

        #region Object pinning support

        /// <summary>
        ///     Free the pinned object
        /// </summary>
        /// <param name="handle"></param>
        private void freeHandle(ref IntPtr handle)
        {
            if (handle == IntPtr.Zero)
                return;
            var handleCallback = GCHandle.FromIntPtr(handle);
            handleCallback.Free();
            handle = IntPtr.Zero;
        }

        /// <summary>
        ///     Pin the object in memory so the C function can find it
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private static IntPtr getHandle(object obj)
        {
            if (obj == null)
                return IntPtr.Zero;
            return GCHandle.ToIntPtr(GCHandle.Alloc(obj, GCHandleType.Pinned));
        }

        /// <summary>
        ///     Returns the object passed to a Set...Data function.
        ///     Cast back to the original object.
        /// </summary>
        /// <param name="userdata"></param>
        /// <returns></returns>
        private static object getObject(IntPtr userdata)
        {
            if (userdata == IntPtr.Zero)
                return null;
            var handle = GCHandle.FromIntPtr(userdata);
            return handle.Target;
        }

        #endregion

#endif

        private CurlCode setFunctionOptions(CurlOption option, object pfn)
        {
            switch (option)
            {
                case CurlOption.WriteFunction:
                {
                    var wf = pfn as CurlWriteCallback;
                    if (wf == null)
                        return CurlCode.BadFunctionArgument;
                    _pfCurlWrite = wf;
                    break;
                }

                case CurlOption.ReadFunction:
                {
                    var rf = pfn as CurlReadCallback;
                    if (rf == null)
                        return CurlCode.BadFunctionArgument;
                    _pfCurlRead = rf;
                    break;
                }

                case CurlOption.ProgressFunction:
                {
                    var pf = pfn as CurlProgressCallback;
                    if (pf == null)
                        return CurlCode.BadFunctionArgument;
                    _pfCurlProgress = pf;
                    break;
                }

                case CurlOption.DebugFunction:
                {
                    var pd = pfn as CurlDebugCallback;
                    if (pd == null)
                        return CurlCode.BadFunctionArgument;
                    _pfCurlDebug = pd;
                    break;
                }

                case CurlOption.HeaderFunction:
                {
                    var hf = pfn as CurlHeaderCallback;
                    if (hf == null)
                        return CurlCode.BadFunctionArgument;
                    _pfCurlHeader = hf;
                    break;
                }

                case CurlOption.SslCtxFunction:
                {
                    var sf = pfn as CurlSslContextCallback;
                    if (sf == null)
                        return CurlCode.BadFunctionArgument;
                    _pfCurlSslContext = sf;
                    break;
                }

                case CurlOption.IoctlFunction:
                {
                    var iof = pfn as CurlIoctlCallback;
                    if (iof == null)
                        return CurlCode.BadFunctionArgument;
                    _pfCurlIoctl = iof;
                    break;
                }

                default:
                    return CurlCode.BadFunctionArgument;
            }
            return CurlCode.Ok;
        }

        /// <summary>
        ///     Perform a transfer.
        /// </summary>
        /// <exception cref="System.NullReferenceException">
        ///     This is thrown if
        ///     the native <c>CURL*</c> handle wasn't created successfully.
        /// </exception>
        /// <returns>
        ///     The <see cref="CurlCode" /> obtained from the internal
        ///     call to <c>curl_easy_perform()</c>.
        /// </returns>
        public CurlCode Perform()
        {
            ensureHandle();
            return NativeMethods.curl_easy_perform(_pCurl);
        }

        /// <summary>
        ///     Clone an CurlEasy object.
        /// </summary>
        /// <exception cref="System.NullReferenceException">
        ///     This is thrown if
        ///     the native <c>CURL*</c> handle wasn't created successfully.
        /// </exception>
        /// <returns>A cloned <c>CurlEasy</c> object.</returns>
        public CurlEasy Clone()
        {
            return new CurlEasy(this);
        }

        /// <summary>
        ///     Get a string description of an error code.
        /// </summary>
        /// <param name="code">Error code.</param>
        /// <returns>String description of the error code.</returns>
        public String StrError(CurlCode code)
        {
            return Marshal.PtrToStringAnsi(NativeMethods.curl_easy_strerror(code));
        }

        /// <summary>
        ///     Extract information from a cURL handle.
        /// </summary>
        /// <param name="info">
        ///     One of the values in the
        ///     <see cref="CurlInfo" /> enumeration.
        /// </param>
        /// <param name="objInfo">
        ///     Reference to an object into which the
        ///     value specified by <c>info</c> is written.
        /// </param>
        /// <returns>
        ///     The <see cref="CurlCode" /> obtained from the internal
        ///     call to <c>curl_easy_getinfo()</c>.
        /// </returns>
        /// <exception cref="System.NullReferenceException">
        ///     This is thrown if
        ///     the native <c>CURL*</c> handle wasn't created successfully.
        /// </exception>
        public CurlCode GetInfo(CurlInfo info, ref Object objInfo)
        {
            ensureHandle();
            var retCode = CurlCode.Ok;
            var ptr = IntPtr.Zero;

            if ((int) info < CURLINFO_STRING)
                return CurlCode.BadFunctionArgument;

            // trickery for filetime
            if (info == CurlInfo.Filetime)
                return CurlCode.BadFunctionArgument;

            // private data
            if (info == CurlInfo.Private)
            {
                objInfo = _privateData;
                return retCode;
            }

            // string case
            if ((int) info < CURLINFO_LONG)
            {
                retCode = NativeMethods.curl_easy_getinfo(_pCurl, info, ref ptr);
                if (retCode == CurlCode.Ok)
                    objInfo = Marshal.PtrToStringAnsi(ptr);
                return retCode;
            }

            // int or double: return problem
            return CurlCode.BadFunctionArgument;
        }

        private string getStringInfo(CurlInfo info)
        {
            ensureHandle();
            var ptr = IntPtr.Zero;
            var retCode = NativeMethods.curl_easy_getinfo(_pCurl, info, ref ptr);
            setLastError(retCode, info);
            return retCode == CurlCode.Ok ? Marshal.PtrToStringAnsi(ptr) : string.Empty;
        }

        private int getIntInfo(CurlInfo info)
        {
            ensureHandle();
            // ensure it's an integral type
            if ((int) info < CURLINFO_LONG || (int) info >= CURLINFO_DOUBLE)
            {
                setLastError(CurlCode.BadFunctionArgument, info);
                return -1;
            }

            var ptr = IntPtr.Zero;
            var retCode = NativeMethods.curl_easy_getinfo(_pCurl, info, ref ptr);
            setLastError(retCode, info);
            return retCode == CurlCode.Ok ? (int) ptr : -1;
        }

        private DateTime getDateTimeInfo(CurlInfo info)
        {
            var dt = new DateTime();
            setLastError(GetInfo(info, ref dt), info);
            return dt;
        }

        private double getDoubleInfo(CurlInfo info)
        {
            double val = 0;
            setLastError(GetInfo(info, ref val), info);
            return val;
        }

        private CurlSlist getSlistInfo(CurlInfo info)
        {
            CurlSlist val = null;
            setLastError(GetInfo(info, ref val), info);
            return val;
        }

        /// <summary>
        ///     Extract <c>CurlSlist</c> information from an <c>CurlEasy</c> object.
        /// </summary>
        /// <param name="info">
        ///     One of the values in the
        ///     <see cref="CurlInfo" /> enumeration. In this case, it must
        ///     specifically be one of the members that obtains an <c>CurlSlist</c>.
        /// </param>
        /// <param name="slist">Reference to an <c>CurlSlist</c> value.</param>
        /// <returns>
        ///     The <see cref="CurlCode" /> obtained from the internal
        ///     call to <c>curl_easy_getinfo()</c>.
        /// </returns>
        /// <exception cref="System.NullReferenceException">
        ///     This is thrown if
        ///     the native <c>CURL*</c> handle wasn't created successfully.
        /// </exception>
        public CurlCode GetInfo(CurlInfo info, ref CurlSlist slist)
        {
            ensureHandle();
            var retCode = CurlCode.Ok;
            IntPtr ptr = IntPtr.Zero, ptrs = IntPtr.Zero;

            if ((int) info < CURLINFO_SLIST)
                return CurlCode.BadFunctionArgument;
            retCode = NativeMethods.curl_easy_getinfo(_pCurl, info, ref ptr);
            if (retCode != CurlCode.Ok)
                return retCode;
            slist = new CurlSlist();
            while (ptr != IntPtr.Zero)
            {
#if USE_LIBCURLSHIM
                ptr = NativeMethods.curl_shim_get_string_from_slist(ptr, ref ptrs);
                slist.Append(Marshal.PtrToStringAnsi(ptrs));
#else
                //TODO: implement
                throw new NotImplementedException();
#endif
            }
            return retCode;
        }

        /// <summary>
        ///     Extract <c>string</c> information from an <c>CurlEasy</c> object.
        /// </summary>
        /// <param name="info">
        ///     One of the values in the
        ///     <see cref="CurlInfo" /> enumeration. In this case, it must
        ///     specifically be one of the members that obtains a <c>string</c>.
        /// </param>
        /// <param name="strVal">Reference to an <c>string</c> value.</param>
        /// <returns>
        ///     The <see cref="CurlCode" /> obtained from the internal
        ///     call to <c>curl_easy_getinfo()</c>.
        /// </returns>
        /// <exception cref="System.NullReferenceException">
        ///     This is thrown if
        ///     the native <c>CURL*</c> handle wasn't created successfully.
        /// </exception>
        public CurlCode GetInfo(CurlInfo info, ref string strVal)
        {
            ensureHandle();
            var retCode = CurlCode.Ok;
            var ptr = IntPtr.Zero;

            if ((int) info < CURLINFO_STRING || (int) info >= CURLINFO_LONG)
                return CurlCode.BadFunctionArgument;
            retCode = NativeMethods.curl_easy_getinfo(_pCurl, info, ref ptr);
            if (retCode == CurlCode.Ok)
                strVal = Marshal.PtrToStringAnsi(ptr);
            return retCode;
        }

        /// <summary>
        ///     Extract <c>int</c> information from an <c>CurlEasy</c> object.
        /// </summary>
        /// <param name="info">
        ///     One of the values in the
        ///     <see cref="CurlInfo" /> enumeration. In this case, it must
        ///     specifically be one of the members that obtains a <c>double</c>.
        /// </param>
        /// <param name="dblVal">Reference to an <c>double</c> value.</param>
        /// <returns>
        ///     The <see cref="CurlCode" /> obtained from the internal
        ///     call to <c>curl_easy_getinfo()</c>.
        /// </returns>
        /// <exception cref="System.NullReferenceException">
        ///     This is thrown if
        ///     the native <c>CURL*</c> handle wasn't created successfully.
        /// </exception>
        public CurlCode GetInfo(CurlInfo info, ref double dblVal)
        {
            ensureHandle();

            // ensure it's an integral type
            if ((int) info < CURLINFO_DOUBLE)
                return CurlCode.BadFunctionArgument;

            return NativeMethods.curl_easy_getinfo(_pCurl, info, ref dblVal);
        }

        /// <summary>
        ///     Extract <c>int</c> information from an <c>CurlEasy</c> object.
        /// </summary>
        /// <param name="info">
        ///     One of the values in the
        ///     <see cref="CurlInfo" /> enumeration. In this case, it must
        ///     specifically be one of the members that obtains an <c>int</c>.
        /// </param>
        /// <param name="intVal">Reference to an <c>int</c> value.</param>
        /// <returns>
        ///     The <see cref="CurlCode" /> obtained from the internal
        ///     call to <c>curl_easy_getinfo()</c>.
        /// </returns>
        /// <exception cref="System.NullReferenceException">
        ///     This is thrown if
        ///     the native <c>CURL*</c> handle wasn't created successfully.
        /// </exception>
        public CurlCode GetInfo(CurlInfo info, ref int intVal)
        {
            ensureHandle();
            var retCode = CurlCode.Ok;
            var ptr = IntPtr.Zero;

            // ensure it's an integral type
            if ((int) info < CURLINFO_LONG || (int) info >= CURLINFO_DOUBLE)
                return CurlCode.BadFunctionArgument;

            retCode = NativeMethods.curl_easy_getinfo(_pCurl, info, ref ptr);
            if (retCode == CurlCode.Ok)
                intVal = (int) ptr;
            return retCode;
        }

        /// <summary>
        ///     Extract <c>DateTime</c> information from an <c>CurlEasy</c> object.
        /// </summary>
        /// <param name="info">
        ///     One of the values in the
        ///     <see cref="CurlInfo" /> enumeration. In this case, it must
        ///     specifically be <see cref="CurlInfo.Filetime" />.
        /// </param>
        /// <param name="dt">Reference to a <c>DateTime</c> value.</param>
        /// <returns>
        ///     The <see cref="CurlCode" /> obtained from the internal
        ///     call to <c>curl_easy_getinfo()</c>.
        /// </returns>
        /// <exception cref="System.NullReferenceException">
        ///     This is thrown if
        ///     the native <c>CURL*</c> handle wasn't created successfully.
        /// </exception>
        public CurlCode GetInfo(CurlInfo info, ref DateTime dt)
        {
            ensureHandle();
            var retCode = CurlCode.Ok;
            var ptr = IntPtr.Zero;

            if (info != CurlInfo.Filetime)
                return CurlCode.BadFunctionArgument;

            retCode = NativeMethods.curl_easy_getinfo(_pCurl, info, ref ptr);
            if (retCode == CurlCode.Ok)
            {
                if ((int) ptr < 0)
                    dt = new DateTime(0);
            }
            return retCode;
        }

        // install the fuctions that will be called from libcurlshim
        private void installDelegates()
        {
            ensureHandle();
            _hThis = GCHandle.Alloc(this);
#if USE_LIBCURLSHIM
            _pcbWrite = _shimWriteCallback;
            _pcbRead = _shimReadCallback;
            _pcbProgress = _shimProgressCallback;
            _pcbDebug = _shimDebugCallback;
            _pcbHeader = _shimHeaderCallback;
            _pcbSslCtx = _shimSslCtxCallback;
            _pcbIoctl = _shimIoctlCallback;
            _ptrThis = (IntPtr)_hThis;
            NativeMethods.curl_shim_install_delegates(
                _pCurl, _ptrThis,
                _pcbWrite, _pcbRead, _pcbProgress,
                _pcbDebug, _pcbHeader, _pcbSslCtx,
                _pcbIoctl);
#else
            _pcbWrite = _curlWriteCallback;
            _pcbRead = _curlReadCallback;
            _pcbProgress = _curlProgressCallback;
            _pcbDebug = _curlDebugCallback;
            _pcbHeader = _curlHeaderCallback;
            _pcbSslCtx = _curlSslCtxCallback;
            _pcbIoctl = _curlIoctlCallback;

            setLastError(NativeMethods.curl_easy_setopt_cb(_pCurl, CurlOption.WriteFunction, _pcbWrite),
                         CurlOption.WriteFunction);
            setLastError(NativeMethods.curl_easy_setopt_cb(_pCurl, CurlOption.ReadFunction, _pcbRead),
                         CurlOption.ReadFunction);
            setLastError(NativeMethods.curl_easy_setopt_cb(_pCurl, CurlOption.ProgressFunction, _pcbProgress),
                         CurlOption.ProgressFunction);
            setLastError(NativeMethods.curl_easy_setopt_cb(_pCurl, CurlOption.HeaderFunction, _pcbHeader),
                         CurlOption.HeaderFunction);
            setLastError(NativeMethods.curl_easy_setopt_cb(_pCurl, CurlOption.DebugFunction, _pcbDebug),
                         CurlOption.DebugFunction);
            setLastError(NativeMethods.curl_easy_setopt_cb(_pCurl, CurlOption.SslCtxFunction, _pcbSslCtx),
                         CurlOption.SslCtxFunction);
            setLastError(NativeMethods.curl_easy_setopt_cb(_pCurl, CurlOption.IoctlFunction, _pcbIoctl),
                         CurlOption.IoctlFunction);
            setLastError(NativeMethods.curl_easy_setopt(_pCurl, CurlOption.NoProgress, (IntPtr) 0),
                         CurlOption.NoProgress);

            setWriteData(null);
            setReadData(null);
            setHeaderData(null);
            setProgressData(null);
            setDebugData(null);
            setSslCtxData(null);
            setIoctlData(null);
#endif
        }

#if USE_LIBCURLSHIM
    // called by libcurlshim
        private static int _shimWriteCallback(IntPtr buf, int sz, int nmemb, IntPtr parm)
        {
            var bytes = sz*nmemb;
            var b = new byte[bytes];
            for (var i = 0; i < bytes; i++)
                b[i] = Marshal.ReadByte(buf, i);
            var gch = (GCHandle) parm;
            var curlEasy = (CurlEasy) gch.Target;
            if (curlEasy == null)
                return 0;
            if (curlEasy._pfCurlWrite == null)
                return bytes; // keep going
            return curlEasy._pfCurlWrite(b, sz, nmemb, curlEasy._writeData);
        }

        // called by libcurlshim
        private static int _shimReadCallback(IntPtr buf, int sz, int nmemb, IntPtr parm)
        {
            var bytes = sz*nmemb;
            var b = new byte[bytes];
            var gch = (GCHandle) parm;
            var curlEasy = (CurlEasy) gch.Target;
            if (curlEasy == null)
                return 0;
            if (curlEasy._pfCurlRead == null)
                return 0;
            var nRead = curlEasy._pfCurlRead(b, sz, nmemb, curlEasy._readData);
            if (nRead > 0)
            {
                for (var i = 0; i < nRead; i++)
                    Marshal.WriteByte(buf, i, b[i]);
            }
            return nRead;
        }

        // called by libcurlshim
        private static int _shimProgressCallback(IntPtr parm, double dlTotal, double dlNow, double ulTotal, double ulNow)
        {
            var gch = (GCHandle) parm;
            var curlEasy = (CurlEasy) gch.Target;
            if (curlEasy == null)
                return 0;
            if (curlEasy._pfCurlProgress == null)
                return 0;
            var nprog = curlEasy._pfCurlProgress(curlEasy._progressData, dlTotal, dlNow, ulTotal, ulNow);
            return nprog;
        }

        // called by libcurlshim
        private static int _shimDebugCallback(CurlInfoType infoType, IntPtr msgBuf, int msgBufSize, IntPtr parm)
        {
            var gch = (GCHandle) parm;
            var curlEasy = (CurlEasy) gch.Target;
            if (curlEasy == null)
                return 0;
            if (curlEasy._pfCurlDebug == null)
                return 0;
            var message = Marshal.PtrToStringAnsi(msgBuf, msgBufSize);
            curlEasy._pfCurlDebug(infoType, message, curlEasy._debugData);
            return 0;
        }

        // called by libcurlshim
        private static int _shimHeaderCallback(IntPtr buf, int sz, int nmemb, IntPtr parm)
        {
            var bytes = sz*nmemb;
            var b = new byte[bytes];
            for (var i = 0; i < bytes; i++)
                b[i] = Marshal.ReadByte(buf, i);
            var gch = (GCHandle) parm;
            var curlEasy = (CurlEasy) gch.Target;
            if (curlEasy == null)
                return 0;
            if (curlEasy._pfCurlHeader == null)
                return bytes; // keep going
            return curlEasy._pfCurlHeader(b, sz, nmemb, curlEasy._headerData);
        }

        // called by libcurlshim
        private static int _shimSslCtxCallback(IntPtr ctx, IntPtr parm)
        {
            const int OK_RETURN = (int) CurlCode.Ok;
            var gch = (GCHandle) parm;
            var curlEasy = (CurlEasy) gch.Target;
            if (curlEasy == null)
                return OK_RETURN;
            if (curlEasy._pfCurlSslContext == null)
                return OK_RETURN; // keep going
            var context = new CurlSslContext(ctx);
            return (int) curlEasy._pfCurlSslContext(context, curlEasy._sslContextData);
        }

        // called by libcurlshim
        private static CurlIoError _shimIoctlCallback(CurlIoCommand cmd, IntPtr parm)
        {
            var gch = (GCHandle) parm;
            var curlEasy = (CurlEasy) gch.Target;
            // let's require all of these to be non-null
            if (curlEasy == null || curlEasy._pfCurlIoctl == null || curlEasy._ioctlData == null)
            {
                return CurlIoError.UnknownCommand;
            }
            return curlEasy._pfCurlIoctl(cmd, curlEasy._ioctlData);
        }
#else
        // called by libcurl
        private int _curlWriteCallback(IntPtr pBuffer, int sz, int nmemb, IntPtr pUserData)
        {
            var bytes = sz*nmemb;
            if (_pfCurlWrite == null) return bytes; // keep going
            var buf = new byte[bytes];
            Marshal.Copy(pBuffer, buf, 0, bytes);
            var userdata = getObject(pUserData);
            return _pfCurlWrite(buf, sz, nmemb, userdata);
        }

        private int _curlReadCallback(IntPtr pBuffer, int sz, int nmemb, IntPtr pUserData)
        {
            var bytes = sz*nmemb;
            if (_pfCurlRead == null) return bytes; // keep going
            var userdata = getObject(pUserData);
            var buffer = new byte[bytes];
            var nRead = _pfCurlRead(buffer, sz, nmemb, userdata);
            if (nRead > 0)
                Marshal.Copy(buffer, 0, pBuffer, nRead);
            return nRead;
        }

        private int _curlProgressCallback(IntPtr ptrUserdata, double dlTotal, double dlNow, double ulTotal, double ulNow)
        {
            if (_pfCurlProgress != null)
            {
                var userdata = getObject(ptrUserdata);
                return _pfCurlProgress(userdata, dlTotal, dlNow, ulTotal, ulNow);
            }
            return 0;
        }

        private int _curlDebugCallback(IntPtr pCurl, CurlInfoType infoType, string message, int size, IntPtr pUserData)
        {
            if (_pfCurlDebug != null)
            {
                var userdata = getObject(pUserData);
                _pfCurlDebug(infoType, message, userdata);
            }
            return 0;
        }

        private int _curlHeaderCallback(IntPtr pBuffer, int sz, int nmemb, IntPtr pUserdata)
        {
            var bytes = sz*nmemb;
            if (_pfCurlHeader != null)
            {
                var buf = new byte[bytes];
                Marshal.Copy(pBuffer, buf, 0, bytes);
                var userdata = getObject(pUserdata);
                return _pfCurlHeader(buf, sz, nmemb, userdata);
            }
            return bytes; // keep going
        }

        private int _curlSslCtxCallback(IntPtr ctx, IntPtr parm)
        {
            if (_pfCurlSslContext == null)
                return (int) CurlCode.Ok; // keep going
            var context = new CurlSslContext(ctx);
            return (int) _pfCurlSslContext(context, _sslContextData);
        }

        private CurlIoError _curlIoctlCallback(CurlIoCommand cmd, IntPtr parm)
        {
            if (_pfCurlIoctl == null || _ioctlData == null)
                return CurlIoError.UnknownCommand;
            return _pfCurlIoctl(cmd, _ioctlData);
        }
#endif
    }
}