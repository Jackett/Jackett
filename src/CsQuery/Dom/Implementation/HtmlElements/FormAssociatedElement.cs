using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.HtmlParser;
using CsQuery.Implementation;

namespace CsQuery.Implementation
{
    /// <summary>
    /// An implementation of properties shared by all form associated elements.
    /// </summary>
    /// 
    /// <url>
    /// http://www.w3.org/html/wg/drafts/html/master/forms.html#form-associated-element
    /// </url>
    public abstract class FormAssociatedElement : DomElement, IFormAssociatedElement
    {
        /// <summary>
        /// Constructor to specify the element's token ID.
        /// </summary>
        /// <param name="tokenId">The token ID of the element.</param>
        protected FormAssociatedElement(ushort tokenId)
            : base(tokenId)
        {
        }

        /// <summary>
        /// The value of form element with which to associate the element.
        /// </summary>
        ///
        /// <remarks>
        /// The HTML5 spec says "The value of the id attribute on the form with which to associate the
        /// element." This is not what browsers currently return; they return the actual element. We'll
        /// keep that for now.
        /// </remarks>

        public IHTMLFormElement Form
        {
            get
            {
                return Closest(HtmlData.tagFORM) as IHTMLFormElement;
            }
        }
    }
}
