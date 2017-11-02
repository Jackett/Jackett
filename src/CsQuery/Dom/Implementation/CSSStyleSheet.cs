using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Implementation
{
    /// <summary>
    /// A CSS style sheet.
    /// </summary>
    ///
    /// <url>
    /// http://www.w3.org/TR/DOM-Level-2-Style/css.html#CSS-CSSStyleSheet
    /// </url>

    public class CSSStyleSheet: ICSSStyleSheet
    {
        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        ///
        /// <param name="ownerNode">
        /// The node that owns this item.
        /// </param>

        public CSSStyleSheet(IDomElement ownerNode)
        {
            OwnerNode = ownerNode;
        }

        #endregion

        #region private properties

        private IList<ICSSRule> _Rules;

        #endregion

        #region public properties

        /// <summary>
        /// Indicates whether the style sheet is applied to the document.
        /// </summary>

        public bool Disabled
        {
            get;set;
        }

        /// <summary>
        /// If the style sheet is a linked style sheet, the value of its attribute is its location. For
        /// inline style sheets, the value of this attribute is null.
        /// </summary>

        public string Href
        {
            get
            {
                return OwnerNode == null ? 
                    null : 
                    OwnerNode["href"];
            }
            set
            {
                if (OwnerNode == null)
                {
                    throw new InvalidOperationException("This CSSStyleSheet is not bound to an element node.");
                }
                OwnerNode["href"] = value;
            }
        }

        /// <summary>
        /// The node that associates this style sheet with the document. For HTML, this may be the
        /// corresponding LINK or STYLE element.
        /// </summary>
        ///
        /// <value>
        /// The owner node.
        /// </value>

        public IDomElement OwnerNode
        {
            get;
            protected set;
        }

        /// <summary>
        /// This specifies the style sheet language for this style sheet. This will always be "text/css".
        /// </summary>
        ///
        /// <value>
        /// The type.
        /// </value>

        public string Type
        {
            get { return "text/css"; }
        }

        /// <summary>
        /// Gets the CSS rules for this style sheet.
        /// </summary>
        ///
        /// <value>
        /// The CSS rules.
        /// </value>

        public IList<ICSSRule> CssRules
        {
            get {
                if (_Rules == null)
                {
                    _Rules = new List<ICSSRule>();
                }
                return _Rules;
            }
        }

        #endregion
    }
}
