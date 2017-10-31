using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        /// Get the HTML contents of the first element in the set of matched elements.
        /// </summary>
        ///
        /// <returns>
        /// A string of HTML.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/html/#html1
        /// </url>

        public string Html()
        {
            return Length > 0 ? this[0].InnerHTML : String.Empty;
        }

        /// <summary>
        /// Set the HTML contents of each element in the set of matched elements. Any elements without
        /// InnerHtml are ignored.
        /// </summary>
        ///
        /// <param name="html">
        /// One or more strings of HTML markup.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/html/#html2
        /// </url>

        public CQ Html(params string[] html)
        {
            CQ htmlElements = EnsureCsQuery(MergeContent(html));
            bool first = true;

            foreach (DomElement obj in OnlyElements(SelectionSet))
            {
                if (obj.InnerHtmlAllowed)
                {
                    obj.ChildNodes.Clear();
                    obj.ChildNodes.AddRange(first ? htmlElements : htmlElements.Clone());
                    first = false;
                }
            }
            return this;
        }
        

    }
}
