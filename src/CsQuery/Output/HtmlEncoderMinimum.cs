using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CsQuery.Output
{
    /// <summary>
    /// Minimum HTML encoder. This only parses the absolute minimum required for correct
    /// interpretation (less-than, greater-than, ampersand). Everthing else is passed through.
    /// </summary>

    public class HtmlEncoderMinimum: HtmlEncoderBase
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
                case '<':
                    encoded = "&lt;";
                    return true;
                case '>':
                    encoded = "&gt;";
                    return true;
                case '&':
                    encoded = "&amp;";
                    return true; ;
                default:
                    encoded = null;
                    return false;
            }
        }

        /// <summary>
        /// Overrides default astral plane encoding, causing unicode characters to never be HTML encoded.
        /// </summary>
        ///
        /// <param name="c">
        /// The text string to encode.
        /// </param>
        /// <param name="encoded">
        /// [out] Null always (never encodes)
        /// </param>
        ///
        /// <returns>
        /// False always (never encodes)
        /// </returns>

        protected override bool TryEncodeAstralPlane(int c, out string encoded)
        {
            encoded = null;
            return false;
        }
    }
}
