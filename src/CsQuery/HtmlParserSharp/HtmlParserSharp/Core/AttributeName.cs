/*
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
using HtmlParserSharp.Common;

#pragma warning disable 1591
#pragma warning disable 1570

namespace HtmlParserSharp.Core
{
	public sealed partial class AttributeName
	{
        
		// [NOCPP[

		public const int NCNAME_HTML = 1;

		public const int NCNAME_FOREIGN = (1 << 1) | (1 << 2);

		public const int NCNAME_LANG = (1 << 3);

		public const int IS_XMLNS = (1 << 4);

		public const int CASE_FOLDED = (1 << 5);

		public const int BOOLEAN = (1 << 6);

		// ]NOCPP]

		/// <summary>
		/// An array representing no namespace regardless of namespace mode (HTML,
		/// SVG, MathML, lang-mapping HTML) used.
		/// </summary>
		[NsUri]
		private static readonly string[] ALL_NO_NS = { "", "", "",
	// [NOCPP[
			""
	// ]NOCPP]
	};

		/// <summary>
		/// An array that has no namespace for the HTML mode but the XMLNS namespace
		/// for the SVG and MathML modes.
		/// </summary>
		[NsUri]
		private static readonly string[] XMLNS_NS = { "",
			"http://www.w3.org/2000/xmlns/", "http://www.w3.org/2000/xmlns/",
			// [NOCPP[
			""
	// ]NOCPP]
	};

		/// <summary>
		/// An array that has no namespace for the HTML mode but the XML namespace
		/// for the SVG and MathML modes.
		/// </summary>
		[NsUri]
		private static readonly string[] XML_NS = { "",
			"http://www.w3.org/XML/1998/namespace",
			"http://www.w3.org/XML/1998/namespace",
			// [NOCPP[
			""
	// ]NOCPP]
	};

		/// <summary>
		/// An array that has no namespace for the HTML mode but the XLink namespace
		/// for the SVG and MathML modes.
		/// </summary>
		[NsUri]
		private static readonly string[] XLINK_NS = { "",
			"http://www.w3.org/1999/xlink", "http://www.w3.org/1999/xlink",
			// [NOCPP[
			""
	// ]NOCPP]
	};

		// [NOCPP[
		/// <summary>
		/// An array that has no namespace for the HTML, SVG and MathML modes but has
		/// the XML namespace for the lang-mapping HTML mode.
		/// </summary>
		[NsUri]
		private static readonly string[] LANG_NS = { "", "", "",
			"http://www.w3.org/XML/1998/namespace" };

		// ]NOCPP]

		/// <summary>
		/// An array for no prefixes in any mode.
		/// </summary>
		[Prefix]
		static readonly string[] ALL_NO_PREFIX = { null, null, null,
	// [NOCPP[
			null
	// ]NOCPP]
	};

		/// <summary>
		/// An array for no prefixe in the HTML mode and the 
		/// <code>xmlns</code> prefix in the SVG and MathML modes.
		/// </summary>
		[Prefix]
		private static readonly string[] XMLNS_PREFIX = { null,
			"xmlns", "xmlns",
			// [NOCPP[
			null
	// ]NOCPP]
	};

		/// <summary>
		/// An array for no prefixe in the HTML mode and the 
		/// <code>xlink</code>
		/// prefix in the SVG and MathML modes.
		/// </summary>
		[Prefix]
		private static readonly string[] XLINK_PREFIX = { null,
			"xlink", "xlink",
			// [NOCPP[
			null
	// ]NOCPP]
	};

		/// <summary>
		/// An array for no prefixe in the HTML mode and the 
		/// <code>xml</code> prefix in the SVG and MathML modes.
		/// </summary>
		[Prefix]
		private static readonly string[] XML_PREFIX = { null, "xml",
			"xml",
			// [NOCPP[
			null
	// ]NOCPP]
	};

		// [NOCPP[

		[Prefix]
		private static readonly string[] LANG_PREFIX = { null, null,
			null, "xml" };

		private static string[] COMPUTE_QNAME(String[] local, String[] prefix)
		{
			string[] arr = new string[4];
			for (int i = 0; i < arr.Length; i++)
			{
				if (prefix[i] == null)
				{
					arr[i] = local[i];
				}
				else
				{
					arr[i] = prefix[i] + ':' + local[i];
				}
			}
			return arr;
		}

		// ]NOCPP]

		/// <summary>
		/// An initialization helper for having a one name in the SVG mode and
		/// another name in the other modes.
		/// </summary>
		/// <param name="name">The name for the non-SVG modes</param>
		/// <param name="camel">The name for the SVG mode</param>
		/// <returns>The initialized name array</returns>
		[Local]
		private static string[] SVG_DIFFERENT([Local] string name, [Local] string camel)
		{
			/*[Local]*/
			string[] arr = new string[4];
			arr[0] = name;
			arr[1] = name;
			arr[2] = camel;
			// [NOCPP[
			arr[3] = name;
			// ]NOCPP]
			return arr;
		}

		/// <summary>
		/// An initialization helper for having a one name in the MathML mode and
		/// another name in the other modes.
		/// </summary>
		/// <param name="name">The name for the non-MathML modes</param>
		/// <param name="camel">The name for the MathML mode</param>
		/// <returns>The initialized name array</returns>
		[Local]
		private static string[] MATH_DIFFERENT([Local] string name, [Local] string camel)
		{
			/*[Local]*/
			string[] arr = new string[4];
			arr[0] = name;
			arr[1] = camel;
			arr[2] = name;
			// [NOCPP[
			arr[3] = name;
			// ]NOCPP]
			return arr;
		}

		/// <summary>
		/// An initialization helper for having a different local name in the HTML
		/// mode and the SVG and MathML modes.
		/// </summary>
		/// <param name="name">The name for the HTML mode</param>
		/// <param name="suffix">The name for the SVG and MathML modes</param>
		/// <returns>The initialized name array</returns>
		[Local]
		private static string[] COLONIFIED_LOCAL([Local] string name, [Local] string suffix)
		{
			/*[Local]*/
			string[] arr = new string[4];
			arr[0] = name;
			arr[1] = suffix;
			arr[2] = suffix;
			// [NOCPP[
			arr[3] = name;
			// ]NOCPP]
			return arr;
		}

		/// <summary>
		/// An initialization helper for having the same local name in all modes.
		/// </summary>
		/// <param name="name">The name</param>
		/// <returns>The initialized name array</returns>
		[Local]
		static string[] SAME_LOCAL([Local] string name)
		{
			/*[Local]*/
			string[] arr = new string[4];
			arr[0] = name;
			arr[1] = name;
			arr[2] = name;
			// [NOCPP[
			arr[3] = name;
			// ]NOCPP]
			return arr;
		}


		/// <summary>
		/// Returns an attribute name by buffer.
		/// <p/>
		/// C++ ownership: The return value is either released by the caller if the
		/// attribute is a duplicate or the ownership is transferred to
		/// HtmlAttributes and released upon clearing or destroying that object.
		/// </summary>
		/// <param name="buf">The buffer</param>
		/// <param name="offset">ignored</param>
		/// <param name="length">Length of data</param>
		/// <param name="checkNcName">Whether to check ncnameness</param>
		/// <returns>An <code>AttributeName</code> corresponding to the argument data</returns>
		internal static AttributeName NameByBuffer(char[] buf, int offset, int length
			// [NOCPP[
				, bool checkNcName
			// ]NOCPP]
				)
		{
			// XXX deal with offset
			int hash = AttributeName.BufToHash(buf, length);
			int index = Array.BinarySearch<int>(AttributeName.ATTRIBUTE_HASHES, hash);
			if (index < 0)
			{
				return AttributeName.CreateAttributeName(
						Portability.NewLocalNameFromBuffer(buf, offset, length)
					// [NOCPP[
						, checkNcName
					// ]NOCPP]
				);
			}
			else
			{
				AttributeName attributeName = AttributeName.ATTRIBUTE_NAMES[index];
				/*[Local]*/
				string name = attributeName.GetLocal(AttributeName.HTML);
				if (!Portability.LocalEqualsBuffer(name, buf, offset, length))
				{
					return AttributeName.CreateAttributeName(
							Portability.NewLocalNameFromBuffer(buf, offset, length)
						// [NOCPP[
							, checkNcName
						// ]NOCPP]
					);
				}
				return attributeName;
			}
		}


		/// <summary>
		/// This method has to return a unique integer for each well-known
		/// lower-cased attribute name.
		/// </summary>
		/// <param name="buf">The buffer.</param>
		/// <param name="len">The length.</param>
		/// <returns></returns>
		private static int BufToHash(char[] buf, int len)
		{
			int hash2 = 0;
			int hash = len;
			hash <<= 5;
			hash += buf[0] - 0x60;
			int j = len;
			for (int i = 0; i < 4 && j > 0; i++)
			{
				j--;
				hash <<= 5;
				hash += buf[j] - 0x60;
				hash2 <<= 6;
				hash2 += buf[i] - 0x5F;
			}
			return hash ^ hash2;
		}

		/// <summary>
		/// The mode value for HTML.
		/// </summary>
		public const int HTML = 0;

		/// <summary>
		/// The mode value for MathML.
		/// </summary>
		public const int MATHML = 1;

		/// <summary>
		/// The mode value for SVG.
		/// </summary>
		public const int SVG = 2;

		// [NOCPP[

		/// <summary>
		/// The mode value for lang-mapping HTML.
		/// </summary>
		public const int HTML_LANG = 3;

		// ]NOCPP]

		/// <summary>
		/// The namespaces indexable by mode.
		/// </summary>
		[NsUri]
		private readonly string[] uri;

		/// <summary>
		/// The local names indexable by mode.
		/// </summary>
		[Local]
		private readonly string[] local;

		/// <summary>
		/// The prefixes indexably by mode.
		/// </summary>
		[Prefix]
		private readonly string[] prefix;

		// [NOCPP[

		private readonly int flags;

		/// <summary>
		/// The qnames indexable by mode.
		/// </summary>
		private readonly string[] qName;

		// ]NOCPP]

		/// <summary>
		/// Initializes a new instance of the <see cref="AttributeName"/> class (The startup-time constructor). 
		/// </summary>
		/// <param name="uri">The namespace.</param>
		/// <param name="local">The local name.</param>
		/// <param name="prefix">The prefix.</param>
		/// <param name="flags">The flags.</param>
		/*protected*/
		AttributeName([NsUri] string[] uri, [Local] string[] local, [Prefix] string[] prefix
			// [NOCPP[
  , int flags
			// ]NOCPP]        
)
		{
			this.uri = uri;
			this.local = local;
			this.prefix = prefix;
			// [NOCPP[
			this.qName = COMPUTE_QNAME(local, prefix);
			this.flags = flags;
			// ]NOCPP]
		}

		/// <summary>
		/// Creates an <code>AttributeName</code> for a local name.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="checkNcName">Whether to check ncnameness.</param>
		/// <returns>An <code>AttributeName</code></returns>
		private static AttributeName CreateAttributeName([Local] string name
			// [NOCPP[
				, bool checkNcName
			// ]NOCPP]
		)
		{
			// [NOCPP[
			int flags = NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG;
			if (name.StartsWith("xmlns:"))
			{
				flags = IS_XMLNS;
			}
			else if (checkNcName && !NCName.IsNCName(name))
			{
				flags = 0;
			}
			// ]NOCPP]
			return new AttributeName(AttributeName.ALL_NO_NS, AttributeName.SAME_LOCAL(name), ALL_NO_PREFIX, flags);
		}

        /// <summary>
        /// TODO: remove this (?)
        /// Clones the attribute using an interner. Returns
        /// <code>this</code> in Java and for non-dynamic instances in C++.
        /// </summary>
        ///
        /// <returns>
        /// A clone.
        /// </returns>

		public /*virtual*/ AttributeName CloneAttributeName(/*Interner interner*/)
		{
			return this;
		}

		// [NOCPP[
		/// <summary>
		/// Creator for use when the XML violation policy requires an attribute name
		/// to be changed.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns>The name of the attribute to create</returns>
		internal static AttributeName Create(string name)
		{
			return new AttributeName(AttributeName.ALL_NO_NS,
					AttributeName.SAME_LOCAL(name), ALL_NO_PREFIX,
					NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		}

		/// <summary>
		/// Determines whether this name is an XML 1.0 4th ed. NCName.
		/// </summary>
		/// <param name="mode">The SVG/MathML/HTML mode</param>
		/// <returns>
		///   <c>true</c> if if this is an NCName in the given mode; otherwise, <c>false</c>.
		/// </returns>
		public bool IsNcName(int mode)
		{
			return (flags & (1 << mode)) != 0;
		}

		/// <summary>
		/// Queries whether this is an <code>xmlns</code> attribute.
		/// </summary>
		/// <returns>
		///   <c>true</c> if this is an <code>xmlns</code> attribute; otherwise, <c>false</c>.
		/// </returns>
		public bool IsXmlns
		{
			get
			{
				return (flags & IS_XMLNS) != 0;
			}
		}

		/// <summary>
		/// Determines whether this attribute has a case-folded value in the HTML4 mode
		/// of the parser.
		/// </summary>
		/// <returns>
		///   <c>true</c> if the value is case-folded; otherwise, <c>false</c>.
		/// </returns>
		internal bool IsCaseFolded
		{
			get
			{
				return (flags & CASE_FOLDED) != 0;
			}
		}

		internal bool IsBoolean
		{
			get
			{
				return (flags & BOOLEAN) != 0;
			}
		}

		public string GetQName(int mode)
		{
			return qName[mode];
		}

		// ]NOCPP]

		[NsUri]
		public string GetUri(int mode)
		{
			return uri[mode];
		}

		[Local]
		public string GetLocal(int mode)
		{
			return local[mode];
		}

		[Prefix]
		public string GetPrefix(int mode)
		{
			return prefix[mode];
		}

        public override int GetHashCode()
        {
            var name = GetLocal(0);
            return BufToHash(name.ToCharArray(), name.Length);
        }
        
        public override bool Equals(object obj)
        {
            var other = obj as AttributeName;

            return other != null && GetLocal(0) == other.GetLocal(0);
        }

        public static bool operator ==(AttributeName a, AttributeName b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            if (((object)a == null) || ((object)b == null))
            {
                return false;
            }

            // Return true if the fields match:
            return a.Equals(b);
        }

        public static bool operator !=(AttributeName a, AttributeName b)
        {
            return !(a == b);
        }

		// START CODE ONLY USED FOR GENERATING CODE uncomment to regenerate

		//override public String toString() {
		//    return "(" + formatNs() + ", " + formatLocal() + ", " + formatPrefix()
		//            + ", " + formatFlags() + ")";
		//}

		//private String formatFlags() {
		//    StringBuilder builder = new StringBuilder();
		//    if ((flags & NCNAME_HTML) != 0) {
		//        if (builder.Length != 0) {
		//            builder.Append(" | ");
		//        }
		//        builder.Append("NCNAME_HTML");
		//    }            
		//    if ((flags & NCNAME_FOREIGN) != 0) {
		//        if (builder.Length!= 0) {
		//            builder.Append(" | ");
		//        }
		//        builder.Append("NCNAME_FOREIGN");
		//    }
		//    if ((flags & NCNAME_LANG) != 0) {
		//        if (builder.Length != 0) {
		//            builder.Append(" | ");
		//        }
		//        builder.Append("NCNAME_LANG");
		//    }
		//    if (IsXmlns()) {
		//        if (builder.Length != 0) {
		//            builder.Append(" | ");
		//        }
		//        builder.Append("IS_XMLNS");
		//    }
		//    if (IsCaseFolded()) {
		//        if (builder.Length != 0) {
		//            builder.Append(" | ");
		//        }
		//        builder.Append("CASE_FOLDED");
		//    }
		//    if (IsBoolean()) {
		//        if (builder.Length != 0) {
		//            builder.Append(" | ");
		//        }
		//        builder.Append("BOOLEAN");
		//    }
		//    if (builder.Length == 0) {
		//        return "0";
		//    }
		//    return builder.ToString();
		//}

		//public int compareTo(AttributeName other) {
		//    int thisHash = this.hash();
		//    int otherHash = other.hash();
		//    if (thisHash < otherHash) {
		//        return -1;
		//    } else if (thisHash == otherHash) {
		//        return 0;
		//    } else {
		//        return 1;
		//    }
		//}

		//private String formatPrefix() {
		//    if (prefix[0] == null && prefix[1] == null && prefix[2] == null
		//            && prefix[3] == null) {
		//        return "ALL_NO_PREFIX";
		//    } else if (prefix[0] == null && prefix[1] == prefix[2]
		//            && prefix[3] == null) {
		//        if ("xmlns" == prefix[1]) {
		//            return "XMLNS_PREFIX";
		//        } else if ("xml" == prefix[1]) {
		//            return "XML_PREFIX";
		//        } else if ("xlink" == prefix[1]) {
		//            return "XLINK_PREFIX";
		//        } else {
		//            throw new InvalidOperationException();
		//        }
		//    } else if (prefix[0] == null && prefix[1] == null && prefix[2] == null
		//            && prefix[3] == "xml") {
		//        return "LANG_PREFIX";
		//    } else {
		//        throw new InvalidOperationException();
		//    }
		//}

		//private String formatLocal() {
		//    if (local[0] == local[1] && local[0] == local[3]
		//            && local[0] != local[2]) {
		//        return "SVG_DIFFERENT(\"" + local[0] + "\", \"" + local[2] + "\")";
		//    }
		//    if (local[0] == local[2] && local[0] == local[3]
		//            && local[0] != local[1]) {
		//        return "MATH_DIFFERENT(\"" + local[0] + "\", \"" + local[1] + "\")";
		//    }
		//    if (local[0] == local[3] && local[1] == local[2]
		//            && local[0] != local[1]) {
		//        return "COLONIFIED_LOCAL(\"" + local[0] + "\", \"" + local[1]
		//                + "\")";
		//    }
		//    for (int i = 1; i < local.Length; i++) {
		//        if (local[0] != local[i]) {
		//            throw new InvalidOperationException();
		//        }
		//    }
		//    return "SAME_LOCAL(\"" + local[0] + "\")";
		//}

		//private String formatNs() {
		//    if (uri[0] == "" && uri[1] == "" && uri[2] == "" && uri[3] == "") {
		//        return "ALL_NO_NS";
		//    } else if (uri[0] == "" && uri[1] == uri[2] && uri[3] == "") {
		//        if ("http://www.w3.org/2000/xmlns/" == uri[1]) {
		//            return "XMLNS_NS";
		//        } else if ("http://www.w3.org/XML/1998/namespace" == uri[1]) {
		//            return "XML_NS";
		//        } else if ("http://www.w3.org/1999/xlink" == uri[1]) {
		//            return "XLINK_NS";
		//        } else {
		//            throw new InvalidOperationException();
		//        }
		//    } else if (uri[0] == "" && uri[1] == "" && uri[2] == ""
		//            && uri[3] == "http://www.w3.org/XML/1998/namespace") {
		//        return "LANG_NS";
		//    } else {
		//        throw new InvalidOperationException();
		//    }
		//}

		//private String ConstName() {
		//    String name = GetLocal(HTML);
		//    char[] buf = new char[name.Length];
		//    for (int i = 0; i < name.Length; i++) {
		//        char c = name[i];
		//        if (c == '-' || c == ':') {
		//            buf[i] = '_';
		//        } else if (c >= 'a' && c <= 'z') {
		//            buf[i] = (char) (c - 0x20);
		//        } else {
		//            buf[i] = c;
		//        }
		//    }
		//    return new String(buf);
		//}

		//private int hash() {
		//    String name = GetLocal(HTML);
		//    return BufToHash(name.ToCharArray(), name.Length);
		//}

		///**
		// * Regenerate self
		// * 
		// * @param args
		// */
		//public static void main(String[] args) {
		//    Arrays.sort(ATTRIBUTE_NAMES);
		//    for (int i = 1; i < ATTRIBUTE_NAMES.length; i++) {
		//        if (ATTRIBUTE_NAMES[i].hash() == ATTRIBUTE_NAMES[i - 1].hash()) {
		//            System.err.println("Hash collision: "
		//                    + ATTRIBUTE_NAMES[i].getLocal(HTML) + ", "
		//                    + ATTRIBUTE_NAMES[i - 1].getLocal(HTML));
		//            return;
		//        }
		//    }
		//    for (int i = 0; i < ATTRIBUTE_NAMES.length; i++) {
		//        AttributeName att = ATTRIBUTE_NAMES[i];
		//        System.out.println("public static readonly AttributeName "
		//                + att.ConstName() + " = new AttributeName" + att.toString()
		//                + ";");
		//    }
		//    System.out.println("private const static @NoLength AttributeName[] ATTRIBUTE_NAMES = {");
		//    for (int i = 0; i < ATTRIBUTE_NAMES.length; i++) {
		//        AttributeName att = ATTRIBUTE_NAMES[i];
		//        System.out.println(att.ConstName() + ",");
		//    }
		//    System.out.println("};");
		//    System.out.println("private const static int[] ATTRIBUTE_HASHES = {");
		//    for (int i = 0; i < ATTRIBUTE_NAMES.length; i++) {
		//        AttributeName att = ATTRIBUTE_NAMES[i];
		//        System.out.println(att.hash().ToString()) + ",");
		//    }
		//    System.out.println("};");
		//}

		// START GENERATED CODE
		public static readonly AttributeName D = new AttributeName(ALL_NO_NS, SAME_LOCAL("d"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName K = new AttributeName(ALL_NO_NS, SAME_LOCAL("k"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName R = new AttributeName(ALL_NO_NS, SAME_LOCAL("r"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName X = new AttributeName(ALL_NO_NS, SAME_LOCAL("x"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName Y = new AttributeName(ALL_NO_NS, SAME_LOCAL("y"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName Z = new AttributeName(ALL_NO_NS, SAME_LOCAL("z"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName BY = new AttributeName(ALL_NO_NS, SAME_LOCAL("by"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName CX = new AttributeName(ALL_NO_NS, SAME_LOCAL("cx"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName CY = new AttributeName(ALL_NO_NS, SAME_LOCAL("cy"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName DX = new AttributeName(ALL_NO_NS, SAME_LOCAL("dx"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName DY = new AttributeName(ALL_NO_NS, SAME_LOCAL("dy"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName G2 = new AttributeName(ALL_NO_NS, SAME_LOCAL("g2"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName G1 = new AttributeName(ALL_NO_NS, SAME_LOCAL("g1"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName FX = new AttributeName(ALL_NO_NS, SAME_LOCAL("fx"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName FY = new AttributeName(ALL_NO_NS, SAME_LOCAL("fy"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName K4 = new AttributeName(ALL_NO_NS, SAME_LOCAL("k4"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName K2 = new AttributeName(ALL_NO_NS, SAME_LOCAL("k2"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName K3 = new AttributeName(ALL_NO_NS, SAME_LOCAL("k3"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName K1 = new AttributeName(ALL_NO_NS, SAME_LOCAL("k1"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ID = new AttributeName(ALL_NO_NS, SAME_LOCAL("id"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName IN = new AttributeName(ALL_NO_NS, SAME_LOCAL("in"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName U2 = new AttributeName(ALL_NO_NS, SAME_LOCAL("u2"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName U1 = new AttributeName(ALL_NO_NS, SAME_LOCAL("u1"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName RT = new AttributeName(ALL_NO_NS, SAME_LOCAL("rt"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName RX = new AttributeName(ALL_NO_NS, SAME_LOCAL("rx"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName RY = new AttributeName(ALL_NO_NS, SAME_LOCAL("ry"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName TO = new AttributeName(ALL_NO_NS, SAME_LOCAL("to"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName Y2 = new AttributeName(ALL_NO_NS, SAME_LOCAL("y2"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName Y1 = new AttributeName(ALL_NO_NS, SAME_LOCAL("y1"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName X1 = new AttributeName(ALL_NO_NS, SAME_LOCAL("x1"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName X2 = new AttributeName(ALL_NO_NS, SAME_LOCAL("x2"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ALT = new AttributeName(ALL_NO_NS, SAME_LOCAL("alt"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName DIR = new AttributeName(ALL_NO_NS, SAME_LOCAL("dir"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED);
		public static readonly AttributeName DUR = new AttributeName(ALL_NO_NS, SAME_LOCAL("dur"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName END = new AttributeName(ALL_NO_NS, SAME_LOCAL("end"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName FOR = new AttributeName(ALL_NO_NS, SAME_LOCAL("for"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName IN2 = new AttributeName(ALL_NO_NS, SAME_LOCAL("in2"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName MAX = new AttributeName(ALL_NO_NS, SAME_LOCAL("max"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName MIN = new AttributeName(ALL_NO_NS, SAME_LOCAL("min"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName LOW = new AttributeName(ALL_NO_NS, SAME_LOCAL("low"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName REL = new AttributeName(ALL_NO_NS, SAME_LOCAL("rel"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName REV = new AttributeName(ALL_NO_NS, SAME_LOCAL("rev"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SRC = new AttributeName(ALL_NO_NS, SAME_LOCAL("src"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName AXIS = new AttributeName(ALL_NO_NS, SAME_LOCAL("axis"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ABBR = new AttributeName(ALL_NO_NS, SAME_LOCAL("abbr"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName BBOX = new AttributeName(ALL_NO_NS, SAME_LOCAL("bbox"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName CITE = new AttributeName(ALL_NO_NS, SAME_LOCAL("cite"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName CODE = new AttributeName(ALL_NO_NS, SAME_LOCAL("code"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName BIAS = new AttributeName(ALL_NO_NS, SAME_LOCAL("bias"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName COLS = new AttributeName(ALL_NO_NS, SAME_LOCAL("cols"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName CLIP = new AttributeName(ALL_NO_NS, SAME_LOCAL("clip"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName CHAR = new AttributeName(ALL_NO_NS, SAME_LOCAL("char"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName BASE = new AttributeName(ALL_NO_NS, SAME_LOCAL("base"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName EDGE = new AttributeName(ALL_NO_NS, SAME_LOCAL("edge"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName DATA = new AttributeName(ALL_NO_NS, SAME_LOCAL("data"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName FILL = new AttributeName(ALL_NO_NS, SAME_LOCAL("fill"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName FROM = new AttributeName(ALL_NO_NS, SAME_LOCAL("from"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName FORM = new AttributeName(ALL_NO_NS, SAME_LOCAL("form"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName FACE = new AttributeName(ALL_NO_NS, SAME_LOCAL("face"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName HIGH = new AttributeName(ALL_NO_NS, SAME_LOCAL("high"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName HREF = new AttributeName(ALL_NO_NS, SAME_LOCAL("href"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName OPEN = new AttributeName(ALL_NO_NS, SAME_LOCAL("open"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ICON = new AttributeName(ALL_NO_NS, SAME_LOCAL("icon"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName NAME = new AttributeName(ALL_NO_NS, SAME_LOCAL("name"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName MODE = new AttributeName(ALL_NO_NS, SAME_LOCAL("mode"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName MASK = new AttributeName(ALL_NO_NS, SAME_LOCAL("mask"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName LINK = new AttributeName(ALL_NO_NS, SAME_LOCAL("link"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName LANG = new AttributeName(LANG_NS, SAME_LOCAL("lang"), LANG_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName LOOP = new AttributeName(ALL_NO_NS, SAME_LOCAL("loop"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName LIST = new AttributeName(ALL_NO_NS, SAME_LOCAL("list"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName TYPE = new AttributeName(ALL_NO_NS, SAME_LOCAL("type"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED);
		public static readonly AttributeName WHEN = new AttributeName(ALL_NO_NS, SAME_LOCAL("when"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName WRAP = new AttributeName(ALL_NO_NS, SAME_LOCAL("wrap"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName TEXT = new AttributeName(ALL_NO_NS, SAME_LOCAL("text"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName PATH = new AttributeName(ALL_NO_NS, SAME_LOCAL("path"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName PING = new AttributeName(ALL_NO_NS, SAME_LOCAL("ping"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName REFX = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("refx", "refX"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName REFY = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("refy", "refY"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SIZE = new AttributeName(ALL_NO_NS, SAME_LOCAL("size"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SEED = new AttributeName(ALL_NO_NS, SAME_LOCAL("seed"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ROWS = new AttributeName(ALL_NO_NS, SAME_LOCAL("rows"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SPAN = new AttributeName(ALL_NO_NS, SAME_LOCAL("span"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName STEP = new AttributeName(ALL_NO_NS, SAME_LOCAL("step"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED);
		public static readonly AttributeName ROLE = new AttributeName(ALL_NO_NS, SAME_LOCAL("role"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName XREF = new AttributeName(ALL_NO_NS, SAME_LOCAL("xref"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ASYNC = new AttributeName(ALL_NO_NS, SAME_LOCAL("async"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED | BOOLEAN);
		public static readonly AttributeName ALINK = new AttributeName(ALL_NO_NS, SAME_LOCAL("alink"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ALIGN = new AttributeName(ALL_NO_NS, SAME_LOCAL("align"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED);
		public static readonly AttributeName CLOSE = new AttributeName(ALL_NO_NS, SAME_LOCAL("close"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName COLOR = new AttributeName(ALL_NO_NS, SAME_LOCAL("color"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName CLASS = new AttributeName(ALL_NO_NS, SAME_LOCAL("class"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName CLEAR = new AttributeName(ALL_NO_NS, SAME_LOCAL("clear"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED);
		public static readonly AttributeName BEGIN = new AttributeName(ALL_NO_NS, SAME_LOCAL("begin"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName DEPTH = new AttributeName(ALL_NO_NS, SAME_LOCAL("depth"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName DEFER = new AttributeName(ALL_NO_NS, SAME_LOCAL("defer"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED | BOOLEAN);
		public static readonly AttributeName FENCE = new AttributeName(ALL_NO_NS, SAME_LOCAL("fence"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName FRAME = new AttributeName(ALL_NO_NS, SAME_LOCAL("frame"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED);
		public static readonly AttributeName ISMAP = new AttributeName(ALL_NO_NS, SAME_LOCAL("ismap"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED | BOOLEAN);
		public static readonly AttributeName ONEND = new AttributeName(ALL_NO_NS, SAME_LOCAL("onend"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName INDEX = new AttributeName(ALL_NO_NS, SAME_LOCAL("index"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ORDER = new AttributeName(ALL_NO_NS, SAME_LOCAL("order"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName OTHER = new AttributeName(ALL_NO_NS, SAME_LOCAL("other"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONCUT = new AttributeName(ALL_NO_NS, SAME_LOCAL("oncut"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName NARGS = new AttributeName(ALL_NO_NS, SAME_LOCAL("nargs"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName MEDIA = new AttributeName(ALL_NO_NS, SAME_LOCAL("media"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName LABEL = new AttributeName(ALL_NO_NS, SAME_LOCAL("label"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName LOCAL = new AttributeName(ALL_NO_NS, SAME_LOCAL("local"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName WIDTH = new AttributeName(ALL_NO_NS, SAME_LOCAL("width"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName TITLE = new AttributeName(ALL_NO_NS, SAME_LOCAL("title"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName VLINK = new AttributeName(ALL_NO_NS, SAME_LOCAL("vlink"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName VALUE = new AttributeName(ALL_NO_NS, SAME_LOCAL("value"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SLOPE = new AttributeName(ALL_NO_NS, SAME_LOCAL("slope"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SHAPE = new AttributeName(ALL_NO_NS, SAME_LOCAL("shape"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED);
		public static readonly AttributeName SCOPE = new AttributeName(ALL_NO_NS, SAME_LOCAL("scope"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED);
		public static readonly AttributeName SCALE = new AttributeName(ALL_NO_NS, SAME_LOCAL("scale"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SPEED = new AttributeName(ALL_NO_NS, SAME_LOCAL("speed"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName STYLE = new AttributeName(ALL_NO_NS, SAME_LOCAL("style"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName RULES = new AttributeName(ALL_NO_NS, SAME_LOCAL("rules"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED);
		public static readonly AttributeName STEMH = new AttributeName(ALL_NO_NS, SAME_LOCAL("stemh"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName STEMV = new AttributeName(ALL_NO_NS, SAME_LOCAL("stemv"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName START = new AttributeName(ALL_NO_NS, SAME_LOCAL("start"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName XMLNS = new AttributeName(XMLNS_NS, SAME_LOCAL("xmlns"), ALL_NO_PREFIX, IS_XMLNS);
		public static readonly AttributeName ACCEPT = new AttributeName(ALL_NO_NS, SAME_LOCAL("accept"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ACCENT = new AttributeName(ALL_NO_NS, SAME_LOCAL("accent"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ASCENT = new AttributeName(ALL_NO_NS, SAME_LOCAL("ascent"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ACTIVE = new AttributeName(ALL_NO_NS, SAME_LOCAL("active"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED | BOOLEAN);
		public static readonly AttributeName ALTIMG = new AttributeName(ALL_NO_NS, SAME_LOCAL("altimg"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ACTION = new AttributeName(ALL_NO_NS, SAME_LOCAL("action"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName BORDER = new AttributeName(ALL_NO_NS, SAME_LOCAL("border"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName CURSOR = new AttributeName(ALL_NO_NS, SAME_LOCAL("cursor"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName COORDS = new AttributeName(ALL_NO_NS, SAME_LOCAL("coords"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName FILTER = new AttributeName(ALL_NO_NS, SAME_LOCAL("filter"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName FORMAT = new AttributeName(ALL_NO_NS, SAME_LOCAL("format"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName HIDDEN = new AttributeName(ALL_NO_NS, SAME_LOCAL("hidden"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName HSPACE = new AttributeName(ALL_NO_NS, SAME_LOCAL("hspace"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName HEIGHT = new AttributeName(ALL_NO_NS, SAME_LOCAL("height"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONMOVE = new AttributeName(ALL_NO_NS, SAME_LOCAL("onmove"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONLOAD = new AttributeName(ALL_NO_NS, SAME_LOCAL("onload"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONDRAG = new AttributeName(ALL_NO_NS, SAME_LOCAL("ondrag"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ORIGIN = new AttributeName(ALL_NO_NS, SAME_LOCAL("origin"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONZOOM = new AttributeName(ALL_NO_NS, SAME_LOCAL("onzoom"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONHELP = new AttributeName(ALL_NO_NS, SAME_LOCAL("onhelp"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONSTOP = new AttributeName(ALL_NO_NS, SAME_LOCAL("onstop"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONDROP = new AttributeName(ALL_NO_NS, SAME_LOCAL("ondrop"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONBLUR = new AttributeName(ALL_NO_NS, SAME_LOCAL("onblur"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName OBJECT = new AttributeName(ALL_NO_NS, SAME_LOCAL("object"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName OFFSET = new AttributeName(ALL_NO_NS, SAME_LOCAL("offset"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ORIENT = new AttributeName(ALL_NO_NS, SAME_LOCAL("orient"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONCOPY = new AttributeName(ALL_NO_NS, SAME_LOCAL("oncopy"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName NOWRAP = new AttributeName(ALL_NO_NS, SAME_LOCAL("nowrap"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED | BOOLEAN);
		public static readonly AttributeName NOHREF = new AttributeName(ALL_NO_NS, SAME_LOCAL("nohref"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED | BOOLEAN);
		public static readonly AttributeName MACROS = new AttributeName(ALL_NO_NS, SAME_LOCAL("macros"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName METHOD = new AttributeName(ALL_NO_NS, SAME_LOCAL("method"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED);
		public static readonly AttributeName LOWSRC = new AttributeName(ALL_NO_NS, SAME_LOCAL("lowsrc"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName LSPACE = new AttributeName(ALL_NO_NS, SAME_LOCAL("lspace"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName LQUOTE = new AttributeName(ALL_NO_NS, SAME_LOCAL("lquote"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName USEMAP = new AttributeName(ALL_NO_NS, SAME_LOCAL("usemap"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName WIDTHS = new AttributeName(ALL_NO_NS, SAME_LOCAL("widths"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName TARGET = new AttributeName(ALL_NO_NS, SAME_LOCAL("target"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName VALUES = new AttributeName(ALL_NO_NS, SAME_LOCAL("values"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName VALIGN = new AttributeName(ALL_NO_NS, SAME_LOCAL("valign"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED);
		public static readonly AttributeName VSPACE = new AttributeName(ALL_NO_NS, SAME_LOCAL("vspace"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName POSTER = new AttributeName(ALL_NO_NS, SAME_LOCAL("poster"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName POINTS = new AttributeName(ALL_NO_NS, SAME_LOCAL("points"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName PROMPT = new AttributeName(ALL_NO_NS, SAME_LOCAL("prompt"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SCOPED = new AttributeName(ALL_NO_NS, SAME_LOCAL("scoped"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName STRING = new AttributeName(ALL_NO_NS, SAME_LOCAL("string"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SCHEME = new AttributeName(ALL_NO_NS, SAME_LOCAL("scheme"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName STROKE = new AttributeName(ALL_NO_NS, SAME_LOCAL("stroke"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName RADIUS = new AttributeName(ALL_NO_NS, SAME_LOCAL("radius"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName RESULT = new AttributeName(ALL_NO_NS, SAME_LOCAL("result"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName REPEAT = new AttributeName(ALL_NO_NS, SAME_LOCAL("repeat"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName RSPACE = new AttributeName(ALL_NO_NS, SAME_LOCAL("rspace"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ROTATE = new AttributeName(ALL_NO_NS, SAME_LOCAL("rotate"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName RQUOTE = new AttributeName(ALL_NO_NS, SAME_LOCAL("rquote"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ALTTEXT = new AttributeName(ALL_NO_NS, SAME_LOCAL("alttext"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARCHIVE = new AttributeName(ALL_NO_NS, SAME_LOCAL("archive"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName AZIMUTH = new AttributeName(ALL_NO_NS, SAME_LOCAL("azimuth"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName CLOSURE = new AttributeName(ALL_NO_NS, SAME_LOCAL("closure"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName CHECKED = new AttributeName(ALL_NO_NS, SAME_LOCAL("checked"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED | BOOLEAN);
		public static readonly AttributeName CLASSID = new AttributeName(ALL_NO_NS, SAME_LOCAL("classid"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName CHAROFF = new AttributeName(ALL_NO_NS, SAME_LOCAL("charoff"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName BGCOLOR = new AttributeName(ALL_NO_NS, SAME_LOCAL("bgcolor"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName COLSPAN = new AttributeName(ALL_NO_NS, SAME_LOCAL("colspan"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName CHARSET = new AttributeName(ALL_NO_NS, SAME_LOCAL("charset"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName COMPACT = new AttributeName(ALL_NO_NS, SAME_LOCAL("compact"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED | BOOLEAN);
		public static readonly AttributeName CONTENT = new AttributeName(ALL_NO_NS, SAME_LOCAL("content"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ENCTYPE = new AttributeName(ALL_NO_NS, SAME_LOCAL("enctype"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED);
		public static readonly AttributeName DATASRC = new AttributeName(ALL_NO_NS, SAME_LOCAL("datasrc"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName DATAFLD = new AttributeName(ALL_NO_NS, SAME_LOCAL("datafld"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName DECLARE = new AttributeName(ALL_NO_NS, SAME_LOCAL("declare"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED | BOOLEAN);
		public static readonly AttributeName DISPLAY = new AttributeName(ALL_NO_NS, SAME_LOCAL("display"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName DIVISOR = new AttributeName(ALL_NO_NS, SAME_LOCAL("divisor"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName DEFAULT = new AttributeName(ALL_NO_NS, SAME_LOCAL("default"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED | BOOLEAN);
		public static readonly AttributeName DESCENT = new AttributeName(ALL_NO_NS, SAME_LOCAL("descent"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName KERNING = new AttributeName(ALL_NO_NS, SAME_LOCAL("kerning"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName HANGING = new AttributeName(ALL_NO_NS, SAME_LOCAL("hanging"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName HEADERS = new AttributeName(ALL_NO_NS, SAME_LOCAL("headers"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONPASTE = new AttributeName(ALL_NO_NS, SAME_LOCAL("onpaste"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONCLICK = new AttributeName(ALL_NO_NS, SAME_LOCAL("onclick"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName OPTIMUM = new AttributeName(ALL_NO_NS, SAME_LOCAL("optimum"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONBEGIN = new AttributeName(ALL_NO_NS, SAME_LOCAL("onbegin"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONKEYUP = new AttributeName(ALL_NO_NS, SAME_LOCAL("onkeyup"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONFOCUS = new AttributeName(ALL_NO_NS, SAME_LOCAL("onfocus"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONERROR = new AttributeName(ALL_NO_NS, SAME_LOCAL("onerror"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONINPUT = new AttributeName(ALL_NO_NS, SAME_LOCAL("oninput"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONABORT = new AttributeName(ALL_NO_NS, SAME_LOCAL("onabort"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONSTART = new AttributeName(ALL_NO_NS, SAME_LOCAL("onstart"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONRESET = new AttributeName(ALL_NO_NS, SAME_LOCAL("onreset"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName OPACITY = new AttributeName(ALL_NO_NS, SAME_LOCAL("opacity"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName NOSHADE = new AttributeName(ALL_NO_NS, SAME_LOCAL("noshade"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED | BOOLEAN);
		public static readonly AttributeName MINSIZE = new AttributeName(ALL_NO_NS, SAME_LOCAL("minsize"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName MAXSIZE = new AttributeName(ALL_NO_NS, SAME_LOCAL("maxsize"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName LARGEOP = new AttributeName(ALL_NO_NS, SAME_LOCAL("largeop"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName UNICODE = new AttributeName(ALL_NO_NS, SAME_LOCAL("unicode"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName TARGETX = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("targetx", "targetX"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName TARGETY = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("targety", "targetY"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName VIEWBOX = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("viewbox", "viewBox"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName VERSION = new AttributeName(ALL_NO_NS, SAME_LOCAL("version"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName PATTERN = new AttributeName(ALL_NO_NS, SAME_LOCAL("pattern"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName PROFILE = new AttributeName(ALL_NO_NS, SAME_LOCAL("profile"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SPACING = new AttributeName(ALL_NO_NS, SAME_LOCAL("spacing"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName RESTART = new AttributeName(ALL_NO_NS, SAME_LOCAL("restart"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ROWSPAN = new AttributeName(ALL_NO_NS, SAME_LOCAL("rowspan"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SANDBOX = new AttributeName(ALL_NO_NS, SAME_LOCAL("sandbox"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SUMMARY = new AttributeName(ALL_NO_NS, SAME_LOCAL("summary"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName STANDBY = new AttributeName(ALL_NO_NS, SAME_LOCAL("standby"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName REPLACE = new AttributeName(ALL_NO_NS, SAME_LOCAL("replace"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED);
		public static readonly AttributeName AUTOPLAY = new AttributeName(ALL_NO_NS, SAME_LOCAL("autoplay"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ADDITIVE = new AttributeName(ALL_NO_NS, SAME_LOCAL("additive"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName CALCMODE = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("calcmode", "calcMode"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName CODETYPE = new AttributeName(ALL_NO_NS, SAME_LOCAL("codetype"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName CODEBASE = new AttributeName(ALL_NO_NS, SAME_LOCAL("codebase"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName CONTROLS = new AttributeName(ALL_NO_NS, SAME_LOCAL("controls"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName BEVELLED = new AttributeName(ALL_NO_NS, SAME_LOCAL("bevelled"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName BASELINE = new AttributeName(ALL_NO_NS, SAME_LOCAL("baseline"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName EXPONENT = new AttributeName(ALL_NO_NS, SAME_LOCAL("exponent"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName EDGEMODE = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("edgemode", "edgeMode"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ENCODING = new AttributeName(ALL_NO_NS, SAME_LOCAL("encoding"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName GLYPHREF = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("glyphref", "glyphRef"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName DATETIME = new AttributeName(ALL_NO_NS, SAME_LOCAL("datetime"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName DISABLED = new AttributeName(ALL_NO_NS, SAME_LOCAL("disabled"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED | BOOLEAN);
		public static readonly AttributeName FONTSIZE = new AttributeName(ALL_NO_NS, SAME_LOCAL("fontsize"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName KEYTIMES = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("keytimes", "keyTimes"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName PANOSE_1 = new AttributeName(ALL_NO_NS, SAME_LOCAL("panose-1"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName HREFLANG = new AttributeName(ALL_NO_NS, SAME_LOCAL("hreflang"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONRESIZE = new AttributeName(ALL_NO_NS, SAME_LOCAL("onresize"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONCHANGE = new AttributeName(ALL_NO_NS, SAME_LOCAL("onchange"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONBOUNCE = new AttributeName(ALL_NO_NS, SAME_LOCAL("onbounce"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONUNLOAD = new AttributeName(ALL_NO_NS, SAME_LOCAL("onunload"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONFINISH = new AttributeName(ALL_NO_NS, SAME_LOCAL("onfinish"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONSCROLL = new AttributeName(ALL_NO_NS, SAME_LOCAL("onscroll"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName OPERATOR = new AttributeName(ALL_NO_NS, SAME_LOCAL("operator"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName OVERFLOW = new AttributeName(ALL_NO_NS, SAME_LOCAL("overflow"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONSUBMIT = new AttributeName(ALL_NO_NS, SAME_LOCAL("onsubmit"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONREPEAT = new AttributeName(ALL_NO_NS, SAME_LOCAL("onrepeat"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONSELECT = new AttributeName(ALL_NO_NS, SAME_LOCAL("onselect"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName NOTATION = new AttributeName(ALL_NO_NS, SAME_LOCAL("notation"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName NORESIZE = new AttributeName(ALL_NO_NS, SAME_LOCAL("noresize"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED | BOOLEAN);
		public static readonly AttributeName MANIFEST = new AttributeName(ALL_NO_NS, SAME_LOCAL("manifest"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName MATHSIZE = new AttributeName(ALL_NO_NS, SAME_LOCAL("mathsize"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName MULTIPLE = new AttributeName(ALL_NO_NS, SAME_LOCAL("multiple"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED | BOOLEAN);
		public static readonly AttributeName LONGDESC = new AttributeName(ALL_NO_NS, SAME_LOCAL("longdesc"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName LANGUAGE = new AttributeName(ALL_NO_NS, SAME_LOCAL("language"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName TEMPLATE = new AttributeName(ALL_NO_NS, SAME_LOCAL("template"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName TABINDEX = new AttributeName(ALL_NO_NS, SAME_LOCAL("tabindex"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName READONLY = new AttributeName(ALL_NO_NS, SAME_LOCAL("readonly"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED | BOOLEAN);
		public static readonly AttributeName SELECTED = new AttributeName(ALL_NO_NS, SAME_LOCAL("selected"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED | BOOLEAN);
		public static readonly AttributeName ROWLINES = new AttributeName(ALL_NO_NS, SAME_LOCAL("rowlines"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SEAMLESS = new AttributeName(ALL_NO_NS, SAME_LOCAL("seamless"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ROWALIGN = new AttributeName(ALL_NO_NS, SAME_LOCAL("rowalign"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName STRETCHY = new AttributeName(ALL_NO_NS, SAME_LOCAL("stretchy"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName REQUIRED = new AttributeName(ALL_NO_NS, SAME_LOCAL("required"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED | BOOLEAN);
		public static readonly AttributeName XML_BASE = new AttributeName(XML_NS, COLONIFIED_LOCAL("xml:base", "base"), XML_PREFIX, NCNAME_FOREIGN);
		public static readonly AttributeName XML_LANG = new AttributeName(XML_NS, COLONIFIED_LOCAL("xml:lang", "lang"), XML_PREFIX, NCNAME_FOREIGN);
		public static readonly AttributeName X_HEIGHT = new AttributeName(ALL_NO_NS, SAME_LOCAL("x-height"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_OWNS = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-owns"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName AUTOFOCUS = new AttributeName(ALL_NO_NS, SAME_LOCAL("autofocus"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED | BOOLEAN);
		public static readonly AttributeName ARIA_SORT = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-sort"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ACCESSKEY = new AttributeName(ALL_NO_NS, SAME_LOCAL("accesskey"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_BUSY = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-busy"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_GRAB = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-grab"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName AMPLITUDE = new AttributeName(ALL_NO_NS, SAME_LOCAL("amplitude"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_LIVE = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-live"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName CLIP_RULE = new AttributeName(ALL_NO_NS, SAME_LOCAL("clip-rule"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName CLIP_PATH = new AttributeName(ALL_NO_NS, SAME_LOCAL("clip-path"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName EQUALROWS = new AttributeName(ALL_NO_NS, SAME_LOCAL("equalrows"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ELEVATION = new AttributeName(ALL_NO_NS, SAME_LOCAL("elevation"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName DIRECTION = new AttributeName(ALL_NO_NS, SAME_LOCAL("direction"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName DRAGGABLE = new AttributeName(ALL_NO_NS, SAME_LOCAL("draggable"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName FILTERRES = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("filterres", "filterRes"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName FILL_RULE = new AttributeName(ALL_NO_NS, SAME_LOCAL("fill-rule"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName FONTSTYLE = new AttributeName(ALL_NO_NS, SAME_LOCAL("fontstyle"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName FONT_SIZE = new AttributeName(ALL_NO_NS, SAME_LOCAL("font-size"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName KEYPOINTS = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("keypoints", "keyPoints"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName HIDEFOCUS = new AttributeName(ALL_NO_NS, SAME_LOCAL("hidefocus"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONMESSAGE = new AttributeName(ALL_NO_NS, SAME_LOCAL("onmessage"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName INTERCEPT = new AttributeName(ALL_NO_NS, SAME_LOCAL("intercept"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONDRAGEND = new AttributeName(ALL_NO_NS, SAME_LOCAL("ondragend"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONMOVEEND = new AttributeName(ALL_NO_NS, SAME_LOCAL("onmoveend"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONINVALID = new AttributeName(ALL_NO_NS, SAME_LOCAL("oninvalid"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONKEYDOWN = new AttributeName(ALL_NO_NS, SAME_LOCAL("onkeydown"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONFOCUSIN = new AttributeName(ALL_NO_NS, SAME_LOCAL("onfocusin"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONMOUSEUP = new AttributeName(ALL_NO_NS, SAME_LOCAL("onmouseup"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName INPUTMODE = new AttributeName(ALL_NO_NS, SAME_LOCAL("inputmode"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONROWEXIT = new AttributeName(ALL_NO_NS, SAME_LOCAL("onrowexit"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName MATHCOLOR = new AttributeName(ALL_NO_NS, SAME_LOCAL("mathcolor"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName MASKUNITS = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("maskunits", "maskUnits"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName MAXLENGTH = new AttributeName(ALL_NO_NS, SAME_LOCAL("maxlength"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName LINEBREAK = new AttributeName(ALL_NO_NS, SAME_LOCAL("linebreak"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName TRANSFORM = new AttributeName(ALL_NO_NS, SAME_LOCAL("transform"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName V_HANGING = new AttributeName(ALL_NO_NS, SAME_LOCAL("v-hanging"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName VALUETYPE = new AttributeName(ALL_NO_NS, SAME_LOCAL("valuetype"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED);
		public static readonly AttributeName POINTSATZ = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("pointsatz", "pointsAtZ"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName POINTSATX = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("pointsatx", "pointsAtX"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName POINTSATY = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("pointsaty", "pointsAtY"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SYMMETRIC = new AttributeName(ALL_NO_NS, SAME_LOCAL("symmetric"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SCROLLING = new AttributeName(ALL_NO_NS, SAME_LOCAL("scrolling"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED);
		public static readonly AttributeName REPEATDUR = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("repeatdur", "repeatDur"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SELECTION = new AttributeName(ALL_NO_NS, SAME_LOCAL("selection"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SEPARATOR = new AttributeName(ALL_NO_NS, SAME_LOCAL("separator"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName XML_SPACE = new AttributeName(XML_NS, COLONIFIED_LOCAL("xml:space", "space"), XML_PREFIX, NCNAME_FOREIGN);
		public static readonly AttributeName AUTOSUBMIT = new AttributeName(ALL_NO_NS, SAME_LOCAL("autosubmit"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED | BOOLEAN);
		public static readonly AttributeName ALPHABETIC = new AttributeName(ALL_NO_NS, SAME_LOCAL("alphabetic"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ACTIONTYPE = new AttributeName(ALL_NO_NS, SAME_LOCAL("actiontype"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ACCUMULATE = new AttributeName(ALL_NO_NS, SAME_LOCAL("accumulate"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_LEVEL = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-level"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName COLUMNSPAN = new AttributeName(ALL_NO_NS, SAME_LOCAL("columnspan"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName CAP_HEIGHT = new AttributeName(ALL_NO_NS, SAME_LOCAL("cap-height"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName BACKGROUND = new AttributeName(ALL_NO_NS, SAME_LOCAL("background"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName GLYPH_NAME = new AttributeName(ALL_NO_NS, SAME_LOCAL("glyph-name"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName GROUPALIGN = new AttributeName(ALL_NO_NS, SAME_LOCAL("groupalign"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName FONTFAMILY = new AttributeName(ALL_NO_NS, SAME_LOCAL("fontfamily"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName FONTWEIGHT = new AttributeName(ALL_NO_NS, SAME_LOCAL("fontweight"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName FONT_STYLE = new AttributeName(ALL_NO_NS, SAME_LOCAL("font-style"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName KEYSPLINES = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("keysplines", "keySplines"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName HTTP_EQUIV = new AttributeName(ALL_NO_NS, SAME_LOCAL("http-equiv"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONACTIVATE = new AttributeName(ALL_NO_NS, SAME_LOCAL("onactivate"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName OCCURRENCE = new AttributeName(ALL_NO_NS, SAME_LOCAL("occurrence"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName IRRELEVANT = new AttributeName(ALL_NO_NS, SAME_LOCAL("irrelevant"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONDBLCLICK = new AttributeName(ALL_NO_NS, SAME_LOCAL("ondblclick"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONDRAGDROP = new AttributeName(ALL_NO_NS, SAME_LOCAL("ondragdrop"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONKEYPRESS = new AttributeName(ALL_NO_NS, SAME_LOCAL("onkeypress"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONROWENTER = new AttributeName(ALL_NO_NS, SAME_LOCAL("onrowenter"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONDRAGOVER = new AttributeName(ALL_NO_NS, SAME_LOCAL("ondragover"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONFOCUSOUT = new AttributeName(ALL_NO_NS, SAME_LOCAL("onfocusout"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONMOUSEOUT = new AttributeName(ALL_NO_NS, SAME_LOCAL("onmouseout"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName NUMOCTAVES = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("numoctaves", "numOctaves"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName MARKER_MID = new AttributeName(ALL_NO_NS, SAME_LOCAL("marker-mid"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName MARKER_END = new AttributeName(ALL_NO_NS, SAME_LOCAL("marker-end"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName TEXTLENGTH = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("textlength", "textLength"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName VISIBILITY = new AttributeName(ALL_NO_NS, SAME_LOCAL("visibility"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName VIEWTARGET = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("viewtarget", "viewTarget"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName VERT_ADV_Y = new AttributeName(ALL_NO_NS, SAME_LOCAL("vert-adv-y"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName PATHLENGTH = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("pathlength", "pathLength"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName REPEAT_MAX = new AttributeName(ALL_NO_NS, SAME_LOCAL("repeat-max"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName RADIOGROUP = new AttributeName(ALL_NO_NS, SAME_LOCAL("radiogroup"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName STOP_COLOR = new AttributeName(ALL_NO_NS, SAME_LOCAL("stop-color"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SEPARATORS = new AttributeName(ALL_NO_NS, SAME_LOCAL("separators"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName REPEAT_MIN = new AttributeName(ALL_NO_NS, SAME_LOCAL("repeat-min"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ROWSPACING = new AttributeName(ALL_NO_NS, SAME_LOCAL("rowspacing"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ZOOMANDPAN = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("zoomandpan", "zoomAndPan"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName XLINK_TYPE = new AttributeName(XLINK_NS, COLONIFIED_LOCAL("xlink:type", "type"), XLINK_PREFIX, NCNAME_FOREIGN);
		public static readonly AttributeName XLINK_ROLE = new AttributeName(XLINK_NS, COLONIFIED_LOCAL("xlink:role", "role"), XLINK_PREFIX, NCNAME_FOREIGN);
		public static readonly AttributeName XLINK_HREF = new AttributeName(XLINK_NS, COLONIFIED_LOCAL("xlink:href", "href"), XLINK_PREFIX, NCNAME_FOREIGN);
		public static readonly AttributeName XLINK_SHOW = new AttributeName(XLINK_NS, COLONIFIED_LOCAL("xlink:show", "show"), XLINK_PREFIX, NCNAME_FOREIGN);
		public static readonly AttributeName ACCENTUNDER = new AttributeName(ALL_NO_NS, SAME_LOCAL("accentunder"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_SECRET = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-secret"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_ATOMIC = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-atomic"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_HIDDEN = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-hidden"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_FLOWTO = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-flowto"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARABIC_FORM = new AttributeName(ALL_NO_NS, SAME_LOCAL("arabic-form"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName CELLPADDING = new AttributeName(ALL_NO_NS, SAME_LOCAL("cellpadding"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName CELLSPACING = new AttributeName(ALL_NO_NS, SAME_LOCAL("cellspacing"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName COLUMNWIDTH = new AttributeName(ALL_NO_NS, SAME_LOCAL("columnwidth"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName CROSSORIGIN = new AttributeName(ALL_NO_NS, SAME_LOCAL("crossorigin"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName COLUMNALIGN = new AttributeName(ALL_NO_NS, SAME_LOCAL("columnalign"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName COLUMNLINES = new AttributeName(ALL_NO_NS, SAME_LOCAL("columnlines"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName CONTEXTMENU = new AttributeName(ALL_NO_NS, SAME_LOCAL("contextmenu"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName BASEPROFILE = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("baseprofile", "baseProfile"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName FONT_FAMILY = new AttributeName(ALL_NO_NS, SAME_LOCAL("font-family"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName FRAMEBORDER = new AttributeName(ALL_NO_NS, SAME_LOCAL("frameborder"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName FILTERUNITS = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("filterunits", "filterUnits"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName FLOOD_COLOR = new AttributeName(ALL_NO_NS, SAME_LOCAL("flood-color"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName FONT_WEIGHT = new AttributeName(ALL_NO_NS, SAME_LOCAL("font-weight"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName HORIZ_ADV_X = new AttributeName(ALL_NO_NS, SAME_LOCAL("horiz-adv-x"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONDRAGLEAVE = new AttributeName(ALL_NO_NS, SAME_LOCAL("ondragleave"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONMOUSEMOVE = new AttributeName(ALL_NO_NS, SAME_LOCAL("onmousemove"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ORIENTATION = new AttributeName(ALL_NO_NS, SAME_LOCAL("orientation"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONMOUSEDOWN = new AttributeName(ALL_NO_NS, SAME_LOCAL("onmousedown"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONMOUSEOVER = new AttributeName(ALL_NO_NS, SAME_LOCAL("onmouseover"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONDRAGENTER = new AttributeName(ALL_NO_NS, SAME_LOCAL("ondragenter"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName IDEOGRAPHIC = new AttributeName(ALL_NO_NS, SAME_LOCAL("ideographic"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONBEFORECUT = new AttributeName(ALL_NO_NS, SAME_LOCAL("onbeforecut"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONFORMINPUT = new AttributeName(ALL_NO_NS, SAME_LOCAL("onforminput"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONDRAGSTART = new AttributeName(ALL_NO_NS, SAME_LOCAL("ondragstart"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONMOVESTART = new AttributeName(ALL_NO_NS, SAME_LOCAL("onmovestart"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName MARKERUNITS = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("markerunits", "markerUnits"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName MATHVARIANT = new AttributeName(ALL_NO_NS, SAME_LOCAL("mathvariant"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName MARGINWIDTH = new AttributeName(ALL_NO_NS, SAME_LOCAL("marginwidth"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName MARKERWIDTH = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("markerwidth", "markerWidth"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName TEXT_ANCHOR = new AttributeName(ALL_NO_NS, SAME_LOCAL("text-anchor"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName TABLEVALUES = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("tablevalues", "tableValues"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SCRIPTLEVEL = new AttributeName(ALL_NO_NS, SAME_LOCAL("scriptlevel"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName REPEATCOUNT = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("repeatcount", "repeatCount"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName STITCHTILES = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("stitchtiles", "stitchTiles"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName STARTOFFSET = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("startoffset", "startOffset"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SCROLLDELAY = new AttributeName(ALL_NO_NS, SAME_LOCAL("scrolldelay"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName XMLNS_XLINK = new AttributeName(XMLNS_NS, COLONIFIED_LOCAL("xmlns:xlink", "xlink"), XMLNS_PREFIX, IS_XMLNS);
		public static readonly AttributeName XLINK_TITLE = new AttributeName(XLINK_NS, COLONIFIED_LOCAL("xlink:title", "title"), XLINK_PREFIX, NCNAME_FOREIGN);
		public static readonly AttributeName ARIA_INVALID = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-invalid"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_PRESSED = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-pressed"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_CHECKED = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-checked"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName AUTOCOMPLETE = new AttributeName(ALL_NO_NS, SAME_LOCAL("autocomplete"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED);
		public static readonly AttributeName ARIA_SETSIZE = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-setsize"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_CHANNEL = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-channel"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName EQUALCOLUMNS = new AttributeName(ALL_NO_NS, SAME_LOCAL("equalcolumns"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName DISPLAYSTYLE = new AttributeName(ALL_NO_NS, SAME_LOCAL("displaystyle"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName DATAFORMATAS = new AttributeName(ALL_NO_NS, SAME_LOCAL("dataformatas"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG | CASE_FOLDED);
		public static readonly AttributeName FILL_OPACITY = new AttributeName(ALL_NO_NS, SAME_LOCAL("fill-opacity"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName FONT_VARIANT = new AttributeName(ALL_NO_NS, SAME_LOCAL("font-variant"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName FONT_STRETCH = new AttributeName(ALL_NO_NS, SAME_LOCAL("font-stretch"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName FRAMESPACING = new AttributeName(ALL_NO_NS, SAME_LOCAL("framespacing"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName KERNELMATRIX = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("kernelmatrix", "kernelMatrix"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONDEACTIVATE = new AttributeName(ALL_NO_NS, SAME_LOCAL("ondeactivate"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONROWSDELETE = new AttributeName(ALL_NO_NS, SAME_LOCAL("onrowsdelete"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONMOUSELEAVE = new AttributeName(ALL_NO_NS, SAME_LOCAL("onmouseleave"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONFORMCHANGE = new AttributeName(ALL_NO_NS, SAME_LOCAL("onformchange"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONCELLCHANGE = new AttributeName(ALL_NO_NS, SAME_LOCAL("oncellchange"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONMOUSEWHEEL = new AttributeName(ALL_NO_NS, SAME_LOCAL("onmousewheel"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONMOUSEENTER = new AttributeName(ALL_NO_NS, SAME_LOCAL("onmouseenter"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONAFTERPRINT = new AttributeName(ALL_NO_NS, SAME_LOCAL("onafterprint"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONBEFORECOPY = new AttributeName(ALL_NO_NS, SAME_LOCAL("onbeforecopy"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName MARGINHEIGHT = new AttributeName(ALL_NO_NS, SAME_LOCAL("marginheight"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName MARKERHEIGHT = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("markerheight", "markerHeight"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName MARKER_START = new AttributeName(ALL_NO_NS, SAME_LOCAL("marker-start"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName MATHEMATICAL = new AttributeName(ALL_NO_NS, SAME_LOCAL("mathematical"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName LENGTHADJUST = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("lengthadjust", "lengthAdjust"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName UNSELECTABLE = new AttributeName(ALL_NO_NS, SAME_LOCAL("unselectable"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName UNICODE_BIDI = new AttributeName(ALL_NO_NS, SAME_LOCAL("unicode-bidi"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName UNITS_PER_EM = new AttributeName(ALL_NO_NS, SAME_LOCAL("units-per-em"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName WORD_SPACING = new AttributeName(ALL_NO_NS, SAME_LOCAL("word-spacing"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName WRITING_MODE = new AttributeName(ALL_NO_NS, SAME_LOCAL("writing-mode"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName V_ALPHABETIC = new AttributeName(ALL_NO_NS, SAME_LOCAL("v-alphabetic"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName PATTERNUNITS = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("patternunits", "patternUnits"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SPREADMETHOD = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("spreadmethod", "spreadMethod"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SURFACESCALE = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("surfacescale", "surfaceScale"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName STROKE_WIDTH = new AttributeName(ALL_NO_NS, SAME_LOCAL("stroke-width"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName REPEAT_START = new AttributeName(ALL_NO_NS, SAME_LOCAL("repeat-start"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName STDDEVIATION = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("stddeviation", "stdDeviation"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName STOP_OPACITY = new AttributeName(ALL_NO_NS, SAME_LOCAL("stop-opacity"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_CONTROLS = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-controls"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_HASPOPUP = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-haspopup"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ACCENT_HEIGHT = new AttributeName(ALL_NO_NS, SAME_LOCAL("accent-height"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_VALUENOW = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-valuenow"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_RELEVANT = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-relevant"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_POSINSET = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-posinset"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_VALUEMAX = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-valuemax"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_READONLY = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-readonly"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_SELECTED = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-selected"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_REQUIRED = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-required"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_EXPANDED = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-expanded"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_DISABLED = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-disabled"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ATTRIBUTETYPE = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("attributetype", "attributeType"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ATTRIBUTENAME = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("attributename", "attributeName"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_DATATYPE = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-datatype"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_VALUEMIN = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-valuemin"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName BASEFREQUENCY = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("basefrequency", "baseFrequency"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName COLUMNSPACING = new AttributeName(ALL_NO_NS, SAME_LOCAL("columnspacing"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName COLOR_PROFILE = new AttributeName(ALL_NO_NS, SAME_LOCAL("color-profile"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName CLIPPATHUNITS = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("clippathunits", "clipPathUnits"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName DEFINITIONURL = new AttributeName(ALL_NO_NS, MATH_DIFFERENT("definitionurl", "definitionURL"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName GRADIENTUNITS = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("gradientunits", "gradientUnits"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName FLOOD_OPACITY = new AttributeName(ALL_NO_NS, SAME_LOCAL("flood-opacity"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONAFTERUPDATE = new AttributeName(ALL_NO_NS, SAME_LOCAL("onafterupdate"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONERRORUPDATE = new AttributeName(ALL_NO_NS, SAME_LOCAL("onerrorupdate"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONBEFOREPASTE = new AttributeName(ALL_NO_NS, SAME_LOCAL("onbeforepaste"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONLOSECAPTURE = new AttributeName(ALL_NO_NS, SAME_LOCAL("onlosecapture"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONCONTEXTMENU = new AttributeName(ALL_NO_NS, SAME_LOCAL("oncontextmenu"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONSELECTSTART = new AttributeName(ALL_NO_NS, SAME_LOCAL("onselectstart"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONBEFOREPRINT = new AttributeName(ALL_NO_NS, SAME_LOCAL("onbeforeprint"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName MOVABLELIMITS = new AttributeName(ALL_NO_NS, SAME_LOCAL("movablelimits"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName LINETHICKNESS = new AttributeName(ALL_NO_NS, SAME_LOCAL("linethickness"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName UNICODE_RANGE = new AttributeName(ALL_NO_NS, SAME_LOCAL("unicode-range"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName THINMATHSPACE = new AttributeName(ALL_NO_NS, SAME_LOCAL("thinmathspace"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName VERT_ORIGIN_X = new AttributeName(ALL_NO_NS, SAME_LOCAL("vert-origin-x"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName VERT_ORIGIN_Y = new AttributeName(ALL_NO_NS, SAME_LOCAL("vert-origin-y"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName V_IDEOGRAPHIC = new AttributeName(ALL_NO_NS, SAME_LOCAL("v-ideographic"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName PRESERVEALPHA = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("preservealpha", "preserveAlpha"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SCRIPTMINSIZE = new AttributeName(ALL_NO_NS, SAME_LOCAL("scriptminsize"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SPECIFICATION = new AttributeName(ALL_NO_NS, SAME_LOCAL("specification"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName XLINK_ACTUATE = new AttributeName(XLINK_NS, COLONIFIED_LOCAL("xlink:actuate", "actuate"), XLINK_PREFIX, NCNAME_FOREIGN);
		public static readonly AttributeName XLINK_ARCROLE = new AttributeName(XLINK_NS, COLONIFIED_LOCAL("xlink:arcrole", "arcrole"), XLINK_PREFIX, NCNAME_FOREIGN);
		public static readonly AttributeName ACCEPT_CHARSET = new AttributeName(ALL_NO_NS, SAME_LOCAL("accept-charset"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ALIGNMENTSCOPE = new AttributeName(ALL_NO_NS, SAME_LOCAL("alignmentscope"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_MULTILINE = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-multiline"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName BASELINE_SHIFT = new AttributeName(ALL_NO_NS, SAME_LOCAL("baseline-shift"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName HORIZ_ORIGIN_X = new AttributeName(ALL_NO_NS, SAME_LOCAL("horiz-origin-x"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName HORIZ_ORIGIN_Y = new AttributeName(ALL_NO_NS, SAME_LOCAL("horiz-origin-y"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONBEFOREUPDATE = new AttributeName(ALL_NO_NS, SAME_LOCAL("onbeforeupdate"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONFILTERCHANGE = new AttributeName(ALL_NO_NS, SAME_LOCAL("onfilterchange"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONROWSINSERTED = new AttributeName(ALL_NO_NS, SAME_LOCAL("onrowsinserted"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONBEFOREUNLOAD = new AttributeName(ALL_NO_NS, SAME_LOCAL("onbeforeunload"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName MATHBACKGROUND = new AttributeName(ALL_NO_NS, SAME_LOCAL("mathbackground"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName LETTER_SPACING = new AttributeName(ALL_NO_NS, SAME_LOCAL("letter-spacing"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName LIGHTING_COLOR = new AttributeName(ALL_NO_NS, SAME_LOCAL("lighting-color"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName THICKMATHSPACE = new AttributeName(ALL_NO_NS, SAME_LOCAL("thickmathspace"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName TEXT_RENDERING = new AttributeName(ALL_NO_NS, SAME_LOCAL("text-rendering"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName V_MATHEMATICAL = new AttributeName(ALL_NO_NS, SAME_LOCAL("v-mathematical"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName POINTER_EVENTS = new AttributeName(ALL_NO_NS, SAME_LOCAL("pointer-events"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName PRIMITIVEUNITS = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("primitiveunits", "primitiveUnits"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SYSTEMLANGUAGE = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("systemlanguage", "systemLanguage"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName STROKE_LINECAP = new AttributeName(ALL_NO_NS, SAME_LOCAL("stroke-linecap"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SUBSCRIPTSHIFT = new AttributeName(ALL_NO_NS, SAME_LOCAL("subscriptshift"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName STROKE_OPACITY = new AttributeName(ALL_NO_NS, SAME_LOCAL("stroke-opacity"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_DROPEFFECT = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-dropeffect"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_LABELLEDBY = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-labelledby"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_TEMPLATEID = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-templateid"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName COLOR_RENDERING = new AttributeName(ALL_NO_NS, SAME_LOCAL("color-rendering"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName CONTENTEDITABLE = new AttributeName(ALL_NO_NS, SAME_LOCAL("contenteditable"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName DIFFUSECONSTANT = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("diffuseconstant", "diffuseConstant"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONDATAAVAILABLE = new AttributeName(ALL_NO_NS, SAME_LOCAL("ondataavailable"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONCONTROLSELECT = new AttributeName(ALL_NO_NS, SAME_LOCAL("oncontrolselect"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName IMAGE_RENDERING = new AttributeName(ALL_NO_NS, SAME_LOCAL("image-rendering"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName MEDIUMMATHSPACE = new AttributeName(ALL_NO_NS, SAME_LOCAL("mediummathspace"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName TEXT_DECORATION = new AttributeName(ALL_NO_NS, SAME_LOCAL("text-decoration"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SHAPE_RENDERING = new AttributeName(ALL_NO_NS, SAME_LOCAL("shape-rendering"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName STROKE_LINEJOIN = new AttributeName(ALL_NO_NS, SAME_LOCAL("stroke-linejoin"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName REPEAT_TEMPLATE = new AttributeName(ALL_NO_NS, SAME_LOCAL("repeat-template"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_DESCRIBEDBY = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-describedby"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName CONTENTSTYLETYPE = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("contentstyletype", "contentStyleType"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName FONT_SIZE_ADJUST = new AttributeName(ALL_NO_NS, SAME_LOCAL("font-size-adjust"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName KERNELUNITLENGTH = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("kernelunitlength", "kernelUnitLength"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONBEFOREACTIVATE = new AttributeName(ALL_NO_NS, SAME_LOCAL("onbeforeactivate"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONPROPERTYCHANGE = new AttributeName(ALL_NO_NS, SAME_LOCAL("onpropertychange"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONDATASETCHANGED = new AttributeName(ALL_NO_NS, SAME_LOCAL("ondatasetchanged"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName MASKCONTENTUNITS = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("maskcontentunits", "maskContentUnits"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName PATTERNTRANSFORM = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("patterntransform", "patternTransform"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName REQUIREDFEATURES = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("requiredfeatures", "requiredFeatures"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName RENDERING_INTENT = new AttributeName(ALL_NO_NS, SAME_LOCAL("rendering-intent"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SPECULAREXPONENT = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("specularexponent", "specularExponent"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SPECULARCONSTANT = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("specularconstant", "specularConstant"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SUPERSCRIPTSHIFT = new AttributeName(ALL_NO_NS, SAME_LOCAL("superscriptshift"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName STROKE_DASHARRAY = new AttributeName(ALL_NO_NS, SAME_LOCAL("stroke-dasharray"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName XCHANNELSELECTOR = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("xchannelselector", "xChannelSelector"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName YCHANNELSELECTOR = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("ychannelselector", "yChannelSelector"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_AUTOCOMPLETE = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-autocomplete"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName CONTENTSCRIPTTYPE = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("contentscripttype", "contentScriptType"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ENABLE_BACKGROUND = new AttributeName(ALL_NO_NS, SAME_LOCAL("enable-background"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName DOMINANT_BASELINE = new AttributeName(ALL_NO_NS, SAME_LOCAL("dominant-baseline"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName GRADIENTTRANSFORM = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("gradienttransform", "gradientTransform"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONBEFORDEACTIVATE = new AttributeName(ALL_NO_NS, SAME_LOCAL("onbefordeactivate"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONDATASETCOMPLETE = new AttributeName(ALL_NO_NS, SAME_LOCAL("ondatasetcomplete"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName OVERLINE_POSITION = new AttributeName(ALL_NO_NS, SAME_LOCAL("overline-position"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONBEFOREEDITFOCUS = new AttributeName(ALL_NO_NS, SAME_LOCAL("onbeforeeditfocus"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName LIMITINGCONEANGLE = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("limitingconeangle", "limitingConeAngle"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName VERYTHINMATHSPACE = new AttributeName(ALL_NO_NS, SAME_LOCAL("verythinmathspace"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName STROKE_DASHOFFSET = new AttributeName(ALL_NO_NS, SAME_LOCAL("stroke-dashoffset"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName STROKE_MITERLIMIT = new AttributeName(ALL_NO_NS, SAME_LOCAL("stroke-miterlimit"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ALIGNMENT_BASELINE = new AttributeName(ALL_NO_NS, SAME_LOCAL("alignment-baseline"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ONREADYSTATECHANGE = new AttributeName(ALL_NO_NS, SAME_LOCAL("onreadystatechange"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName OVERLINE_THICKNESS = new AttributeName(ALL_NO_NS, SAME_LOCAL("overline-thickness"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName UNDERLINE_POSITION = new AttributeName(ALL_NO_NS, SAME_LOCAL("underline-position"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName VERYTHICKMATHSPACE = new AttributeName(ALL_NO_NS, SAME_LOCAL("verythickmathspace"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName REQUIREDEXTENSIONS = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("requiredextensions", "requiredExtensions"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName COLOR_INTERPOLATION = new AttributeName(ALL_NO_NS, SAME_LOCAL("color-interpolation"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName UNDERLINE_THICKNESS = new AttributeName(ALL_NO_NS, SAME_LOCAL("underline-thickness"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName PRESERVEASPECTRATIO = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("preserveaspectratio", "preserveAspectRatio"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName PATTERNCONTENTUNITS = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("patterncontentunits", "patternContentUnits"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_MULTISELECTABLE = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-multiselectable"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName SCRIPTSIZEMULTIPLIER = new AttributeName(ALL_NO_NS, SAME_LOCAL("scriptsizemultiplier"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName ARIA_ACTIVEDESCENDANT = new AttributeName(ALL_NO_NS, SAME_LOCAL("aria-activedescendant"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName VERYVERYTHINMATHSPACE = new AttributeName(ALL_NO_NS, SAME_LOCAL("veryverythinmathspace"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName VERYVERYTHICKMATHSPACE = new AttributeName(ALL_NO_NS, SAME_LOCAL("veryverythickmathspace"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName STRIKETHROUGH_POSITION = new AttributeName(ALL_NO_NS, SAME_LOCAL("strikethrough-position"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName STRIKETHROUGH_THICKNESS = new AttributeName(ALL_NO_NS, SAME_LOCAL("strikethrough-thickness"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName EXTERNALRESOURCESREQUIRED = new AttributeName(ALL_NO_NS, SVG_DIFFERENT("externalresourcesrequired", "externalResourcesRequired"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName GLYPH_ORIENTATION_VERTICAL = new AttributeName(ALL_NO_NS, SAME_LOCAL("glyph-orientation-vertical"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName COLOR_INTERPOLATION_FILTERS = new AttributeName(ALL_NO_NS, SAME_LOCAL("color-interpolation-filters"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		public static readonly AttributeName GLYPH_ORIENTATION_HORIZONTAL = new AttributeName(ALL_NO_NS, SAME_LOCAL("glyph-orientation-horizontal"), ALL_NO_PREFIX, NCNAME_HTML | NCNAME_FOREIGN | NCNAME_LANG);
		private static readonly AttributeName[] ATTRIBUTE_NAMES = {
	D,
	K,
	R,
	X,
	Y,
	Z,
	BY,
	CX,
	CY,
	DX,
	DY,
	G2,
	G1,
	FX,
	FY,
	K4,
	K2,
	K3,
	K1,
	ID,
	IN,
	U2,
	U1,
	RT,
	RX,
	RY,
	TO,
	Y2,
	Y1,
	X1,
	X2,
	ALT,
	DIR,
	DUR,
	END,
	FOR,
	IN2,
	MAX,
	MIN,
	LOW,
	REL,
	REV,
	SRC,
	AXIS,
	ABBR,
	BBOX,
	CITE,
	CODE,
	BIAS,
	COLS,
	CLIP,
	CHAR,
	BASE,
	EDGE,
	DATA,
	FILL,
	FROM,
	FORM,
	FACE,
	HIGH,
	HREF,
	OPEN,
	ICON,
	NAME,
	MODE,
	MASK,
	LINK,
	LANG,
	LIST,
	TYPE,
	WHEN,
	WRAP,
	TEXT,
	PATH,
	PING,
	REFX,
	REFY,
	SIZE,
	SEED,
	ROWS,
	SPAN,
	STEP,
	ROLE,
	XREF,
	ASYNC,
	ALINK,
	ALIGN,
	CLOSE,
	COLOR,
	CLASS,
	CLEAR,
	BEGIN,
	DEPTH,
	DEFER,
	FENCE,
	FRAME,
	ISMAP,
	ONEND,
	INDEX,
	ORDER,
	OTHER,
	ONCUT,
	NARGS,
	MEDIA,
	LABEL,
	LOCAL,
	WIDTH,
	TITLE,
	VLINK,
	VALUE,
	SLOPE,
	SHAPE,
	SCOPE,
	SCALE,
	SPEED,
	STYLE,
	RULES,
	STEMH,
	STEMV,
	START,
	XMLNS,
	ACCEPT,
	ACCENT,
	ASCENT,
	ACTIVE,
	ALTIMG,
	ACTION,
	BORDER,
	CURSOR,
	COORDS,
	FILTER,
	FORMAT,
	HIDDEN,
	HSPACE,
	HEIGHT,
	ONMOVE,
	ONLOAD,
	ONDRAG,
	ORIGIN,
	ONZOOM,
	ONHELP,
	ONSTOP,
	ONDROP,
	ONBLUR,
	OBJECT,
	OFFSET,
	ORIENT,
	ONCOPY,
	NOWRAP,
	NOHREF,
	MACROS,
	METHOD,
	LOWSRC,
	LSPACE,
	LQUOTE,
	USEMAP,
	WIDTHS,
	TARGET,
	VALUES,
	VALIGN,
	VSPACE,
	POSTER,
	POINTS,
	PROMPT,
	SCOPED,
	STRING,
	SCHEME,
	STROKE,
	RADIUS,
	RESULT,
	REPEAT,
	RSPACE,
	ROTATE,
	RQUOTE,
	ALTTEXT,
	ARCHIVE,
	AZIMUTH,
	CLOSURE,
	CHECKED,
	CLASSID,
	CHAROFF,
	BGCOLOR,
	COLSPAN,
	CHARSET,
	COMPACT,
	CONTENT,
	ENCTYPE,
	DATASRC,
	DATAFLD,
	DECLARE,
	DISPLAY,
	DIVISOR,
	DEFAULT,
	DESCENT,
	KERNING,
	HANGING,
	HEADERS,
	ONPASTE,
	ONCLICK,
	OPTIMUM,
	ONBEGIN,
	ONKEYUP,
	ONFOCUS,
	ONERROR,
	ONINPUT,
	ONABORT,
	ONSTART,
	ONRESET,
	OPACITY,
	NOSHADE,
	MINSIZE,
	MAXSIZE,
	LARGEOP,
	UNICODE,
	TARGETX,
	TARGETY,
	VIEWBOX,
	VERSION,
	PATTERN,
	PROFILE,
	SPACING,
	RESTART,
	ROWSPAN,
	SANDBOX,
	SUMMARY,
	STANDBY,
	REPLACE,
	AUTOPLAY,
	ADDITIVE,
	CALCMODE,
	CODETYPE,
	CODEBASE,
	CONTROLS,
	BEVELLED,
	BASELINE,
	EXPONENT,
	EDGEMODE,
	ENCODING,
	GLYPHREF,
	DATETIME,
	DISABLED,
	FONTSIZE,
	KEYTIMES,
	PANOSE_1,
	HREFLANG,
	ONRESIZE,
	ONCHANGE,
	ONBOUNCE,
	ONUNLOAD,
	ONFINISH,
	ONSCROLL,
	OPERATOR,
	OVERFLOW,
	ONSUBMIT,
	ONREPEAT,
	ONSELECT,
	NOTATION,
	NORESIZE,
	MANIFEST,
	MATHSIZE,
	MULTIPLE,
	LONGDESC,
	LANGUAGE,
	TEMPLATE,
	TABINDEX,
	READONLY,
	SELECTED,
	ROWLINES,
	SEAMLESS,
	ROWALIGN,
	STRETCHY,
	REQUIRED,
	XML_BASE,
	XML_LANG,
	X_HEIGHT,
	ARIA_OWNS,
	AUTOFOCUS,
	ARIA_SORT,
	ACCESSKEY,
	ARIA_BUSY,
	ARIA_GRAB,
	AMPLITUDE,
	ARIA_LIVE,
	CLIP_RULE,
	CLIP_PATH,
	EQUALROWS,
	ELEVATION,
	DIRECTION,
	DRAGGABLE,
	FILTERRES,
	FILL_RULE,
	FONTSTYLE,
	FONT_SIZE,
	KEYPOINTS,
	HIDEFOCUS,
	ONMESSAGE,
	INTERCEPT,
	ONDRAGEND,
	ONMOVEEND,
	ONINVALID,
	ONKEYDOWN,
	ONFOCUSIN,
	ONMOUSEUP,
	INPUTMODE,
	ONROWEXIT,
	MATHCOLOR,
	MASKUNITS,
	MAXLENGTH,
	LINEBREAK,
	TRANSFORM,
	V_HANGING,
	VALUETYPE,
	POINTSATZ,
	POINTSATX,
	POINTSATY,
	SYMMETRIC,
	SCROLLING,
	REPEATDUR,
	SELECTION,
	SEPARATOR,
	XML_SPACE,
	AUTOSUBMIT,
	ALPHABETIC,
	ACTIONTYPE,
	ACCUMULATE,
	ARIA_LEVEL,
	COLUMNSPAN,
	CAP_HEIGHT,
	BACKGROUND,
	GLYPH_NAME,
	GROUPALIGN,
	FONTFAMILY,
	FONTWEIGHT,
	FONT_STYLE,
	KEYSPLINES,
	HTTP_EQUIV,
	ONACTIVATE,
	OCCURRENCE,
	IRRELEVANT,
	ONDBLCLICK,
	ONDRAGDROP,
	ONKEYPRESS,
	ONROWENTER,
	ONDRAGOVER,
	ONFOCUSOUT,
	ONMOUSEOUT,
	NUMOCTAVES,
	MARKER_MID,
	MARKER_END,
	TEXTLENGTH,
	VISIBILITY,
	VIEWTARGET,
	VERT_ADV_Y,
	PATHLENGTH,
	REPEAT_MAX,
	RADIOGROUP,
	STOP_COLOR,
	SEPARATORS,
	REPEAT_MIN,
	ROWSPACING,
	ZOOMANDPAN,
	XLINK_TYPE,
	XLINK_ROLE,
	XLINK_HREF,
	XLINK_SHOW,
	ACCENTUNDER,
	ARIA_SECRET,
	ARIA_ATOMIC,
	ARIA_HIDDEN,
	ARIA_FLOWTO,
	ARABIC_FORM,
	CELLPADDING,
	CELLSPACING,
	COLUMNWIDTH,
	CROSSORIGIN,
	COLUMNALIGN,
	COLUMNLINES,
	CONTEXTMENU,
	BASEPROFILE,
	FONT_FAMILY,
	FRAMEBORDER,
	FILTERUNITS,
	FLOOD_COLOR,
	FONT_WEIGHT,
	HORIZ_ADV_X,
	ONDRAGLEAVE,
	ONMOUSEMOVE,
	ORIENTATION,
	ONMOUSEDOWN,
	ONMOUSEOVER,
	ONDRAGENTER,
	IDEOGRAPHIC,
	ONBEFORECUT,
	ONFORMINPUT,
	ONDRAGSTART,
	ONMOVESTART,
	MARKERUNITS,
	MATHVARIANT,
	MARGINWIDTH,
	MARKERWIDTH,
	TEXT_ANCHOR,
	TABLEVALUES,
	SCRIPTLEVEL,
	REPEATCOUNT,
	STITCHTILES,
	STARTOFFSET,
	SCROLLDELAY,
	XMLNS_XLINK,
	XLINK_TITLE,
	ARIA_INVALID,
	ARIA_PRESSED,
	ARIA_CHECKED,
	AUTOCOMPLETE,
	ARIA_SETSIZE,
	ARIA_CHANNEL,
	EQUALCOLUMNS,
	DISPLAYSTYLE,
	DATAFORMATAS,
	FILL_OPACITY,
	FONT_VARIANT,
	FONT_STRETCH,
	FRAMESPACING,
	KERNELMATRIX,
	ONDEACTIVATE,
	ONROWSDELETE,
	ONMOUSELEAVE,
	ONFORMCHANGE,
	ONCELLCHANGE,
	ONMOUSEWHEEL,
	ONMOUSEENTER,
	ONAFTERPRINT,
	ONBEFORECOPY,
	MARGINHEIGHT,
	MARKERHEIGHT,
	MARKER_START,
	MATHEMATICAL,
	LENGTHADJUST,
	UNSELECTABLE,
	UNICODE_BIDI,
	UNITS_PER_EM,
	WORD_SPACING,
	WRITING_MODE,
	V_ALPHABETIC,
	PATTERNUNITS,
	SPREADMETHOD,
	SURFACESCALE,
	STROKE_WIDTH,
	REPEAT_START,
	STDDEVIATION,
	STOP_OPACITY,
	ARIA_CONTROLS,
	ARIA_HASPOPUP,
	ACCENT_HEIGHT,
	ARIA_VALUENOW,
	ARIA_RELEVANT,
	ARIA_POSINSET,
	ARIA_VALUEMAX,
	ARIA_READONLY,
	ARIA_SELECTED,
	ARIA_REQUIRED,
	ARIA_EXPANDED,
	ARIA_DISABLED,
	ATTRIBUTETYPE,
	ATTRIBUTENAME,
	ARIA_DATATYPE,
	ARIA_VALUEMIN,
	BASEFREQUENCY,
	COLUMNSPACING,
	COLOR_PROFILE,
	CLIPPATHUNITS,
	DEFINITIONURL,
	GRADIENTUNITS,
	FLOOD_OPACITY,
	ONAFTERUPDATE,
	ONERRORUPDATE,
	ONBEFOREPASTE,
	ONLOSECAPTURE,
	ONCONTEXTMENU,
	ONSELECTSTART,
	ONBEFOREPRINT,
	MOVABLELIMITS,
	LINETHICKNESS,
	UNICODE_RANGE,
	THINMATHSPACE,
	VERT_ORIGIN_X,
	VERT_ORIGIN_Y,
	V_IDEOGRAPHIC,
	PRESERVEALPHA,
	SCRIPTMINSIZE,
	SPECIFICATION,
	XLINK_ACTUATE,
	XLINK_ARCROLE,
	ACCEPT_CHARSET,
	ALIGNMENTSCOPE,
	ARIA_MULTILINE,
	BASELINE_SHIFT,
	HORIZ_ORIGIN_X,
	HORIZ_ORIGIN_Y,
	ONBEFOREUPDATE,
	ONFILTERCHANGE,
	ONROWSINSERTED,
	ONBEFOREUNLOAD,
	MATHBACKGROUND,
	LETTER_SPACING,
	LIGHTING_COLOR,
	THICKMATHSPACE,
	TEXT_RENDERING,
	V_MATHEMATICAL,
	POINTER_EVENTS,
	PRIMITIVEUNITS,
	SYSTEMLANGUAGE,
	STROKE_LINECAP,
	SUBSCRIPTSHIFT,
	STROKE_OPACITY,
	ARIA_DROPEFFECT,
	ARIA_LABELLEDBY,
	ARIA_TEMPLATEID,
	COLOR_RENDERING,
	CONTENTEDITABLE,
	DIFFUSECONSTANT,
	ONDATAAVAILABLE,
	ONCONTROLSELECT,
	IMAGE_RENDERING,
	MEDIUMMATHSPACE,
	TEXT_DECORATION,
	SHAPE_RENDERING,
	STROKE_LINEJOIN,
	REPEAT_TEMPLATE,
	ARIA_DESCRIBEDBY,
	CONTENTSTYLETYPE,
	FONT_SIZE_ADJUST,
	KERNELUNITLENGTH,
	ONBEFOREACTIVATE,
	ONPROPERTYCHANGE,
	ONDATASETCHANGED,
	MASKCONTENTUNITS,
	PATTERNTRANSFORM,
	REQUIREDFEATURES,
	RENDERING_INTENT,
	SPECULAREXPONENT,
	SPECULARCONSTANT,
	SUPERSCRIPTSHIFT,
	STROKE_DASHARRAY,
	XCHANNELSELECTOR,
	YCHANNELSELECTOR,
	ARIA_AUTOCOMPLETE,
	CONTENTSCRIPTTYPE,
	ENABLE_BACKGROUND,
	DOMINANT_BASELINE,
	GRADIENTTRANSFORM,
	ONBEFORDEACTIVATE,
	ONDATASETCOMPLETE,
	OVERLINE_POSITION,
	ONBEFOREEDITFOCUS,
	LIMITINGCONEANGLE,
	VERYTHINMATHSPACE,
	STROKE_DASHOFFSET,
	STROKE_MITERLIMIT,
	ALIGNMENT_BASELINE,
	ONREADYSTATECHANGE,
	OVERLINE_THICKNESS,
	UNDERLINE_POSITION,
	VERYTHICKMATHSPACE,
	REQUIREDEXTENSIONS,
	COLOR_INTERPOLATION,
	UNDERLINE_THICKNESS,
	PRESERVEASPECTRATIO,
	PATTERNCONTENTUNITS,
	ARIA_MULTISELECTABLE,
	SCRIPTSIZEMULTIPLIER,
	ARIA_ACTIVEDESCENDANT,
	VERYVERYTHINMATHSPACE,
	VERYVERYTHICKMATHSPACE,
	STRIKETHROUGH_POSITION,
	STRIKETHROUGH_THICKNESS,
	EXTERNALRESOURCESREQUIRED,
	GLYPH_ORIENTATION_VERTICAL,
	COLOR_INTERPOLATION_FILTERS,
	GLYPH_ORIENTATION_HORIZONTAL,
	};
		private static readonly int[] ATTRIBUTE_HASHES = {
	1153,
	1383,
	1601,
	1793,
	1827,
	1857,
	68600,
	69146,
	69177,
	70237,
	70270,
	71572,
	71669,
	72415,
	72444,
	74846,
	74904,
	74943,
	75001,
	75276,
	75590,
	84742,
	84839,
	85575,
	85963,
	85992,
	87204,
	88074,
	88171,
	89130,
	89163,
	3207892,
	3283895,
	3284791,
	3338752,
	3358197,
	3369562,
	3539124,
	3562402,
	3574260,
	3670335,
	3696933,
	3721879,
	135280021,
	135346322,
	136317019,
	136475749,
	136548517,
	136652214,
	136884919,
	136902418,
	136942992,
	137292068,
	139120259,
	139785574,
	142250603,
	142314056,
	142331176,
	142519584,
	144752417,
	145106895,
	146147200,
	146765926,
	148805544,
	149655723,
	149809441,
	150018784,
	150445028,
	150813181,
	150923321,
	152528754,
	152536216,
	152647366,
	152962785,
	155219321,
	155654904,
	157317483,
	157350248,
	157437941,
	157447478,
	157604838,
	157685404,
	157894402,
	158315188,
	166078431,
	169409980,
	169700259,
	169856932,
	170007032,
	170409695,
	170466488,
	170513710,
	170608367,
	173028944,
	173896963,
	176090625,
	176129212,
	179390001,
	179489057,
	179627464,
	179840468,
	179849042,
	180004216,
	181779081,
	183027151,
	183645319,
	183698797,
	185922012,
	185997252,
	188312483,
	188675799,
	190977533,
	190992569,
	191006194,
	191033518,
	191038774,
	191096249,
	191166163,
	191194426,
	191522106,
	191568039,
	200104642,
	202506661,
	202537381,
	202602917,
	203070590,
	203120766,
	203389054,
	203690071,
	203971238,
	203986524,
	209040857,
	209125756,
	212055489,
	212322418,
	212746849,
	213002877,
	213055164,
	213088023,
	213259873,
	213273386,
	213435118,
	213437318,
	213438231,
	213493071,
	213532268,
	213542834,
	213584431,
	213659891,
	215285828,
	215880731,
	216112976,
	216684637,
	217369699,
	217565298,
	217576549,
	218186795,
	219743185,
	220082234,
	221623802,
	221986406,
	222283890,
	223089542,
	223138630,
	223311265,
	224547358,
	224587256,
	224589550,
	224655650,
	224785518,
	224810917,
	224813302,
	225429618,
	225432950,
	225440869,
	236107233,
	236709921,
	236838947,
	237117095,
	237143271,
	237172455,
	237209953,
	237354143,
	237372743,
	237668065,
	237703073,
	237714273,
	239743521,
	240512803,
	240522627,
	240560417,
	240656513,
	241015715,
	241062755,
	241065383,
	243523041,
	245865199,
	246261793,
	246556195,
	246774817,
	246923491,
	246928419,
	246981667,
	247014847,
	247058369,
	247112833,
	247118177,
	247119137,
	247128739,
	247316903,
	249533729,
	250235623,
	250269543,
	251402351,
	252339047,
	253260911,
	253293679,
	254844367,
	255547879,
	256077281,
	256345377,
	258124199,
	258354465,
	258605063,
	258744193,
	258845603,
	258856961,
	258926689,
	269869248,
	270174334,
	270709417,
	270778994,
	270781796,
	271102503,
	271478858,
	271490090,
	272870654,
	273335275,
	273369140,
	273924313,
	274108530,
	274116736,
	276818662,
	277476156,
	279156579,
	279349675,
	280108533,
	280128712,
	280132869,
	280162403,
	280280292,
	280413430,
	280506130,
	280677397,
	280678580,
	280686710,
	280689066,
	282736758,
	283110901,
	283275116,
	283823226,
	283890012,
	284479340,
	284606461,
	286700477,
	286798916,
	291557706,
	291665349,
	291804100,
	292138018,
	292166446,
	292418738,
	292451039,
	300298041,
	300374839,
	300597935,
	303073389,
	303083839,
	303266673,
	303354997,
	303430688,
	303576261,
	303724281,
	303819694,
	304242723,
	304382625,
	306247792,
	307227811,
	307468786,
	307724489,
	309671175,
	310252031,
	310358241,
	310373094,
	311015256,
	313357609,
	313683893,
	313701861,
	313706996,
	313707317,
	313710350,
	314027746,
	314038181,
	314091299,
	314205627,
	314233813,
	316741830,
	316797986,
	317486755,
	317794164,
	320076137,
	322657125,
	322887778,
	323506876,
	323572412,
	323605180,
	325060058,
	325320188,
	325398738,
	325541490,
	325671619,
	333868843,
	336806130,
	337212108,
	337282686,
	337285434,
	337585223,
	338036037,
	338298087,
	338566051,
	340943551,
	341190970,
	342995704,
	343352124,
	343912673,
	344585053,
	346977248,
	347218098,
	347262163,
	347278576,
	347438191,
	347655959,
	347684788,
	347726430,
	347727772,
	347776035,
	347776629,
	349500753,
	350880161,
	350887073,
	353384123,
	355496998,
	355906922,
	355979793,
	356545959,
	358637867,
	358905016,
	359164318,
	359247286,
	359350571,
	359579447,
	365560330,
	367399355,
	367420285,
	367510727,
	368013212,
	370234760,
	370353345,
	370710317,
	371074566,
	371122285,
	371194213,
	371448425,
	371448430,
	371545055,
	371593469,
	371596922,
	371758751,
	371964792,
	372151328,
	376550136,
	376710172,
	376795771,
	376826271,
	376906556,
	380514830,
	380774774,
	380775037,
	381030322,
	381136500,
	381281631,
	381282269,
	381285504,
	381330595,
	381331422,
	381335911,
	381336484,
	383907298,
	383917408,
	384595009,
	384595013,
	387799894,
	387823201,
	392581647,
	392584937,
	392742684,
	392906485,
	393003349,
	400644707,
	400973830,
	404428547,
	404432113,
	404432865,
	404469244,
	404478897,
	404694860,
	406887479,
	408294949,
	408789955,
	410022510,
	410467324,
	410586448,
	410945965,
	411845275,
	414327152,
	414327932,
	414329781,
	414346257,
	414346439,
	414639928,
	414835998,
	414894517,
	414986533,
	417465377,
	417465381,
	417492216,
	418259232,
	419310946,
	420103495,
	420242342,
	420380455,
	420658662,
	420717432,
	423183880,
	424539259,
	425929170,
	425972964,
	426050649,
	426126450,
	426142833,
	426607922,
	437289840,
	437347469,
	437412335,
	437423943,
	437455540,
	437462252,
	437597991,
	437617485,
	437986305,
	437986507,
	437986828,
	437987072,
	438015591,
	438034813,
	438038966,
	438179623,
	438347971,
	438483573,
	438547062,
	438895551,
	441592676,
	442032555,
	443548979,
	447881379,
	447881655,
	447881895,
	447887844,
	448416189,
	448445746,
	448449012,
	450942191,
	452816744,
	453668677,
	454434495,
	456610076,
	456642844,
	456738709,
	457544600,
	459451897,
	459680944,
	468058810,
	468083581,
	470964084,
	471470955,
	471567278,
	472267822,
	481177859,
	481210627,
	481435874,
	481455115,
	481485378,
	481490218,
	485105638,
	486005878,
	486383494,
	487988916,
	488103783,
	490661867,
	491574090,
	491578272,
	493041952,
	493441205,
	493582844,
	493716979,
	504577572,
	504740359,
	505091638,
	505592418,
	505656212,
	509516275,
	514998531,
	515571132,
	515594682,
	518712698,
	521362273,
	526592419,
	526807354,
	527348842,
	538294791,
	539214049,
	544689535,
	545535009,
	548544752,
	548563346,
	548595116,
	551679010,
	558034099,
	560329411,
	560356209,
	560671018,
	560671152,
	560692590,
	560845442,
	569212097,
	569474241,
	572252718,
	572768481,
	575326764,
	576174758,
	576190819,
	582099184,
	582099438,
	582372519,
	582558889,
	586552164,
	591325418,
	594231990,
	594243961,
	605711268,
	615672071,
	616086845,
	621792370,
	624879850,
	627432831,
	640040548,
	654392808,
	658675477,
	659420283,
	672891587,
	694768102,
	705890982,
	725543146,
	759097578,
	761686526,
	795383908,
	843809551,
	878105336,
	908643300,
	945213471,
	};

	}

}
