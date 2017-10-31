/*
 * Copyright (c) 2009-2010 Mozilla Foundation
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
	/// Interface for exposing the state of the HTML5 tree builder so that the
	/// interface can be implemented by the tree builder itself and by snapshots.
	/// </summary>
	public interface ITreeBuilderState<T> where T : class
	{
		/// <summary>
		/// Gets the stack.
		/// </summary>
		/// <returns>The stack</returns>
		StackNode<T>[] Stack { get; }

		/// <summary>
		/// Gets the list of active formatting elements.
		/// </summary>
		/// <returns>The list of active formatting elements.</returns>
		StackNode<T>[] ListOfActiveFormattingElements { get; }

		/// <summary>
		/// Gets the form pointer.
		/// </summary>
		/// <returns>The form pointer</returns>
		T FormPointer { get; }

		/// <summary>
		/// Gets the head pointer.
		/// </summary>
		/// <returns>The head pointer.</returns>
		T HeadPointer { get; }

		/// <summary>
		/// Gets the deep tree surrogate parent.
		/// </summary>
		/// <returns>The deep tree surrogate parent.</returns>
		T DeepTreeSurrogateParent { get; }

		/// <summary>
		/// Gets the mode.
		/// </summary>
		/// <returns>The mode.</returns>
		InsertionMode Mode { get; }

		/// <summary>
		/// Gets the original mode.
		/// </summary>
		/// <returns>The original mode.</returns>
		InsertionMode OriginalMode { get; }

		/// <summary>
		/// Determines whether the frameset is OK.
		/// </summary>
		/// <returns>
		///   <c>true</c> if the frameset is OK; otherwise, <c>false</c>.
		/// </returns>
		bool IsFramesetOk { get; }

		/// <summary>
		/// Determines whether we need to drop LF.
		/// </summary>
		/// <returns>
		///   <c>true</c> if we need to drop LF; otherwise, <c>false</c>.
		/// </returns>
		bool IsNeedToDropLF { get; }

		/// <summary>
		/// Determines whether this instance is in quirks mode.
		/// </summary>
		/// <returns>
		///   <c>true</c> if this instance is in quirks mode; otherwise, <c>false</c>.
		/// </returns>
		bool IsQuirks { get; }
	}
}
