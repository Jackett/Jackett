using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Promises
{
    /// <summary>
    /// A promise that resolves or fails after a certain amount of time
    /// </summary>

    public class Timeout: Timeout<object>,IPromise
    {
        /// <summary>
        /// Create a new Timeout that rejects after the specified time.
        /// </summary>
        ///
        /// <param name="timeoutMilliseconds">
        /// The timeout in milliseconds.
        /// </param>

        public Timeout(int timeoutMilliseconds): base(timeoutMilliseconds)
        {

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

        public Timeout(int timeoutMilliseconds, object parameterValue):
            base(timeoutMilliseconds,parameterValue)
        {
        }

        /// <summary>
        /// Create a new Timeout that resolves or rejects with the provided parameter value after the specified time.
        /// </summary>
        ///
        /// <param name="timeoutMilliseconds">
        /// The timeout in milliseconds.
        /// </param>
        /// <param name="succeedOnTimeout">
        /// true to resolve the promise on the timeout, false to reject it.
        /// </param>

        public Timeout(int timeoutMilliseconds, bool succeedOnTimeout):
            base(timeoutMilliseconds,succeedOnTimeout)
        {

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
        /// <param name="succeedOnTimeout">
        /// true to resolve the promise on the timeout, false to reject it.
        /// </param>

        public Timeout(int timeoutMilliseconds, object parameterValue, bool succeedOnTimeout) :
            base(timeoutMilliseconds,parameterValue,succeedOnTimeout)
        {
        }
    }
}
