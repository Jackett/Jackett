using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery
{   
    /// <summary>
    /// An PROGRESS element
    /// </summary>
    ///
    /// <url>
    /// http://dev.w3.org/html5/spec/the-meter-element.html#the-meter-element
    /// </url>

    public interface IHTMLMeterElement: IDomElement
    {
        /// <summary>
        /// The current value
        /// </summary>

        new int Value {get;set;}

        /// <summary>
        /// The maximum value
        /// </summary>

        double Min{ get; set; }

        /// <summary>
        /// The maximum value
        /// </summary>

        double Max {get;set;}

        /// <summary>
        /// The low value
        /// </summary>

        double Low { get; set; }

        /// <summary>
        /// The high value
        /// </summary>

        double High { get; set; }

        /// <summary>
        /// The optimum value
        /// </summary>

        double Optimum { get; set; }

        /// <summary>
        ///  A NodeList of all LABEL elements within this Progress element
        /// </summary>

        INodeList<IDomElement> Labels {get;}
    }
}
