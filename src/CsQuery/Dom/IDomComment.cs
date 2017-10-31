using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery
{
    /// <summary>
    /// An interface for HTML Comment elements.
    /// </summary>

    public interface IDomComment : IDomSpecialElement
    {
        /// <summary>
        /// Gets or sets a value indicating whether this object is quoted.
        /// </summary>

        bool IsQuoted { get; set; }
    }
}
