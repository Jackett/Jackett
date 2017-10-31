using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CsQuery.Utility;
using CsQuery.ExtensionMethods;
using CsQuery.ExtensionMethods.Internal;
using CsQuery.Implementation;
using CsQuery.Engine;
using CsQuery.HtmlParser;

namespace CsQuery
{
    public partial class CQ
    {
        #region regular constructors

        /// <summary>
        /// Creates a new, empty CQ object.
        /// </summary>

        public CQ()
        {
        }

        /// <summary>
        /// Create a new CQ object from an HTML string.
        /// </summary>
        ///
        /// <param name="html">
        /// The HTML source.
        /// </param>
        /// <param name="parsingMode">
        /// The HTML parsing mode.
        /// </param>
        /// <param name="parsingOptions">
        /// (optional) options for controlling the parsing.
        /// </param>
        /// <param name="docType">
        /// (optional) type of the document.
        /// </param>

        public CQ(string html, 
            HtmlParsingMode parsingMode = HtmlParsingMode.Auto,
            HtmlParsingOptions parsingOptions=HtmlParsingOptions.Default,
            DocType docType = DocType.Default)
        {
            var encoding = new UTF8Encoding(false);
            using (var stream = Support.GetEncodedStream(html ?? "", encoding))
            {
                CreateNew(this, stream, encoding, parsingMode, parsingOptions, docType);
            }
        }

        /// <summary>
        /// Create a new CQ object from an HTML stream.
        /// <see cref="CQ.Create(char[])"/>
        /// </summary>
        ///
        /// <param name="html">
        /// The html source of the new document.
        /// </param>
        /// <param name="encoding">
        /// The character set encoding.
        /// </param>
        /// <param name="parsingMode">
        /// The HTML parsing mode.
        /// </param>
        /// <param name="parsingOptions">
        /// (optional) options for controlling the parsing.
        /// </param>
        /// <param name="docType">
        /// (optional) type of the document.
        /// </param>

        public CQ(Stream html, 
            Encoding encoding,
            HtmlParsingMode parsingMode = HtmlParsingMode.Auto, 
            HtmlParsingOptions parsingOptions =HtmlParsingOptions.Default,
            DocType docType = DocType.Default)
        {

            CreateNew(this, html, encoding, parsingMode, parsingOptions, docType);
            
        }

        /// <summary>
        /// Create a new CQ object from an HTML string.
        /// </summary>
        ///
        /// <param name="html">
        /// The html source of the new document.
        /// </param>
        /// <param name="parsingMode">
        /// The HTML parsing mode.
        /// </param>
        /// <param name="parsingOptions">
        /// (optional) options for controlling the parsing.
        /// </param>
        /// <param name="docType">
        /// (optional) type of the document.
        /// </param>

        public CQ(TextReader html,
            HtmlParsingMode parsingMode = HtmlParsingMode.Auto,
            HtmlParsingOptions parsingOptions = HtmlParsingOptions.Default,
            DocType docType = DocType.Default)
        {
            Encoding encoding = Encoding.UTF8;

            var stream = new MemoryStream(encoding.GetBytes(html.ReadToEnd()));
            CreateNew(this, stream, encoding,parsingMode, parsingOptions, docType);
        }

        /// <summary>
        /// Create a new CQ object wrapping a single element.
        /// </summary>
        /// 
        /// <remarks>
        /// This differs from the <see cref="CQ.Create(IDomObject)"/> method in that this document is still
        /// related to its owning document; this is the same as if the element had just been selected.
        /// The Create method, conversely, creates an entirely new Document context contining a single
        /// element (a clone of this element).
        /// </remarks>
        ///
        /// <param name="element">
        /// The element.
        /// </param>

        public CQ(IDomObject element)
        {
            Document = element.Document;
            AddSelection(element);
        }

        /// <summary>
        /// Create a new CsQuery object wrapping an existing sequence of elements.
        /// </summary>
        ///
        /// <param name="elements">
        /// A sequence of elements to populate the object
        /// </param>

        public CQ(IEnumerable<IDomObject> elements)
        {
            ConfigureNewInstance(this, elements);
        }

        /// <summary>
        /// Create a new CQ object wrapping a single DOM element, in the context of another CQ object.
        /// </summary>
        ///
        /// <remarks>
        /// This differs from the overload accepting a single IDomObject parameter in that it associates
        /// the new object with a previous object, as if it were part of a selector chain. In practice
        /// this will rarely make a difference, but some methods such as <see cref="CQ.End"/> use
        /// this information.
        /// </remarks>
        ///
        /// <param name="element">
        /// The element to wrap.
        /// </param>
        /// <param name="context">
        /// The context.
        /// </param>

        public CQ(IDomObject element, CQ context)
        {
            ConfigureNewInstance(this, element, context);
        }

        /// <summary>
        /// Create a new CsQuery object using an existing instance and a selector. if the selector is
        /// null or missing, then it will contain no selection results.
        /// </summary>
        ///
        /// <param name="selector">
        /// A valid CSS selector.
        /// </param>
        /// <param name="context">
        /// The context.
        /// </param>

        public CQ(string selector, CQ context)
        {
            ConfigureNewInstance(selector,context);
        }

        /// <summary>
        /// Create a new CsQuery object from a selector HTML, and assign CSS from a JSON string, within a context.
        /// </summary>
        ///
        /// <param name="selector">
        /// The 
        /// </param>
        /// <param name="cssJson">
        /// The JSON containing CSS
        /// </param>
        /// <param name="context">
        /// The context
        /// </param>

        public CQ(string selector, string cssJson, CQ context)
        {
            ConfigureNewInstance(selector, context);
            AttrSet(cssJson);
        }

        /// <summary>
        /// Create a new CsQuery object from a selector or HTML, and assign CSS, within a context.
        /// </summary>
        ///
        /// <param name="selector">
        /// The selector or HTML markup
        /// </param>
        /// <param name="css">
        /// The object whose property names and values map to CSS
        /// </param>
        /// <param name="context">
        /// The context
        /// </param>

        public CQ(string selector, object css, CQ context)
        {
            ConfigureNewInstance(selector, context);
            AttrSet(css);
        }

        

       
        /// <summary>
        /// Create a new CsQuery object from a set of DOM elements, assigning the 2nd parameter as a context for this object.
        /// </summary>
        ///
        /// <param name="elements">
        /// The elements that make up the selection set in the new object
        /// </param>
        /// <param name="context">
        /// A CQ object that will be assigned as the context for this one.
        /// </param>

        public CQ(IEnumerable<IDomObject> elements, CQ context)
        {
            ConfigureNewInstance(this,elements, context);
        }
       
    
        #endregion

        #region implicit constructors

        /// <summary>
        /// Create a new CQ object from html.
        /// </summary>
        ///
        /// <param name="html">
        /// A string of HTML
        /// </param>


        public static implicit operator CQ(string html)
        {
            return CQ.Create(html);
        }

        #endregion

        #region Internal DOM creation methods

        /// <summary>
        /// Bind this instance to a new empty DomDocument configured with the default options.
        /// </summary>

        protected void CreateNewDocument()
        {
            Document = new DomDocument();
        }

        /// <summary>
        /// Bind this instance to a new empty DomFragment configured with the default options.
        /// </summary>

        protected void CreateNewFragment()
        {
            Document = new DomFragment();
        }

        /// <summary>
        /// Bind this instance to a new DomFragment created from a sequence of elements.
        /// </summary>
        ///
        /// <param name="elements">
        /// The elements to provide the source for this object's DOM.
        /// </param>

        protected void CreateNewFragment(IEnumerable<IDomObject> elements)
        {
            Document = DomDocument.Create(elements.Clone(), HtmlParsingMode.Fragment);
            AddSelection(Document.ChildNodes);
        }

        /// <summary>
        /// Bind this instance to a new DomFragment created from HTML in a specific HTML tag context.
        /// </summary>
        ///
        /// <param name="target">
        /// The target.
        /// </param>
        /// <param name="html">
        /// The HTML.
        /// </param>
        /// <param name="encoding">
        /// The character set encoding.
        /// </param>
        /// <param name="parsingMode">
        /// The HTML parsing mode.
        /// </param>
        /// <param name="parsingOptions">
        /// (optional) options for controlling the parsing.
        /// </param>
        /// <param name="docType">
        /// (optional) type of the document.
        /// </param>

        protected void CreateNew(CQ target,
          Stream html,
          Encoding encoding,
          HtmlParsingMode parsingMode,
          HtmlParsingOptions parsingOptions,
          DocType docType)
        {
            target.Document = DomDocument.Create(html, encoding, parsingMode,parsingOptions, docType);

            //  enumerate ChildNodes when creating a new fragment to be sure the selection set only
            //  reflects the original document. 

            target.SetSelection(Document.ChildNodes.ToList(), SelectionSetOrder.Ascending);
        }

        /// <summary>
        /// Bind this instance to a new DomFragment created from HTML using the specified parsing mode and element context
        /// </summary>
        ///
        /// <param name="target">
        /// The target.
        /// </param>
        /// <param name="html">
        /// The HTML.
        /// </param>
        /// <param name="context">
        /// The context (e.g. an HTML tag name)
        /// </param>
        /// <param name="docType">
        /// (optional) type of the document.
        /// </param>

        protected void CreateNewFragment(CQ target,
          string html,
          string context,
          DocType docType)
        {
            target.Document = DomFragment.Create(html, context, docType);

            //  enumerate ChildNodes when creating a new fragment to be sure the selection set only
            //  reflects the original document. 

            target.SetSelection(Document.ChildNodes.ToList(), SelectionSetOrder.Ascending);
        }

        private CQ NewInstance(string html)
        {
            var cq = NewCqUnbound();
            Encoding encoding = Encoding.UTF8;
            var stream = new MemoryStream(encoding.GetBytes(html));
            CreateNew(cq, stream, encoding,HtmlParsingMode.Auto, HtmlParsingOptions.Default, DocType.Default);
            return cq;
        }

        private CQ NewInstance(IEnumerable<IDomObject> elements, CQ context)
        {
            var cq = NewCqUnbound();
            ConfigureNewInstance(cq, elements, context);
            return cq;
        }

        /// <summary>
        /// Configures a new instance for a sequence of elements and an existing context.
        /// </summary>
        ///
        /// <param name="dom">
        /// The dom.
        /// </param>
        /// <param name="elements">
        /// A sequence of elements.
        /// </param>
        /// <param name="context">
        /// The context.
        /// </param>

        private void ConfigureNewInstance(CQ dom, IEnumerable<IDomObject> elements, CQ context)
        {
            dom.CsQueryParent = context;
            dom.AddSelection(elements);
        }

        private CQ NewInstance(IEnumerable<IDomObject> elements)
        {
            var cq = NewCqUnbound();
            ConfigureNewInstance(cq, elements);
            return cq;
        }

        private void ConfigureNewInstance(CQ dom, IEnumerable<IDomObject> elements)
        {
            var list = elements.ToList();

            if (elements is CQ)
            {
                CQ asCq = (CQ)elements;
                dom.CsQueryParent = asCq;
                dom.Document = asCq.Document;
            }
            else
            {
                // not actually a CQ object, we can get the Document the els are bound to from one of the
                // elements. 

                var el = list.FirstOrDefault();
                if (el != null)
                {
                    dom.Document = el.Document;
                }
            }
            dom.SetSelection(list, SelectionSetOrder.OrderAdded);
        }

        /// <summary>
        /// Configures a new instance for a sequence of elements and an existing context.
        /// </summary>
        ///
        /// <param name="selector">
        /// A valid CSS selector.
        /// </param>
        /// <param name="context">
        /// The context.
        /// </param>

        private void ConfigureNewInstance(string selector, CQ context)
        {
            CsQueryParent = context;

            if (!String.IsNullOrEmpty(selector))
            {
                Selector = new Selector(selector);

                SetSelection(Selector.ToContextSelector().Select(Document, context),
                    Selector.IsHtml ?
                        SelectionSetOrder.OrderAdded :
                        SelectionSetOrder.Ascending);
            }

        }

        private CQ NewInstance(IDomObject element, CQ context)
        {
            var cq = NewCqUnbound();
            ConfigureNewInstance(cq, element, context);
            return cq;
        }

        private void ConfigureNewInstance(CQ dom, IDomObject element, CQ context)
        {
            dom.CsQueryParent = context;
            dom.SetSelection(element, SelectionSetOrder.OrderAdded);
        }
        
        #endregion
    }
}
