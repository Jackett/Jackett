using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// TODO: this should be fully commented; however it's not part of the main public API

#pragma warning disable 1591
#pragma warning disable 1570


namespace CsQuery.EquationParser
{
    /// <summary>
    /// Interface for an equation clause.
    /// </summary>

    public interface IClause : IFunction, IVariableContainer
    {

        ///// <summary>
        ///// Builds an expression by returning the open clause: either the same clause, with the first 
        ///// operand replace by a new clause containing (oldOpA, operandB, op), or a new clause that
        ///// has been added as OperandB to the previous clause. The operand returned is based on the
        ///// associativity of the operator, and will result in a correctly constructed association chain.
        ///// </summary>
        ///// <param name="operandB">The new operand</param>
        ///// <param name="op">The operator</param>
        ///// <returns></returns>
        //IClause Chain(IOperand operandB, IOperator op);
        //IClause Chain(IOperand operandB, string op);
        //IClause Chain(IConvertible operandB, string op);
        
        new IClause Clone();
    }
    //public interface IClause<T> : IFunction<T>, IClause where T : IConvertible, IEquatable<T>
    //{
    //    //new IClause<T> Chain(IOperand operandB, IOperator op);
    //    //new IClause<T> Chain(IOperand operandB, string op);
    //    //new IClause<T> Chain(IConvertible operandB, string op);
    //    new IClause<T> Clone();
    //}
}
