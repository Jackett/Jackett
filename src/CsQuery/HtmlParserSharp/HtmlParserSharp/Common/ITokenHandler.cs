/*
 * Copyright (c) 2007 Henri Sivonen
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

using HtmlParserSharp.Core;

#pragma warning disable 1591

namespace HtmlParserSharp.Common
{
	/// <summary>
	/// <code>Tokenizer</code> reports tokens through this interface.
	/// </summary>
	public interface ITokenHandler
	{

		/// <summary>
		/// This method is called at the start of tokenization before any other
		/// methods on this interface are called. Implementations should hold the
		/// reference to the <code>Tokenizer</code> in order to set the content
		/// model flag and in order to be able to query for <code>Locator</code> data.
		/// </summary>
		/// <param name="self">The Tokenizer.</param>
		void StartTokenization(Tokenizer self);

		/// <summary>
		/// If this handler implementation cares about comments, return <code>true</code>.
		/// If not, return <code>false</code>
		/// </summary>
		/// <returns>Whether this handler wants comments</returns>
		bool WantsComments { get; }

		/// <summary>
		/// Receive a doctype token.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="publicIdentifier">The public identifier.</param>
		/// <param name="systemIdentifier">The system identifier.</param>
		/// <param name="forceQuirks">Whether the token is correct.</param>
		void Doctype(string name, string publicIdentifier, string systemIdentifier, bool forceQuirks);

		/// <summary>
		/// Receive a start tag token.
		/// </summary>
		/// <param name="eltName">The tag name.</param>
		/// <param name="attributes">The attributes.</param>
		/// <param name="selfClosing">TODO</param>
		void StartTag(ElementName eltName, HtmlAttributes attributes, bool selfClosing);

		/// <summary>
		/// Receive an end tag token.
		/// </summary>
		/// <param name="eltName">The tag name.</param>
		void EndTag(ElementName eltName);

		/// <summary>
		/// Receive a comment token. The data is junk if the<code>wantsComments()</code>
		/// returned <code>false</code>.
		/// </summary>
		/// <param name="buf">The buffer holding the data.</param>
		/// <param name="start">The offset into the buffer.</param>
		/// <param name="length">The number of code units to read.</param>
		void Comment(char[] buf, int start, int length);

		/// <summary>
		/// Receive character tokens. This method has the same semantics as the SAX
		/// method of the same name.
		/// </summary>
		/// <param name="buf">A buffer holding the data.</param>
		/// <param name="start">The offset into the buffer.</param>
		/// <param name="length">The number of code units to read.</param>
		void Characters(char[] buf, int start, int length);

		/// <summary>
		/// Reports a U+0000 that's being turned into a U+FFFD.
		/// </summary>
		void ZeroOriginatingReplacementCharacter();

		/// <summary>
		/// The end-of-file token.
		/// </summary>
		void Eof();

		/// <summary>
		/// The perform final cleanup.
		/// </summary>
		void EndTokenization();

		/// <summary>
		/// Checks if the CDATA sections are allowed.
		/// </summary>
		/// <returns><c>true</c> if CDATA sections are allowed</returns>
		bool IsCDataSectionAllowed { get; }

        /// <summary>
        /// Gets a value indicating whether self-closing tags should be allowed. When true, any tag may
        /// close itself. When false, a self-closing tag is treated like an opening-tag only.
        /// </summary>

        bool AllowSelfClosingTags { get; }
	}
}
