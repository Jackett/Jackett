using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.EquationParser.Implementation;

// TODO this should be fully commented; however it's not part of the main public API

#pragma warning disable 1591
#pragma warning disable 1570


namespace CsQuery.EquationParser
{
    // TODO:  unimplemented
    // Add/subtract/multiply etc should just be functions with two arguments. Operator should just
    // return a function.
    public interface IFunction: IOperand, IVariableContainer
    {
        /// <summary>
        /// The name of this variable
        /// </summary>
        string Name { get; }
        AssociationType AssociationType { get; }
        int RequiredParmCount { get; }
        int MaxParmCount { get; }
        IList<IOperand> Operands { get; }
        void AddOperand(IConvertible operand);
        void Compile();
        //IOperand StealLastOperand();
    }
    /// <summary>
    /// T is the output type of the function.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IFunction<T> : IOperand<T>, IFunction where T : IConvertible
    {
       
    }

}
