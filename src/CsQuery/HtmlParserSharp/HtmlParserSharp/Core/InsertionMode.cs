/*
 * Copyright (c) 2007 Henri Sivonen
 * Copyright (c) 2007-2011 Mozilla Foundation
 * Portions of comments Copyright 2004-2008 Apple Computer, Inc., Mozilla 
 * Foundation, and Opera Software ASA.
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
	public enum InsertionMode
	{
		INITIAL = 0,

		BEFORE_HTML = 1,

		BEFORE_HEAD = 2,

		IN_HEAD = 3,

		IN_HEAD_NOSCRIPT = 4,

		AFTER_HEAD = 5,

		IN_BODY = 6,

		IN_TABLE = 7,

		IN_CAPTION = 8,

		IN_COLUMN_GROUP = 9,

		IN_TABLE_BODY = 10,

		IN_ROW = 11,

		IN_CELL = 12,

		IN_SELECT = 13,

		IN_SELECT_IN_TABLE = 14,

		AFTER_BODY = 15,

		IN_FRAMESET = 16,

		AFTER_FRAMESET = 17,

		AFTER_AFTER_BODY = 18,

		AFTER_AFTER_FRAMESET = 19,

		TEXT = 20,

		FRAMESET_OK = 21
	}
}
