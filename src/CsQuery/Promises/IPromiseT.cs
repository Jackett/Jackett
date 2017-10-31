using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Promises
{
    /// <summary>
    /// Interface for a promise that accepts a strongly-typed parameter.
    /// </summary>
    ///
    /// <typeparam name="T">
    /// The type of data accepted by the resolution parameter.
    /// </typeparam>

    public interface IPromise<T> : IPromise
    {
        /// <summary>
        /// Chains delegates that will be executed on success or failure of a promise
        /// </summary>
        ///
        /// <param name="success">
        /// The success delegate.
        /// </param>
        /// <param name="failure">
        /// (optional) the failure delegate.
        /// </param>
        ///
        /// <returns>
        /// A promise
        /// </returns>

        IPromise Then(PromiseAction<T> success, PromiseAction<T> failure = null);

        /// <summary>
        /// Chains delegates that will be executed on success or failure of a promise.
        /// </summary>
        ///
        /// <param name="success">
        /// The success delegate.
        /// </param>
        /// <param name="failure">
        /// (optional) the failure delegate.
        /// </param>
        ///
        /// <returns>
        /// A promise.
        /// </returns>

        IPromise Then(PromiseFunction<T> success, PromiseFunction<T> failure = null);
    }
}
