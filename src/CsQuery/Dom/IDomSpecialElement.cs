using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery
{
    /// <summary>
    /// Interface for an IDomSpecialElement; and element whose data is contained as non-structured
    /// data in the tag itself.
    /// </summary>

    public interface IDomSpecialElement : IDomObject
    {
        /// <summary>
        /// Gets or sets the non-structured data in the tag
        /// </summary>

        string NonAttributeData { get; set; }

    }
}
