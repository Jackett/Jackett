using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery
{   
    /// <summary>
    /// An Anchor (A) element.
    /// </summary>
    ///
    /// <url>
    /// http://dev.w3.org/html5/spec/single-page.html#the-a-element
    /// </url>

    public interface IHTMLAnchorElement : IDomElement
    {
        /// <summary>
        /// A name or keyword giving a browsing context for UAs to use when following the hyperlink.
        /// </summary>

        string Target {get;set;}

        /// <summary>
        /// A URL that provides the destination of the hyperlink. If the href attribute is not specified,
        /// the element represents a placeholder hyperlink.
        /// </summary>

        string Href {get;set;}

        /// <summary>
        /// The rel attribute on a and area elements controls what kinds of links the elements create.
        /// The attribue's value must be a set of space-separated tokens. 
        /// </summary>

        RelAnchor Rel { get; set; }

        /// <summary>
        /// A list of tokens that specify the relationship between the document containing the hyperlink
        /// and the destination indicated by the hyperlink.
        /// </summary>

        string Hreflang {get;set;}

        /// <summary>
        /// The media for which the destination of the hyperlink was designed.
        /// </summary>

        string Media {get;set;}

    }

    
}
