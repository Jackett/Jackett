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

    public class HtmlEncoderNone: IHtmlEncoder
    {
        /// <summary>
        /// Encodes text as HTML, writing the processed output to the TextWriter.
        /// </summary>
        ///
        /// <param name="text">
        /// The text to be encoded.
        /// </param>
        /// <param name="output">
        /// The target for the ouput.
        /// </param>

        public void Encode(string text, TextWriter output)
        {
            output.Write(text);
        }
       
    }
}
