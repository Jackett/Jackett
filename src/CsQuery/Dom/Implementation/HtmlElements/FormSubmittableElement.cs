using System.Linq;
using CsQuery.HtmlParser;

namespace CsQuery.Implementation
{
    /// <summary>
    /// An implementation of properties shared by all form submittable elements.
    /// </summary>
    /// 
    /// <url>
    /// http://www.w3.org/html/wg/drafts/html/master/forms.html#category-submit
    /// </url>
    public class FormSubmittableElement : FormReassociateableElement, IFormSubmittableElement
    {
        /// <summary>
        /// Constructor to specify the element's token ID.
        /// </summary>
        /// <param name="tokenId">The token ID of the element.</param>
        protected FormSubmittableElement(ushort tokenId)
            : base(tokenId)
        {
        }

        /// <summary>
        /// A form submittable element is disabled if it has the disabled attribute,
        /// or it is in a disabled fieldset and not in the legend.
        /// </summary>
        /// 
        /// <url>
        /// http://www.w3.org/html/wg/drafts/html/master/forms.html#attr-fe-disabled
        /// </url>
        public override bool Disabled
        {
            get
            {
                if (base.Disabled)
                {
                    return true;
                }

                IDomContainer fieldset = GetAncestors().FirstOrDefault(c => c.NodeName == "FIELDSET");
                if (fieldset == null || !fieldset.Disabled)
                {
                    return false;
                }

                IDomElement firstLegend = fieldset.GetDescendentElements().FirstOrDefault(o => o.NodeName == "LEGEND");
                return firstLegend == null || !GetAncestors().Contains(firstLegend);
            }
            set
            {
                SetProp(HtmlData.attrDISABLED, value);
            }
        }
    }
}
