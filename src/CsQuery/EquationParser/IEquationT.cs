using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.EquationParser
{
    /// <summary>
    /// Interface for a strongly-typed equation.
    /// </summary>
    ///
    /// <typeparam name="T">
    /// The type of value returned by the equation.
    /// </typeparam>

    public interface IEquation<T> : IOperand<T>, IEquation where T : IConvertible
    {
        /// <summary>
        /// Execute the equation and return the result
        /// </summary>
        ///
        /// <param name="values">
        /// The values of the variables for this equation, in the order the variables were created.
        /// </param>
        ///
        /// <returns>
        /// The value.
        /// </returns>

        new T GetValue(params IConvertible[] values);

        /// <summary>
        /// Execute the equation; if an error occurs, return false.
        /// </summary>
        ///
        /// <param name="result">
        /// [out] The result.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        bool TryGetValue(out T result);

        /// <summary>
        /// Execute the equation; if an error occurs, return false.
        /// </summary>
        ///
        /// <param name="result">
        /// [out] The result.
        /// </param>
        /// <param name="values">
        /// The values of the variables for this equation, in the order the variables were created.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        bool TryGetValue(out T result, params IConvertible[] values);

        /// <summary>
        /// Makes a deep copy of this object.
        /// </summary>
        ///
        /// <returns>
        /// A copy of this object.
        /// </returns>

        new IEquation<T> Clone();

        /// <summary>
        /// Clone the equation, changing the output type.
        /// </summary>
        ///
        /// <typeparam name="U">
        /// Generic type parameter.
        /// </typeparam>
        ///
        /// <returns>
        /// A clone of the equation that returns type U.
        /// </returns>

        IEquation<U> CloneAs<U>() where U : IConvertible;
    }
}
