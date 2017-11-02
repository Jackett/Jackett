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
    /// http://dev.w3.org/html5/markup/progress.html
    /// </url>

    public interface IHTMLProgressElement: IDomElement
    {
        /// <summary>
        /// The current value
        /// </summary>

        new int Value {get;set;}

        /// <summary>
        /// The maximum value
        /// </summary>

        double Max {get;set;}

        /// <summary>
        /// If the progress bar is an indeterminate progress bar, then the position IDL attribute must
        /// return −1. Otherwise, it must return the result of dividing the current value by the maximum
        /// value.
        /// </summary>

        double Position {get;}

        /// <summary>
        ///  A NodeList of all LABEL elements within this Progress element
        /// </summary>

        INodeList<IHTMLLabelElement> Labels {get;}
    }
}
