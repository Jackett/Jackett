using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery
{
    /// <summary>
    /// Flags specifying how the document should be rendered
    /// </summary>

    [Flags]
    public enum DomRenderingOptions
    {
        /// <summary>
        /// No option flags. This is not the same as "default", but rather explicitly uses "false" values for all flags.
        /// </summary>
        None=0,

        /// <summary>
        /// Render with default options as determined by CsQuery.Config.DomRenderingOptions
        /// </summary>
        Default = 32,

        /// <summary>
        /// This option only appies to the old HTML parser. It is obsolete, has no effect, and will be
        /// removed in a future version of CsQuery.
        /// </summary>
        
        [Obsolete]
        RemoveMismatchedCloseTags = 1,
        
        /// <summary>
        /// Remove comments from the output
        /// </summary>
        
        RemoveComments = 2,
        
        /// <summary>
        /// Add quotes around each attribute value, whether or not they are needed. The alternative is to only 
        /// use quotes when they are necesssary to delimit the value (e.g. because it includes spaces or other quote characters)
        /// </summary>
        
        QuoteAllAttributes = 4



    }
 
}
