using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery
{   
    /// <summary>
    /// An LI element.
    /// </summary>
    ///
    /// <url>
    /// http://dev.w3.org/html5/spec/single-page.html#the-li-element
    /// </url>

    public interface IHTMLLIElement : IDomElement
    {
        /// <summary>
        /// A valid integer giving the ordinal value of the list item.
        /// </summary>

        new int Value { get; set; }
    }
}
