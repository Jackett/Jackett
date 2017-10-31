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
        /// Return the active selection set.
        /// </summary>
        ///
        /// <returns>
        /// An sequence of IDomObject elements representing the current selection set.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/get/
        /// </url>

        public IEnumerable<IDomObject> Get()
        {
            return SelectionSet;
        }

        /// <summary>
        /// Return a specific element from the selection set.
        /// </summary>
        ///
        /// <param name="index">
        /// The zero-based index of the element to be returned.
        /// </param>
        ///
        /// <returns>
        /// An IDomObject.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/get/
        /// </url>

        public IDomObject Get(int index)
        {
            int effectiveIndex = index < 0 ? SelectionSet.Count + index - 1 : index;
            return effectiveIndex >= 0 && effectiveIndex < SelectionSet.Count ?
                SelectionSet.ElementAt(effectiveIndex) :
                null;
        }
        

    }
}
