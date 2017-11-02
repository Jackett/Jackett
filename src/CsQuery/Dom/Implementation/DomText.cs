using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CsQuery.HtmlParser;
using CsQuery.Output;

namespace CsQuery.Implementation
{
    /// <summary>
    /// Used for literal text (not part of a tag)
    /// </summary>
    public class DomText : DomObject<DomText>, IDomText
    {
        /// <summary>
        /// Create a new empty Text node
        /// </summary>

        public DomText()
            : base()
        {
            
        }

        /// <summary>
        /// Create a new Text node containing the text passed
        /// </summary>
        ///
        /// <param name="nodeValue">
        /// The text value of this Text node.
        /// </param>

        public DomText(string nodeValue)
            : base()
        {
            NodeValue = nodeValue;
        }


        /// <summary>
        /// The inner node value; the text.
        /// </summary>

        protected string _NodeValue;
       

        /// <summary>
        /// The node (tag) name, in upper case. For Text nodes, this is always "#text".
        /// </summary>

        public override string NodeName
        {
            get
            {
                return "#text";
            }
        }

        /// <summary>
        /// Gets the type of the node. For Text nodes, this is always NodeType.TEXT_NODE
        /// </summary>

        public override NodeType NodeType
        {
            get { return NodeType.TEXT_NODE; }
        }



        /// <summary>
        /// Gets or sets the text value of this Text node. Null values will be converted to an empty string.
        /// </summary>

        public override string NodeValue
        {
            get
            {
                return _NodeValue ?? "";
            }
            set
            {
                _NodeValue=value;
            }
        }

        /// <summary>
        /// Makes a clone of this TextNode
        /// </summary>
        ///
        /// <returns>
        /// A copy of this object.
        /// </returns>

        public override DomText Clone()
        {
            return new DomText(NodeValue);
        }

        /// <summary>
        /// Gets a value indicating whether HTML is allowed as a child of this element. For Text nodes,
        /// this is always false.
        /// </summary>

        public override bool InnerHtmlAllowed
        {
            get { return false; }
        }

        /// <summary>
        /// For Text nodes, this is always false
        /// </summary>

        public override bool HasChildren
        {
            get { return false; }
        }

        /// <summary>
        /// Return the value of this text node
        /// </summary>
        ///
        /// <returns>
        /// This object as a string.
        /// </returns>

        public override string ToString()
        {
            return NodeValue;
        }

    }
}
