/*
 * Copyright (c) 2007 Henri Sivonen
 * Copyright (c) 2007-2011 Mozilla Foundation
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
using System.Diagnostics;
using HtmlParserSharp.Common;

#pragma warning disable 1591 // Missing XML comment
#pragma warning disable 1570 // XML comment on 'construct' has badly formed XML — 'reason'
#pragma warning disable 1587 // XML comment is not placed on a valid element

namespace HtmlParserSharp.Core
{
	public sealed class StackNode<T>
	{
		readonly int flags;

		[Local]
		internal readonly string name;

		[Local]
		internal readonly string popName;

		[NsUri]
		internal readonly string ns;

		internal readonly T node;

		// Only used on the list of formatting elements
		internal HtmlAttributes attributes;

		private int refcount = 1;

		// [NOCPP[

		private readonly TaintableLocator locator;

		public TaintableLocator Locator
		{
			get
			{
				return locator;
			}
		}

		// ]NOCPP]

		public int Flags
		{
			get
			{
				return flags;
			}
		}

		public DispatchGroup Group
		{
			get
			{
				return (DispatchGroup)(flags & ElementName.GROUP_MASK);
			}
		}

		public bool IsScoping
		{
			get
			{
				return (flags & ElementName.SCOPING) != 0;
			}
		}

		public bool IsSpecial
		{
			get
			{
				return (flags & ElementName.SPECIAL) != 0;
			}
		}

		public bool IsFosterParenting
		{
			get
			{
				return (flags & ElementName.FOSTER_PARENTING) != 0;
			}
		}

		public bool IsHtmlIntegrationPoint
		{
			get
			{
				return (flags & ElementName.HTML_INTEGRATION_POINT) != 0;
			}
		}

		// [NOCPP[

		public bool IsOptionalEndTag
		{
			get
			{
				return (flags & ElementName.OPTIONAL_END_TAG) != 0;
			}
		}

		// ]NOCPP]

		/// <summary>
		/// Constructor for copying. This doesn't take another <code>StackNode</code>
		/// because in C++ the caller is reponsible for reobtaining the local names
		/// from another interner.
		/// </summary>
		internal StackNode(int flags, [NsUri] String ns, [Local] String name, T node,
				[Local] String popName, HtmlAttributes attributes
			// [NOCPP[
				, TaintableLocator locator
			// ]NOCPP]
		)
		{
			this.flags = flags;
			this.name = name;
			this.popName = popName;
			this.ns = ns;
			this.node = node;
			this.attributes = attributes;
			this.refcount = 1;
			// [NOCPP[
			this.locator = locator;
			// ]NOCPP]
		}

		/// <summary>
		/// Short hand for well-known HTML elements.
		/// </summary>
		internal StackNode(ElementName elementName, T node
			// [NOCPP[
				, TaintableLocator locator
			// ]NOCPP]
		)
		{
			this.flags = elementName.Flags;
			this.name = elementName.name;
			this.popName = elementName.name;
			this.ns = "http://www.w3.org/1999/xhtml";
			this.node = node;
			this.attributes = null;
			this.refcount = 1;
			Debug.Assert(!elementName.IsCustom, "Don't use this constructor for custom elements.");
			// [NOCPP[
			this.locator = locator;
			// ]NOCPP]
		}

		/// <summary>
		/// Constructor for HTML formatting elements.
		/// </summary>
		internal StackNode(ElementName elementName, T node, HtmlAttributes attributes
			// [NOCPP[
				, TaintableLocator locator
			// ]NOCPP]
		)
		{
			this.flags = elementName.Flags;
			this.name = elementName.name;
			this.popName = elementName.name;
			this.ns = "http://www.w3.org/1999/xhtml";
			this.node = node;
			this.attributes = attributes;
			this.refcount = 1;
			Debug.Assert(!elementName.IsCustom, "Don't use this constructor for custom elements.");
			// [NOCPP[
			this.locator = locator;
			// ]NOCPP]
		}

		/// <summary>
		/// The common-case HTML constructor.
		/// </summary>
		internal StackNode(ElementName elementName, T node, [Local] string popName
			// [NOCPP[
				, TaintableLocator locator
			// ]NOCPP]
		)
		{
			this.flags = elementName.Flags;
			this.name = elementName.name;
			this.popName = popName;
			this.ns = "http://www.w3.org/1999/xhtml";
			this.node = node;
			this.attributes = null;
			this.refcount = 1;
			// [NOCPP[
			this.locator = locator;
			// ]NOCPP]
		}

		/// <summary>
		/// Constructor for SVG elements. Note that the order of the arguments is
		/// what distinguishes this from the HTML constructor. This is ugly, but
		/// AFAICT the least disruptive way to make this work with Java's generics
		/// and without unnecessary branches. :-(
		/// </summary>
		internal StackNode(ElementName elementName, [Local] string popName, T node
			// [NOCPP[
				, TaintableLocator locator
			// ]NOCPP]
		)
		{
			this.flags = PrepareSvgFlags(elementName.Flags);
			this.name = elementName.name;
			this.popName = popName;
			this.ns = "http://www.w3.org/2000/svg";
			this.node = node;
			this.attributes = null;
			this.refcount = 1;
			// [NOCPP[
			this.locator = locator;
			// ]NOCPP]
		}

		/// <summary>
		/// Constructor for MathML.
		/// </summary>
		internal StackNode(ElementName elementName, T node, [Local] string popName,
				bool markAsIntegrationPoint
			// [NOCPP[
				, TaintableLocator locator
			// ]NOCPP]
		)
		{
			this.flags = PrepareMathFlags(elementName.Flags, markAsIntegrationPoint);
			this.name = elementName.name;
			this.popName = popName;
			this.ns = "http://www.w3.org/1998/Math/MathML";
			this.node = node;
			this.attributes = null;
			this.refcount = 1;
			// [NOCPP[
			this.locator = locator;
			// ]NOCPP]
		}

		private static int PrepareSvgFlags(int flags)
		{
			flags &= ~(ElementName.FOSTER_PARENTING | ElementName.SCOPING
					| ElementName.SPECIAL | ElementName.OPTIONAL_END_TAG);
			if ((flags & ElementName.SCOPING_AS_SVG) != 0)
			{
				flags |= (ElementName.SCOPING | ElementName.SPECIAL | ElementName.HTML_INTEGRATION_POINT);
			}
			return flags;
		}

		private static int PrepareMathFlags(int flags, bool markAsIntegrationPoint)
		{
			flags &= ~(ElementName.FOSTER_PARENTING | ElementName.SCOPING
					| ElementName.SPECIAL | ElementName.OPTIONAL_END_TAG);
			if ((flags & ElementName.SCOPING_AS_MATHML) != 0)
			{
				flags |= (ElementName.SCOPING | ElementName.SPECIAL);
			}
			if (markAsIntegrationPoint)
			{
				flags |= ElementName.HTML_INTEGRATION_POINT;
			}
			return flags;
		}

		public void DropAttributes()
		{
			attributes = null;
		}

		// [NOCPP[

		/// <summary>
		/// Returns a <see cref="System.String"/> that represents this instance.
		/// </summary>
		/// <returns>
		/// A <see cref="System.String"/> that represents this instance.
		/// </returns>
		override public String ToString()
		{
			return name;
		}

		// ]NOCPP]

		// TODO: probably we won't need these
		public void Retain()
		{
			refcount++;
		}

		public void Release()
		{
			refcount--;
			/*if (refcount == 0) {
				Portability.delete(this);
			}*/
		}
	}
}
