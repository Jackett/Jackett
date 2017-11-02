using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery
{
    /// <summary>
    /// Values that represent NodeType.
    /// </summary>

    public enum NodeType: byte
    {
        /// <summary>
        /// An element node.
        /// </summary>
        ELEMENT_NODE = 1,
        
        //ATTRIBUTE_NODE =2,
        /// <summary>
        /// A text node.
        /// </summary>
        TEXT_NODE = 3,
        /// <summary>
        /// A CDATA node.
        /// </summary>
        CDATA_SECTION_NODE = 4,
        //ENTITY_REFERENCE_NODE = 5,
        //ENTITY_NODE=  6,
        //PROCESSING_INSTRUCTION_NODE =7,
        /// <summary>
        /// A comment node.
        /// </summary>
        COMMENT_NODE = 8,
        /// <summary>
        /// A document node.
        /// </summary>
        DOCUMENT_NODE = 9,
        /// <summary>
        /// The DOCTYPE node.
        /// </summary>
        DOCUMENT_TYPE_NODE = 10,
        /// <summary>
        /// A document fragment node.
        /// </summary>
        DOCUMENT_FRAGMENT_NODE = 11,
        //NOTATION_NODE  =12
    }
}
