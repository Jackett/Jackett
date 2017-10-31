using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery
{
    /// <summary>
    /// An element that can be associated with a form during form submission.
    /// </summary>
    /// <url>
    /// http://www.w3.org/html/wg/drafts/html/master/forms.html#form-associated-element
    /// </url>
    public interface IFormAssociatedElement
    {
        /// <summary>
        /// The form with which to associate the element.
        /// </summary>
        IHTMLFormElement Form { get; }
    }
}