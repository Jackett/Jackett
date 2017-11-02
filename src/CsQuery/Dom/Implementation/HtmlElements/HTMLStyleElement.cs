using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.HtmlParser;

namespace CsQuery.Implementation
{
    /// <summary>
    /// A STYLE element
    /// </summary>

    public class HTMLStyleElement : DomElement
    {
        /// <summary>
        /// Default constructor
        /// </summary>


        public HTMLStyleElement()
                : base(HtmlData.tagSTYLE)
            {

            }
        

    }
}
