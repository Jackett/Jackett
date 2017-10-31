using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Promises
{
    /// <summary>
    /// An action accepting a single parameter that runs on a promise resolution.
    /// </summary>
    ///
    /// <typeparam name="T">
    /// The type of the parameter
    /// </typeparam>
    /// <param name="parameter">
    /// The parameter.
    /// </param>

    public delegate void PromiseAction<T>(T parameter);

    /// <summary>
    /// An action accepting a single parameter that runs on a promise resolution, and returns another
    /// promise.
    /// </summary>
    ///
    /// <typeparam name="T">
    /// The type of the parameter
    /// </typeparam>
    /// <param name="parameter">
    /// The parameter.
    /// </param>
    ///
    /// <returns>
    /// A new promise that can be chained.
    /// </returns>

    public delegate IPromise PromiseFunction<T>(T parameter);

    /// <summary>
    /// A promise is an object exposing "Then" which will be called on the resoluton of a particular process.
    /// </summary>

    public interface IPromise
    {
        /// <summary>
        /// Chains delegates that will be executed on success or failure of a promise.
        /// </summary>
        ///
        /// <param name="success">
        /// The delegate to call upon successful resolution of the promise.
        /// </param>
        /// <param name="failure">
        /// (optional) The delegate to call upon unsuccessful resolution of the promise.
        /// </param>
        ///
        /// <returns>
        /// A new promise which will resolve when this promise has resolved.
        /// </returns>

        IPromise Then(Delegate success, Delegate failure=null);

        /// <summary>
        /// Chains delegates that will be executed on success or failure of a promise.
        /// </summary>
        ///
        /// <param name="success">
        /// The delegate to call upon successful resolution of the promise.
        /// </param>
        /// <param name="failure">
        /// (optional) The delegate to call upon unsuccessful resolution of the promise.
        /// </param>
        ///
        /// <returns>
        /// A new promise which will be chained to this promise.
        /// </returns>

        IPromise Then(Action success, Action failure = null);

        /// <summary>
        /// Chains delegates that will be executed on success or failure of a promise.
        /// </summary>
        ///
        /// <param name="success">
        /// The delegate to call upon successful resolution of the promise.
        /// </param>
        /// <param name="failure">
        /// (optional) The delegate to call upon unsuccessful resolution of the promise.
        /// </param>
        ///
        /// <returns>
        /// A new promise which will be chained to this promise.
        /// </returns>

        IPromise Then(Func<IPromise> success, Func<IPromise> failure = null);

        /// <summary>
        /// Chains delegates that will be executed on success or failure of a promise.
        /// </summary>
        ///
        /// <param name="success">
        /// The delegate to call upon successful resolution of the promise.
        /// </param>
        /// <param name="failure">
        /// (optional) The delegate to call upon unsuccessful resolution of the promise.
        /// </param>
        ///
        /// <returns>
        /// A new promise which will resolve when this promise has resolved.
        /// </returns>

        IPromise Then(PromiseAction<object> success, PromiseAction<object> failure = null);

        /// <summary>
        /// Chains delegates that will be executed on success or failure of a promise.
        /// </summary>
        ///
        /// <param name="success">
        /// The delegate to call upon successful resolution of the promise.
        /// </param>
        /// <param name="failure">
        /// (optional) The delegate to call upon unsuccessful resolution of the promise.
        /// </param>
        ///
        /// <returns>
        /// A new promise which will resolve when this promise has resolved.
        /// </returns>

        IPromise Then(PromiseFunction<object> success, PromiseFunction<object> failure = null);
    }

   

}
