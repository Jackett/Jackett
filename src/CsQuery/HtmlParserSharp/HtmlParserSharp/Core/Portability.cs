/*
 * Copyright (c) 2008-2009 Mozilla Foundation
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
using HtmlParserSharp.Common;

#pragma warning disable 1591 // Missing XML comment
#pragma warning disable 1570 // XML comment on 'construct' has badly formed XML — 'reason'
#pragma warning disable 1587 // XML comment is not placed on a valid element

namespace HtmlParserSharp.Core
{
	/// <summary>
	/// Class for C++ portability.
	/// TODO: Remove this
	/// </summary>
	public sealed class Portability
	{
		// Allocating methods

		/// <summary>
		/// Allocates a new local name object. In C++, the refcount must be set up in such a way that
		/// calling <code>releaseLocal</code> on the return value balances the refcount set by this method.
		/// </summary>
		[Local]
		public static string NewLocalNameFromBuffer(char[] buf, int offset, int length)
		{
			return new string(buf, offset, length);
		}

		// Comparison methods

		public static bool LocalEqualsBuffer([Local] string local, char[] buf, int offset, int length)
		{
			if (local.Length != length)
			{
				return false;
			}
			for (int i = 0; i < length; i++)
			{
				if (local[i] != buf[offset + i])
				{
					return false;
				}
			}
			return true;
		}

		public static bool LowerCaseLiteralIsPrefixOfIgnoreAsciiCaseString(string lowerCaseLiteral,	string str)
		{
			if (str == null)
			{
				return false;
			}
			if (lowerCaseLiteral.Length > str.Length)
			{
				return false;
			}
			for (int i = 0; i < lowerCaseLiteral.Length; i++)
			{
				char c0 = lowerCaseLiteral[i];
				char c1 = str[i];
				if (c1 >= 'A' && c1 <= 'Z')
				{
					c1 += (char)0x20;
				}
				if (c0 != c1)
				{
					return false;
				}
			}
			return true;
		}

		public static bool LowerCaseLiteralEqualsIgnoreAsciiCaseString(string lowerCaseLiteral, string str)
		{
			if (str == null)
			{
				return false;
			}
			if (lowerCaseLiteral.Length != str.Length)
			{
				return false;
			}
			for (int i = 0; i < lowerCaseLiteral.Length; i++)
			{
				char c0 = lowerCaseLiteral[i];
				char c1 = str[i];
				if (c1 >= 'A' && c1 <= 'Z')
				{
					c1 += (char)0x20;
				}
				if (c0 != c1)
				{
					return false;
				}
			}
			return true;
		}
	}
}
