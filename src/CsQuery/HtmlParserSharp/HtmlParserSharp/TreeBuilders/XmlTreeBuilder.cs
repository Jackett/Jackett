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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using HtmlParserSharp.Common;
using HtmlParserSharp.Core;

#pragma warning disable 1591 // Missing XML comment
#pragma warning disable 1570 // XML comment on 'construct' has badly formed XML — 'reason'
#pragma warning disable 1587 // XML comment is not placed on a valid element

namespace HtmlParserSharp
{
    /// <summary>
    /// The tree builder glue for building a tree through the public DOM APIs.
    /// </summary>

	public class XmlTreeBuilder : CoalescingTreeBuilder<XmlElement>
	{
		/// <summary>
		/// The current doc.
		/// </summary>
		private XmlDocument document;

		override protected void AddAttributesToElement(XmlElement element, HtmlAttributes attributes) {
			for (int i = 0; i < attributes.Length; i++) {
				String localName = attributes.GetLocalName(i);
				String uri = attributes.GetURI(i);
				if (!element.HasAttribute(localName, uri)) {
					element.SetAttribute(localName, uri, attributes.GetValue(i));
				}
			}
		}

		override protected void AppendCharacters(XmlElement parent, string text)
		{
			XmlNode lastChild = parent.LastChild;
			if (lastChild != null && lastChild.NodeType == XmlNodeType.Text) {
				XmlText lastAsText = (XmlText) lastChild;
				lastAsText.Data += text;
				return;
			}
			parent.AppendChild(document.CreateTextNode(text));
		}

		override protected void AppendChildrenToNewParent(XmlElement oldParent, XmlElement newParent) {
			while (oldParent.HasChildNodes) {
				newParent.AppendChild(oldParent.FirstChild);
			}
		}

		protected override void AppendDoctypeToDocument(string name, string publicIdentifier, string systemIdentifier)
		{
			// TODO: this method was not there originally. is it correct?

			if (publicIdentifier == String.Empty)
				publicIdentifier = null;
			if (systemIdentifier == String.Empty)
				systemIdentifier = null;

            throw new NotImplementedException();

			//var doctype = document.CreateDocumentType(name, publicIdentifier, systemIdentifier, null);
			//document.XmlResolver = new XmlUrlResolver();
			//document.AppendChild(doctype);
		}

		override protected void AppendComment(XmlElement parent, String comment)
		{
			parent.AppendChild(document.CreateComment(comment));
		}

		override protected void AppendCommentToDocument(String comment)
		{
			document.AppendChild(document.CreateComment(comment));
		}

		override protected XmlElement CreateElement(string ns, string name, HtmlAttributes attributes)
		{
			XmlElement rv = document.CreateElement(name, ns);
			for (int i = 0; i < attributes.Length; i++)
			{
				rv.SetAttribute(attributes.GetLocalName(i), attributes.GetURI(i), attributes.GetValue(i));
				if (attributes.GetType(i) == "ID")
				{
					//rv.setIdAttributeNS(null, attributes.GetLocalName(i), true); // FIXME
				}
			}
			return rv;
		}

		override protected XmlElement CreateHtmlElementSetAsRoot(HtmlAttributes attributes)
		{
			XmlElement rv = document.CreateElement("html", "http://www.w3.org/1999/xhtml");
			for (int i = 0; i < attributes.Length; i++) {
				rv.SetAttribute(attributes.GetLocalName(i), attributes.GetURI(i), attributes.GetValue(i));
			}
			document.AppendChild(rv);
			return rv;
		}

		override protected void AppendElement(XmlElement child, XmlElement newParent)
		{
			newParent.AppendChild(child);
		}

		override protected bool HasChildren(XmlElement element)
		{
			return element.HasChildNodes;
		}

		override protected XmlElement CreateElement(string ns, string name, HtmlAttributes attributes, XmlElement form) {
			XmlElement rv = CreateElement(ns, name, attributes);
			//rv.setUserData("nu.validator.form-pointer", form, null); // TODO
			return rv;
		}

		override protected void Start(bool fragment) {
			document = new XmlDocument(); // implementation.createDocument(null, null, null);
			// TODO: fragment?
		}

		protected override void ReceiveDocumentMode(DocumentMode mode, String publicIdentifier,
				String systemIdentifier, bool html4SpecificAdditionalErrorChecks)
				{
			//document.setUserData("nu.validator.document-mode", mode, null); // TODO
		}

		/// <summary>
		/// Returns the document.
		/// </summary>
		/// <returns>The document</returns>
		internal XmlDocument Document
		{
			get
			{
				return document;
			}
		}

		/// <summary>
		/// Return the document fragment.
		/// </summary>
		/// <returns>The document fragment</returns>
		internal XmlDocumentFragment getDocumentFragment() {
			XmlDocumentFragment rv = document.CreateDocumentFragment();
			XmlNode rootElt = document.FirstChild;
			while (rootElt.HasChildNodes) {
				rv.AppendChild(rootElt.FirstChild);
			}
			document = null;
			return rv;
		}

		override protected void InsertFosterParentedCharacters(string text,	XmlElement table, XmlElement stackParent) {
			XmlNode parent = table.ParentNode;
			if (parent != null) { // always an element if not null
				XmlNode previousSibling = table.PreviousSibling;
				if (previousSibling != null
						&& previousSibling.NodeType == XmlNodeType.Text) {
					XmlText lastAsText = (XmlText) previousSibling;
					lastAsText.Data += text;
					return;
				}
				parent.InsertBefore(document.CreateTextNode(text), table);
				return;
			}
			XmlNode lastChild = stackParent.LastChild;
			if (lastChild != null && lastChild.NodeType == XmlNodeType.Text) {
				XmlText lastAsText = (XmlText) lastChild;
				lastAsText.Data += text;
				return;
			}
			stackParent.AppendChild(document.CreateTextNode(text));
		}

		override protected void InsertFosterParentedChild(XmlElement child, XmlElement table, XmlElement stackParent) {
			XmlNode parent = table.ParentNode;
			if (parent != null) { // always an element if not null
				parent.InsertBefore(child, table);
			} else {
				stackParent.AppendChild(child);
			}
		}

		override protected void DetachFromParent(XmlElement element)
		{
			XmlNode parent = element.ParentNode;
			if (parent != null) {
				parent.RemoveChild(element);
			}
		}
	}
}
