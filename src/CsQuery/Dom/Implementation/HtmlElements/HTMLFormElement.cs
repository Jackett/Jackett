using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using CsQuery.HtmlParser;
using CsQuery.Utility;

namespace CsQuery.Implementation
{
    /// <summary>
    /// A FORM element.
    /// </summary>
    ///
    /// <url>
    /// http://dev.w3.org/html5/spec/single-page.html#the-form-element
    /// </url>

    public class HtmlFormElement : DomElement, IHTMLFormElement
    {
        /// <summary>
        /// Default constructor.
        /// </summary>

        public HtmlFormElement()
            : base(HtmlData.tagFORM)
        {
        }

        /// <summary>
        /// A name or keyword giving a browsing context for UAs to use when following the hyperlink.
        /// </summary>

        public string Target
        {
            get
            {
                return GetAttribute("target");
            }
            set
            {
                SetAttribute("target", value);
            }
        }

        /// <summary>
        /// The accept-charset attribute gives the character encodings that are to be used for the
        /// submission. If specified, the value must be an ordered set of unique space-separated tokens
        /// that are ASCII case-insensitive, and each token must be an ASCII case-insensitive match for
        /// the preferred MIME name of an ASCII-compatible character encoding.
        /// </summary>
        ///
        /// <value>
        /// The accept charset.
        /// </value>
        ///
        /// <url>
        /// http://dev.w3.org/html5/spec/single-page.html#attr-form-accept-charset
        /// </url>

        public string AcceptCharset
        {
            get
            {
                return GetAttribute("acceptcharset");
            }
            set
            {
                SetAttribute("acceptcharset", value);
            }
        }

        /// <summary>
        /// The action and formaction content attributes, if specified, must have a value that is a valid
        /// non-empty URL potentially surrounded by spaces.
        /// </summary>
        ///
        /// <value>
        /// A string
        /// </value>

        public string Action
        {
            get
            {
                return GetAttribute("action");
            }
            set
            {
                SetAttribute("action", value);
            }
        }

        /// <summary>
        /// The automcomplete attribute. The "off" state indicates that by default, input elements in the
        /// form will have their resulting autocompletion state set to off; the "on" state indicates that
        /// by default, input elements in the form will have their resulting autocompletion state set to
        /// on.
        /// </summary>
        ///
        /// <value>
        /// The autocomplete.
        /// </value>

        public string Autocomplete
        {
            get
            {
                return GetAttribute("autocomplete");
            }
            set
            {
                SetAttribute("autocomplete", value);
            }
        }

        /// <summary>
        /// Gets or sets the encoding type for the form. This must be one of "application/x-www-form-urlencoded",
        /// "multipart/form-data", or "text/plain".
        /// </summary>
        ///
        /// <value>
        /// The enctype.
        /// </value>

        public string Enctype
        {
            get
            {
                return GetAttribute("enctype","application/x-www-form-urlencoded");
            }
            set
            {
                SetAttribute("enctype", value);
            }
        }

        /// <summary>
        /// Gets or sets the encoding. This is a synonym for Enctype.
        /// </summary>
        ///
        /// <value>
        /// The encoding.
        /// </value>

        public string Encoding
        {
            get
            {
                return Enctype;
            }
            set
            {
                Enctype = value;
            }
        }

        /// <summary>
        /// Gets or sets the method attribute. This must be one of GET or POST. When missing, the default
        /// value is GET.
        /// </summary>
        ///
        /// <value>
        /// The method.
        /// </value>
        ///
        /// <url>
        /// http://dev.w3.org/html5/spec/single-page.html#attr-fs-method
        /// </url>

        public string Method
        {
            get
            {
                return GetAttribute("method","GET");
            }
            set
            {
                SetAttribute("method", value.ToUpper());
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the form should be validated during submission.
        /// </summary>
        ///
        /// <value>
        /// true to skip validation, false for normal behavior.
        /// </value>

        public bool NoValidate
        {
            get
            {
                return HasAttribute("novalidate");
            }
            set
            {
                SetProp("novalidate",value);
            }
        }

        /// <summary>
        /// An INodeList containing the form elements.
        /// </summary>
        ///
        /// <value>
        /// The elements.
        /// </value>

        public INodeList<IDomElement> Elements
        {
            get {
                return new NodeList<IDomElement>(Document.QuerySelectorAll(":input"));
            }
        }

        /// <summary>
        /// Converts this object to a list.
        /// </summary>
        ///
        /// <returns>
        /// This object as an IList&lt;IDomElement&gt;
        /// </returns>

        public IList<IDomElement> ToList()
        {
            return new List<IDomElement>(this).AsReadOnly();
        }

        /// <summary>
        /// The number of elements in this form.
        /// </summary>
        ///
        /// <value>
        /// An integer
        /// </value>

        public int Length
        {
            get { return Elements.Length; }
        }

        /// <summary>
        /// The form element at the specified index
        /// </summary>
        ///
        /// <param name="index">
        /// Zero-based index of the form element to obtain.
        /// </param>
        ///
        /// <returns>
        /// An IDomElement
        /// </returns>

        public IDomElement Item(int index)
        {
            return Elements[index];
        }

        /// <summary>
        /// The form element at the specified index.
        /// </summary>
        ///
        /// <param name="index">
        /// The zero-based index of the element to access.
        /// </param>
        ///
        /// <returns>
        /// IDomObject
        /// </returns>

        [IndexerName("Indexer")]
        public new IDomElement this[int index]
        {
            get { return Item(index); }
        }

        /// <summary>
        /// Gets an enumerator of the form's elements.
        /// </summary>
        ///
        /// <returns>
        /// The enumerator.
        /// </returns>

        public IEnumerator<IDomElement> GetEnumerator()
        {
            return Elements.GetEnumerator();
        }

        int System.Collections.Generic.IReadOnlyCollection<IDomElement>.Count
        {
            get { return Elements.Length; }
        }


        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
