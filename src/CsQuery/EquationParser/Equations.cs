using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.EquationParser.Implementation;

// TODO: this should be fully commented; however it's not part of the main public API so the XMLDOC warnings
// have been disabled to make compiling clean

#pragma warning disable 1591
#pragma warning disable 1570


namespace CsQuery.EquationParser
{
    /// <summary>
    /// Factory class for creating equation objects.
    /// </summary>

    public static class Equations
    {
        //public static Operator CreateOperator(string oper) {
        //    return new Operator(oper);
        //}
        public static Equation<T> CreateEquation<T>() where T : IConvertible
        {
            return new Equation<T>();
        }
        public static Equation<T> CreateEquation<T>(IOperand operand) where T : IConvertible
        {
            return new Equation<T>(operand);
        }

        /// <summary>
        /// Creates an equation returning a specic type from a string
        /// </summary>
        ///
        /// <typeparam name="T">
        /// The type of value returned by the equation.
        /// </typeparam>
        /// <param name="text">
        /// The text of the equation.
        /// </param>
        ///
        /// <returns>
        /// The new equation&lt; t&gt;
        /// </returns>

        public static Equation<T> CreateEquation<T>(string text) where T : IConvertible
        {
            IEquationParser parser = new EquationParser.Implementation.EquationParserEngine();
            IOperand operand = parser.Parse<T>(text);

            Equation<T> equation = new Equation<T>(operand);
            return equation;
        }

        /// <summary>
        /// Creates a new equation from a string.
        /// </summary>
        ///
        /// <param name="text">
        /// The text of the equation
        /// </param>
        ///
        /// <returns>
        /// The new equation.
        /// </returns>

        public static Equation CreateEquation(string text)
        {
            IEquationParser parser = new EquationParser.Implementation.EquationParserEngine();
            IOperand operand = parser.Parse(text);

            Equation equation = new Equation(operand);
            return equation;
        }

        /// <summary>
        /// Create an operand by parsing a string. Like CreateEquation but does not wrap in an Equation
        /// object.
        /// </summary>
        ///
        /// <param name="text">
        /// The operand text
        /// </param>
        ///
        /// <returns>
        /// The new equation operand.
        /// </returns>

        public static Equation CreateEquationOperand(string text)
        {
            IEquationParser parser = new EquationParser.Implementation.EquationParserEngine();
            IOperand operand = parser.Parse(text);

            Equation equation = new Equation(operand);
            return equation;
        }
        public static IOperand CreateOperand(IConvertible value)
        {
            if (value is IOperand)
            {
                return (IOperand)value;
            } else if (value is string) {
                //TODO: parse quotes and return a string liteeral if need be
                return CreateVariable((string)value);
            } else  {
                return CreateLiteral(value);
            }
        }
        public static IOperand CreateOperand<T>(T value) where T: IConvertible
        {
            if (value is IOperand)
            {
                return (IOperand)value;
            }
            else if (value is string)
            {
                return CreateVariable<T>((string)(object)value);
            }
            else
            {
                return CreateLiteral<T>(value);
            }
        }
        public static IVariable CreateVariable<T>(string name) where T: IConvertible
        {
            return new Variable<T>(name);
        }
        public static IVariable CreateVariable(string name) 
        {
            return new Variable(name);
        }
        public static ILiteral CreateLiteral<T>(IConvertible value) where T : IConvertible
        {
            return new Literal<T>(value);
        }
        public static ILiteral CreateLiteral(IConvertible value)
        {
            if (Utils.IsText(value))
            {
                return new Literal<string>(value.ToString());
            }
            if (Utils.IsIntegralType(value))
            {
                return new Literal<int>(Convert.ToInt64(value));
            }
            else
            {
                return new Literal<double>(Convert.ToDouble(value));
            }
        }
        //public static IClause CreateClause<T>(IConvertible operandA, IConvertible operandB, IOperator oper) where T: IConvertible
        //{
        //    return new Clause<T>(operandA,operandB, oper);
        //}
        //public static IClause CreateClause<T>(IConvertible operandA, IConvertible operandB, string oper) where T : IConvertible
        //{
        //    return new Clause<T>(operandA, operandB, oper);
        //}

        public static bool TryCreate(IConvertible value, out IOperand operand)
        {
            try
            {
                operand = CreateOperand(value);
                return true;
            }
            catch
            {
                operand = null;
                return false;
            }
        }

        public static bool TryCreateEquation<T>(string text, out Equation<T> equation) where T : IConvertible
        {
            try
            {
                equation = CreateEquation<T>(text);
                return true;
            }
            catch
            {
                equation = null;
                return false;
            }
        }

    }
}
