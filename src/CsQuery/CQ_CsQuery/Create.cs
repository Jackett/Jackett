using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.IO;
using CsQuery.ExtensionMethods.Internal;
using CsQuery.Utility;
using CsQuery.Engine;
using CsQuery.Promises;
using CsQuery.Implementation;

namespace CsQuery
{
    public partial class CQ
    {
        /// <summary>
        /// Create an empty CQ object.
        /// </summary>
        ///
        /// <returns>
        /// A new CQ object
        /// </returns>

        public static CQ Create()
        {
            return new CQ();
        }

        /// <summary>
        /// Create a new CQ object from a single element. Unlike the constructor method
        /// <see cref="CsQuery.CQ"/> this new objet is not bound to any context from the element.
        /// </summary>
        ///
        /// <param name="html">
        /// A string containing HTML.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>

        public static CQ Create(string html)
        {
            return new CQ(html,HtmlParsingMode.Auto,HtmlParsingOptions.Default,DocType.Default);
        }

        /// <summary>
        /// Create a new CQ object from an HTML character array. Node: this method is obsolete; it may be
        /// removed in a future release. Character arrays were supported in prior versions because this
        /// was how all data was converted internally; this is not the case any more, and it's an
        /// unlikely format for typical input. Use string or stream methods instead.
        /// </summary>
        ///
        /// <param name="html">
        /// The HTML source for the document.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>

        public static CQ Create(char[] html)
        {
            return new CQ(html.AsString(), HtmlParsingMode.Auto, HtmlParsingOptions.Default, DocType.Default);
        }

        /// <summary>
        /// Create a new CQ object from a single element. Unlike the constructor method <see cref="CsQuery.CQ"/>
        /// this new objet is not bound to any context from the element.
        /// </summary>
        ///
        /// <param name="element">
        /// The element to wrap
        /// </param>
        ///
        /// <returns>
        /// A new CQ object
        /// </returns>
        
        public static CQ Create(IDomObject element)
        {
            CQ csq = new CQ();
            if (element is IDomDocument) {
                csq.Document = (IDomDocument)element;
                csq.AddSelection(csq.Document.ChildNodes);
            } else {
                csq.CreateNewFragment(Objects.Enumerate(element));
            }
            return csq;
        }

        /// <summary>
        /// Creeate a new CQ object from an HTML string.
        /// </summary>
        ///
        /// <param name="html">
        /// A string containing HTML.
        /// </param>
        /// <param name="parsingMode">
        /// (optional) the mode.
        /// </param>
        /// <param name="parsingOptions">
        /// (optional) options for controlling the parsing.
        /// </param>
        /// <param name="docType">
        /// (optional) type of the document.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>

        public static CQ Create(string html, 
            HtmlParsingMode parsingMode =HtmlParsingMode.Auto, 
            HtmlParsingOptions parsingOptions = HtmlParsingOptions.Default,
            DocType docType = DocType.Default)
        {
            return new CQ(html, parsingMode, parsingOptions, docType);
        }

        /// <summary>
        /// Create a new CQ from an HTML fragment, and use quickSet to create attributes (and/or css)
        /// </summary>
        ///
        /// <param name="html">
        /// A string of HTML.
        /// </param>
        /// <param name="quickSet">
        /// an object containing CSS properties and attributes to be applied to the resulting fragment.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object
        /// </returns>

        public static CQ Create(string html, object quickSet)
        {
            CQ csq = CQ.Create(html);
            return csq.AttrSet(quickSet, true);
        }

        /// <summary>
        /// Creeate a new CQ object from a squence of elements, or another CQ object. The new object will
        /// contain clones of the original objects; they are no longer bound to their owning context. If
        /// you want to wrap these elements and retain their context, use "new CQ(...)" instead.
        /// </summary>
        ///
        /// <param name="elements">
        /// A sequence of elements.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>

        public static CQ Create(IEnumerable<IDomObject> elements)
        {
            CQ csq = new CQ();
            csq.CreateNewFragment(elements);
            return csq;
        }

        /// <summary>
        /// Create a new CQ object from a stream of HTML text, attempting to automatically detect the
        /// character set encoding from BOM. 
        /// </summary>
        ///
        /// <param name="html">
        /// An open Stream.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>

        public static CQ Create(Stream html)
        {
            return new CQ(html, null,HtmlParsingMode.Auto, HtmlParsingOptions.Default, DocType.Default);
        }

        /// <summary>
        /// Create a new CQ from a stream of HTML text in the specified encoding.
        /// </summary>
        ///
        /// <param name="html">
        /// An open Stream.
        /// </param>
        /// <param name="encoding">
        /// The character set encoding.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>

        public static CQ Create(Stream html, Encoding encoding)
        {
            return new CQ(html, encoding, HtmlParsingMode.Auto, HtmlParsingOptions.Default, DocType.Default);
        }


        /// <summary>
        /// Create a new CQ object from a TextReader containing HTML.
        /// </summary>
        ///
        /// <param name="html">
        /// A TextReader containing HTML.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>

        public static CQ Create(TextReader html)
        {
            return new CQ(html, HtmlParsingMode.Auto, HtmlParsingOptions.Default, DocType.Default);
        }

        /// <summary>
        /// Create a new CQ object from a stream of HTML, treating the HTML as a content document.
        /// </summary>
        ///
        /// <param name="html">
        /// An open Stream.
        /// </param>
        /// <param name="encoding">
        /// The character set encoding.
        /// </param>
        /// <param name="parsingMode">
        /// (optional) the mode.
        /// </param>
        /// <param name="parsingOptions">
        /// (optional) options for controlling the parsing.
        /// </param>
        /// <param name="docType">
        /// (optional) type of the document.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>

        public static CQ Create(Stream html, 
            Encoding encoding = null,
            HtmlParsingMode parsingMode=HtmlParsingMode.Auto, 
            HtmlParsingOptions parsingOptions = HtmlParsingOptions.Default,
            DocType docType = DocType.Default)
        {
            return new CQ(html, encoding,parsingMode,parsingOptions, docType);
        }

        /// <summary>
        /// Create a new CQ object from a TextReader containg HTML
        /// </summary>
        ///
        /// <param name="html">
        /// A string of HTML.
        /// </param>
        /// <param name="parsingMode">
        /// (optional) the mode.
        /// </param>
        /// <param name="parsingOptions">
        /// (optional) options for controlling the parsing.
        /// </param>
        /// <param name="docType">
        /// (optional) type of the document.
        /// </param>
        ///
        /// <returns>
        /// The new fragment.
        /// </returns>

        public static CQ Create(TextReader html,
           HtmlParsingMode parsingMode = HtmlParsingMode.Auto,
           HtmlParsingOptions parsingOptions = HtmlParsingOptions.Default,
           DocType docType = DocType.Default)
        {
            return new CQ(html, parsingMode, parsingOptions, docType);
        }

        /// <summary>
        /// Create a new fragment from a TextReader containing HTML text.
        /// </summary>
        ///
        /// <param name="html">
        /// A string of HTML.
        /// </param>
        ///
        /// <returns>
        /// The new fragment.
        /// </returns>

        public static CQ CreateFragment(string html)
        {
            return new CQ(html,HtmlParsingMode.Fragment,HtmlParsingOptions.AllowSelfClosingTags, DocType.Default);
        }

        /// <summary>
        /// Creeate a new fragment from HTML text, in the context of a specific HTML tag.
        /// </summary>
        ///
        /// <param name="html">
        /// A string of HTML.
        /// </param>
        /// <param name="context">
        /// The HTML tag name which is the context
        /// </param>
        ///
        /// <returns>
        /// The new fragment.
        /// </returns>

        public static CQ CreateFragment(string html, string context)
        {
            CQ cq = new CQ();
            cq.CreateNewFragment(cq, html, context, DocType.Default);
            return cq;
        }

        /// <summary>
        /// Create a new CQ object from a sequence of elements, or another CQ object.
        /// </summary>
        ///
        /// <param name="elements">
        /// A sequence of elements.
        /// </param>
        ///
        /// <returns>
        /// The new fragment.
        /// </returns>

        public static CQ CreateFragment(IEnumerable<IDomObject> elements)
        {
            // this is synonymous with the Create method of the same sig because we definitely
            // would never autogenerate elements from a sequence of elements

            return Create(elements);
        }

        /// <summary>
        /// Creeate a new DOM from HTML text using full HTML5 tag generation.
        /// </summary>
        ///
        /// <param name="html">
        /// A string of HTML
        /// </param>
        ///
        /// <returns>
        /// The new document.
        /// </returns>

        public static CQ CreateDocument(string html)
        {
            return new CQ(html, HtmlParsingMode.Document, HtmlParsingOptions.Default, DocType.Default);
        }

        /// <summary>
        /// Creates a new DOM from a stream containing HTML
        /// </summary>
        ///
        /// <param name="html">
        /// An open Stream
        /// </param>
        ///
        /// <returns>
        /// The new document.
        /// </returns>

        public static CQ CreateDocument(Stream html)
        {
            return new CQ(html, null,HtmlParsingMode.Document, HtmlParsingOptions.Default, DocType.Default);
        }

        /// <summary>
        /// Creeate a new DOM from HTML text using full HTML5 tag generation.
        /// </summary>
        ///
        /// <param name="html">
        /// An open Stream.
        /// </param>
        /// <param name="encoding">
        /// The character set encoding.
        /// </param>
        ///
        /// <returns>
        /// The new document.
        /// </returns>

        public static CQ CreateDocument(Stream html, Encoding encoding)
        {
            return new CQ(html, encoding, HtmlParsingMode.Document, HtmlParsingOptions.Default, DocType.Default);
        }
        /// <summary>
        /// Creates a new DOM from a stream containing HTML
        /// </summary>
        ///
        /// <param name="html">
        /// A n open Stream
        /// </param>
        ///
        /// <returns>
        /// The new document.
        /// </returns>

        public static CQ CreateDocument(TextReader html)
        {
            return new CQ(html, HtmlParsingMode.Document, HtmlParsingOptions.Default, DocType.Default);
        }

        /// <summary>
        /// Creates a new DOM from an HTML file.
        /// </summary>
        ///
        /// <param name="htmlFile">
        /// The full path to the file
        /// </param>
        ///
        /// <returns>
        /// The new document from file.
        /// </returns>

        public static CQ CreateDocumentFromFile(string htmlFile)
        {
            using (Stream strm = Support.GetFileStream(htmlFile))
            {
                return CQ.CreateDocument(strm);
            }
        }

        /// <summary>
        /// Creates a new DOM from an HTML file.
        /// </summary>
        ///
        /// <param name="htmlFile">
        /// The full path to the file
        /// </param>
        ///
        /// <returns>
        /// The new from file.
        /// </returns>

        public static CQ CreateFromFile(string htmlFile)
        {
            using (Stream strm = Support.GetFileStream(htmlFile))
            {
                return CQ.Create(strm);
            }
        }

    }
}
