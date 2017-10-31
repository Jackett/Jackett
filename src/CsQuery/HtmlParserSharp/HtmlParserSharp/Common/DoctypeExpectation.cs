/*
 * Copyright (c) 2007 Henri Sivonen
 * Copyright (c) 2012 Patrick Reisert
 *
 * Permission is hereby granted, free of charge, to any person obtaining a 
 * copy of this software and associated documentation files (the "Software"), 
 * to deal in the Software without restriction, including without limitation 
 * the rights to use, copy, modify, merge, publish, distribute, sublicense, 
 * and/or sell copies of the Software, and to permit persons to whom the 
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in 
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
 * THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 * DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#pragma warning disable 1591

namespace HtmlParserSharp.Common
{
	/// <summary>
	/// Used for indicating desired behavior with legacy doctypes.
	/// </summary>
	public enum DoctypeExpectation
	{
		/// <summary>
		/// Be a pure HTML5 parser.
		/// </summary>
		Html,

		/// <summary>
		/// Require the HTML 4.01 Transitional public id. Turn on HTML4-specific
		/// additional errors regardless of doctype.
		/// </summary>
		Html401Transitional,

		/// <summary>
		/// Require the HTML 4.01 Transitional public id and a system id. Turn on
		/// HTML4-specific additional errors regardless of doctype.
		/// </summary>
		Html401Strict,

		/// <summary>
		/// Treat the doctype required by HTML 5, doctypes with the HTML 4.01 Strict
		/// public id and doctypes with the HTML 4.01 Transitional public id and a
		/// system id as non-errors. Turn on HTML4-specific additional errors if the
		/// public id is the HTML 4.01 Strict or Transitional public id.
		/// </summary>
		Auto,

		/// <summary>
		/// Never enable HTML4-specific error checks. Never report any doctype
		/// condition as an error. (Doctype tokens in wrong places will be
		/// reported as errors, though.) The application may decide what to log
		/// in response to calls to  <code>DocumentModeHanler</code>. This mode
		/// is meant for doing surveys on existing content.
		/// </summary>
		NoDoctypeErrors
	}
}
