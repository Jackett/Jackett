/*
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

#pragma warning disable 1591

namespace HtmlParserSharp.Common
{
	public class DocumentModeEventArgs : EventArgs
	{
		public DocumentMode Mode { get; private set; }
		public string PublicIdentifier { get; private set; }
		public string SystemIdentifier { get; private set; }
		public bool Html4SpecificAdditionalErrorChecks { get; private set; }

		/// <summary>
		/// Receive notification of the document mode.
		/// </summary>
		/// <param name="mode">The document mode.</param>
		/// <param name="publicIdentifier">The public identifier of the doctype or <c>null</c> if unavailable.</param>
		/// <param name="systemIdentifier">The system identifier of the doctype or <c>null</c> if unavailable.</param>
		/// <param name="html4SpecificAdditionalErrorChecks"><c>true</c>  if HTML 4-specific checks were enabled,
		/// <c>false</c> otherwise</param>
		public DocumentModeEventArgs(DocumentMode mode, string publicIdentifier, string systemIdentifier, bool html4SpecificAdditionalErrorChecks)
		{
			Mode = mode;
			PublicIdentifier = publicIdentifier;
			SystemIdentifier = systemIdentifier;
			Html4SpecificAdditionalErrorChecks = html4SpecificAdditionalErrorChecks;
		}
	}
}
