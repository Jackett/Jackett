using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.Output;

namespace CsQuery
{   
    /// <summary>
    /// A regular DOM element
    /// </summary>
    
    public interface IDomElement : IDomContainer, IDomIndexedNode
    {
        /// <summary>
        /// The element is a block element.
        /// </summary>

        bool IsBlock { get; }

        /// <summary>
        /// Returns the HTML for this element, but ignoring children/innerHTML.
        /// </summary>
        ///
        /// <returns>
        /// A string of HTML.
        /// </returns>
        
        string ElementHtml();

        /// <summary>
        /// Get this element's index only among other elements (e.g. excluding text &amp; other non-
        /// element node types)
        /// </summary>

        int ElementIndex { get; }




    }
}
