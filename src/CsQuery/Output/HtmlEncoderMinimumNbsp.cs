using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CsQuery.Output
{
    /// <summary>
    /// Minimum HTML encoder (including nonbreaking space). This only parses the absolute minimum
    /// required for correct interpretation (less-than, greater-than, ampersand), plus non-breaking
    /// space. Everthing else is passed through.
    /// </summary>

    public class HtmlEncoderMinimumNbsp: HtmlEncoderMinimum
    {
        /// <summary>
        /// Determines of a character must be encoded; if so, encodes it as the output parameter and
        /// returns true; if not, returns false.
        /// </summary>
        ///
        /// <param name="c">
        /// The text string to encode.
        /// </param>
        /// <param name="encoded">
        /// [out] The encoded string.
        /// </param>
        ///
        /// <returns>
        /// True if the character was encoded.
        /// </returns>

        protected override bool TryEncode(char c, out string encoded)
        {
            switch (c)
            {
                case (char)160:
                    encoded = "&nbsp;";
                    return true;
                default:
                    return base.TryEncode(c, out encoded);
            }
        }

    }
}
