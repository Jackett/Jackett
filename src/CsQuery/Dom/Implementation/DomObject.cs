using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using CsQuery.HtmlParser;
using CsQuery.ExtensionMethods;
using CsQuery.ExtensionMethods.Internal;
using CsQuery.Output;

namespace CsQuery.Implementation
{
    /// <summary>
    /// Something that appears in the DOM. This is essentially the same as a Node in the browser DOM,
    /// but IDomObject represents only things that may appear in the DOM (e.g. not an attribute,
    /// document, doctype)
    /// </summary>

    public abstract class DomObject: IDomObject, IDomNode
    {

        #region private properties

        /// <summary>
        /// A reference to the owning document. This is also the topmost node in the tree.
        /// </summary>

        private IDomDocument _Document;

#if CACHE_PATH
        /// <summary>
        /// Backing prop for path
        /// </summary>

        private string _Path = null;

#endif

        /// <summary>
        /// The parent node. Do not expose this. _ParentNode should only be managed by the ParentNode
        /// property.
        /// </summary>

        private DomObject _ParentNode;


        /// <summary>
        /// The implementation for Clone.
        /// </summary>
        ///
        /// <returns>
        /// A clone of this object.
        /// </returns>

        protected abstract IDomObject CloneImplementation();

        /// <summary>
        /// Flags indicating particular states regarding the owning document.
        /// </summary>

        [Flags]
        protected enum DocumentInfo : byte
        {
            /// <summary>
            /// The parent document is indexed.
            /// </summary>
            IsIndexed = 1,
            /// <summary>
            /// The parent document is a document (not a fragment).
            /// </summary>
            IsDocument = 2,
            /// <summary>
            /// The parent document is valid.
            /// </summary>
            IsConnected = 4,
            /// <summary>
            /// The parent document has already been tested..
            /// </summary>
            IsParentTested = 8
        }


        /// <summary>
        /// Information describing metadata about the element's owning document. This is essentially a
        /// cache, it prevents us from having to check to see if there's an owning document and access it
        /// directly. This is an optimizaton as this happens often.
        /// </summary>

        protected DocumentInfo DocInfo;

        #endregion
        
        #region public properties

        /// <summary>
        /// Gets the type of the node.
        /// </summary>

        public abstract NodeType NodeType { get; }

        /// <summary>
        /// Gets a value indicating whether this object has children.
        /// </summary>

        public abstract bool HasChildren { get; }

        /// <summary>
        /// Gets a value indicating whether HTML is allowed as a child of this element. It is possible
        /// for this value to be false but InnerTextAllowed to be true for elements which can have inner
        /// content, but no child HTML markup, such as &lt;textarea&gt; and &lt;script&gt;
        /// </summary>

        public abstract bool InnerHtmlAllowed { get; }

        /// <summary>
        /// Gets the identifier of the node name.
        /// </summary>

        public virtual ushort NodeNameID 
        {
            get 
            {
                // This used to throw an exception; for consistency with NodeName (returning null) changed to 0
                // 9/22/2012. Caused test failure when integrating validator.nu parser (since everything has a root now)
                return 0;
            } 
        }

        /// <summary>
        /// Gets a value indicating whether text content is allowed as a child of this element.
        /// </summary>

        public virtual bool InnerTextAllowed
        {
            get
            {
                return InnerHtmlAllowed;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this element may have children. When false, it means this is
        /// a void element.
        /// </summary>

        public virtual bool ChildrenAllowed
        {
            get { return InnerHtmlAllowed; }
        }

        /// <summary>
        /// Gets a value indicating whether this object type should be indexed.
        /// </summary>

        public virtual bool IsIndexed { 
            get { 
                return false; 
            } 
        }

        /// <summary>
        /// The full path to this node. This is calculated by requesting the parent path and adding its
        /// own ID.
        /// </summary>

        public virtual ushort[] NodePath
        {
            get
            {
#if CACHE_PATH
                if (_Path == null)
                {
                    var path = _ParentNode != null ?
                       GetPath() :
                        "_" + PathID;

                    if (path.Length % 100 == 0)
                    {
                        _Path = path;
                    }
                    return path;
                }
                else
                {
                    return _Path;
                }

#else
                // The underscore is just a convention to easily identify disconnected nodes when debugging. 

                return _ParentNode != null ?
                   GetPath() :
                    new ushort[2] { (ushort)'_', (ushort)Index };
#endif

            }
        }

        /// <summary>
        /// Gets the unique path to this element as a string. THIS METHOD IS OBSOLETE. It has been replaced by NodePath.
        /// </summary>

        [Obsolete]
        public virtual string Path
        {
            get
            {
                StringBuilder path = new StringBuilder();
                foreach (var item in NodePath)
                {
                    path.Append(item);
                }
                return path.ToString();
            }
        }


        /// <summary>
        /// Gets the full path to this document.
        /// </summary>
        ///
        /// <returns>
        /// The path.
        /// </returns>

        protected virtual ushort[] GetPath_UnOptimized()
        {
            DomObject curNode = this;
            List<ushort> path = new List<ushort>();

            while (curNode != null)
            {
#if CACHE_PATH
                if (curNode._Path != null)
                {
                    return curNode._Path + sb.Reverse().ToString();
                }

#endif

                path.Add((ushort)curNode.Index);
                curNode = curNode._ParentNode;
            }

            path.Reverse();
            return path.ToArray();
        }

        /// <summary>
        /// Gets the full path to this document.
        /// </summary>
        ///
        /// <returns>
        /// The path.
        /// </returns>
        
        protected virtual ushort[] GetPath()
        {

            DomObject curNode = this;
            ushort index = 0;

            ushort[] path = new ushort[32];
            int len = 32;

            while (curNode != null)
            {
                path[index++] = (ushort)(curNode.Index);

                if (index == len)
                {
                    len <<= 1;

                    var newPath= new ushort[len];
                    Buffer.BlockCopy(path, 0, newPath, 0, index<<1);
                    path = newPath;
                }

                curNode = curNode._ParentNode;
            }

            // because we obtained the path by traversing backards up the tree, instead of recursing (which
            // will cause a stack overflow & performs a lot worse anyway) we must reverse the array before
            // returning it. 
            
            ushort[] output = new ushort[index];
            int i = 0;
            
            while (index>0)
            {
                output[i++] = path[--index];
            }

            return output;
            
        }
        /// <summary>
        /// The DOM for this object. This is obtained by looking at its parents value until it finds a
        /// non-null Document in a parent. The value is cached locally as long as the current value of
        /// Parent remains the same.
        /// </summary>

        public virtual IDomDocument Document
        {
            get
            {
                if ((DocInfo & DocumentInfo.IsParentTested) == 0)
                {
                    UpdateDocumentFlags();
                }
                return _Document;
            }
          
        }

        /// <summary>
        /// Gets or sets the text content of a node and its descendants.
        /// </summary>

        public virtual string InnerText
        {
            get
            {
                throw new InvalidOperationException("Accessing InnerText is not valid for this element type.");
            }
            set
            {
                throw new InvalidOperationException("Assigning InnerText is not valid for this element type.");
            }
        }

        /// <summary>
        /// Gets or sets the text content of a node and its descendants, including all whitespace.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/Node.textContent
        /// </url>

        public virtual string TextContent
        {
            get
            {
                throw new InvalidOperationException("Accessing TextContent is not valid for this element type.");
            }
            set
            {
                throw new InvalidOperationException("Assigning TextContent is not valid for this element type.");
            }
        }

        // Owner can be null (this is an unbound element)
        // if so create an arbitrary one.

        /// <summary>
        /// Gets or sets or gets the HTML of an elements descendants.
        /// </summary>

        public virtual string InnerHTML
        {
            get
            {
                throw new InvalidOperationException("Accessing InnerHTML is not valid for this element type.");
            }
            set
            {
                throw new InvalidOperationException("Assigning InnerHTML is not valid for this element type.");
            }
        }

        /// <summary>
        /// Gets or sets the outer HTML.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en-US/docs/DOM/element.outerHTML
        /// </url>

        public virtual string OuterHTML
        {
            get
            {
                throw new InvalidOperationException("Accessing OuterHTML is not valid for this element type.");
            }
            set
            {
                throw new InvalidOperationException("Assigning OuterHTML is not valid for this element type.");
            }
        }
        /// <summary>
        /// Gets the child nodes.
        /// </summary>

        public virtual INodeList ChildNodes
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// The direct parent of this node.
        /// </summary>

        public virtual IDomContainer ParentNode
        {
            get
            {
                return (IDomContainer)_ParentNode;
            }
            internal set
            {
                _ParentNode = (DomObject)value;

                // Update the parent info cache
                UpdateDocumentFlags();
            }
        }

        /// <summary>
        /// Returns all of the ancestors of the given node, in descending order of their depth from the root node.
        /// </summary>
        /// <returns>The ancestors.</returns>
        public virtual IEnumerable<IDomContainer> GetAncestors()
        {
            IDomContainer current = ParentNode;
            while (current != null)
            {
                yield return current;
                current = current.ParentNode;
            }
        }

        /// <summary>
        /// Returns all of the descendents of the given node, in pre-order depth first order.
        /// </summary>
        /// <returns>The descendents.</returns>
        public virtual IEnumerable<IDomObject> GetDescendents()
        {
            if (HasChildren)
            {
                foreach (IDomObject child in ChildNodes)
                {
                    yield return child;
                    foreach (IDomObject descendent in child.GetDescendents())
                    {
                        yield return descendent;
                    }
                }
            }
        }

        /// <summary>
        /// Returns all IDomElement descendents of the given node, in pre-order depth first order.
        /// </summary>
        /// <returns>The descendents.</returns>
        public virtual IEnumerable<IDomElement> GetDescendentElements()
        {
            return GetDescendents().OfType<IDomElement>();
        }

        /// <summary>
        /// The element is not associated with an IDomDocument.
        /// </summary>

        public virtual bool IsFragment
        {
            get
            {
                //return IsDisconnected || Document.NodeType == NodeType.DOCUMENT_FRAGMENT_NODE;
                return (DocInfo & DocumentInfo.IsDocument) == 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this object belongs to a Document or not.
        /// </summary>

        public virtual bool IsDisconnected
        {
            get
            {
                //return Document==null;
                return (DocInfo & DocumentInfo.IsConnected) == 0;
            }
        }


        /// <summary>
        /// Gets a unique ID for this element among its siblings. THIS METHOD IS OBSOLETE. It has been replaced by NodePath.
        /// </summary>
        
        [Obsolete]
        public virtual char PathID
        {
            get
            {
                // Don't actually store paths with non-element nodes as they aren't indexed and don't have children.
                // Fast read access is less important than not having to reset them when moved.

                return (char)(Index + 2);
            }
        }

        
        /// <summary>
        /// Gets the depth of the current node.
        /// </summary>

        public virtual int Depth
        {
            get
            {
                return GetDepth();
            }

        }

        /// <summary>
        /// Gets the depth of the current node.
        /// </summary>
        ///
        /// <returns>
        /// The depth.
        /// </returns>

        protected int GetDepth()
        {
            DomObject curNode = this._ParentNode;
            int depth = 0;
            while (curNode != null && curNode.NodeType != NodeType.DOCUMENT_NODE)
            {
                depth++;
                curNode = curNode._ParentNode;
            }
            return depth;
        }

        /// <summary>
        /// Gets the child elements.
        /// </summary>

        public virtual IEnumerable<IDomElement> ChildElements
        {
            get
            {
                yield break;
            }
        }

        /// <summary>
        /// The element's absolute index among its siblings.
        /// </summary>
        public int Index
        {
            get;
            internal set;
        }

        /// <summary>
        /// Gets or sets the identifier of the node path.
        /// TODO: We are going to use this to create a sparse index so we don't have to reindex each time a node is removed
        /// </summary>

        public ushort NodePathID
        {
            get
            {
                return (ushort)Index;
            }
        }

        /// <summary>
        /// The value of an input element, or the text of a textarea element.
        /// </summary>

        public virtual string DefaultValue
        {
            get
            {
                return null;
            }

            set
            {
                throw new InvalidOperationException("DefaultValue is not valid for this node type");
            }
        }

        /// <summary>
        /// Gets or sets the node value.
        /// </summary>

        public virtual string NodeValue
        {
            get
            {
                return null;
            }
            set
            {
                throw new InvalidOperationException("You can't set NodeValue for this node type.");
            }
        }

        /// <summary>
        /// The value of the "type" attribute. For input elements, this property always returns a
        /// lowercase value and defaults to "text" if there is no type attribute. For other element types,
        /// it simply returns the value of the "type" attribute.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/XUL/Property/type
        /// </url>

        public virtual string Type
        {
            get
            {
                return null;
            }
            set
            {
                throw new InvalidOperationException("You can't set Type for this node type.");
            }
        }

        /// <summary>
        /// Gets or sets the name attribute of an DOM object, it only applies to the following elements:
        /// &lt;a&gt; , &lt;applet&gt; , &lt;form&gt; , &lt;frame&gt; , &lt;iframe&gt; , &lt;img&gt; ,
        /// &lt;input&gt; , &lt;map&gt; , &lt;meta&gt; , &lt;object&gt; , &lt;option&gt; , &lt;param&gt; ,
        /// &lt;select&gt; , and &lt;textarea&gt; .
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/element.name
        /// </url>

        public virtual string Name
        {
            get
            {
                return null;
            }
            set
            {
                throw new InvalidOperationException("You can't set Name for this node type.");
            }
        }

        /// <summary>
        /// Get or set value of the id attribute.
        /// </summary>

        public virtual string Id
        {
            get
            {
                return null;
            }
            set
            {
                throw new InvalidOperationException("Cannot set ID for this node type.");
            }
        }

        /// <summary>
        /// For input elements, the "value" property of this element. Returns null for other element
        /// types.
        /// </summary>

        public virtual string Value
        {
            get
            {
                return null;
            }
            set
            {
                throw new InvalidOperationException("Cannot set value for this node type.");
            }
        }

        /// <summary>
        /// gets and sets the value of the class attribute of the specified element.
        /// </summary>

        public virtual string ClassName
        {
            get
            {
                return null;
            }
            set
            {
                throw new InvalidOperationException("ClassName is not applicable to this node type.");
            }
        }

        /// <summary>
        /// A sequence of all unique class names defined on this element.
        /// </summary>

        public virtual IEnumerable<string> Classes
        {
            get
            {
                yield break;
            }

        }

        /// <summary>
        /// An interface to access the attributes collection of this element.
        /// </summary>

        public virtual IAttributeCollection Attributes
        {
            get
            {
                return null;
            }
            protected set
            {
                throw new InvalidOperationException("Attributes collection is not applicable to this node type.");
            }
        }

        /// <summary>
        /// An object encapsulating the Styles associated with this element.
        /// </summary>

        public virtual CSSStyleDeclaration Style
        {
            get
            {
                return null;
            }
            set
            {
                throw new InvalidOperationException("Style is not applicable to this node type.");
            }
        }

        /// <summary>
        /// The node (tag) name, in upper case.
        /// </summary>

        public virtual string NodeName
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the node's first child in the tree, or null if the node is childless. If the node is
        /// a Document, it returns the first node in the list of its direct children.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/element.firstChild
        /// </url>

        public virtual IDomObject FirstChild
        {
            get { return null; }
        }

        /// <summary>
        /// Returns the last child of a node.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/Node.lastChild
        /// </url>

        public virtual IDomObject LastChild
        {
            get { return null; }
        }

        /// <summary>
        /// Returns the element's first child element or null if there are no child elements.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/Element.firstElementChild
        /// </url>

        public virtual IDomElement FirstElementChild
        {
            get { return null; }
        }

        /// <summary>
        /// Returns the element's last child element or null if there are no child elements.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/Element.lastElementChild
        /// </url>

        public virtual IDomElement LastElementChild
        {
            get { return null; }
        }

        /// <summary>
        /// Returns true if this node has any attributes.
        /// </summary>

        public virtual bool HasAttributes
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if this node has CSS classes.
        /// </summary>

        public virtual bool HasClasses
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns true if this node has any styles defined.
        /// </summary>

        public virtual bool HasStyles
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the element is checked.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/XUL/Property/checked
        /// </url>

        public virtual bool Checked
        {
            get
            {
                return false;
            }
            set
            {
                throw new InvalidOperationException("Not valid for this element type.");
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the element is disabled.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/XUL/Property/disabled
        /// </url>

        public virtual bool Disabled
        {
            get
            {
                return false;
            }
            set
            {
                throw new InvalidOperationException("Not valid for this element type.");
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the only should be read.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/XUL/Property/readOnly
        /// </url>

        public virtual bool ReadOnly
        {
            get
            {
                return true;
            }
            set
            {
                throw new InvalidOperationException("Not valid for this element type.");
            }
        }

        /// <summary>
        /// Returns the node immediately following the specified one in its parent's childNodes list, or
        /// null if the specified node is the last node in that list.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/Node.nextSibling
        /// </url>

        public IDomObject NextSibling
        {
            get
            {
                return ParentNode != null && ParentNode.ChildNodes.Count - 1 > Index ?
                    ParentNode.ChildNodes[Index + 1] :
                    null;
            }
        }

        /// <summary>
        /// Returns the node immediately preceding the specified one in its parent's childNodes list,
        /// null if the specified node is the first in that list.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/Node.previousSibling
        /// </url>

        public IDomObject PreviousSibling
        {
            get
            {
                return ParentNode != null && Index > 0 ?
                    ParentNode.ChildNodes[Index - 1] :
                    null;
            }
        }

        /// <summary>
        /// Returns the element immediately following the specified one in its parent's children list, or
        /// null if the specified element is the last one in the list.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/Element.nextElementSibling
        /// </url>

        public IDomElement NextElementSibling
        {
            get
            {
                if (ParentNode == null)
                {
                    return null;
                }
                int curIndex = Index;
                var elements = ParentNode.ChildNodes;

                while (++curIndex < elements.Count)
                {
                    if (elements[curIndex].NodeType == NodeType.ELEMENT_NODE)
                    {
                        return (IDomElement)elements[curIndex];
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Returns the element immediately prior to the specified one in its parent's children list, or
        /// null if the specified element is the first one in the list.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/Element.previousElementSibling
        /// </url>

        public IDomElement PreviousElementSibling
        {
            get
            {
                if (ParentNode == null)
                {
                    return null;
                }

                int curIndex = Index;
                var elements = ParentNode.ChildNodes;
                while (--curIndex >= 0)
                {
                    if (elements[curIndex].NodeType == NodeType.ELEMENT_NODE)
                    {
                        return (IDomElement)elements[curIndex];
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// The child node at the specified index.
        /// </summary>
        ///
        /// <param name="index">
        /// The zero-based index of the child node to access.
        /// </param>
        ///
        /// <returns>
        /// IDomObject, the element at the specified index within this node's children.
        /// </returns>

        public virtual IDomObject this[int index]
        {
            get
            {
                throw new InvalidOperationException("This element type does not have children");
            }
        }

        /// <summary>
        /// The child node at the specified index.
        /// </summary>
        ///
        /// <param name="attribute">
        /// The zero-based index of the child node to access.
        /// </param>
        ///
        /// <returns>
        /// IDomObject, the element at the specified index within this node's children.
        /// </returns>

        public virtual string this[string attribute]
        {
            get
            {
                throw new InvalidOperationException("This element type does not have attributes");
            }
            set
            {
                throw new InvalidOperationException("This element type does not have attributes");
            }
        }

        #endregion

        #region public methods

        /// <summary>
        /// Renders the complete HTML for this element, including its children.
        /// </summary>
        ///
        /// <returns>
        /// a string of HTML.
        /// </returns>

        public virtual string Render()
        {
            return Render(OutputFormatters.Default);
        }

        /// <summary>
        /// Renders the complete HTML for this element, including its children, using the specified
        /// OutputFormatter.
        /// </summary>
        ///
        /// <param name="formatter">
        /// The formatter that controls how the ouput is rendered.
        /// </param>
        /// <param name="writer">
        /// The writer to which output should be written.
        /// </param>

        public virtual void Render(IOutputFormatter formatter, TextWriter writer)
        {
            formatter.Render(this,writer);
        }

        /// <summary>
        /// Renders the complete HTML for this element, including its children, using the specified
        /// OutputFormatter.
        /// </summary>
        ///
        /// <param name="formatter">
        /// The formatter.
        /// </param>
        ///
        /// <returns>
        /// a string of HTML.
        /// </returns>

        public virtual string Render(IOutputFormatter formatter)
        {
            return formatter.Render(this);
        }

        /// <summary>
        /// Renders the complete HTML for this element, including its children.
        /// </summary>
        ///
        /// <returns>
        /// a string of HTML
        /// </returns>

        public virtual string Render(DomRenderingOptions options)
        {
            var formatter = new FormatDefault(options, HtmlEncoders.Default);
            return formatter.Render(this);

        }

        /// <summary>
        /// Renders the complete HTML for this element, including its children, using the OutputFormatter.
        /// </summary>
        ///
        /// <param name="sb">
        /// An existing StringBuilder instance to append this element's HTML.
        /// </param>


        [Obsolete]
        public virtual void Render(StringBuilder sb)
        {
            Render(sb, DomRenderingOptions.Default);
        }


        /// <summary>
        /// Renders the complete HTML for this element to a StringBuilder. Note: This obsolete and will
        /// be removed; please use Render(IOutputFormatter).
        /// </summary>
        ///
        /// <param name="sb">
        /// An existing StringBuilder instance to append this element's HTML.
        /// </param>
        /// <param name="options">
        /// (optional) options for controlling the operation.
        /// </param>

        [Obsolete]
        public virtual void Render(StringBuilder sb, DomRenderingOptions options)
        {
            sb.Append(Render(options));
        }

        
        /// <summary>
        /// Wrap this element in a CQ object. This is the CsQuery equivalent of the common jQuery
        /// construct $(el). Since there is no default method in C# that we can use to create a similar
        /// syntax, this method serves the same purpose.
        /// </summary>
        ///
        /// <returns>
        /// A new CQ object wrapping this element.
        /// </returns>

        public CQ Cq()
        {
            return new CQ(this);
        }

        /// <summary>
        /// Clone this element.
        /// </summary>
        ///
        /// <returns>
        /// A copy of this element that is not bound to the original.
        /// </returns>

        public virtual IDomObject Clone()
        {
            return CloneImplementation();
        }

        /// <summary>
        /// Removes this object from it's parent, and consequently the Document, if any, to which it
        /// belongs.
        /// </summary>
        ///
        /// <exception cref="InvalidOperationException">
        /// Thrown when the the node has no parent.
        /// </exception>

        public virtual void Remove()
        {
            if (ParentNode == null)
            {
                throw new InvalidOperationException("This element has no parent.");
            }
            ParentNode.ChildNodes.Remove(this);
        }

        /// <summary>
        /// Return the total number of descendants of this element.
        /// </summary>
        ///
        /// <returns>
        /// int, the total number of descendants.
        /// </returns>

        public virtual int DescendantCount()
        {
            return 0;
        }

        /// <summary>
        /// Adds a node to the end of the list of children of a specified parent node. If the node
        /// already exists it is removed from current parent node, then added to new parent node.
        /// </summary>
        ///
        /// <exception cref="InvalidOperationException">
        /// Thrown when the requested operation is invalid.
        /// </exception>
        ///
        /// <param name="element">
        /// The element to append.
        /// </param>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/Node.appendChild
        /// </url>

        public virtual void AppendChild(IDomObject element)
        {
            throw new InvalidOperationException("This type of element does not have children.");
        }

        /// <summary>
        /// Appends a child without checking to see if its already a member of the DOM. For use in DOM
        /// construction to avoid unneeded overhead.
        /// </summary>
        ///
        /// <exception cref="InvalidOperationException">
        /// Thrown when the requested operation is invalid.
        /// </exception>
        ///
        /// <param name="element">
        /// The element to append.
        /// </param>

        internal virtual void AppendChildUnsafe(IDomObject element) 
        {
            throw new InvalidOperationException("This type of element does not have children.");
        }
        /// <summary>
        /// Removes a child node from the DOM. Returns removed node.
        /// </summary>
        ///
        /// <exception cref="InvalidOperationException">
        /// Thrown when the requested operation is invalid.
        /// </exception>
        ///
        /// <param name="element">
        /// The element to remove.
        /// </param>
        ///
        /// <url>
        /// https://developer.mozilla.org/En/DOM/Node.removeChild
        /// </url>

        public virtual void RemoveChild(IDomObject element)
        {
            throw new InvalidOperationException("This type of element does not have children.");
        }

        /// <summary>
        /// Inserts the specified node before a reference element as a child of the current node.
        /// </summary>
        ///
        /// <exception cref="InvalidOperationException">
        /// Thrown when the requested operation is invalid.
        /// </exception>
        ///
        /// <param name="newNode">
        /// The node to insert.
        /// </param>
        /// <param name="referenceNode">
        /// The node before which the new node will be inserted.
        /// </param>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/Node.insertBefore
        /// </url>

        public virtual void InsertBefore(IDomObject newNode, IDomObject referenceNode)
        {
            throw new InvalidOperationException("This type of element does not have children.");
        }

        /// <summary>
        /// Inserts the specified node after a reference element as a child of the current node.
        /// </summary>
        ///
        /// <exception cref="InvalidOperationException">
        /// Thrown when the requested operation is invalid.
        /// </exception>
        ///
        /// <param name="newNode">
        /// The new node to be inserted.
        /// </param>
        /// <param name="referenceNode">
        /// The node after which the new node will be inserted.
        /// </param>

        public virtual void InsertAfter(IDomObject newNode, IDomObject referenceNode)
        {
            throw new InvalidOperationException("This type of element does not have children.");
        }

        /// <summary>
        /// Adds a new boolean attribute or sets its value to true.
        /// </summary>
        ///
        /// <exception cref="InvalidOperationException">
        /// Thrown when the object type does not support attributes
        /// </exception>
        ///
        /// <param name="name">
        /// The attribute name.
        /// </param>

        public virtual void SetAttribute(string name)
        {
            throw new InvalidOperationException("You can't set attributes for this element type.");
        }

        /// <summary>
        /// Adds a new attribute or changes the value of an existing attribute on the specified element.
        /// </summary>
        ///
        /// <exception cref="InvalidOperationException">
        /// Thrown when the object type does not support attributes
        /// </exception>
        ///
        /// <param name="name">
        /// The attribute name.
        /// </param>
        /// <param name="value">
        /// For input elements, the "value" property of this element. Returns null for other element
        /// types.
        /// </param>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/element.setAttribute
        /// </url>

        public virtual void SetAttribute(string name, string value)
        {
            throw new InvalidOperationException("You can't set attributes for this element type.");
        }

        /// <summary>
        /// Returns the value of the named attribute on the specified element. If the named attribute
        /// does not exist, the value returned will either be null or "" (the empty string)
        /// </summary>
        ///
        /// <param name="name">
        /// The attribute name.
        /// </param>
        ///
        /// <returns>
        /// The attribute value string.
        /// </returns>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/element.getAttribute
        /// </url>

        public virtual string GetAttribute(string name)
        {
            return null;
        }

        /// <summary>
        /// Returns the value of the named attribute on the specified element. If the named attribute
        /// does not exist, the value returned will either be the provide "defaultValue".
        /// </summary>
        ///
        /// <param name="name">
        /// The attribute name.
        /// </param>
        /// <param name="defaultValue">
        /// A string to return if the attribute does not exist.
        /// </param>
        ///
        /// <returns>
        /// The attribute value string.
        /// </returns>
        ///
        /// <seealso cref="T:CsQuery.IDomObject.GetAttribute"/>

        public virtual string GetAttribute(string name, string defaultValue)
        {
            return null;
        }

        /// <summary>
        /// Try to get a named attribute.
        /// </summary>
        ///
        /// <param name="name">
        /// The attribute name.
        /// </param>
        /// <param name="value">
        /// The attribute value, or null if the named attribute does not exist.
        /// </param>
        ///
        /// <returns>
        /// true if the attribute exists, false if it does not.
        /// </returns>

        public virtual bool TryGetAttribute(string name, out string value)
        {
            value = null;
            return false;
        }

        /// <summary>
        /// Returns a boolean value indicating whether the specified element has the specified attribute
        /// or not.
        /// </summary>
        ///
        /// <param name="name">
        /// The attribute name.
        /// </param>
        ///
        /// <returns>
        /// true if the named attribute exists, false if not.
        /// </returns>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/element.hasAttribute
        /// </url>

        public virtual bool HasAttribute(string name)
        {
            return false;
        }

      
        /// <summary>
        /// Removes an attribute from the specified element.
        /// </summary>
        ///
        /// <exception cref="InvalidOperationException">
        /// Thrown when the requested operation is invalid.
        /// </exception>
        ///
        /// <param name="name">
        /// The attribute name.
        /// </param>
        ///
        /// <returns>
        /// true if it the attribute exists, false if the attribute did not exist. If the attribute
        /// exists it will always be removed, that is, it is not possible for this method to fail unless
        /// the attribute does not exist.
        /// </returns>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/element.removeAttribute
        /// </url>

        public virtual bool RemoveAttribute(string name)
        {
            throw new InvalidOperationException("You can't remove attributes from this element type.");
        }

        /// <summary>
        /// Returns a boolean value indicating whether the named class exists on this element.
        /// </summary>
        ///
        /// <param name="className">
        /// The class name for which to test.
        /// </param>
        ///
        /// <returns>
        /// true if the class is a member of this elements classes, false if not.
        /// </returns>

        public virtual bool HasClass(string className)
        {
            return false;
        }

        /// <summary>
        /// Adds the class.
        /// </summary>
        ///
        /// <exception cref="InvalidOperationException">
        /// Thrown when the requested operation is invalid.
        /// </exception>
        ///
        /// <param name="className">
        /// The class name for which to test.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public virtual bool AddClass(string className)
        {
            throw new InvalidOperationException("You can't add classes to this element type.");
        }

        /// <summary>
        /// Removes the named class from the classes defined for this element.
        /// </summary>
        ///
        /// <exception cref="InvalidOperationException">
        /// Thrown when the requested operation is invalid.
        /// </exception>
        ///
        /// <param name="className">
        /// The class name to remove.
        /// </param>
        ///
        /// <returns>
        /// true if the class exists and was removed from this element, false if the class did not exist.
        /// If the class exists it will always be removed, that is, it is not possible for this method to
        /// fail if the class exists.
        /// </returns>

        public virtual bool RemoveClass(string className)
        {
            throw new InvalidOperationException("You can't remove classes from this element type.");
        }

        /// <summary>
        /// Returns a boolean value indicating whether the named style is defined in the styles for this
        /// element.
        /// </summary>
        ///
        /// <param name="styleName">
        /// Name of the style to test.
        /// </param>
        ///
        /// <returns>
        /// true if the style is explicitly defined on this element, false if not.
        /// </returns>

        public virtual bool HasStyle(string styleName)
        {
            return false;
        }

        /// <summary>
        /// Adds a style descriptor to this element, validating the style name and value against the CSS3
        /// ruleset. The string should be of the form "styleName: styleDef;", e.g.
        /// 
        ///     "width: 10px;"
        /// 
        /// The trailing semicolon is optional.
        /// </summary>
        ///
        /// <exception cref="InvalidOperationException">
        /// Thrown when the requested operation is invalid.
        /// </exception>
        ///
        /// <param name="styleString">
        /// The style string.
        /// </param>

        public virtual void AddStyle(string styleString)
        {
            throw new InvalidOperationException("You can't add styles to this element type.");
        }

        /// <summary>
        /// Adds a style descriptor to this element, optionally validating against the CSS3 ruleset. The
        /// default method always validates; this overload should be used if validation is not desired.
        /// </summary>
        ///
        /// <exception cref="InvalidOperationException">
        /// Thrown when the requested operation is invalid.
        /// </exception>
        ///
        /// <param name="styleString">
        /// An object encapsulating the Styles associated with this element.
        /// </param>
        /// <param name="strict">
        /// true to enforce validation of CSS3 styles.
        /// </param>

        public virtual void AddStyle(string styleString, bool strict)
        {
            throw new InvalidOperationException("You can't add styles to this element type.");
        }

        /// <summary>
        /// Removes the named style from this element.
        /// </summary>
        ///
        /// <exception cref="InvalidOperationException">
        /// Thrown when the requested operation is invalid.
        /// </exception>
        ///
        /// <param name="name">
        /// The style name.
        /// </param>
        ///
        /// <returns>
        /// true if the style exists and is removed, false if the style did not exist.
        /// </returns>

        public virtual bool RemoveStyle(string name)
        {
            throw new InvalidOperationException("You can't remove styles to this element type.");
        }

        /// <summary>
        /// The ToString() override for an object depends on the type of element.
        /// </summary>
        ///
        /// <returns>
        /// A <see cref="T:System.String" /> that represents the current IDomObject.
        /// </returns>

        public override string ToString()
        {
            return Render();
        }

        #endregion
        
        #region element properties


        /// <summary>
        /// The index excluding text nodes.
        /// </summary>

        public virtual int ElementIndex
        {
            get
            {
                throw new InvalidOperationException("This is not an Element object.");
            }
        }

        /// <summary>
        /// An enumeration of clones of the chilren of this object
        /// </summary>
        ///
        /// <returns>
        /// An enumerator 
        /// </returns>

        public virtual IEnumerable<IDomObject> CloneChildren()
        {
             throw new InvalidOperationException("This is not a Container object.");
        }

        /// <summary>
        /// Returns the HTML for this element, but ignoring children/innerHTML.
        /// </summary>
        ///
        /// <returns>
        /// A string of HTML
        /// </returns>

        public virtual string ElementHtml()
        {
            throw new InvalidOperationException("This is not an Element object.");
        }

        /// <summary>
        /// Gets a value indicating whether this object is a block element
        /// </summary>

        public virtual bool IsBlock
        {
            get
            {
                throw new InvalidOperationException("This is not an Element object.");
            }
        }

        /// <summary>
        /// Enumerates index keys for this element.
        /// </summary>
        ///
        /// <exception cref="InvalidOperationException">
        /// Thrown when the requested operation is not valid for this element type
        /// </exception>
        ///
        /// <returns>
        /// A sequence of keys
        /// </returns>

        public virtual IEnumerable<ushort[]> IndexKeysRanged()
        {
            throw new InvalidOperationException("This is not an indexed object.");
        }

        /// <summary>
        /// Enumerates index keys in this collection.
        /// </summary>
        ///
        /// <exception cref="InvalidOperationException">
        /// Thrown when the requested operation is invalid.
        /// </exception>
        ///
        /// <returns>
        /// An enumerator that allows foreach to be used to process index keys in this collection.
        /// </returns>

        public virtual IEnumerable<ushort[]> IndexKeys()
        {
            throw new InvalidOperationException("This is not an indexed object.");
        }


        /// <summary>
        /// Gets the object to which this index entry refers.
        /// </summary>

        public virtual IDomObject IndexReference
        {
            get
            {
                throw new InvalidOperationException("This is not an indexed object.");
            }
        }

        #endregion

        #region option element properties

        /// <summary>
        /// Indicates whether the element is selected or not. This value is read-only. To change the
        /// selection, set either the selectedIndex or selectedItem property of the containing element.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/XUL/Attribute/selected
        /// </url>

        public virtual bool Selected
        {
            get
            {
                throw new Exception("The Selected property cannot does not apply to this element type.");
            }
            set
            {
                throw new Exception("The Selected property cannot be set for this element type.");
            }
        }


        #endregion

        #region private methods

        /// <summary>
        /// Updates the cached Document and property flags.
        /// </summary>

        protected void UpdateDocumentFlags()
        {
            UpdateDocumentFlags(ParentNode == null ? null : ParentNode.Document);

        }

        /// <summary>
        /// Updates the cached Document and property flags.
        /// </summary>
        ///
        /// <param name="document">
        /// A reference to the owning document. This is also the topmost node in the tree.
        /// </param>

        protected void UpdateDocumentFlags(IDomDocument document)
        {
            _Document = document;
            SetDocFlags();
            // I think we can get away without resetting children. When removing something from a document,
            // you are exclusively going to be adding it to something else. We only need to update the parents
            // during the add operation.

            if (HasChildren && _Document != null)
            {
                foreach (var item in ChildNodes.Cast<DomObject>())
                {
                    item.UpdateDocumentFlags(_Document);
                }
            }
        }

        private void SetDocFlags()
        {
            DocInfo = DocumentInfo.IsParentTested |
                (_Document == null ? 0 :
                    DocumentInfo.IsConnected |
                    (_Document.NodeType == NodeType.DOCUMENT_NODE ?
                        DocumentInfo.IsDocument : 0) |
                    (_Document.IsIndexed ?
                        DocumentInfo.IsIndexed : 0));

        }


        #endregion

        #region interface members

        /// <summary>
         /// Makes a deep copy of this object.
         /// </summary>
         ///
         /// <returns>
         /// A copy of this object.
         /// </returns>

         IDomNode IDomNode.Clone()
         {
             return Clone();
         }

         /// <summary>
         /// Compares this IDomObject object to another to determine their relative ordering.
         /// </summary>
         ///
         /// <param name="other">
         /// Another instance to compare.
         /// </param>
         ///
         /// <returns>
         /// Negative if this object is less than the other, 0 if they are equal, or positive if this is
         /// greater.
         /// </returns>

         public int CompareTo(IDomObject other)
         {
             return PathKeyComparer.Comparer.Compare(NodePath, other.NodePath);
         }
        #endregion

         
    }
}
