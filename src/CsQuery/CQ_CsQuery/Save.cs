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

namespace CsQuery
{
    public partial class CQ
    {
        /// <summary>
        /// Save the current Document to an HTML file.
        /// </summary>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>

        public void Save(string fileName, DomRenderingOptions renderingOptions=DomRenderingOptions.Default)
        {
            File.WriteAllText(fileName, Render(renderingOptions));
        }
    }
}
