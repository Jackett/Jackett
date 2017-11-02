using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CsQuery.Output
{
    /// <summary>
    /// Interface for HTML encoder/decoder
    /// </summary>

    public interface IHtmlEncoder
    {
        /// <summary>
        /// Encodes text as HTML, writing the processed output to the TextWriter.
        /// </summary>
        ///
        /// <param name="text">
        /// The text to be encoded.
        /// </param>
        /// <param name="output">
        /// The target for the ouput
        /// </param>

        void Encode(string text, TextWriter output);

    }
}
