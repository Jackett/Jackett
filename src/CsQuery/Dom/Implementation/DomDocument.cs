using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CsQuery.ExtensionMethods;
using CsQuery.ExtensionMethods.Internal;
using CsQuery.Utility;
using CsQuery.Engine;
using CsQuery.HtmlParser;

namespace CsQuery.Implementation
{

    /// <summary>
    /// Special node type to represent the DOM.
    /// </summary>
    public class DomDocument : DomContainer<DomDocument>, IDomDocument
    {
        #region static methods 

        /// <summary>
        /// Creates a new, empty DomDocument
        /// </summary>
        ///
        /// <returns>
        /// A new DomDocument
        /// </returns>

        public static IDomDocument Create()
        {
            return new DomDocument();
        }

        /// <summary>
        /// Creates a new DomDocument (or derived object) using the options specified.
        /// </summary>
        ///
        /// <param name="elements">
        /// The elements that are the source for the new document.
        /// </param>
        /// <param name="parsingMode">
        /// (optional) the parsing mode.
        /// </param>
        /// <param name="docType">
        /// The DocType for this document.
        /// </param>
        ///
        /// <returns>
        /// A new IDomDocument object
        /// </returns>

        public static IDomDocument Create(IEnumerable<IDomObject> elements, 
            HtmlParsingMode parsingMode = HtmlParsingMode.Content,
            DocType docType = DocType.Default)
        {
            DomDocument doc = parsingMode == HtmlParsingMode.Document ?
                new DomDocument() :
                new DomFragment();

            // only set a DocType node for documents.
             
            if (parsingMode == HtmlParsingMode.Document)
            {
                doc.DocType = docType;
            }
            doc.Populate(elements);
            return doc;
        }

        /// <summary>
        /// Creates a new DomDocument (or derived) object
        /// </summary>
        ///
        /// <param name="html">
        /// The HTML source for the document
        /// </param>
        /// <param name="parsingMode">
        /// (optional) the parsing mode.
        /// </param>
        /// <param name="parsingOptions">
        /// (optional) options for controlling the parsing.
        /// </param>
        /// <param name="docType">
        /// The DocType for this document.
        /// </param>
        ///
        /// <returns>
        /// A new IDomDocument object
        /// </returns>

        public static IDomDocument Create(string html, 
            HtmlParsingMode parsingMode = HtmlParsingMode.Auto,
            HtmlParsingOptions parsingOptions = HtmlParsingOptions.Default,
            DocType docType = DocType.Default)
        {
            var encoding = Encoding.UTF8;

            using (var stream = new MemoryStream(encoding.GetBytes(html)))
            {
                return ElementFactory.Create(stream, encoding, parsingMode, parsingOptions, docType);
            }
        }

        /// <summary>
        /// Creates a new DomDocument (or derived) object.
        /// </summary>
        ///
        /// <param name="html">
        /// The HTML source for the document.
        /// </param>
        /// <param name="encoding">
        /// (optional) the character set encoding.
        /// </param>
        /// <param name="parsingMode">
        /// (optional) the HTML parsing mode.
        /// </param>
        /// <param name="parsingOptions">
        /// (optional) options for controlling the parsing.
        /// </param>
        /// <param name="docType">
        /// The DocType for this document.
        /// </param>
        ///
        /// <returns>
        /// A new IDomDocument object.
        /// </returns>

        public static IDomDocument Create(Stream html, 
            Encoding encoding=null,
            HtmlParsingMode parsingMode= HtmlParsingMode.Content,
            HtmlParsingOptions parsingOptions= HtmlParsingOptions.Default,
            DocType docType = DocType.Default)
        {
            
            return ElementFactory.Create(html, encoding,parsingMode,parsingOptions, docType);
        }

        #endregion

        #region constructors

        /// <summary>
        /// Create a new, empty DOM document using the default DomIndex provider.
        /// </summary>
        /// 
        
        public DomDocument()
            : this(null)
        {
            
        }

        /// <summary>
        /// Create a new, empty DOM document using the provided DomIndex instance
        /// </summary>
        ///
        /// <param name="domIndex">
        /// An index provider
        /// </param>

        public DomDocument(IDomIndex domIndex)
            : base()
        {
            DocumentIndex = domIndex ?? CsQuery.Config.DomIndexProvider.GetDomIndex();
        }

        /// <summary>
        /// Populates this instance with the sequence of elements
        /// </summary>
        ///
        /// <param name="elements">
        /// The elements that are the source for the new document.
        /// </param>

        protected void Populate(IEnumerable<IDomObject> elements)
        {
            foreach (var item in elements)
            {
                ChildNodesInternal.AddAlways(item);
            }

        }

        #endregion

        #region private properties

        private IList<ICSSStyleSheet> _StyleSheets;
        private IDictionary<string, object> _Data;

        #endregion

        #region public properties

        /// <summary>
        /// Gets the style sheets for this document. (This feature is not implemented completely).
        /// </summary>

        public IList<ICSSStyleSheet> StyleSheets
        {
            get
            {
                if (_StyleSheets == null)
                {
                    _StyleSheets = new List<ICSSStyleSheet>();
                }
                return _StyleSheets;
            }
        }

        /// <summary>
        /// Return the DocumentIndex for this document.
        /// </summary>

        public IDomIndex DocumentIndex
        {
            get; protected set;
        }


        /// <summary>
        /// The direct parent of this node.
        /// </summary>

        public override IDomContainer ParentNode
        {
            get
            {
                return null;
            }
            internal set
            {
                throw new InvalidOperationException("Cannot set parent for a DOM root node.");
            }
        }

        /// <summary>
        /// The full path to this node. For Document nodes, this is always empty.
        /// </summary>

        public override ushort[] NodePath
        {
            get
            {
                return new ushort[] {};
            }
        }

        /// <summary>
        /// Gets the unique path to this element as a string. THIS METHOD IS OBSOLETE. It has been
        /// replaced by NodePath.
        /// </summary>

        [Obsolete]
        public override string Path
        {
            get
            {
                return "";
            }
        }

        /// <summary>
        /// The depth in the node tree at which this node occurs. This is always 0 for the DomDocument.
        /// </summary>

        public override int Depth
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Deprecated: DomRenderingOptions are no longer bound to a particular Document instance. Pass
        /// options to the Render() method, or create an IOutputFormatter instance using options, instead.
        /// This method will be removed in a future release.
        /// </summary>

        [Obsolete]
        public DomRenderingOptions DomRenderingOptions
        {
            get; set;
        }

        /// <summary>
        /// The DOM for this object. For Document objects, this returns the same object.
        /// </summary>

        public override IDomDocument Document
        {
            get
            {
                return this;
            }
        }

        /// <summary>
        /// Gets the type of the node. For Document objects, this is always NodeType.DOCUMENT_NODE
        /// </summary>

        public override NodeType NodeType
        {
            get
            {
                return NodeType.DOCUMENT_NODE;
            }
        }

        /// <summary>
        /// Gets the DOCUMENT_TYPE node for this document, or null if none exists.
        /// </summary>

        public IDomDocumentType DocTypeNode
        {
            get
            {
                foreach (IDomObject obj in ChildNodes)
                {
                    if (obj.NodeType == NodeType.DOCUMENT_TYPE_NODE)
                    {
                        return (DomDocumentType)obj;
                    }
                }
                return null;
            }
            set
            {
                var docTypeNode = DocTypeNode;
                if (docTypeNode != null)
                {
                    docTypeNode.Remove();
                }

                ChildNodes.Insert(0,value);

            }
        }

        /// <summary>
        /// Gets the DocType for this document. 
        /// </summary>

        public DocType DocType
        {
            get
            {
                // If explicitly set, return that value, otherwise get from DomDocument node

                IDomDocumentType docNode = DocTypeNode;
                if (docNode == null)
                {
                    return Config.DocType;
                }
                else
                {
                    return docNode.DocType;
                }
            }
            protected set
            {
                IDomDocumentType docNode = DocTypeNode;
                if (docNode != null)
                {
                    docNode.Remove();
                }
                AppendChild(CreateDocumentType(value));
            }
        }

        /// <summary>
        /// Gets a value indicating whether HTML is allowed as a child of this element. For Document
        /// nodes, this is always true.
        /// </summary>

        public override bool InnerHtmlAllowed
        {
            get { return true; }
        }

        /// <summary>
        /// Any user data to be persisted with this DOM.
        /// </summary>

        public IDictionary<string, object> Data
        {
            get
            {
                if (_Data == null)
                {
                    _Data = new Dictionary<string, object>();
                }
                return _Data;
            }
            set
            {
                _Data = value;
            }
        }

        /// <summary>
        /// Return the body element for this Document.
        /// </summary>

        public IDomElement Body
        {
            get
            {
                return this.QuerySelectorAll("body").FirstOrDefault();
            }
        }

        /// <summary>
        /// Gets a value indicating whether this object type should be indexed.
        /// </summary>

        public override bool IsIndexed
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// The element is not associated with an IDomDocument.
        /// </summary>

        public override bool IsFragment
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this object belongs to a Document or not.
        /// </summary>

        public override bool IsDisconnected
        {
            get
            {
                return false;
            }
        }

        #endregion

        #region public methods


     
        /// <summary>
        /// Returns a reference to the element by its ID.
        /// </summary>
        ///
        /// <param name="id">
        /// The identifier.
        /// </param>
        ///
        /// <returns>
        /// The element by identifier.
        /// </returns>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/document.getElementById
        /// </url>

        public IDomElement GetElementById(string id)
        {

            return GetElementById<IDomElement>(id);
        }

        /// <summary>
        /// Gets an element by identifier, and return a strongly-typed interface.
        /// </summary>
        ///
        /// <typeparam name="T">
        /// Generic type parameter.
        /// </typeparam>
        /// <param name="id">
        /// The identifier.
        /// </param>
        ///
        /// <returns>
        /// The element by id&lt; t&gt;
        /// </returns>

        public T GetElementById<T>(string id) where T: IDomElement
        {

            // construct the selector manually so there's no syntax checking as if it were a general-purpose selector

            SelectorClause selector = new SelectorClause();
            selector.SelectorType = SelectorType.ID;
            selector.ID = id;

            Selector selectors = new Selector(selector);
            return (T)selectors.Select(Document).FirstOrDefault();
        }

        /// <summary>
        /// Gets element by tag name.
        /// </summary>
        ///
        /// <param name="tagName">
        /// Name of the tag.
        /// </param>
        ///
        /// <returns>
        /// The element by tag name.
        /// </returns>

        public IDomElement GetElementByTagName(string tagName)
        {
            Selector selectors = new Selector(tagName);
            return (IDomElement)selectors.Select(Document).FirstOrDefault();
        }

        /// <summary>
        /// Returns a list of elements with the given tag name. The subtree underneath the specified
        /// element is searched, excluding the element itself.
        /// </summary>
        ///
        /// <param name="tagName">
        /// Name of the tag.
        /// </param>
        ///
        /// <returns>
        /// The element by tag name.
        /// </returns>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/element.getElementsByTagName
        /// </url>

        public INodeList<IDomElement> GetElementsByTagName(string tagName)
        {
            Selector selectors = new Selector(tagName);
            return new NodeList<IDomElement>(new List<IDomElement>(OnlyElements(selectors.Select(Document))));
        }

        /// <summary>
        /// Returns the first element within the document (using depth-first pre-order traversal of the
        /// document's nodes) that matches the specified group of selectors.
        /// </summary>
        ///
        /// <param name="selector">
        /// The selector.
        /// </param>
        ///
        /// <returns>
        /// An element, the first that matches the selector.
        /// </returns>
        ///
        /// <url>
        /// https://developer.mozilla.org/En/DOM/Document.querySelector
        /// </url>

        public IDomElement QuerySelector(string selector)
        {
            Selector selectors = new Selector(selector);
            return OnlyElements(selectors.Select(Document)).FirstOrDefault();
        }

        /// <summary>
        /// Returns a list of the elements within the document (using depth-first pre-order traversal of
        /// the document's nodes) that match the specified group of selectors.
        /// </summary>
        ///
        /// <param name="selector">
        /// The selector.
        /// </param>
        ///
        /// <returns>
        /// A sequence of elements matching the selector.
        /// </returns>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/Document.querySelectorAll
        /// </url>

        public IList<IDomElement> QuerySelectorAll(string selector)
        {
            Selector selectors = new Selector(selector);
            return (new List<IDomElement>(OnlyElements(selectors.Select(Document)))).AsReadOnly();
        }

        /// <summary>
        /// Creates a new Element node.
        /// </summary>
        ///
        /// <param name="nodeName">
        /// Name of the node.
        /// </param>
        ///
        /// <returns>
        /// The new element.
        /// </returns>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/document.createElement
        /// </url>

        public IDomElement CreateElement(string nodeName) 
        {
            return DomElement.Create(nodeName);
        }

        /// <summary>
        /// Creates a new Text node.
        /// </summary>
        ///
        /// <param name="text">
        /// The text.
        /// </param>
        ///
        /// <returns>
        /// The new text node.
        /// </returns>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/document.createTextNode
        /// </url>

        public IDomText CreateTextNode(string text)
        {
            return new DomText(text);
        }

        /// <summary>
        /// Creates a new comment node.
        /// </summary>
        ///
        /// <param name="comment">
        /// The comment.
        /// </param>
        ///
        /// <returns>
        /// The new comment.
        /// </returns>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/document.createComment
        /// </url>

        public IDomComment CreateComment(string comment)
        {
            return new DomComment(comment);
        }

        /// <summary>
        /// Creates a document type node.
        /// </summary>
        ///
        /// <param name="type">
        /// The type.
        /// </param>
        /// <param name="access">
        /// The access type, public or private.
        /// </param>
        /// <param name="FPI">
        /// The formal public identifier of the doc type.
        /// </param>
        /// <param name="URI">
        /// The URI of the doc type.
        /// </param>
        ///
        /// <returns>
        /// The new document type.
        /// </returns>

        public IDomDocumentType CreateDocumentType(string type, string access, string FPI, string URI)
        {
            return new DomDocumentType(type, access,FPI,URI);
        }

        /// <summary>
        /// Creates the document type node.
        /// </summary>
        ///
        /// <param name="docType">
        /// The DocType for this document.
        /// </param>
        ///
        /// <returns>
        /// The new document type.
        /// </returns>

        public IDomDocumentType CreateDocumentType(DocType docType)
        {
            return new DomDocumentType(docType);
        }


        /// <summary>
        /// Makes a deep copy of this object.
        /// </summary>
        ///
        /// <returns>
        /// A copy of this object.
        /// </returns>

        public override DomDocument Clone()
        {
            DomDocument clone = new DomDocument();
            return clone;
        }

        /// <summary>
        /// Clones the child elements of this document
        /// </summary>
        ///
        /// <returns>
        /// A sequence of cloned elements
        /// </returns>

        public override IEnumerable<IDomObject> CloneChildren()
        {
            if (HasChildren)
            {
                foreach (IDomObject obj in ChildNodes)
                {
                    yield return obj.Clone();
                }
            }
            yield break;
        }

        /// <summary>
        /// Convert this object into a string representation; provides summary information about the
        /// document.
        /// </summary>
        ///
        /// <returns>
        /// This object as a string.
        /// </returns>

        public override string ToString()
        {
            return "DOM Root (" + DocType.ToString() + ", " + DescendantCount().ToString() + " elements)";
        }

        /// <summary>
        /// Creates an IDomDocument that is derived from this one. The new type can also be a derived
        /// type, such as IDomFragment. The new object will inherit DomRenderingOptions from this one.
        /// </summary>
        ///
        /// <typeparam name="T">
        /// The type of object to create that is IDomDocument.
        /// </typeparam>
        ///
        /// <returns>
        /// A new, empty concrete class that is represented by the interface T, configured with the same
        /// options as the current object.
        /// </returns>

        public IDomDocument CreateNew<T>() where T : IDomDocument
        {
            return CreateNew(typeof(T));
        }

        /// <summary>
        /// Creates an IDomDocument that is derived from this one. The new type can also be a derived
        /// type, such as IDomFragment. The new object will inherit DomRenderingOptions from this one.
        /// </summary>
        ///
        /// <returns>
        /// A new, empty concrete class that is represented by the interface T, configured with the same
        /// options as the current object.
        /// </returns>

        public virtual IDomDocument CreateNew()
        {
            return CreateNew<IDomDocument>();
        }



        /// <summary>
        /// Creates an IDomDocument that is derived from this one. The new type can also be a derived
        /// type, such as IDomFragment. The new object will inherit DomRenderingOptions from this one.
        /// </summary>
        ///
        /// <exception cref="ArgumentException">
        /// Thrown when one or more arguments have unsupported or illegal values.
        /// </exception>
        ///
        /// <typeparam name="T">
        /// The type of object to create that is IDomDocument.
        /// </typeparam>
        /// <param name="elements">
        /// The elements that are the source for the new document.
        /// </param>
        ///
        /// <returns>
        /// A new, empty concrete class that is represented by the interface T, configured with the same
        /// options as the current object.
        /// </returns>

        public IDomDocument CreateNew<T>(IEnumerable<IDomObject> elements) where T : IDomDocument
        {
            IDomDocument newDoc;
            if (typeof(T) == typeof(IDomDocument))
            {
                newDoc = DomDocument.Create(elements, HtmlParsingMode.Document);

            }
            else if (typeof(T) == typeof(IDomFragment))
            {
                newDoc = DomDocument.Create(elements, HtmlParsingMode.Fragment);
            }

            else
            {
                throw new ArgumentException(String.Format("I don't know about an IDomDocument subclass \"{1}\"",
                    typeof(T).ToString()));
            }

            return newDoc;
        }

        #endregion

        #region private methods


        private IDomDocument CreateNew(Type t)
        {
            IDomDocument newDoc;
            if (t == typeof(IDomDocument))
            {
                newDoc = new DomDocument();

            }
            else if (t == typeof(IDomFragment))
            {
                newDoc = new DomFragment();
            }

            else
            {
                throw new ArgumentException(String.Format("I don't know about an IDomDocument subclass \"{1}\"",
                    t.ToString()));
            }

            return newDoc;
        }


        /// <summary>
        /// Return a sequence of elements that excludes non-Element (e.g. Text) nodes
        /// </summary>
        ///
        /// <param name="objectList">
        /// The input sequence
        /// </param>
        ///
        /// <returns>
        /// A sequence of elements
        /// </returns>

        protected IEnumerable<IDomElement> OnlyElements(IEnumerable<IDomObject> objectList)
        {
            foreach (IDomObject obj in objectList)
            {
                if (obj.NodeType == NodeType.ELEMENT_NODE)
                {
                    yield return (IDomElement)obj;
                }
            }
            yield break;
        }

        #endregion
        

    }
    
}
