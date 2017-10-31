using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.HtmlParser;

namespace CsQuery.Implementation
{
    /// <summary>
    /// A SCRIPT
    /// </summary>

    public class HTMLScriptElement : DomElement
    {
        /// <summary>
        /// Default constructor
        /// </summary>


            public HTMLScriptElement()
                : base(HtmlData.tagSCRIPT)
            {

            }
        

    }
}
