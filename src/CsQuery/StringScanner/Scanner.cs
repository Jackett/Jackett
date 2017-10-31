using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.StringScanner
{
    /// <summary>
    /// Factory for StringScanner objects
    /// </summary>

    public static class Scanner
    {
        /// <summary>
        /// Creates a new StringScanner from a string.
        /// </summary>
        ///
        /// <param name="text">
        /// The text.
        /// </param>
        ///
        /// <returns>
        /// A new StringScsanner.
        /// </returns>

        public static IStringScanner Create(string text)
        {
            return new Implementation.StringScannerEngine(text);
        }
    }
}
