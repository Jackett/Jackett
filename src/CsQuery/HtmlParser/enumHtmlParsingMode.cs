using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery
{
    /// <summary>
    /// The methodology applied when parsing strings of HTML
    /// </summary>

    public enum HtmlParsingMode : byte
    {
        
        /// <summary>
        /// Automatically detect the document type. When no DocType node is provided, will default to FragmentWithSelfClosingTags.
        /// </summary>
        
        Auto = 0,

        /// <summary>
        /// A fragment whose context is determined by the first tag.
        /// </summary>
        
        Fragment = 1,
        
        /// <summary>
        /// A content block, assumed to be in BODY context.
        /// </summary>
        
        Content = 2,
        
        /// <summary>
        /// A complete document; the HTML and BODY tag constructs will be addded if missing..
        /// </summary>
        
        Document = 3
    }
}
