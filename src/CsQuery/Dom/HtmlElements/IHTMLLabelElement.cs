using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery
{   
    /// <summary>
    /// A LABEL element.
    /// </summary>
    ///
    /// <url>
    /// http://dev.w3.org/html5/spec/single-page.html#the-label-element
    /// </url>

    public interface IHTMLLabelElement : IDomElement, IFormReassociateableElement
    {
        /// <summary>
        /// Gets or sets the for attribute
        /// </summary>

        string HtmlFor {get;set;}

        /// <summary>
        /// The control bound to this label
        /// </summary>

        IDomElement Control { get; }
    }
}
