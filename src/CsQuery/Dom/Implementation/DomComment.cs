using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Implementation
{
    /// <summary>
    /// A comment node
    /// </summary>

    public class DomComment : DomObject<DomComment>, IDomComment
    {
        #region constructors

        /// <summary>
        /// Default constructor.
        /// </summary>

        public DomComment()
            : base()
        {
            _NonAttributeData = "";
        }

        /// <summary>
        /// Constructor for a comment containing the specified text.
        /// </summary>
        ///
        /// <param name="text">
        /// The text.
        /// </param>

        public DomComment(string text): base()
        {
            NodeValue = text;
        }


        #endregion

        #region private properties

        private string _NonAttributeData;

        #endregion

        #region public properties

        /// <summary>
        /// Gets the type of the node (COMMENT_NODE)
        /// </summary>
        ///
        /// <value>
        /// The type of the node.
        /// </value>

        public override NodeType NodeType
        {
            get { return NodeType.COMMENT_NODE; }
        }

        /// <summary>
        /// The node (tag) name, in upper case. For a 
        /// </summary>
        ///
        /// <value>
        /// The name of the node.
        /// </value>

        public override string NodeName
        {
            get
            {
                return "#comment";
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this object is quoted.
        /// </summary>
        ///
        /// <remarks>
        /// TODO: Remove this. This has to do with GetTagOpener etc.
        /// </remarks>
        ///
        /// <value>
        /// true if this object is quoted, false if not.
        /// </value>

        public bool IsQuoted { get; set; }


        /// <summary>
        /// Gets a value indicating whether HTML is allowed as a child of this element (false)
        /// </summary>
        ///
        /// <value>
        /// true if inner HTML allowed, false if not.
        /// </value>

        public override bool InnerHtmlAllowed
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether this object has children (false)
        /// </summary>
        ///
        /// <value>
        /// true if this object has children, false if not.
        /// </value>

        public override bool HasChildren
        {
            get { return false; }
        }

        #endregion

        #region IDomSpecialElement Members

        /// <summary>
        /// Gets or sets the non-attribute data in the tag. For comments, this is the same as the text of
        /// the comment. Null values will be converted to an empty string.
        /// </summary>

        public string NonAttributeData
        {
            get
            {
                return _NonAttributeData;
            }
            set
            {
                _NonAttributeData = value ?? "";
            }
        }


        /// <summary>
        /// Gets or sets the node value. For CDATA nodes, this is the content.
        /// </summary>

        public override string NodeValue
        {
            get
            {
                return NonAttributeData;
            }
            set
            {
                NonAttributeData = value;
            }
        }

        /// <summary>
        /// Makes a deep copy of this object.
        /// </summary>
        ///
        /// <returns>
        /// A copy of this object.
        /// </returns>

        public override DomComment Clone()
        {
            DomComment clone = new DomComment();
            clone.NonAttributeData = NonAttributeData;
            clone.IsQuoted = IsQuoted;
            return clone;
        }

        IDomNode IDomNode.Clone()
        {
            return Clone();
        }
        
        #endregion
    }
}
