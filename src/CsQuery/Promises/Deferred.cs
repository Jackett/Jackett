using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace CsQuery.Promises
{
    /// <summary>
    /// A Deferred object. Deferred objects implement the IPromise interface, and have methods for
    /// resolving or rejecting the promise.
    /// </summary>

    public class Deferred: IPromise
    {
        /// <summary>
        /// Default constuctor for a Deferred object.
        /// </summary>

        public Deferred()
        {
            if (When.Debug)
            {
                FailOnResolutionExceptions = true;
            }
        }

        #region private properties

        /// <summary>
        /// The thread locker object
        /// </summary>

        internal object Locker = new object();
        private Func<object, IPromise> _Success;
        private Func<object, IPromise> _Failure;

        /// <summary>
        /// The success delegate
        /// </summary>

        protected Func<object, IPromise> Success
        {
            get
            {
                return _Success;
            }
            set
            {
                if (_Success != null)
                {
                    throw new InvalidOperationException("This promise has already been assigned a success delegate.");
                }
                _Success = value;
                if (Resolved == true)
                {
                    ResolveImpl();
                }
            }
        }

        /// <summary>
        /// The failure delegate
        /// </summary>

        protected Func<object, IPromise> Failure
        {
            get
            {
                return _Failure;
            }
            set
            {
                if (_Failure != null)
                {
                    throw new InvalidOperationException("This promise has already been assigned a failure delegate.");
                }
                _Failure = value;
                if (Resolved == false)
                {
                    RejectImpl();
                }
            }

        }

        /// <summary>
        /// The next deferred objected in the chain; resolved or rejected when any bound delegate is
        /// resolved or rejected./.
        /// </summary>

        protected List<Deferred> NextDeferred=null;

        /// <summary>
        /// Indicates whether this object has been resolved yet. A null value means unresolved; true or
        /// false indicate success or failure.
        /// </summary>

        protected bool? Resolved;

        /// <summary>
        /// The parameter that was passed with a resolution or rejection.
        /// </summary>

        protected object Parameter;

        #endregion

        #region public properties

        /// <summary>
        /// When false (default), errors thrown during promise resoluton will be turned into a rejected
        /// promise. If this is true, no error handling will occur, meaning that errors could bubble, or
        /// in the event that a promise was resolved by an asynchronous event, be unhandled. Typically,
        /// you would only want this to be false when debugging, as it could result in unhandled
        /// exceptions.
        /// </summary>

        public bool FailOnResolutionExceptions { get; set; }
 
        #endregion

        #region public methods

        /// <summary>
        /// Resolves the promise.
        /// </summary>
        ///
        /// <param name="parm">
        /// (optional) a value passed to the promise delegate
        /// </param>

        public void Resolve(object parm = null)
        {
            Parameter = parm;
            Resolved = true;
            ResolveImpl();
        }

        /// <summary>
        /// Rejects the promise
        /// </summary>
        ///
        /// <param name="parm">
        /// (optional) a value passed to the promise delegate.
        /// </param>

        public void Reject(object parm =null)
        {
            Parameter = parm;
            Resolved = false;
            RejectImpl();
        }

        /// <summary>
        /// Chains a delegate to be invoked upon resolution or failure of the Deferred promise object.
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
            lock (Locker)
            {

                var deferred = GetNextDeferred();

                MethodInfo method = success != null ?
                    success.GetMethodInfo() :
                    failure.GetMethodInfo();

                Type returnType = method.ReturnType;
                Type[] parameters = method.GetParameters().Select(item => item.ParameterType).ToArray();

                bool useParms = parameters.Length > 0;

                Success = new Func<object, IPromise>((parm) =>
                {
                    object result = success.DynamicInvoke(GetParameters(useParms));
                    return result as IPromise;

                });
                Failure = new Func<object, IPromise>((parm) =>
                {
                    object result = failure.DynamicInvoke(GetParameters(useParms));
                    return result as IPromise;
                });

                return deferred;
            
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

        public IPromise Then(PromiseAction<object> success, PromiseAction<object> failure = null)
        {
            lock (Locker)
            {
                var deferred = GetNextDeferred();
                Success = new Func<object, IPromise>((parm) =>
                {
                    success(parm);
                    return null;
                });
                if (failure != null)
                {
                    Failure = new Func<object, IPromise>((parm) =>
                    {
                        failure(parm);
                        return null;
                    });

                }

                return deferred;
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

        public IPromise Then(PromiseFunction<object> success, PromiseFunction<object> failure = null)
        {
            lock (Locker)
            {
                var deferred = GetNextDeferred();

                Success = new Func<object, IPromise>((parm) =>
                {
                    return success(Parameter);
                });
                if (failure != null)
                {
                    Failure = new Func<object, IPromise>((parm) =>
                    {
                        return success(Parameter);
                    });
                }

                return deferred;
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
        /// A new promise which will be chained to this promise.
        /// </returns>

        public IPromise Then(Action success, Action failure = null)
        {
            lock (Locker)
            {
                var deferred = GetNextDeferred();
                Success = new Func<object, IPromise>((parm) =>
                {
                    success();
                    return null;
                });
                if (failure != null)
                {
                    Failure = new Func<object, IPromise>((parm) =>
                    {
                        failure();
                        return null;
                    });

                }

                return deferred;
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
        /// A new promise which will be chained to this promise.
        /// </returns>

        public IPromise Then(Func<IPromise> success, Func<IPromise> failure = null)
        {
            lock (Locker)
            {
                var deferred = GetNextDeferred();
                Success = new Func<object, IPromise>((parm) =>
                {
                    return success();
                });
                if (failure != null)
                {
                    Failure = new Func<object, IPromise>((parm) =>
                    {
                        return failure();
                    });

                }

                return deferred;
            }
        }

        /// <summary>
        /// Gets the parameters that should be invoked on the success/fail delegate.
        /// </summary>
        ///
        /// <param name="useParms">
        /// When true, the target delegate has parameters and this should return a non-null result.
        /// </param>
        ///
        /// <returns>
        /// The parameters.
        /// </returns>

        protected object[] GetParameters(bool useParms)
        {
            object[] parms = null;

            if (useParms)
            {
                parms = new object[] { Parameter };
            }
            return parms;
        }

        /// <summary>
        /// Implementation of the Resolve function.
        /// </summary>

        protected void ResolveImpl()
        {
            lock (Locker)
            {
                if (Success != null)
                {
                    if (!FailOnResolutionExceptions)
                    {
                        try
                        {
                            Success(Parameter);
                        }
                        catch
                        {
                            RejectImpl();
                            return;
                        }
                    }
                    else
                    {
                        Success(Parameter);
                    }
                }

                if (NextDeferred != null)
                {
                    NextDeferred.ForEach(item =>
                    {
                        item.Resolve(Parameter);
                    });
                }
            }
        }

        /// <summary>
        /// Implementation of the Reject function
        /// </summary>

        protected void RejectImpl()
        {
            if (Failure != null)
            {
                if (!FailOnResolutionExceptions)
                {
                    try
                    {
                        Failure(Parameter);
                    }
                    catch { }
                }
                else
                {
                    Failure(Parameter);
                }
            }
            if (NextDeferred != null)
            {
                NextDeferred.ForEach(item =>
                {
                    item.Reject(Parameter);
                });
            }
        }

        private Deferred GetNextDeferred()
        {
            var deferred = new Deferred();
            if (NextDeferred == null)
            {
                NextDeferred = new List<Deferred>();
            }
            NextDeferred.Add(deferred);
            return deferred;
        }
        #endregion


    }
}
