using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Engine
{
    /// <summary>
    /// Enumerator of possible quoting rules that determine how parameters for CSS selector functions
    /// should be parsed.
    /// </summary>

    public enum QuotingRule
    {
        /// <summary>
        /// The parameter value should never be quoted (e.g. is numeric data, or the function simply doesn't expect quotes).
        /// </summary>
        NeverQuoted = 1,
        /// <summary>
        /// The parameter value should always be quoted.
        /// </summary>
        AlwaysQuoted = 2,
        /// <summary>
        /// The parameter value may be quoted: if the first character is a double- or single-quote, then a matching quote terminates the parameter value..
        /// </summary>
        OptionallyQuoted = 3
    }
}
