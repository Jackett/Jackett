using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Promises
{
    /// <summary>
    /// A strongly-typed deferred object
    /// </summary>
    ///
    /// <typeparam name="T">
    /// Generic type parameter.
    /// </typeparam>

    public class Deferred<T> : Deferred, IPromise<T>
    {
        /// <summary>
        /// Bind delegates to the success or failure of a promise
        /// </summary>
        ///
        /// <param name="success">
        /// The success delegate
        /// </param>
        /// <param name="failure">
        /// (optional) the failure delegate
        /// </param>
        ///
        /// <returns>
        /// A new promise that resolves when the current promise resolves.
        /// </returns>

        public IPromise Then(PromiseAction<T> success, PromiseAction<T> failure = null)
        {
            return base.Then(success, failure);
        }

        /// <summary>
        /// Bind delegates to the success or failure of a promise
        /// </summary>
        ///
        /// <param name="success">
        /// The success delegate
        /// </param>
        /// <param name="failure">
        /// (optional) the failure delegate
        /// </param>
        ///
        /// <returns>
        /// A new promise that resolves when the current promise resolves.
        /// </returns>

        public IPromise Then(PromiseFunction<T> success, PromiseFunction<T> failure = null)
        {
            return base.Then(success, failure);
        }

     
    }
   
}
