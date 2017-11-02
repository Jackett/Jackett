using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Implementation
{
    /// <summary>
    /// A CDATA node
    /// </summary>

    public class DomCData : DomObject<DomCData>, IDomCData
    {

        /// <summary>
        /// Default constructor.
        /// </summary>

        public DomCData()
            : base()
        {
            _NonAttributeData = "";
        }

        /// <summary>
        /// Constructor that populates the node with the passed value.
        /// </summary>
        ///
        /// <param name="value">
        /// The contents of the CDATA node
        /// </param>

        public DomCData(string value)
            : base()
        {
            NodeValue = value;
        }

        private string _NonAttributeData;

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
        /// Gets the type of the node. For CDATA nodes, this is NodeType.CDATA_SECTION_NODE.
        /// </summary>

        public override NodeType NodeType
        {
            get { return NodeType.CDATA_SECTION_NODE; }
        }


        #region IDomSpecialElement Members

        /// <summary>
        /// Gets or sets the non-attribute data in the tag. For CDATA nodes, this is the same as the
        /// content of the node..
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
        /// Gets a value indicating whether HTML is allowed as a child of this element. For CDATA nodes,
        /// this is always false.
        /// </summary>

        public override bool InnerHtmlAllowed
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether this object has children. For CDATA nodes, this is always
        /// false.
        /// </summary>

        public override bool HasChildren
        {
            get { return false; }
        }

        /// <summary>
        /// Gets or sets the text of the CDATA element.
        /// </summary>

        public string Text
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

        public override DomCData Clone()
        {
            DomCData clone = new DomCData();
            clone.NonAttributeData = NonAttributeData;
            return clone;
        }

        IDomNode IDomNode.Clone()
        {
            return Clone();
        }
        
        #endregion

      
    }
    
}
