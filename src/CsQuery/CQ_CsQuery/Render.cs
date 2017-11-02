using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CsQuery.Utility;
using CsQuery.ExtensionMethods;
using CsQuery.ExtensionMethods.Internal;
using CsQuery.Engine;
using CsQuery.Implementation;
using CsQuery.Output;

namespace CsQuery
{
    public partial class CQ
    {
        /// <summary>
        /// Renders just the selection set completely.
        /// </summary>
        ///
        /// <remarks>
        /// This method will only render the HTML for elements in the current selection set. To render
        /// the entire document for output, use the Render method.
        /// </remarks>
        ///
        /// <returns>
        /// A string of HTML.
        /// </returns>

        public string RenderSelection()
        {
            return RenderSelection(OutputFormatters.Default);
        }

        /// <summary>
        /// Renders just the selection set completely.
        /// </summary>
        ///
        /// <param name="outputFormatter">
        /// The output formatter.
        /// </param>
        ///
        /// <returns>
        /// A string of HTML.
        /// </returns>

        public string RenderSelection(IOutputFormatter outputFormatter)
        {
            StringWriter writer = new StringWriter();
            RenderSelection(outputFormatter, writer);

            return writer.ToString();
        }

        /// <summary>
        /// Renders just the selection set completely.
        /// </summary>
        ///
        /// <param name="outputFormatter">
        /// The output formatter.
        /// </param>
        /// <param name="writer">
        /// The writer.
        /// </param>
        ///
        /// <returns>
        /// A string of HTML.
        /// </returns>

        public void RenderSelection(IOutputFormatter outputFormatter, StringWriter writer)
        {
            foreach (IDomObject elm in this)
            {
                elm.Render(outputFormatter, writer);
            }

        }

        /// <summary>
        /// Renders the document to a string.
        /// </summary>
        ///
        /// <remarks>
        /// This method renders the entire document, regardless of the current selection. This is the
        /// primary method used for rendering the final HTML of a document after manipulation; it
        /// includes the &lt;doctype&gt; and &lt;html&gt; nodes.
        /// </remarks>
        ///
        /// <returns>
        /// A string of HTML.
        /// </returns>

        public string Render()
        {
            return Document.Render();
        }

        /// <summary>
        /// Render the complete DOM with specific options.
        /// </summary>
        ///
        /// <param name="options">
        /// (optional) option flags that control how the output is rendered.
        /// </param>
        ///
        /// <returns>
        /// A string of HTML.
        /// </returns>

        public string Render(DomRenderingOptions options)
        {
            return Document.Render(options);
        }


        /// <summary>
        /// Render the entire document, parsed through a formatter passed using the parameter.
        /// </summary>
        ///
        /// <remarks>
        /// CsQuery by default does not format the output at all, but rather returns exactly the same
        /// contents of each element from the source, including all extra whitespace. If you want to
        /// produce output that is formatted in a specific way, you can create an OutputFormatter for
        /// this purpose. The included <see cref="T:CsQuery.OutputFormatters.FormatPlainText"/> does some
        /// basic formatting by removing extra whitespace and adding newlines in a few useful places.
        /// (This formatter is pretty basic). A formatter to perform indenting to create human-readable
        /// output would be useful and will be included in some future release.
        /// </remarks>
        ///
        /// <param name="formatter">
        /// An object that parses a CQ object and returns a string of HTML.
        /// </param>
        ///
        /// <returns>
        /// A string of HTML.
        /// </returns>

        public string Render(IOutputFormatter formatter)
        {
            StringBuilder sb= new StringBuilder();
            StringWriter writer = new StringWriter(sb);
            Render(formatter, writer);
            return sb.ToString();
        }

        /// <summary>
        /// Render the entire document, parsed through a formatter passed using the parameter, to the
        /// specified writer.
        /// </summary>
        ///
        /// <param name="formatter">
        /// The formatter.
        /// </param>
        /// <param name="writer">
        /// The writer.
        /// </param>

        public void Render(IOutputFormatter formatter, TextWriter writer)
        {
            foreach (var element in Document.ChildNodes)
            {
                formatter.Render(element, writer);
            }
        }
        /// <summary>
        /// Render the entire document, parsed through a formatter passed using the parameter, with the
        /// specified options.
        /// </summary>
        ///
        /// <param name="sb">
        /// The sb.
        /// </param>
        /// <param name="options">
        /// (optional) options for controlling the operation.
        /// </param>
        
        [Obsolete]
        public void Render(StringBuilder sb, DomRenderingOptions options = DomRenderingOptions.Default)
        {
            Document.Render(sb, options);
        }
    }
}
