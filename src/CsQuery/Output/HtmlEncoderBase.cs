using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CsQuery.StringScanner;

namespace CsQuery.Output
{
    /// <summary>
    /// Abstract base class for custom HTML encoder implementations
    /// </summary>

    public abstract class HtmlEncoderBase: IHtmlEncoder
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

        protected abstract bool TryEncode(char c, out string encoded);

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

        protected abstract bool TryEncodeAstralPlane(int c, out string encoded);

        /// <summary>
        /// Encodes text as HTML, writing the processed output to the TextWriter.
        /// </summary>
        ///
        /// <param name="html">
        /// The text to be encoded.
        /// </param>
        /// <param name="output">
        /// The target for the ouput.
        /// </param>

        public virtual void Encode(string html, TextWriter output)
        {
            StringBuilder sb = new StringBuilder();
            int pos = 0,
                len = html.Length;

            while (pos < len)
            {
                char c = html[pos++];
                string encoded;
                if ((c & 0xF800) == 0xD800)
                {

                    // http://www.russellcottrell.com/greek/utilities/SurrogatePairCalculator.htm
                    // algo. to convert a surrogate pair to an int
                    char c2 = html[pos++];

                    int val = ((c - 0xD800) * 0x400) + (c2 - 0xDC00) + 0x10000;
                    if (TryEncodeAstralPlane(val, out encoded))
                    {
                        output.Write(encoded);
                    }
                    else
                    {
                        output.Write(c);
                        output.Write(c2);
                    }
                }
                else
                {
                    if (TryEncode(c, out encoded))
                    {
                        output.Write(encoded);
                    }
                    else
                    {
                        output.Write(c);
                    }
                }
            }
        }
    }
}
