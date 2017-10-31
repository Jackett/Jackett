using CsQuery.HtmlParser;

namespace CsQuery.Implementation
{
    /// <summary>
    /// An HTML button element.
    /// </summary>

    public class HTMLButtonElement : FormSubmittableElement, IHTMLButtonElement
    {
        /// <summary>
        /// Default constructor.
        /// </summary>

        public HTMLButtonElement()
            : base(HtmlData.tagBUTTON)
        {
        }

        /// <summary>
        /// The value of the "type" attribute. For button elements, this property always returns a
        /// lowercase value and defaults to "submit" if there is no type attribute.
        /// </summary>
        ///
        /// <value>
        /// The type.
        /// </value>

        public override string Type
        {
            get
            {
                return GetAttribute(HtmlData.attrTYPE, "submit").ToLower();
            }
            set
            {
                base.Type = value;
            }
        }
    }
}
