using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CsQuery.Output
{
    /// <summary>
    /// Standard HTML encoder. This parses less-than, greater-than, ampersand, double-qoute, and non-
    /// breaking space into HTML entities, plus all characters above ascii 160 into ther HTML numeric-
    /// coded equivalent.
    /// </summary>

    public class HtmlEncoderBasic: HtmlEncoderBase
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
                case '"':
                    encoded = "&quot;";
                    return true; ;
                case '&':
                    encoded = "&amp;";
                    return true; ;
                case (char)160:
                    encoded = "&nbsp;";
                    return true; ;
                default:
                    if (c > 160)
                    {
                        // decimal numeric entity
                        encoded = EncodeNumeric(c);
                        return true;
                    }
                    else
                    {
                        encoded = null;
                        return false;
                    }
            }
        }

        /// <summary>
        /// Determines of a character must be encoded (for unicode chars using astral planes); if so,
        /// encodes it as the output parameter and returns true; if not, returns false. This method will
        /// be passed the integral representation of the mult-byte unicode character. If the method
        /// returns false, then the character will be output as the orginal two-byte sequence.
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

        protected override bool TryEncodeAstralPlane(int c, out string encoded)
        {
            encoded = EncodeNumeric(c);
            return true;
        }

        /// <summary>
        /// Encodes an integer as an HTML numeric coded entity e.g. &amp;#nnn;
        /// </summary>
        ///
        /// <param name="value">
        /// The value.
        /// </param>
        ///
        /// <returns>
        /// An HTML string.
        /// </returns>

        protected string EncodeNumeric(int value)
        {
            return "&#" + (value).ToString(System.Globalization.CultureInfo.InvariantCulture) + ";";
        }
    }
}
