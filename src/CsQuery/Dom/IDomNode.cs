using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CsQuery.Output;

namespace CsQuery
{
    /// <summary>
    /// Interface for a node. This is the most generic construct in the CsQuery DOM.
    /// </summary>

    public interface IDomNode
    {
        /// <summary>
        /// Gets the type of the node.
        /// </summary>

        NodeType NodeType { get; }

        /// <summary>
        /// The node (tag) name, in upper case.
        /// </summary>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/Node.nodeName
        /// </url>

        string NodeName { get; }

        /// <summary>
        /// Gets or sets the value of this node.
        /// </summary>
        ///
        /// <remarks>
        /// For the document itself, nodeValue returns null. For text, comment, and CDATA nodes,
        /// nodeValue returns the content of the node.
        /// </remarks>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/Node.nodeValue
        /// </url>

        string NodeValue { get; set; }

        /// <summary>
        /// Gets a value indicating whether this object has any children. For node types that cannot have
        /// children, it will always return false. To determine if a node is allowed to have children,
        /// use the ChildrenAllowed property.
        /// </summary>
        ///
        /// <seealso cref="CsQuery.IDomObject.ChildrenAllowed"/>

        bool HasChildren { get; }

        /// <summary>
        /// Gets zero-based index of this object relative to its siblings including all node types.
        /// </summary>

        int Index { get; }

        /// <summary>
        /// Return an INodeList of the direct children of this node.
        /// </summary>

        INodeList ChildNodes { get; }

        /// <summary>
        /// Return a sequence containing only the element children of this node (e.g. no text, cdata, comments)
        /// </summary>

        IEnumerable<IDomElement> ChildElements { get; }

        /// <summary>
        /// Renders the complete HTML for this element, including its children.
        /// </summary>
        ///
        /// <returns>
        /// a string of HTML.
        /// </returns>

        string Render();

        /// <summary>
        /// Renders the complete HTML for this element, including its children.
        /// </summary>
        ///
        /// <returns>
        /// a string of HTML
        /// </returns>

        string Render(DomRenderingOptions options);


        /// <summary>
        /// Renders the complete HTML for this element, including its children, using the OutputFormatter.
        /// </summary>
        ///
        /// <returns>
        /// a string of HTML
        /// </returns>

        string Render(IOutputFormatter formatter);


        /// <summary>
        /// Renders the complete HTML for this element, including its children, using the OutputFormatter.
        /// </summary>
        ///
        /// <returns>
        /// a string of HTML
        /// </returns>

        void Render(IOutputFormatter formatter, TextWriter writer);

        /// <summary>
        /// Renders the complete HTML for this element to a StringBuilder. Note: This is obsolete; use Render(IOutputFormatter)
        /// </summary>
        ///
        /// <param name="sb">
        /// An existing StringBuilder instance to append this element's HTML.
        /// </param>

        [Obsolete]
        void Render(StringBuilder sb);

        /// <summary>
        /// Renders the complete HTML for this element, including its children, using the OutputFormatter.
        /// </summary>
        ///
        /// <param name="sb">
        /// An existing StringBuilder instance to append this element's HTML.
        /// </param>
        /// <param name="options">
        /// (optional) options for controlling the operation.
        /// </param>

        [Obsolete]
        void Render(StringBuilder sb, DomRenderingOptions options=DomRenderingOptions.Default);

        /// <summary>
        /// Removes this object from it's parent, and consequently the Document, if any, to which it belongs.
        /// </summary>

        void Remove();

        /// <summary>
        /// Gets a value indicating whether this node should be is indexed. Generally, this is true for IDomElement
        /// nodes that are within an IDomDocument and false otherwise.
        /// </summary>

        bool IsIndexed { get; }

        /// <summary>
        /// Gets a value indicating whether this object belongs to a Document or not.
        /// </summary>
        ///
        /// <remarks>
        /// Disconnected elements are not bound to a DomDocument object. This could be because
        /// they were instantiated outside a document context, or were removed as a result of
        /// an operation such as ReplaceWith.
        /// </remarks>

        bool IsDisconnected { get; }

        /// <summary>
        /// Gets a value indicating whether this object belongs is a fragmment and is bound to an 
        /// IDomFragment object.
        /// </summary>

        bool IsFragment { get; }

        /// <summary>
        /// Makes a deep copy of this object.
        /// </summary>
        ///
        /// <returns>
        /// A copy of this object.
        /// </returns>

        IDomNode Clone();
    }

}
