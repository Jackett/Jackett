using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.EquationParser.Implementation;

namespace CsQuery.EquationParser
{
    /// <summary>
    /// Interface for an equation.
    /// </summary>

    public interface IEquation : IOperand, IVariableContainer
    {
        /// <summary>
        /// A dictionary of variable names and values.
        /// </summary>

        IOrderedDictionary<string, IConvertible> VariableValues { get; }

        /// <summary>
        /// Sets the value of a named variable.
        /// </summary>
        ///
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>

        void SetVariable(string name, IConvertible value);

        /// <summary>
        /// Sets the value of a strongly-typed named variable.
        /// </summary>
        ///
        /// <typeparam name="U">
        /// The type of the variable.
        /// </typeparam>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>

        void SetVariable<U>(string name, U value) where U : IConvertible;

        /// <summary>
        /// Executes the equation, setting the variables in order they were created with the passed
        /// values. Any variables that were already set using SetValue will be unaffected; if this method
        /// is uncertain in a given context, then it should be called with no parameters and all
        /// variables set with SetValue. If errors occur while parsing the equation, and exception will
        /// be thrown.
        /// </summary>
        ///
        /// <param name="values">
        /// A variable-length parameters list containing values.
        /// </param>
        ///
        /// <returns>
        /// The value.
        /// </returns>

        IConvertible GetValue(params IConvertible[] values);

        /// <summary>
        /// Execute the equation using the values passed; if any errors occur, return false. 
        /// </summary>
        ///
        /// <param name="result">
        /// [out] The result.
        /// </param>
        /// <param name="values">
        /// A variable-length parameters list containing values.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        bool TryGetValue(out IConvertible result, params IConvertible[] values);

        /// <summary>
        /// Execute the equation using existing variable data; if any errors occur, return false.
        /// </summary>
        ///
        /// <param name="result">
        /// [out] The result.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        bool TryGetValue(out IConvertible result);

        /// <summary>
        /// Makes a deep copy of this object.
        /// </summary>
        ///
        /// <returns>
        /// A copy of this object.
        /// </returns>

        new IEquation Clone();

        /// <summary>
        /// Gets or sets the outermost operand of this equation.
        /// </summary>

        IOperand Operand { get; set; }

        /// <summary>
        /// Compiles the equation.
        /// </summary>

        void Compile();
    }

}
