using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery
{
    /// <summary>
    /// Values that represent the HTML document type.
    /// </summary>

    public enum DocType: byte
    {
        /// <summary>
        /// Use the default doc type (from CsQuery.Config.DocType).
        /// </summary>
        Default = 0,
        /// <summary>
        /// HTML5
        /// </summary>
        HTML5 = 1,
        /// <summary>
        /// HTML 4 Transitional
        /// </summary>
        HTML4 = 2,
        
        /// <summary>
        /// XHTML Transitional
        /// </summary>
        XHTML = 3,
        /// <summary>
        /// An unsupported document type.
        /// </summary>
        Unknown = 4,
        /// <summary>
        /// HTML 4 Strict
        /// </summary>
        HTML4Strict = 5,

        /// <summary>
        /// XHTML Strict.
        /// </summary>
        XHTMLStrict = 6
    }
}
