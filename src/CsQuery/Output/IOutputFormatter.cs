using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using CsQuery.Implementation;

namespace CsQuery.Output
{
    /// <summary>
    /// Interface for an OutputFormatter. This is an object that renders a CsQuery tree to a TextWriter
    /// </summary>

    public interface IOutputFormatter
    {
        /// <summary>
        /// Renders this object to the passed TextWriter
        /// </summary>
        ///
        /// <param name="node">
        /// The node.
        /// </param>
        /// <param name="writer">
        /// The writer.
        /// </param>

        void Render(IDomObject node, TextWriter writer);

        /// <summary>
        /// Renders this object and returns the output as a string
        /// </summary>
        ///
        /// <param name="node">
        /// The node.
        /// </param>
        ///
        /// <returns>
        /// A string of HTML
        /// </returns>

        string Render(IDomObject node);
    }
}
