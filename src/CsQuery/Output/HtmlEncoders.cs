using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.Output;

namespace CsQuery
{
    /// <summary>
    /// Factory for HTML encoders included with CsQuery
    /// </summary>

    public static class HtmlEncoders
    {
        /// <summary>
        /// The default HTML encoder
        /// </summary>

        public static IHtmlEncoder Default
        {
            get
            {
                return Config.HtmlEncoder;
            }
        }

        /// <summary>
        /// The standard HTML encoder; encodes most entities, and any characters that are above ascii 160.
        /// </summary>

        public static IHtmlEncoder Basic = new HtmlEncoderBasic();


        /// <summary>
        /// The minimum HTML encoder; only encodes left-caret, right-caret, and ampersand. All other
        /// characters are passed through.
        /// </summary>

        public static IHtmlEncoder Minimum = new HtmlEncoderMinimum();

        /// <summary>
        /// The same as the minimum HTML encoder, but also encodes nonbreaking space (ascii 160 becomes
        /// &amp;nbsp;).
        /// </summary>

        public static IHtmlEncoder MinimumNbsp = new HtmlEncoderMinimumNbsp();

        /// <summary>
        /// No HTML encoding -- all characters are passed through. Will likely produce invalid HTML.
        /// </summary>

        public static IHtmlEncoder None = new HtmlEncoderNone();

        /// <summary>
        /// Full HTML encoding -- all entities mapped to their named (not numeric) entities when
        /// available.
        /// </summary>

        public static IHtmlEncoder Full = new HtmlEncoderFull();
    }
}
