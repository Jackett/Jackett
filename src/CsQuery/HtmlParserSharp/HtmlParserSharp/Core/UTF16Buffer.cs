/*
 * Copyright (c) 2008-2010 Mozilla Foundation
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

#pragma warning disable 1591 // Missing XML comment
#pragma warning disable 1570 // XML comment on 'construct' has badly formed XML — 'reason'
#pragma warning disable 1587 // XML comment is not placed on a valid element

namespace HtmlParserSharp.Core
{
	/// <summary>
	/// An UTF-16 buffer that knows the start and end indeces of its unconsumed
	/// content.
	/// </summary>
	public sealed class UTF16Buffer
	{
		/// <summary>
		/// Gets the backing store of the buffer. May be larger than the logical content
		/// of this <code>UTF16Buffer</code>.
		/// </summary>
		public char[] Buffer { get; private set; }

		/// <summary>
		/// Gets or sets the index of the first unconsumed character in the backing buffer.
		/// </summary>
		public int Start { get; set; }

		/// <summary>
		/// Gets or sets the index of the slot immediately after the last character in the backing
		/// buffer that is part of the logical content of this <code>UTF16Buffer</code>.
		/// </summary>
		public int End { get; set; }

		/// <summary>
		/// Constructor for wrapping an existing UTF-16 code unit array.
		/// </summary>
		/// <param name="buffer">The backing buffer.</param>
		/// <param name="start">The index of the first character to consume.</param>
		/// <param name="end">The index immediately after the last character to consume.</param>
		public UTF16Buffer(char[] buffer, int start, int end)
		{
			Buffer = buffer;
			Start = start;
			End = end;
		}

		/// <summary>
		/// Determines whether this instance has data left.
		/// </summary>
		/// <returns>
		///   <c>true</c> if there's data left; otherwise, <c>false</c>.
		/// </returns>
		public bool HasMore
		{
			get
			{
				return Start < End;
			}
		}

		/// <summary>
		/// Adjusts the start index to skip over the first character if it is a line
		/// feed and the previous character was a carriage return.
		/// </summary>
		/// <param name="lastWasCR">Whether the previous character was a carriage return.</param>
		public void Adjust(bool lastWasCR)
		{
			if (lastWasCR && Buffer[Start] == '\n')
			{
				Start++;
			}
		}
	}
}
