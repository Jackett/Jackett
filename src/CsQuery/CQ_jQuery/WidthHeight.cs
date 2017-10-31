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
        /// Set the CSS width of each element in the set of matched elements.
        /// </summary>
        ///
        /// <remarks>
        /// There is no Width() method in CsQuery because this is a value calculated by the browser.
        /// While we can set the CSS of an element, it would be futile to try to return a useful value.
        /// If you want to inspect the current CSS width for an element, please use Css() methods
        /// instead. This ensures there is no confusion about the use of Width() in CsQuery.
        /// </remarks>
        ///
        /// <param name="value">
        /// An integer representing the number of pixels.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/width/#width2
        /// </url>

        public CQ Width(int value)
        {
            return Width(value.ToString() + "px");
        }

        /// <summary>
        /// Set the CSS width of each element in the set of matched elements.
        /// </summary>
        ///
        /// <remarks>
        /// There are no Height() or Width() methods  in CsQuery because these are value calculated by
        /// the browser that depend on the page layout, as well as things like the browser window size
        /// which don't even exist in CsQuery. While we can set the CSS of an element, it would be futile
        /// to try to return a useful value. If you want to inspect the current CSS width for an element,
        /// please use Css() methods instead. This ensures there is no confusion about the use of Width()
        /// and Height()
        /// in CsQuery.
        /// </remarks>
        ///
        /// <param name="value">
        /// An integer along with a unit of measure appended (as a string), e.g. "100px".
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/width/#width2
        /// </url>

        public CQ Width(string value)
        {
            return Css("width", value);
        }

        /// <summary>
        /// Set the CSS width of each element in the set of matched elements.
        /// </summary>
        ///
        /// <remarks>
        /// There are no Height() or Width() methods  in CsQuery because these are value calculated by
        /// the browser that depend on the page layout, as well as things like the browser window size
        /// which don't even exist in CsQuery. While we can set the CSS of an element, it would be futile
        /// to try to return a useful value. If you want to inspect the current CSS width for an element,
        /// please use Css() methods instead. This ensures there is no confusion about the use of Width()
        /// and Height()
        /// in CsQuery.
        /// </remarks>
        ///
        /// <param name="value">
        /// An integer representing the number of pixels.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/height/#height2
        /// </url>

        public CQ Height(int value)
        {
            return Height(value.ToString() + "px");
        }

        /// <summary>
        /// Set the CSS height of each element in the set of matched elements.
        /// </summary>
        ///
        /// <remarks>
        /// There are no Height() or Width() methods  in CsQuery because these are value calculated by
        /// the browser that depend on the page layout, as well as things like the browser window size
        /// which don't even exist in CsQuery. While we can set the CSS of an element, it would be futile
        /// to try to return a useful value. If you want to inspect the current CSS width for an element,
        /// please use Css() methods instead. This ensures there is no confusion about the use of Width()
        /// and Height()
        /// in CsQuery.
        /// </remarks>
        ///
        /// <param name="value">
        /// An integer along with a unit of measure appended (as a string), e.g. "100px".
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/height/#height2
        /// </url>

        public CQ Height(string value)
        {
            return Css("height", value);
        }
        

    }
}
