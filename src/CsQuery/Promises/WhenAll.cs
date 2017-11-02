using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.Promises;

namespace CsQuery.Promises
{
    /// <summary>
    /// A promise that resolves when one or more other promises have all resolved
    /// </summary>

    public class WhenAll: IPromise
    {
        #region constructors

        /// <summary>
        /// Constructor
        /// </summary>
        ///
        /// <param name="promises">
        /// A variable-length parameters list containing promises that must all resolve
        /// </param>

        public WhenAll(params IPromise[] promises)
        {
            Configure(promises);
        }

        /// <summary>
        /// Constructor to create a promise that resolves when one or more other promises have all
        /// resolved or a timeout elapses.
        /// </summary>
        ///
        /// <param name="timeoutMilliseconds">
        /// The timeout in milliseconds.
        /// </param>
        /// <param name="promises">
        /// A variable-length parameters list containing promises that must all resolve.
        /// </param>

        public WhenAll(int timeoutMilliseconds, params IPromise[] promises)
        {
            Configure(promises);
            timeout = new Timeout(timeoutMilliseconds);
            timeout.Then((Action)null,(Action)TimedOut);
        }

        #endregion

        #region private properties

        private  Deferred Deferred;
        private int TotalCount = 0;
        private int _ResolvedCount = 0;
        private Timeout timeout;
        private bool Complete;
        private object _locker = new Object();

        private int ResolvedCount
        {
            get
            {
                return _ResolvedCount;
            }

            set
            {
                lock (_locker)
                {
                    _ResolvedCount = value;
                    if (_ResolvedCount == TotalCount)
                    {
                        CompletePromise();
                    }
                }
            }
        }

        /// <summary>
        /// When false, means one or more of the promises was rejected, and the All will be rejected.
        /// </summary>

        private bool Success;

        #endregion

        #region public properties

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
            return Deferred.Then(success,failure);
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
        /// A new promise which will be chained to this promise.
        /// </returns>

        public IPromise Then(Action success, Action failure = null)
        {
            return Deferred.Then(success, failure);
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

        public IPromise Then(PromiseAction<object> success, PromiseAction<object> failure = null)
        {
            return Deferred.Then(success, failure);
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
        /// A new promise which will be chained to this promise.
        /// </returns>

        public IPromise Then(Func<IPromise> success, Func<IPromise> failure = null)
        {
            return Deferred.Then(success, failure);
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

        public IPromise Then(PromiseFunction<object> success,PromiseFunction<object> failure = null)
        {
            return Deferred.Then(success, failure);
        }

        #endregion

        #region private properties

        private void Configure(IEnumerable<IPromise> promises)
        {
            lock (_locker)
            {
                Success = true;
                Deferred = new Deferred();

                int count = 0;
                foreach (var promise in promises)
                {
                    count++;
                    promise.Then((Action)PromiseResolve, (Action)PromiseReject);
                }
                TotalCount = count;
            }

        }

        /// <summary>
        /// Called when a client promise is resolved.
        /// </summary>

        private void PromiseResolve()
        {
            lock (_locker)
            {
                ResolvedCount++;
            }
        }

        /// <summary>
        /// Called when a client promise is rejected.
        /// </summary>

        private void PromiseReject()
        {
            lock (_locker)
            {
                Success = false;
                ResolvedCount++;
            }
        }

        private void TimedOut()
        {
            lock (_locker)
            {
                Success = false;
                CompletePromise();
            }
            }

        private void CompletePromise()
        {
            lock (_locker)
            {
                if (Complete)
                {
                    return;
                }

                Complete = true;
                if (timeout != null)
                {
                    timeout.Stop(true);
                }
                if (Success)
                {
                    Deferred.Resolve();
                }
                else
                {
                    Deferred.Reject();
                }
            }
        }

        #endregion

    }
}
