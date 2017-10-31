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
	public class StateSnapshot<T> : ITreeBuilderState<T> where T : class
	{
		/// <summary>
		/// Gets the stack.
		/// </summary>
		public StackNode<T>[] Stack { get; private set; }

		/// <summary>
		/// Gets the list of active formatting elements.
		/// </summary>
		public StackNode<T>[] ListOfActiveFormattingElements { get; private set; }

		public T FormPointer { get; private set; }

		public T HeadPointer { get; private set; }

		public T DeepTreeSurrogateParent { get; private set; }

		/// <summary>
		/// Gets the mode.
		/// </summary>
		public InsertionMode Mode { get; private set; }

		/// <summary>
		/// Gets the original mode.
		/// </summary>
		public InsertionMode OriginalMode { get; private set; }

		/// <summary>
		/// Gets a value indicating whether this instance is frameset ok.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is frameset ok; otherwise, <c>false</c>.
		/// </value>
		public bool IsFramesetOk { get; private set; }

		/// <summary>
		/// Gets a value indicating whether this instance is need to drop LF.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is need to drop LF; otherwise, <c>false</c>.
		/// </value>
		public bool IsNeedToDropLF { get; private set; }

		/// <summary>
		/// Gets a value indicating whether this instance is quirks.
		/// </summary>
		/// <value>
		///   <c>true</c> if this instance is quirks; otherwise, <c>false</c>.
		/// </value>
		public bool IsQuirks { get; private set; }

		internal StateSnapshot(StackNode<T>[] stack,
				StackNode<T>[] listOfActiveFormattingElements,
				T formPointer,
				T headPointer,
				T deepTreeSurrogateParent,
				InsertionMode mode,
				InsertionMode originalMode,
				bool framesetOk,
				bool needToDropLF,
				bool quirks)
		{
			Stack = stack;
			ListOfActiveFormattingElements = listOfActiveFormattingElements;
			FormPointer = formPointer;
			HeadPointer = headPointer;
			DeepTreeSurrogateParent = deepTreeSurrogateParent;
			Mode = mode;
			OriginalMode = originalMode;
			IsFramesetOk = framesetOk;
			IsNeedToDropLF = needToDropLF;
			IsQuirks = quirks;
		}
	}

}
