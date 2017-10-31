/*
 * Copyright (c) 2007 Henri Sivonen
 * Copyright (c) 2008-2011 Mozilla Foundation
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
	/// <summary>
	/// Be careful with this class. QName is the name in from HTML tokenization.
	/// Otherwise, please refer to the interface doc.
	/// </summary>
	public sealed class HtmlAttributes : IEquatable<HtmlAttributes> /* : Sax.IAttributes*/ {

		// [NOCPP[

		private static readonly AttributeName[] EMPTY_ATTRIBUTENAMES = new AttributeName[0];

		private static readonly string[] EMPTY_stringS = new string[0];

		// ]NOCPP]

		public static readonly HtmlAttributes EMPTY_ATTRIBUTES = new HtmlAttributes(AttributeName.HTML);

		private int mode;

		private int length;

	    private AttributeName[] names;

		private string[] values;

		// [NOCPP[

		private string idValue;

		private int xmlnsLength;

		private AttributeName[] xmlnsNames;

		private string[] xmlnsValues;

		// ]NOCPP]

		public HtmlAttributes(int mode)
		{
			this.mode = mode;
			this.length = 0;
			/*
			 * The length of 5 covers covers 98.3% of elements
			 * according to Hixie
			 */
			this.names = new AttributeName[5];
			this.values = new string[5];

			// [NOCPP[

			this.idValue = null;

			this.xmlnsLength = 0;

			this.xmlnsNames = HtmlAttributes.EMPTY_ATTRIBUTENAMES;

			this.xmlnsValues = HtmlAttributes.EMPTY_stringS;

			// ]NOCPP]
		}
		/*
		public HtmlAttributes(HtmlAttributes other) {
			this.mode = other.mode;
			this.length = other.length;
			this.names = new AttributeName[other.length];
			this.values = new string[other.length];
			// [NOCPP[
			this.idValue = other.idValue;
			this.xmlnsLength = other.xmlnsLength;
			this.xmlnsNames = new AttributeName[other.xmlnsLength];
			this.xmlnsValues = new string[other.xmlnsLength];
			// ]NOCPP]
		}
		*/

		/// <summary>
		/// Only use with a static argument
		/// </summary>
		public int GetIndex(AttributeName name)
		{
			for (int i = 0; i < length; i++)
			{
				if (names[i] == name)
				{
					return i;
				}
			}
			return -1;
		}

		// [NOCPP[

		public int GetIndex(string qName)
		{
			for (int i = 0; i < length; i++)
			{
				if (names[i].GetQName(mode) == qName)
				{
					return i;
				}
			}
			return -1;
		}

		public int GetIndex(string uri, string localName)
		{
			for (int i = 0; i < length; i++)
			{
				if (names[i].GetLocal(mode) == localName
						&& names[i].GetUri(mode) == uri)
				{
					return i;
				}
			}
			return -1;
		}

		public string GetType(string qName)
		{
			int index = GetIndex(qName);
			if (index == -1)
			{
				return null;
			}
			else
			{
				return GetType(index);
			}
		}

		public string GetType(string uri, string localName)
		{
			int index = GetIndex(uri, localName);
			if (index == -1)
			{
				return null;
			}
			else
			{
				return GetType(index);
			}
		}

		public string GetValue(string qName)
		{
			int index = GetIndex(qName);
			if (index == -1)
			{
				return null;
			}
			else
			{
				return GetValue(index);
			}
		}

		public string GetValue(string uri, string localName)
		{
			int index = GetIndex(uri, localName);
			if (index == -1)
			{
				return null;
			}
			else
			{
				return GetValue(index);
			}
		}

		// ]NOCPP]

		public int Length
		{
			get
			{
				return length;
			}
		}

		[Local]
		public string GetLocalName(int index)
		{
			if (index < length && index >= 0)
			{
				return names[index].GetLocal(mode);
			}
			else
			{
				return null;
			}
		}

		// [NOCPP[

		public string GetQName(int index)
		{
			if (index < length && index >= 0)
			{
				return names[index].GetQName(mode);
			}
			else
			{
				return null;
			}
		}

		public string GetType(int index)
		{
			if (index < length && index >= 0)
			{
				return (names[index] == AttributeName.ID) ? "ID" : "CDATA";
			}
			else
			{
				return null;
			}
		}

		// ]NOCPP]

		public AttributeName GetAttributeName(int index)
		{
			if (index < length && index >= 0)
			{
				return names[index];
			}
			else
			{
				return null;
			}
		}

		[NsUri]
		public string GetURI(int index)
		{
			if (index < length && index >= 0)
			{
				return names[index].GetUri(mode);
			}
			else
			{
				return null;
			}
		}

		[Prefix]
		public string GetPrefix(int index)
		{
			if (index < length && index >= 0)
			{
				return names[index].GetPrefix(mode);
			}
			else
			{
				return null;
			}
		}

		public string GetValue(int index)
		{
			if (index < length && index >= 0)
			{
				return values[index];
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Only use with static argument.
		/// </summary>
		public string GetValue(AttributeName name)
		{
			int index = GetIndex(name);
			if (index == -1)
			{
				return null;
			}
			else
			{
				return GetValue(index);
			}
		}

		// [NOCPP[

		public string Id
		{
			get
			{
				return idValue;
			}
		}

		public int XmlnsLength
		{
			get
			{
				return xmlnsLength;
			}
		}

		[Local]
		public string GetXmlnsLocalName(int index)
		{
			if (index < xmlnsLength && index >= 0)
			{
				return xmlnsNames[index].GetLocal(mode);
			}
			else
			{
				return null;
			}
		}

		[NsUri]
		public string GetXmlnsURI(int index)
		{
			if (index < xmlnsLength && index >= 0)
			{
				return xmlnsNames[index].GetUri(mode);
			}
			else
			{
				return null;
			}
		}

		public string GetXmlnsValue(int index)
		{
			if (index < xmlnsLength && index >= 0)
			{
				return xmlnsValues[index];
			}
			else
			{
				return null;
			}
		}

		public int GetXmlnsIndex(AttributeName name)
		{
			for (int i = 0; i < xmlnsLength; i++)
			{
				if (xmlnsNames[i] == name)
				{
					return i;
				}
			}
			return -1;
		}

		public string GetXmlnsValue(AttributeName name)
		{
			int index = GetXmlnsIndex(name);
			if (index == -1)
			{
				return null;
			}
			else
			{
				return GetXmlnsValue(index);
			}
		}

		public AttributeName GetXmlnsAttributeName(int index)
		{
			if (index < xmlnsLength && index >= 0)
			{
				return xmlnsNames[index];
			}
			else
			{
				return null;
			}
		}

		// ]NOCPP]

		internal void AddAttribute(AttributeName name, string value
			// [NOCPP[
				, XmlViolationPolicy xmlnsPolicy
			// ]NOCPP]        
		)
		{
			// [NOCPP[
			if (name == AttributeName.ID)
			{
				idValue = value;
			}

			if (name.IsXmlns)
			{
				if (xmlnsNames.Length == xmlnsLength)
				{
					int newLen = xmlnsLength == 0 ? 2 : xmlnsLength << 1;
					AttributeName[] newNames = new AttributeName[newLen];
					Array.Copy(xmlnsNames, newNames, xmlnsNames.Length);
                    
					xmlnsNames = newNames;
					string[] newValues = new string[newLen];
					Array.Copy(xmlnsValues, newValues, xmlnsValues.Length);
					xmlnsValues = newValues;
				}
				xmlnsNames[xmlnsLength] = name;
				xmlnsValues[xmlnsLength] = value;
				xmlnsLength++;
				switch (xmlnsPolicy)
				{
					case XmlViolationPolicy.Fatal:
						// this is ugly (TODO)
						throw new Exception("Saw an xmlns attribute.");
					case XmlViolationPolicy.AlterInfoset:
						return;
					case XmlViolationPolicy.Allow:
						break; // fall through
				}
			}

			// ]NOCPP]

			if (names.Length == length)
			{
				int newLen = length << 1; // The first growth covers virtually
				// 100% of elements according to
				// Hixie
				AttributeName[] newNames = new AttributeName[newLen];
				Array.Copy(names, newNames, names.Length);
				names = newNames;
				string[] newValues = new string[newLen];
				Array.Copy(values, newValues, values.Length);
				values = newValues;
			}
			names[length] = name;
			values[length] = value;
			length++;
		}

		internal void Clear(int m)
		{
			for (int i = 0; i < length; i++)
			{
				names[i] = null;
				values[i] = null;
			}
			length = 0;
			mode = m;
			// [NOCPP[
			idValue = null;
			for (int i = 0; i < xmlnsLength; i++)
			{
				xmlnsNames[i] = null;
				xmlnsValues[i] = null;
			}
			xmlnsLength = 0;
			// ]NOCPP]
		}

		/// <summary>
		/// This is only used for <code>AttributeName</code> ownership transfer
		/// in the isindex case to avoid freeing custom names twice in C++.
		/// </summary>
		internal void ClearWithoutReleasingContents()
		{
			for (int i = 0; i < length; i++)
			{
				names[i] = null;
				values[i] = null;
			}
			length = 0;
		}

		public bool Contains(AttributeName name)
		{
			for (int i = 0; i < length; i++)
			{
				if (name.Equals(names[i]))
				{
					return true;
				}
			}
			// [NOCPP[
			for (int i = 0; i < xmlnsLength; i++)
			{
				if (name.Equals(xmlnsNames[i]))
				{
					return true;
				}
			}
			// ]NOCPP]
			return false;
		}

		public void AdjustForMath()
		{
			mode = AttributeName.MATHML;
		}

		public void AdjustForSvg()
		{
			mode = AttributeName.SVG;
		}

		public HtmlAttributes CloneAttributes()
		{
			Debug.Assert((length == 0 && xmlnsLength == 0) || mode == 0 || mode == 3);
			HtmlAttributes clone = new HtmlAttributes(0);
			for (int i = 0; i < length; i++)
			{
				clone.AddAttribute(names[i].CloneAttributeName(), values[i]
					// [NOCPP[
					   , XmlViolationPolicy.Allow
					// ]NOCPP]
				);
			}
			// [NOCPP[
			for (int i = 0; i < xmlnsLength; i++)
			{
				clone.AddAttribute(xmlnsNames[i],
						xmlnsValues[i], XmlViolationPolicy.Allow);
			}
			// ]NOCPP]
			return clone; // XXX!!!
		}

		public bool Equals(HtmlAttributes other)
		{
			Debug.Assert(mode == 0 || mode == 3, "Trying to compare attributes in foreign content.");
			int otherLength = other.Length;
			if (length != otherLength)
			{
				return false;
			}
			for (int i = 0; i < length; i++)
			{
				// Work around the limitations of C++
				bool found = false;
				// The comparing just the local names is OK, since these attribute
				// holders are both supposed to belong to HTML formatting elements
				/*[Local]*/
				string ownLocal = names[i].GetLocal(AttributeName.HTML);
				for (int j = 0; j < otherLength; j++)
				{
					if (ownLocal == other.names[j].GetLocal(AttributeName.HTML))
					{
						found = true;
						if (values[i] != other.values[j])
						{
							return false;
						}
					}
				}
				if (!found)
				{
					return false;
				}
			}
			return true;
		}

		// [NOCPP[

		internal void ProcessNonNcNames<T>(TreeBuilder<T> treeBuilder, XmlViolationPolicy namePolicy) where T : class
		{
			for (int i = 0; i < length; i++)
			{
				AttributeName attName = names[i];
				if (!attName.IsNcName(mode))
				{
					string name = attName.GetLocal(mode);
					switch (namePolicy)
					{
						case XmlViolationPolicy.AlterInfoset:
							names[i] = AttributeName.Create(NCName.EscapeName(name));
							goto case XmlViolationPolicy.Allow; // fall through
						case XmlViolationPolicy.Allow:
							if (attName != AttributeName.XML_LANG)
							{
								treeBuilder.Warn("Attribute \u201C" + name + "\u201D is not serializable as XML 1.0.");
							}
							break;
						case XmlViolationPolicy.Fatal:
							treeBuilder.Fatal("Attribute \u201C" + name + "\u201D is not serializable as XML 1.0.");
							break;
					}
				}
			}
		}

		public void Merge(HtmlAttributes attributes)
		{
			int len = attributes.Length;
			for (int i = 0; i < len; i++)
			{
				AttributeName name = attributes.GetAttributeName(i);
				if (!Contains(name))
				{
					AddAttribute(name, attributes.GetValue(i), XmlViolationPolicy.Allow);
				}
			}
		}

		// ]NOCPP]
	}
}
