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

using System;
using System.Text;

#pragma warning disable 1591 // Missing XML comment
#pragma warning disable 1570 // XML comment on 'construct' has badly formed XML — 'reason'
#pragma warning disable 1587 // XML comment is not placed on a valid element

namespace HtmlParserSharp.Core
{
   

	/// <summary>
	/// A common superclass for tree builders that coalesce their text nodes.
	/// </summary>
	public abstract class CoalescingTreeBuilder<T> : TreeBuilder<T> where T : class
	{
		override protected void AppendCharacters(T parent, char[] buf, int start, int length)
		{
			AppendCharacters(parent, new String(buf, start, length));
		}
        override protected void AppendCharacters(T parent, StringBuilder sb)
        {
            AppendCharacters(parent, sb.ToString());
        }

		override protected void AppendIsindexPrompt(T parent)
		{
			AppendCharacters(parent, "This is a searchable index. Enter search keywords: ");
		}

		protected abstract void AppendCharacters(T parent, string text);

		override protected void AppendComment(T parent, char[] buf, int start, int length)
		{
			AppendComment(parent, new String(buf, start, length));
		}

		protected abstract void AppendComment(T parent, string comment);

		override protected void AppendCommentToDocument(char[] buf, int start, int length)
		{
			// TODO Auto-generated method stub
			AppendCommentToDocument(new String(buf, start, length));
		}

		protected abstract void AppendCommentToDocument(string comment);

        //override protected void InsertFosterParentedCharacters(char[] buf, int start,
        //        int length, T table, T stackParent)
        //{
        //    InsertFosterParentedCharacters(new String(buf, start, length), table, stackParent);
        //}

        protected override void InsertFosterParentedCharacters(StringBuilder sb, T table, T stackParent)
        {
            InsertFosterParentedCharacters(sb.ToString(), table, stackParent);
        }

		protected abstract void InsertFosterParentedCharacters(string text, T table, T stackParent);
	}

}
