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
        /// Returns the HTML for all selected documents, separated by commas. No inner html or children
        /// are included.
        /// </summary>
        ///
        /// <remarks>
        /// This method does not return valid HTML, but rather a single string containing an abbreviated
        /// version of the markup for only documents in the selection set, separated by commas. This is
        /// intended for inspecting a selection set, for example while debugging.
        /// </remarks>
        ///
        /// <returns>
        /// A string of HTML.
        /// </returns>

        public string SelectionHtml()
        {
            return SelectionHtml(false);
        }

        /// <summary>
        /// Returns the HTML for all selected documents, separated by commas.
        /// </summary>
        ///
        /// <remarks>
        /// This method does not return valid HTML, but rather a single string containing an abbreviated
        /// version of the markup for only documents in the selection set, separated by commas. This is
        /// intended for inspecting a selection set, for example while debugging.
        /// </remarks>
        ///
        /// <param name="includeInner">
        /// When true, the complete HTML (e.g. including children) is included for each element.
        /// </param>
        ///
        /// <returns>
        /// A string of HTML.
        /// </returns>

        public string SelectionHtml(bool includeInner)
        {
            StringBuilder sb = new StringBuilder();
            foreach (IDomObject elm in this)
            {

                sb.Append(sb.Length == 0 ? String.Empty : ", ");
                sb.Append(includeInner ? elm.Render() : elm.ToString());
            }
            return sb.ToString();
        }
    }
}
