using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery
{   
    /// <summary>
    /// An HTML INPUT element.
    /// </summary>
    ///
    /// <url>
    /// http://dev.w3.org/html5/markup/input.html
    /// </url>

    public interface IHTMLInputElement : IDomElement, IFormSubmittableElement, IFormReassociateableElement
    {
        /// <summary>
        /// A URL that provides the destination of the hyperlink. If the href attribute is not specified,
        /// the element represents a placeholder hyperlink.
        /// </summary>

        bool Autofocus { get; set; }

        /// <summary>
        /// Specifies that the element is a required part of form submission.
        /// </summary>

        bool Required {get;set;}
    }
}
