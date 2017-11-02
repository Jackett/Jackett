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
        /// Hide the matched elements.
        /// </summary>
        ///
        /// <remarks>
        /// The jQuery docs say "This is roughly equivalent to calling .css('display', 'none')." With
        /// CsQuery, it is exactly equivalent. Unlike jQuery, CsQuery does not store the current value of
        /// the "display" style and restore it, because there is no concept of "effective style" in
        /// CsQuery. We don't attempt to calculate the actual style that would be in effect since we
        /// don't do any style sheet parsing. Instead, this method really just sets display: none. When
        /// showing again, any "display" style is removed.
        /// 
        /// This means if you were to assign a non-default value for "display" such as "inline" to a div,
        /// then Hide(), then Show(), it would no longer be displayed inline, as it would in jQuery.
        /// Since CsQuery is not used interactively (yet, anyway), this sequence of events seems unlikely,
        /// and supporting it exactly as jQuery does seems unnecessary. This functionality could
        /// certainly be added in the future.
        /// </remarks>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/hide/
        /// </url>

        public CQ Hide()
        {
            foreach (IDomElement e in Elements)
            {
                e.Style["display"] = "none";
            }
            return this;

        }

        /// <summary>
        /// Display the matched elements.
        /// </summary>
        ///
        /// <remarks>
        /// This method simply removes the "display: none" css style, if present. See
        /// <see cref="T:CsQuery.CQ.Hide"/> for an explanation of how this differs from jQuery.
        /// </remarks>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/show/
        /// </url>

        public CQ Show()
        {
            foreach (IDomElement e in Elements)
            {
                if (e.Style["display"] == "none")
                {
                    e.RemoveStyle("display");
                }
            }
            return this;
        }

        /// <summary>
        /// Display or hide the matched elements.
        /// </summary>
        ///
        /// <returns>
        /// The curren CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/toggle/
        /// </url>

        public CQ Toggle()
        {
            foreach (IDomElement e in Elements)
            {
                string displ = e.Style["display"];
                bool isVisible = displ == null || displ != "none";
                e.Style["display"] = isVisible ? "none" : null;
            }
            return this;
        }

        /// <summary>
        /// Display or hide the matched elements based on the value of the parameter.
        /// </summary>
        ///
        /// <param name="isVisible">
        /// true to show the matched elements, or false to hide them.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/toggle/
        /// </url>

        public CQ Toggle(bool isVisible)
        {
            return isVisible ?
                Show() :
                Hide();
        }
        

    }
}
