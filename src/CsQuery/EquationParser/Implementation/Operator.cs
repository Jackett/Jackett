using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// TODO this should be fully commented; however it's not part of the main public API

#pragma warning disable 1591
#pragma warning disable 1570


namespace CsQuery.EquationParser.Implementation
{
    public class Operator : IOperator
    {
        #region constructors
        public Operator()
        {

        }
        public Operator(string op)
        {
            Set(op);
        }
        public Operator(OperationType op)
        {
            _OperationType = op;
        }
        public static implicit operator Operator(string op)
        {
            return new Operator(op);
        }
        #endregion

        #region private members
        // The order must match the enum

        protected static List<string> _Operators = new List<string>(new string[] { "+", "-", "*", "/", "%", "^" });

        public static IEnumerable<string> Operators
        {
            get
            {
                return _Operators;
            }
        }
        protected static HashSet<string> ValidOperators = new HashSet<string>(Operators);

        protected OperationType _OperationType;

        #endregion

        #region public properties

        public bool IsInverted
        {
            get
            {
                return (OperationType == OperationType.Subtraction
                    || OperationType == OperationType.Division);
            }
        }
        public AssociationType AssociationType
        {
            get
            {
                switch (OperationType)
                {
                    case OperationType.Addition:
                    case OperationType.Subtraction:
                        return AssociationType.Addition;
                    case OperationType.Multiplication:
                    case OperationType.Division:
                        return AssociationType.Multiplicaton;
                    case OperationType.Power:
                    case OperationType.Modulus:
                        return AssociationType.Power;
                    default:
                        throw new NotImplementedException("Unknown operation type, can't determine association");
                }
            }
        }

        public OperationType OperationType
        {
            get { return _OperationType; }
        }
        #endregion

        #region public methods
        /// <summary>
        /// Return the fuction class for this type of operator
        /// </summary>
        /// <returns></returns>
        public IOperation GetFunction()
        {
            switch (OperationType)
            {
                case OperationType.Addition:
                case OperationType.Subtraction:
                    return new Functions.Sum();
                case OperationType.Multiplication:
                case OperationType.Division:
                    return new Functions.Product();
                case OperationType.Power:
                    return new Functions.Power();
                default:
                    throw new NotImplementedException("Not yet supported");
            }
        }
        public void Set(string op)
        {
            if (!TrySet(op))
            {
                throw new ArgumentException("'" + op + "' is not a valid operator.");
            }
        }

        public bool TrySet(string value)
        {
            if (!ValidOperators.Contains(value))
            {
                return false;
            }
            switch (value)
            {
                case "+":
                    _OperationType = OperationType.Addition;
                    break;
                case "-":
                    _OperationType = OperationType.Subtraction;
                    break;
                case "*":
                    _OperationType = OperationType.Multiplication;
                    break;
                case "/":
                    _OperationType = OperationType.Division;
                    break;
                case "^":
                    _OperationType = OperationType.Power;
                    break;
                case "%":
                    _OperationType = OperationType.Modulus;
                    break;

            }
            return true;
        }

        public IOperator Clone()
        {
            return new Operator(OperationType);
        }
        public override string ToString()
        {
            return _Operators[((int)OperationType)-1];
        }
        #endregion

        #region interface members

        #endregion
    }
}
