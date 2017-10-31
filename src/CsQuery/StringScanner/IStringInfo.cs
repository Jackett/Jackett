using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.StringScanner
{
    /// <summary>
    /// Interface that describes characterstics of a string
    /// </summary>

    public interface IStringInfo : IValueInfo<string>
    {
        /// <summary>
        /// The string is a valid HTML attribute name
        /// </summary>

        bool HtmlAttributeName { get; }

        /// <summary>
        /// The string contains alpha characters.
        /// </summary>

        bool HasAlpha { get; }
    }
}
