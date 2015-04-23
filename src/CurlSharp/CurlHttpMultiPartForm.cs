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
    ///     This trivial class wraps the internal <c>curl_forms</c> struct.
    /// </summary>
    public sealed class CurlForms
    {
        /// <summary>The <see cref="CurlFormOption" />.</summary>
        public CurlFormOption Option;

        /// <summary>Value for the option.</summary>
        public Object Value;
    }

    /// <summary>
    ///     Wraps a section of multipart form data to be submitted via the
    ///     <see cref="CurlOption.HttpPost" /> option in the
    ///     <see cref="CurlEasy.SetOpt" /> member of the <see cref="CurlEasy" /> class.
    /// </summary>
    public class CurlHttpMultiPartForm : IDisposable
    {
        // the two curlform pointers
        private readonly IntPtr[] _pItems;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        ///     This is thrown
        ///     if <see cref="Curl" /> hasn't bee properly initialized.
        /// </exception>
        public CurlHttpMultiPartForm()
        {
            Curl.EnsureCurl();
            _pItems = new IntPtr[2];
            _pItems[0] = IntPtr.Zero;
            _pItems[1] = IntPtr.Zero;
        }

        /// <summary>
        ///     Free unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Destructor
        /// </summary>
        ~CurlHttpMultiPartForm()
        {
            Dispose(false);
        }

        // for CurlEasy.SetOpt()
        internal IntPtr GetHandle()
        {
            return _pItems[0];
        }

        /// <summary>
        ///     Add a multi-part form section.
        /// </summary>
        /// <param name="args">
        ///     Argument list, as described in the remarks.
        /// </param>
        /// <returns>
        ///     A <see cref="CurlFormCode" />, hopefully
        ///     <c>CurlFormCode.Ok</c>.
        /// </returns>
        /// <remarks>
        ///     This is definitely the workhorse method for this class. It
        ///     should be called in roughly the same manner as
        ///     <c>curl_formadd()</c>, except you would omit the first two
        ///     <c>struct curl_httppost**</c> arguments (<c>firstitem</c> and
        ///     <c>lastitem</c>), which are wrapped in this class. So you should
        ///     pass arguments in the following sequence:
        ///     <para>
        ///         <c>
        ///             CurlHttpMultiPartForm.AddSection(option1, value1, ..., optionX, valueX,
        ///             CurlFormOption.End)
        ///         </c>
        ///         ;
        ///     </para>
        ///     <para>
        ///         For a complete list of possible options, see the documentation for
        ///         the <see cref="CurlFormOption" /> enumeration.
        ///     </para>
        ///     <note>
        ///         The pointer options (<c>PtrName</c>, etc.) make an
        ///         internal copy of the passed <c>byte</c> array. Therefore, any
        ///         changes you make to the client copy of this array AFTER calling
        ///         this method, won't be reflected internally with <c>cURL</c>. The
        ///         purpose of providing the pointer options is to support the
        ///         posting of non-string binary data.
        ///     </note>
        /// </remarks>
        public CurlFormCode AddSection(params object[] args)
        {
            var nCount = args.Length;
            var nRealCount = nCount;
            var retCode = CurlFormCode.Ok;
            CurlForms[] aForms = null;

            // one arg or even number of args is an error
            if ((nCount == 1) || (nCount%2 == 0))
                return CurlFormCode.Incomplete;

            // ensure the last argument is End
            var iCode = (CurlFormOption)
                Convert.ToInt32(args.GetValue(nCount - 1));
            if (iCode != CurlFormOption.End)
                return CurlFormCode.Incomplete;

            // walk through any passed arrays to get the true number of
            // items and ensure the child arrays are properly (and not
            // prematurely) terminated with End
            for (var i = 0; i < nCount; i += 2)
            {
                iCode = (CurlFormOption) Convert.ToInt32(args.GetValue(i));
                switch (iCode)
                {
                    case CurlFormOption.Array:
                    {
                        aForms = args.GetValue(i + 1) as CurlForms[];
                        if (aForms == null)
                            return CurlFormCode.Incomplete;
                        var nFormsCount = aForms.Length;
                        for (var j = 0; j < nFormsCount; j++)
                        {
                            var pcf = aForms.GetValue(j) as CurlForms;
                            if (pcf == null)
                                return CurlFormCode.Incomplete;
                            if (j == nFormsCount - 1)
                            {
                                if (pcf.Option != CurlFormOption.End)
                                    return CurlFormCode.Incomplete;
                            }
                            else
                            {
                                if (pcf.Option == CurlFormOption.End)
                                    return CurlFormCode.Incomplete;
                            }
                        }
                        // -2 accounts for the fact that we're a) not
                        // including the item with End and b) not
                        // including Array in what we pass to cURL
                        nRealCount += 2*(nFormsCount - 2);
                        break;
                    }
                }
            }

            // allocate the IntPtr array for the data
            var aPointers = new IntPtr[nRealCount];
            for (var i = 0; i < nRealCount - 1; i++)
                aPointers[i] = IntPtr.Zero;
            aPointers[nRealCount - 1] = (IntPtr) CurlFormOption.End;

            // now we go through the args
            aForms = null;
            var formArrayPos = 0;
            var argArrayPos = 0;
            var ptrArrayPos = 0;
            Object obj = null;

            while ((retCode == CurlFormCode.Ok) &&
                   (ptrArrayPos < nRealCount))
            {
                if (aForms != null)
                {
                    var pcf = aForms.GetValue(formArrayPos++)
                        as CurlForms;
                    if (pcf == null)
                    {
                        retCode = CurlFormCode.UnknownOption;
                        break;
                    }
                    iCode = pcf.Option;
                    obj = pcf.Value;
                }
                else
                {
                    iCode = (CurlFormOption) Convert.ToInt32(
                        args.GetValue(argArrayPos++));
                    obj = (iCode == CurlFormOption.End)
                        ? null
                        : args.GetValue(argArrayPos++);
                }

                switch (iCode)
                {
                        // handle byte-array pointer-related items
                    case CurlFormOption.PtrName:
                    case CurlFormOption.PtrContents:
                    case CurlFormOption.BufferPtr:
                    {
                        var bytes = obj as byte[];
                        if (bytes == null)
                            retCode = CurlFormCode.UnknownOption;
                        else
                        {
                            var nLen = bytes.Length;
                            var ptr = Marshal.AllocHGlobal(nLen);
                            if (ptr != IntPtr.Zero)
                            {
                                aPointers[ptrArrayPos++] = (IntPtr) iCode;
                                // copy bytes to unmanaged buffer
                                for (var j = 0; j < nLen; j++)
                                    Marshal.WriteByte(ptr, bytes[j]);
                                aPointers[ptrArrayPos++] = ptr;
                            }
                            else
                                retCode = CurlFormCode.Memory;
                        }
                        break;
                    }

                        // length values
                    case CurlFormOption.NameLength:
                    case CurlFormOption.ContentsLength:
                    case CurlFormOption.BufferLength:
                        aPointers[ptrArrayPos++] = (IntPtr) iCode;
                        aPointers[ptrArrayPos++] = (IntPtr)
                            Convert.ToInt32(obj);
                        break;

                        // strings
                    case CurlFormOption.CopyName:
                    case CurlFormOption.CopyContents:
                    case CurlFormOption.FileContent:
                    case CurlFormOption.File:
                    case CurlFormOption.ContentType:
                    case CurlFormOption.Filename:
                    case CurlFormOption.Buffer:
                    {
                        aPointers[ptrArrayPos++] = (IntPtr) iCode;
                        var s = obj as String;
                        if (s == null)
                            retCode = CurlFormCode.UnknownOption;
                        else
                        {
                            var p = Marshal.StringToHGlobalAnsi(s);
                            if (p != IntPtr.Zero)
                                aPointers[ptrArrayPos++] = p;
                            else
                                retCode = CurlFormCode.Memory;
                        }
                        break;
                    }

                        // array case: already handled
                    case CurlFormOption.Array:
                        if (aForms != null)
                            retCode = CurlFormCode.IllegalArray;
                        else
                        {
                            aForms = obj as CurlForms[];
                            if (aForms == null)
                                retCode = CurlFormCode.UnknownOption;
                        }
                        break;

                        // slist
                    case CurlFormOption.ContentHeader:
                    {
                        aPointers[ptrArrayPos++] = (IntPtr) iCode;
                        var s = obj as CurlSlist;
                        if (s == null)
                            retCode = CurlFormCode.UnknownOption;
                        else
                            aPointers[ptrArrayPos++] = s.Handle;
                        break;
                    }

                        // erroneous stuff
                    case CurlFormOption.Nothing:
                        retCode = CurlFormCode.Incomplete;
                        break;

                        // end
                    case CurlFormOption.End:
                        if (aForms != null) // end of form
                        {
                            aForms = null;
                            formArrayPos = 0;
                        }
                        else
                            aPointers[ptrArrayPos++] = (IntPtr) iCode;
                        break;

                        // default is unknown
                    default:
                        retCode = CurlFormCode.UnknownOption;
                        break;
                }
            }

            // ensure we didn't come up short on parameters
            if (ptrArrayPos != nRealCount)
                retCode = CurlFormCode.Incomplete;

            // if we're OK here, call into curl
            if (retCode == CurlFormCode.Ok)
            {
#if USE_LIBCURLSHIM
                retCode = (CurlFormCode) NativeMethods.curl_shim_formadd(_pItems, aPointers, nRealCount);
#else
                retCode = (CurlFormCode) NativeMethods.curl_formadd(ref _pItems[0], ref _pItems[1],
                                                                    (int) aPointers[0], aPointers[1],
                                                                    (int) aPointers[2], aPointers[3],
                                                                    (int) aPointers[4]);
#endif
            }

            // unmarshal native allocations
            for (var i = 0; i < nRealCount - 1; i += 2)
            {
                iCode = (CurlFormOption) (int) aPointers[i];
                switch (iCode)
                {
                    case CurlFormOption.CopyName:
                    case CurlFormOption.CopyContents:
                    case CurlFormOption.FileContent:
                    case CurlFormOption.File:
                    case CurlFormOption.ContentType:
                    case CurlFormOption.Filename:
                    case CurlFormOption.Buffer:
                        // byte buffer cases
                    case CurlFormOption.PtrName:
                    case CurlFormOption.PtrContents:
                    case CurlFormOption.BufferPtr:
                    {
                        if (aPointers[i + 1] != IntPtr.Zero)
                            Marshal.FreeHGlobal(aPointers[i + 1]);
                        break;
                    }

                    default:
                        break;
                }
            }

            return retCode;
        }

        private void Dispose(bool disposing)
        {
            lock (this)
            {
                if (disposing)
                {
                    // clean up managed objects
                }

                // clean up native objects
                if (_pItems[0] != IntPtr.Zero)
                    NativeMethods.curl_formfree(_pItems[0]);
                _pItems[0] = IntPtr.Zero;
                _pItems[1] = IntPtr.Zero;
            }
        }
    }
}