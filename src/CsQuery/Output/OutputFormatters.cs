using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.Output;

namespace CsQuery
{
    /// <summary>
    /// Factory for OuputFormatters included with CsQuery.
    /// </summary>

    public static class OutputFormatters
    {
        /// <summary>
        /// Creates an instance of the default OutputFormatter using the options passed.
        /// </summary>
        ///
        /// <param name="options">
        /// (optional) options for controlling the operation.
        /// </param>
        /// <param name="encoder">
        /// (optional) the encoder.
        /// </param>
        ///
        /// <returns>
        /// An OutputFormatter.
        /// </returns>

        public static IOutputFormatter Create(DomRenderingOptions options, IHtmlEncoder encoder)
        {
            return new FormatDefault(options, encoder ?? HtmlEncoders.Default);
        }

        /// <summary>
        /// Creates an instance of the default OutputFormatter using the options passed and the default encoder.
        /// </summary>
        ///
        /// <param name="options">
        /// (optional) options for controlling the operation.
        /// </param>
        ///
        /// <returns>
        /// An OutputFormatter.
        /// </returns>

        public static IOutputFormatter Create(DomRenderingOptions options)
        {
            return new FormatDefault(options, HtmlEncoders.Default);
        }

        /// <summary>
        /// Creates an instance of the default OutputFormatter using the default options and the encoder
        /// passed.
        /// </summary>
        ///
        /// <param name="encoder">
        /// (optional) the encoder.
        /// </param>
        ///
        /// <returns>
        /// An OutputFormatter.
        /// </returns>


        public static IOutputFormatter Create(IHtmlEncoder encoder)
        {
            return new FormatDefault(DomRenderingOptions.Default,encoder);
        }
        /// <summary>
        /// Gets an instance of the default OuputFormatter configured with the default HTML encoder and options
        /// </summary>

        public static IOutputFormatter Default
        {
            get {
                return Config.OutputFormatter;
            }
        }
        /// <summary>
        /// Gets an instance of the default OuputFormatter configured with no HTML encoding
        /// </summary>

        public static IOutputFormatter HtmlEncodingNone
        {
            get
            {
                return Create(HtmlEncoders.None);
            }
        }

        /// <summary>
        /// Gets an instance of the default OuputFormatter configured with basic encoding
        /// </summary>

        public static IOutputFormatter HtmlEncodingBasic
        {
            get
            {
                return Create(HtmlEncoders.Basic);
            }
        }

        /// <summary>
        /// Gets an instance of the default OuputFormatter configured for full HTML encoding
        /// </summary>

        public static IOutputFormatter HtmlEncodingFull
        {
            get
            {
                return Create(HtmlEncoders.Full);
            }
        }
        /// <summary>
        /// Gets an instance of the default OutputFormatter, using the minimum HTML encoding scheme.
        /// </summary>

        public static IOutputFormatter HtmlEncodingMinimum
        {
            get
            {
                return Create(HtmlEncoders.Minimum);
            }
        }
        /// <summary>
        /// Gets an instance of the default OutputFormatter, using the minimum HTML + NBSP encoding scheme.
        /// </summary>

        public static IOutputFormatter HtmlEncodingMinimumNbsp
        {
            get
            {
                return Create(HtmlEncoders.MinimumNbsp);
            }
        }

        /// <summary>
        /// An OutputFormatter that returns a basic ASCII version of the HTML document.
        /// </summary>

        public static IOutputFormatter PlainText
        {
            get
            {
                return new FormatPlainText();
            }
        }



        /// <summary>
        /// Merge options with defaults when needed
        /// </summary>
        ///
        /// <param name="options">
        /// (optional) options for controlling the operation.
        /// </param>

        private static void MergeOptions(ref DomRenderingOptions options)
        {
            if (options.HasFlag(DomRenderingOptions.Default))
            {
                options = CsQuery.Config.DomRenderingOptions | options & ~(DomRenderingOptions.Default);
            }
        }
    }
}
