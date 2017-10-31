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
        /// Map each element of the result set to a new form. If a value is returned from the function,
        /// the element will be excluded.
        /// </summary>
        ///
        /// <typeparam name="T">
        /// .
        /// </typeparam>
        /// <param name="elements">
        /// .
        /// </param>
        /// <param name="function">
        /// .
        /// </param>
        ///
        /// <returns>
        /// An enumerator that allows foreach to be used to process map&lt; t&gt; in this collection.
        /// </returns>

        public static IEnumerable<T> Map<T>(IEnumerable<IDomObject> elements, Func<IDomObject, T> function)
        {
            foreach (var element in elements)
            {
                T result = function(element);
                if (result != null)
                {
                    yield return result;
                }
            }
        }

        /// <summary>
        /// Map each element of the result set to a new form. If a value is returned from the function,
        /// the element will be excluded.
        /// </summary>
        ///
        /// <typeparam name="T">
        /// Generic type parameter.
        /// </typeparam>
        /// <param name="function">
        /// .
        /// </param>
        ///
        /// <returns>
        /// An enumerator that allows foreach to be used to process map&lt; t&gt; in this collection.
        /// </returns>

        public IEnumerable<T> Map<T>(Func<IDomObject, T> function)
        {
            return CQ.Map(this, function);
        }

    }
}
