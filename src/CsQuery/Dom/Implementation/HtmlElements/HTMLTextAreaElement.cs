using System.IO;
using CsQuery.HtmlParser;

namespace CsQuery.Implementation
{
    /// <summary>
    /// An HTML text area element.
    /// </summary>

    public class HTMLTextAreaElement : FormSubmittableElement, IHTMLTextAreaElement
    {
        /// <summary>
        /// Default constructor.
        /// </summary>

        public HTMLTextAreaElement(): base(HtmlData.tagTEXTAREA)
        {

        }

        /// <summary>
        /// The value of the HTMLRawInnerTextElementBase's contents
        /// </summary>

        public override string Value
        {
            get
            {
                var formatter = new Output.FormatDefault();
                StringWriter sw = new StringWriter();

                formatter.RenderChildren(this, sw);
                return sw.ToString();
            }
            set
            {
                ChildNodes.Clear();
                ChildNodes.Add(Document.CreateTextNode(value));
            }
        }


        /// <summary>
        /// The string "textarea", per the HTML5 spec.
        /// </summary>
        /// 
        /// <url>
        /// http://www.w3.org/html/wg/drafts/html/master/forms.html#dom-textarea-type
        /// </url>
        public override string Type
        {
            get { return "textarea"; }
        }


        /// <summary>
        /// For HTMLRawInnerTextElementBase elements, InnerText doesn't actually do anything, whereas Value is the InnerText.
        /// </summary>

        public new string InnerText
        {
            get
            {
                return "";
            }
            set
            {
                return;
            }
        }

    }
}
