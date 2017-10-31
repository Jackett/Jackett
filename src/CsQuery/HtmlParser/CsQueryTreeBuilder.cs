using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using HtmlParserSharp.Common;
using HtmlParserSharp.Core;
using CsQuery;
using CsQuery.Implementation;
using CsQuery.Engine;

namespace CsQuery.HtmlParser
{
    /// <summary>
    /// The tree builder glue for building a tree through the public DOM APIs.
    /// </summary>
    public class CsQueryTreeBuilder : CoalescingTreeBuilder<DomObject>
    {
        /// <summary>
        /// Constructor; requires a DomDocument object to populate.
        /// </summary>
        ///
        /// <param name="domIndexProvider">
        /// The DomIndexProvider that provides instances of DomIndex objects that determine the indexing
        /// strategy for new documents.
        /// </param>

        public CsQueryTreeBuilder(IDomIndexProvider domIndexProvider)
            : base()
        {
            DomIndexProvider = domIndexProvider;
        }


        /// <summary>
        /// Returns the document.
        /// </summary>
        ///
        /// <value>
        /// The document.
        /// </value>

        internal DomDocument Document;


        private IDomIndexProvider DomIndexProvider;

        /// <summary>
        /// This is a fragment
        /// </summary>

        private bool isFragment;

        /// <summary>
        /// Adds the attributes passed by parameter to the element.
        /// </summary>
        ///
        /// <param name="element">
        /// The element.
        /// </param>
        /// <param name="attributes">
        /// The attributes.
        /// </param>

        override protected void AddAttributesToElement(DomObject element, HtmlAttributes attributes)
        {
            for (int i = 0; i < attributes.Length; i++)
            {
                string attributeName = AttributeName(attributes.GetLocalName(i), attributes.GetURI(i));
                //if (!element.HasAttribute(attributeName))
                //{
                    element.SetAttribute(attributeName, attributes.GetValue(i));
                //}
            }
        }

        /// <summary>
        /// Appends text a node.
        /// </summary>
        ///
        /// <param name="parent">
        /// The parent.
        /// </param>
        /// <param name="text">
        /// The text.
        /// </param>

        override protected void AppendCharacters(DomObject parent, string text)
        {
            IDomText lastChild = parent.LastChild as IDomText;
            if (lastChild != null)
            {
                lastChild.NodeValue += text;
                
            } else {
                lastChild = Document.CreateTextNode(text);
                parent.AppendChildUnsafe(lastChild);
            }
        }

        /// <summary>
        /// Move elements from one parent to another
        /// </summary>
        ///
        /// <param name="oldParent">
        /// The old parent.
        /// </param>
        /// <param name="newParent">
        /// The new parent.
        /// </param>

        override protected void AppendChildrenToNewParent(DomObject oldParent, DomObject newParent)
        {
            while (oldParent.HasChildren)
            {
                // cannot use unsafe method here - this method specifically moves children from another element
                
                newParent.AppendChild(oldParent.FirstChild);
            }
        }

        /// <summary>
        /// Appends a doctype node to the document.
        /// </summary>
        ///
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="fpi">
        /// The formal public identifier
        /// </param>
        /// <param name="uri">
        /// The URI
        /// </param>

        protected override void AppendDoctypeToDocument(string name, string fpi, string uri)
        {
           var doctype = Document.CreateDocumentType(name,"PUBLIC",fpi,uri);

            Document.AppendChildUnsafe(doctype);
        }

        /// <summary>
        /// Appends a comment node
        /// </summary>
        ///
        /// <param name="parent">
        /// The parent.
        /// </param>
        /// <param name="comment">
        /// The comment.
        /// </param>

        override protected void AppendComment(DomObject parent, String comment)
        {
            parent.AppendChildUnsafe(new DomComment(comment));
        }

        /// <summary>
        /// Appends a comment to document root.
        /// </summary>
        ///
        /// <param name="comment">
        /// The comment.
        /// </param>

        override protected void AppendCommentToDocument(String comment)
        {
            Document.AppendChildUnsafe(Document.CreateComment(comment));
        }

        /// <summary>
        /// Create a new element.
        /// </summary>
        ///
        /// <param name="ns">
        /// The namespace.
        /// </param>
        /// <param name="name">
        /// The node name.
        /// </param>
        /// <param name="attributes">
        /// The attributes.
        /// </param>
        ///
        /// <returns>
        /// The new element.
        /// </returns>

        override protected DomObject CreateElement(string ns, string name, HtmlAttributes attributes)
        {
            // ns is not used
            DomElement rv = DomElement.Create(name);
            for (int i = 0; i < attributes.Length; i++)
            {

                string attributeName = AttributeName(attributes.GetLocalName(i), attributes.GetURI(i));
                rv.SetAttribute(attributeName, attributes.GetValue(i));
                //if (attributes.GetType(i) == "ID")
                //{
                    //rv.setIdAttributeNS(null, attributes.GetLocalName(i), true); // FIXME
                //}
            }
            return rv;
        }

        /// <summary>
        /// Creates the root HTML element.
        /// </summary>
        ///
        /// <param name="attributes">
        /// The attributes.
        /// </param>
        ///
        /// <returns>
        /// The new HTML element.
        /// </returns>

        override protected DomObject CreateHtmlElementSetAsRoot(HtmlAttributes attributes)
        {
            if (!isFragment)
            {
                DomElement rv = DomElement.Create("html");
                for (int i = 0; i < attributes.Length; i++)
                {
                    string attributeName = AttributeName(attributes.GetLocalName(i), attributes.GetURI(i));
                    rv.SetAttribute(attributeName, attributes.GetValue(i));
                }
                Document.AppendChildUnsafe(rv);
                return rv;
            }
            else
            {
                return Document;
            }
        }

        /// <summary>
        /// Appends an element as a child of another element.
        /// </summary>
        ///
        /// <param name="child">
        /// The child.
        /// </param>
        /// <param name="newParent">
        /// The parent.
        /// </param>

        override protected void AppendElement(DomObject child, DomObject newParent)
        {
           newParent.AppendChildUnsafe(child);
        }

        /// <summary>
        /// Test whether the element has any children.
        /// </summary>
        ///
        /// <param name="element">
        /// The element.
        /// </param>
        ///
        /// <returns>
        /// true if it has children, false if not.
        /// </returns>

        override protected bool HasChildren(DomObject element)
        {
            return element.HasChildren;
        }

        /// <summary>
        /// Create a new element.
        /// </summary>
        ///
        /// <param name="ns">
        /// The namespace.
        /// </param>
        /// <param name="name">
        /// The node name.
        /// </param>
        /// <param name="attributes">
        /// The attributes.
        /// </param>
        /// <param name="form">
        /// The form.
        /// </param>
        ///
        /// <returns>
        /// The new element.
        /// </returns>

        override protected DomObject CreateElement(string ns, string name, HtmlAttributes attributes, DomObject form)
        {

            DomObject rv = CreateElement(ns, name, attributes);
            //rv.setUserData("nu.validator.form-pointer", form, null); // TODO
            return rv;
        }

        /// <summary>
        /// Run when the parsing process begins. Any config properties should be set here
        /// </summary>
        ///
        /// <param name="fragment">
        /// This is a fragment.
        /// </param>

        override protected void Start(bool fragment)
        {
            isFragment = fragment;
            if (Document == null)
            {
                Document = fragment ?
                    new DomFragment(DomIndexProvider.GetDomIndex()) :
                    new DomDocument(DomIndexProvider.GetDomIndex());
            }

            // don't queue changes this while creating the document; while this improves performance when
            // working interactively, but it would add overhead now. 

            IDomIndexQueue indexQueue = Document.DocumentIndex as IDomIndexQueue;
            if (indexQueue != null)
            {
                indexQueue.QueueChanges = false;
            }
        }

        /// <summary>
        /// Run when the document mode is set.
        /// </summary>
        ///
        /// <param name="mode">
        /// The mode.
        /// </param>
        /// <param name="publicIdentifier">
        /// DocType public identifier.
        /// </param>
        /// <param name="systemIdentifier">
        /// DocType system identifier.
        /// </param>
        /// <param name="html4SpecificAddcionalErrorChecks">
        /// true to HTML 4 specific addcional error checks.
        /// </param>

        protected override void ReceiveDocumentMode(DocumentMode mode, String publicIdentifier,
                String systemIdentifier, bool html4SpecificAddcionalErrorChecks)
        {
            //document.setUserData("nu.validator.document-mode", mode, null); // TODO
        }


        /// <summary>
        /// Inserts foster parented characters.
        /// </summary>
        ///
        /// <param name="text">
        /// The text.
        /// </param>
        /// <param name="table">
        /// The table.
        /// </param>
        /// <param name="stackParent">
        /// The stack parent.
        /// </param>

        override protected void InsertFosterParentedCharacters(string text, DomObject table, DomObject stackParent)
        {
            IDomObject parent = table.ParentNode;
            if (parent != null)
            { 
                // always an element if not null
                IDomObject previousSibling = table.PreviousSibling;
                if (previousSibling != null
                        && previousSibling.NodeType == NodeType.TEXT_NODE)
                {
                    IDomText lastAsText = (IDomText)previousSibling;
                    lastAsText.NodeValue += text;
                    return;
                }
                parent.InsertBefore(Document.CreateTextNode(text), table);
                return;
            }
            // fall through
            
            IDomText lastChild = stackParent.LastChild as IDomText;
            if (lastChild != null)
            {
                lastChild.NodeValue += text;
                return;
            }
            else
            {
                stackParent.AppendChildUnsafe(Document.CreateTextNode(text));
            }
        }

        /// <summary>
        /// Inserts a foster parented child.
        /// </summary>
        ///
        /// <param name="child">
        /// The child.
        /// </param>
        /// <param name="table">
        /// The table.
        /// </param>
        /// <param name="stackParent">
        /// The stack parent.
        /// </param>

        override protected void InsertFosterParentedChild(DomObject child, DomObject table, DomObject stackParent)
        {
            IDomObject parent = table.ParentNode;
            if (parent != null)
            { 
                // always an element if not null
                parent.InsertBefore(child, table);
            }
            else
            {
                stackParent.AppendChildUnsafe(child);
            }
        }

        /// <summary>
        /// Detach an element from its parent.
        /// </summary>
        ///
        /// <param name="element">
        /// The element.
        /// </param>

        override protected void DetachFromParent(DomObject element)
        {
            IDomObject parent = element.ParentNode;
            if (parent != null)
            {
                parent.RemoveChild(element);
            }
        }

        /// <summary>
        /// Combine a local name &amp; uri into a single attribute name/.
        /// </summary>
        ///
        /// <param name="localName">
        /// Name of the local.
        /// </param>
        /// <param name="uri">
        /// URI of the document.
        /// </param>
        ///
        /// <returns>
        /// The attribute name.
        /// </returns>

        private string AttributeName(string localName, string uri)
        {
            return String.IsNullOrEmpty(uri) ?
                localName :
                localName += ":" + uri;
        }
    }
}
