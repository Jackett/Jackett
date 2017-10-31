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
    /// http://www.w3.org/html/wg/drafts/html/master/forms.html#category-submit
    /// </url>
    public interface IFormSubmittableElement : IFormAssociatedElement
    {
    }
}