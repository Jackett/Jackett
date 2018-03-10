/***************************************************************************
 *
 * Project: libcurl.NET
 *
 * Copyright (c) 2004, 2005 Jeff Phillips (jeff@jeffp.net)
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
 * $Id: Enums.cs,v 1.1 2005/02/17 22:47:25 jeffreyphillips Exp $
 **************************************************************************/

namespace CurlSharp.Enums
{
    /// <summary>
    ///     One of these is returned by <see cref="CurlHttpMultiPartForm.AddSection" />.
    /// </summary>
    public enum CurlFormCode
    {
        /// <summary>
        ///     The section was added properly.
        /// </summary>
        Ok = 0,

        /// <summary>
        ///     Out-of-memory when adding the section.
        /// </summary>
        Memory = 1,

        /// <summary>
        ///     Invalid attempt to add the same option more than once to a
        ///     section.
        /// </summary>
        OptionTwice = 2,

        /// <summary>
        ///     Invalid attempt to pass a <c>null</c> string or byte array in
        ///     one of the arguments.
        /// </summary>
        Null = 3,

        /// <summary>
        ///     Invalid attempt to pass an unrecognized option in one of the
        ///     arguments.
        /// </summary>
        UnknownOption = 4,

        /// <summary>
        ///     Incomplete argument lists.
        /// </summary>
        Incomplete = 5,

        /// <summary>
        ///     Invalid attempt to provide a nested <c>Array</c>.
        /// </summary>
        IllegalArray = 6,

        /// <summary>
        ///     This will not be returned so long as HTTP is enabled, which
        ///     it always is in libcurl.NET.
        /// </summary>
        Disabled = 7,

        /// <summary>
        ///     End-of-enumeration marker; do not use in application code.
        /// </summary>
        Last = 8
    };
}