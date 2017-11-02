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
        /// Reduce the set of matched elements to a subset beginning with the 0-based index provided.
        /// </summary>
        ///
        /// <param name="start">
        /// The 0-based index at which to begin selecting.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/slice/
        /// </url>

        public CQ Slice(int start)
        {
            return Slice(start, SelectionSet.Count);
        }

        /// <summary>
        /// Reduce the set of matched elements to a subset specified by a range of indices.
        /// </summary>
        ///
        /// <param name="start">
        /// The 0-based index at which to begin selecting.
        /// </param>
        /// <param name="end">
        /// The 0-based index of the element at which to stop selecting. The actual element at this
        /// position is not included in the result.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/slice/
        /// </url>

        public CQ Slice(int start, int end)
        {
            if (start < 0)
            {
                start = SelectionSet.Count + start;
                if (start < 0) { start = 0; }
            }
            if (end < 0)
            {
                end = SelectionSet.Count + end;
                if (end < 0) { end = 0; }
            }
            if (end >= SelectionSet.Count)
            {
                end = SelectionSet.Count;
            }

            CQ output = NewCqInDomain();

            for (int i = start; i < end; i++)
            {
                output.SelectionSet.Add(SelectionSet[i]);
            }

            return output;
        }

        

    }
}
