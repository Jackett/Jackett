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
        /// Iterate over each matched element, calling the delegate passed by parameter for each element.
        /// If the delegate returns false, the iteration is stopped.
        /// </summary>
        ///
        /// <remarks>
        /// The overloads of Each the inspect the return value have a different method name (EachUntil)
        /// because the C# compiler will not choose the best-matchine method when passing method groups.
        /// See: http://stackoverflow.com/questions/2057146/compiler-ambiguous-invocation-error-anonymous-
        /// method-and-method-group-with-fun.
        /// </remarks>
        ///
        /// <param name="func">
        /// A function delegate returning a boolean, and accepting an integer and an IDomObject
        /// parameter. The integer is the zero-based index of the current iteration, and the IDomObject
        /// is the current element.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/each/
        /// </url>

        public CQ EachUntil(Func<int, IDomObject, bool> func)
        {
            int index = 0;
            foreach (IDomObject obj in Selection)
            {
                if (!func(index++, obj))
                {
                    break;
                }

            }
            return this;
        }

        /// <summary>
        /// Iterate over each matched element, calling the delegate passed by parameter for each element.
        /// If the delegate returns false, the iteration is stopped.
        /// </summary>
        ///
        /// <remarks>
        /// The overloads of Each the inspect the return value have a different method name (EachUntil)
        /// because the C# compiler will not choose the best-matchine method when passing method groups.
        /// See: http://stackoverflow.com/questions/2057146/compiler-ambiguous-invocation-error-anonymous-
        /// method-and-method-group-with-fun.
        /// </remarks>
        ///
        /// <param name="func">
        /// A function delegate returning a boolean.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/each/
        /// </url>

        public CQ EachUntil(Func<IDomObject, bool> func)
        {

            foreach (IDomObject obj in Selection)
            {
                if (!func(obj))
                {
                    break;
                }
            }
            return this;
        }

        /// <summary>
        /// Iterate over each matched element, calling the delegate passed by parameter for each element
        /// </summary>
        ///
        /// <param name="func">
        /// A delegate accepting a single IDomObject paremeter
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/each/
        /// </url>

        public CQ Each(Action<IDomObject> func)
        {
            foreach (IDomObject obj in Selection)
            {
                func(obj);
            }
            return this;
        }

        /// <summary>
        /// Iterate over each matched element, calling the delegate passed by parameter for each element.
        /// </summary>
        ///
        /// <param name="func">
        /// A delegate accepting an integer parameter, and an IDomObject paremeter. The integer is the
        /// zero-based index of the current iteration.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/each/
        /// </url>

        public CQ Each(Action<int, IDomObject> func)
        {
            int index = 0;
            foreach (IDomObject obj in Selection)
            {
                func(index++, obj);
            }
            return this;
        }

        /// <summary>
        /// Iterate over each element in a sequence, and call a delegate for each element
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="func"></param>
        public static void Each<T>(IEnumerable<T> list, Action<T> func)
        {
            foreach (var obj in list)
            {
                func(obj);
            }
        }
        

    }
}
