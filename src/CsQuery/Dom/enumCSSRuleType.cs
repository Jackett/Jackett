using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery
{
    /// <summary>
    /// Values that represent CSSRuleType.
    /// </summary>

    public enum CSSRuleType
    {
        /// <summary>
        /// An unknown rule.
        /// </summary>
        UNKNOWN_RULE =0,
        /// <summary>
        /// A CSS Style rule.
        /// </summary>
        STYLE_RULE=1,
        /// <summary>
        /// A character set rule.
        /// </summary>
        CHARSET_RULE=2,
        /// <summary>
        /// An import rule.
        /// </summary>
        IMPORT_RULE=3,
        /// <summary>
        /// A media rule.
        /// </summary>
        MEDIA_RULE=4,
        /// <summary>
        /// A font face rule.
        /// </summary>
        FONT_FACE_RULE=5,
        /// <summary>
        /// A page rule.
        /// </summary>
        PAGE_RULE=6
    }
}
