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

    public class HTMLMeterElement : DomElement, IHTMLMeterElement
    {
        /// <summary>
        /// Default constructor.
        /// </summary>

        public HTMLMeterElement()
            : base(HtmlData.tagMETER)
        {
        }

        /// <summary>
        /// The value of the meter
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
        /// The maximum value.
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

        /// <summary>
        /// The minimum value.
        /// </summary>

        public double Min
        {
            get
            {
                return Support.DoubleOrZero(GetAttribute("min"));
            }
            set
            {
                SetAttribute("min", value.ToString());
            }
        }
        /// <summary>
        /// The low value.
        /// </summary>

        public double Low
        {
            get
            {
                return Support.DoubleOrZero(GetAttribute("low"));
            }
            set
            {
                SetAttribute("low", value.ToString());
            }
        }

        /// <summary>
        /// The high value.
        /// </summary>

        public double High
        {
            get
            {
                return Support.DoubleOrZero(GetAttribute("high"));
            }
            set
            {
                SetAttribute("high", value.ToString());
            }
        }

        /// <summary>
        /// The optimum value.
        /// </summary>

        public double Optimum
        {
            get
            {
                return Support.DoubleOrZero(GetAttribute("optimum"));
            }
            set
            {
                SetAttribute("optimum", value.ToString());
            }
        }
       

        /// <summary>
        /// A NodeList of all LABEL elements within this Progress element
        /// </summary>

        public INodeList<IDomElement> Labels
        {
            get {
                return new NodeList<IDomElement>(ChildElementsOfTag<IDomElement>(HtmlData.tagLABEL));
            }
        }


     
    }
}
