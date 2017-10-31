using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace CsQuery.Promises
{
    /// <summary>
    /// A promise that resolves or fails after a certain amount of time
    /// </summary>

    public class Timeout<T>: IPromise<T>
    {
        /// <summary>
        /// Create a new Timeout that rejects after the specified time.
        /// </summary>
        ///
        /// <param name="timeoutMilliseconds">
        /// The timeout in milliseconds.
        /// </param>

        public Timeout(int timeoutMilliseconds)
        {
            ConfigureTimeout(timeoutMilliseconds,default(T),false);
        }

        /// <summary>
        /// Create a new Timeout that rejects with the provided parameter value after the specified time.
        /// </summary>
        ///
        /// <param name="timeoutMilliseconds">
        /// The timeout in milliseconds.
        /// </param>
        /// <param name="parameterValue">
        /// The parameter value.
        /// </param>

        public Timeout(int timeoutMilliseconds, T parameterValue)
        {
            useParameter = true;
            ConfigureTimeout(timeoutMilliseconds,parameterValue,false);
        }

        /// <summary>
        /// Create a new Timeout that resolves or rejects with the provided parameter value after the specified time.
        /// </summary>
        ///
        /// <param name="timeoutMilliseconds">
        /// The timeout in milliseconds.
        /// </param>
        /// <param name="resolveOnTimeout">
        /// true to resolve the promise on the timeout, false to reject it.
        /// </param>

        public Timeout(int timeoutMilliseconds, bool resolveOnTimeout)
        {
            ConfigureTimeout(timeoutMilliseconds,default(T),resolveOnTimeout);
        }

        /// <summary>
        /// Create a new Timeout that resolves or rejects with the provided parameter value after the specified time.
        /// </summary>
        ///
        /// <param name="timeoutMilliseconds">
        /// The timeout in milliseconds.
        /// </param>
        /// <param name="parameterValue">
        /// The parameter value.
        /// </param>
        /// <param name="resolveOnTimeout">
        /// true to resolve the promise on the timeout, false to reject it.
        /// </param>

        public Timeout(int timeoutMilliseconds, T parameterValue, bool resolveOnTimeout)
        {
            useParameter = true;
            ConfigureTimeout(timeoutMilliseconds,parameterValue,resolveOnTimeout);
        }

        private void ConfigureTimeout(int timeoutMilliseconds, T parameterValue, bool succeedOnTimeout)
        {
            TimeoutMilliseconds = timeoutMilliseconds;
            ResolveOnTimeout = succeedOnTimeout;
            ParameterValue = parameterValue;
            deferred = new Deferred<T>();
 
            Timer = new Timer(Timer_Elapsed, null, timeoutMilliseconds, System.Threading.Timeout.Infinite);
        }

        /// <summary>
        /// Stops the timer with the specified resolution.
        /// </summary>
        ///
        /// <param name="resolve">
        /// True to resolve the promise, false to reject it.
        /// </param>

        public void Stop(bool resolve)
        {
            CompletePromise(resolve);
        }

        /// <summary>
        /// Stops the timer with it's default resolution
        /// </summary>

        public void Stop()
        {
            CompletePromise(ResolveOnTimeout);
        }
        private Timer Timer;
        private int TimeoutMilliseconds;
        private bool ResolveOnTimeout;
        private T ParameterValue;
        private bool useParameter;
        private Deferred<T> deferred;

        /// <summary>
        /// Event handler called when the specified time has elapsed.
        /// </summary>
        ///
        /// <param name="sender">
        /// The timer object.
        /// </param>
        /// <param name="e">
        /// Elapsed event information.
        /// </param>

        protected void Timer_Elapsed(object sender)
        {
            CompletePromise(ResolveOnTimeout);  
        }

        /// <summary>
        /// Completes the promise promise using the specified resolution
        /// </summary>

        protected void CompletePromise(bool resolve)
        {
            Timer.Dispose();
            if (resolve)
            {
                if (useParameter)
                {
                    deferred.Resolve(ParameterValue);
                }
                else
                {
                    deferred.Resolve();
                }
            }
            else
            {
                if (useParameter)
                {
                    deferred.Reject(ParameterValue);
                }
                else
                {
                    deferred.Reject();
                }
            }
        }

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

        public IPromise Then(PromiseAction<T> success, PromiseAction<T> failure = null)
        {
            return deferred.Then(success, failure);
        }

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

        public IPromise Then(PromiseFunction<T> success, PromiseFunction<T> failure = null)
        {
            return deferred.Then(success, failure);
        }

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

        public IPromise Then(Delegate success, Delegate failure = null)
        {
            return deferred.Then(success, failure);
        }

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

        public IPromise Then(Action success, Action failure = null)
        {
            return deferred.Then(success, failure);
        }

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

        public IPromise Then(Func<IPromise> success, Func<IPromise> failure = null)
        {
            return deferred.Then(success, failure);
        }

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

        IPromise IPromise.Then(PromiseAction<object> success, PromiseAction<object> failure)
        {
            return deferred.Then(success, failure);
        }

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

        IPromise IPromise.Then(PromiseFunction<object> success, PromiseFunction<object> failure)
        {
            return deferred.Then(success, failure);
        }
    }
}
