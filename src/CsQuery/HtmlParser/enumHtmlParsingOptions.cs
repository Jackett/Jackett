using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery
{
    /// <summary>
    /// The options used when parsing strings of HTML
    /// </summary>

    [Flags]
    public enum HtmlParsingOptions : byte
    {
        /// <summary>
        /// No options applied.
        /// </summary>
        
        None=0,

        /// <summary>
        /// Default options (from Config.HtmlParsingOptions) are applied.
        /// </summary>
        
        Default=1,

       /// <summary>
       /// Tags may be self-closing.
       /// </summary>

        AllowSelfClosingTags=2,


        /// <summary>
        /// Comments are ignored entirely.
        /// </summary>
        
        IgnoreComments=4
    }
}
