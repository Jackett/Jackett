using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.HtmlParser;
using CsQuery.Utility;

namespace CsQuery.Implementation
{
    /// <summary>
    /// An HTML progress element.
    /// </summary>

    public class HTMLProgressElement : DomElement, IHTMLProgressElement
    {
        /// <summary>
        /// Default constructor.
        /// </summary>

        public HTMLProgressElement()
            : base(HtmlData.tagPROGRESS)
        {
        }

        /// <summary>
        /// For Progress elements, returns the value of the "value" attribute, or zero.
        /// </summary>

        public new int Value
        {
            get
            {
                return Support.IntOrZero(GetAttribute(HtmlData.ValueAttrId));
            }
            set
            {
                SetAttribute(HtmlData.ValueAttrId, value.ToString());
            }
        }

        /// <summary>
        /// The maximum value allowed for this Progress bar.
        /// </summary>

        public double Max
        {
            get
            {
                return Support.DoubleOrZero(GetAttribute("max"));
            }
            set
            {
                SetAttribute("max", value.ToString());
            }
        }

        ///  <summary>
        /// If the progress bar is an indeterminate progress bar, then the position IDL attribute must
        /// return −1. Otherwise, it must return the result of dividing the current value by the maximum
        /// value.
        /// </summary>

        public double Position
        {
            get
            {
                if (!HasAttribute("value"))
                {
                    return -1;
                }
                else
                {
                    return Value / Max;

                }
            }
          
        }

        /// <summary>
        /// A NodeList of all LABEL elements within this Progress element
        /// </summary>

        public INodeList<IHTMLLabelElement> Labels
        {
            get {
                return new NodeList<IHTMLLabelElement>(ChildElementsOfTag<IHTMLLabelElement>(HtmlData.tagLABEL));
            }
        }
    }
}
