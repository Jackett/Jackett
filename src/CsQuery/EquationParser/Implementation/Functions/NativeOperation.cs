using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// TODO this should be fully commented; however it's not part of the main public API

#pragma warning disable 1591
#pragma warning disable 1570


namespace CsQuery.EquationParser.Implementation.Functions
{
    public abstract class NativeOperation: Function, IOperation, INativeOperation
    {
        public NativeOperation(string name)
            : base(name)
        {

        }
        public NativeOperation(string name, params IConvertible[] operands)
            : base(name)
        {
            foreach (var operand in operands)
            {
                AddOperand(Equations.CreateOperand(operand));
            }
        }

        protected abstract OperationType ComplementaryOperator
        { get; }

        protected abstract OperationType PrimaryOperator
        { get; }

        public override bool IsInteger
        {
            get
            {
                return base.IsInteger;
            }
        }
        protected List<OperationType> _Operators = new List<OperationType>();

        public IList<OperationType> Operators
        {
            get { return _Operators.AsReadOnly(); }
        }
        protected override IOperand CopyTo(IOperand operand)
        {
            NativeOperation target = (NativeOperation)operand;
            foreach (var item in _Operators)
            {
                target._Operators.Add(item);
            }
            return base.CopyTo(target);
        }

        public override void AddOperand(IConvertible operand)
        {
            base.AddOperand(Utils.EnsureOperand(operand));
            _Operators.Add(PrimaryOperator);
        }
        public virtual void AddOperand(IConvertible operand, bool invert)
        {
            base.AddOperand(Utils.EnsureOperand(operand));
            _Operators.Add(invert ? ComplementaryOperator  : PrimaryOperator);
        }

        public void ReplaceLastOperand(IOperand operand)
        {
            if (Operands.Count == 0)
            {
                throw new InvalidOperationException("There are no operands to replace.");
            }


            int stealIndex = Operands.Count - 1;
            
            NativeOperation op = Operands[stealIndex] as NativeOperation;

            // Check if the "steal" target is itself something that we can steal from, if so, 
            // recurse. This rule of checkiung association type is a bit confusing, this should
            // be a property?

            if (op !=null && op.Operands.Count > 1 && op.AssociationType == AssociationType.Multiplicaton)
            {
                IOperation func = (IOperation)op;
                func.ReplaceLastOperand(operand);
            }
            else
            {
                _Operands[Operands.Count - 1] = operand;
            }
        }
        
        public override int RequiredParmCount
        {
            get { return 1; }
        }

        public override int MaxParmCount
        {
            get { return 1; }
        }

        public override string ToString()
        {
            string output = WrapParenthesis(Operands[0]);
            for (int i = 1; i < Operands.Count; i++)
            {
                output +=  OperationTypeName(Operators[i]) + WrapParenthesis(Operands[i]);           
            }
            return output;
        }

        #region protected methods

        protected override IConvertible GetValue()
        {
            return GetValueDouble();
        }
        protected double GetValueDouble()
        {
            double value = Convert.ToDouble(Operands[0].Value); ;
            for (var i = 1; i < Operands.Count; i++)
            {
                double valueN = Convert.ToDouble(Operands[i].Value);
                switch (Operators[i])
                {
                    case OperationType.Addition:
                        value += valueN;
                        break;
                    case OperationType.Subtraction:
                        value -= valueN;
                        break;
                    case OperationType.Multiplication:
                        value *= valueN;
                        break;
                    case OperationType.Division:
                        value /= valueN;
                        break;
                    case OperationType.Power:
                        value = Math.Pow(value, valueN);
                        break;
                    case OperationType.Modulus:
                        value %= valueN;
                        break;
                }
            }
            return value;
        }
        protected long GetValueLong()
        {
            long value = Convert.ToInt64(Operands[0].Value); ;
            for (var i = 1; i < Operands.Count; i++)
            {
                long valueN = Convert.ToInt64(Operands[i].Value);
                switch (Operators[i])
                {
                    case OperationType.Addition:
                        value += valueN;
                        break;
                    case OperationType.Subtraction:
                        value -= valueN;
                        break;
                    case OperationType.Multiplication:
                        value *= valueN;
                        break;
                    case OperationType.Division:
                        value /= valueN;
                        break;
                    case OperationType.Power:
                        value = (long)Math.Pow((double)Convert.ToDouble(value), (double)Convert.ToDouble(valueN));
                        break;
                    case OperationType.Modulus:
                        value %= valueN;
                        break;
                }
            }
            return value;

        }
        protected string WrapParenthesis(IOperand operand)
        {
            INativeOperation oper = operand as INativeOperation;
            if (oper != null && oper.Operands.Count > 1 && oper.AssociationType == AssociationType.Addition)
            {
                return "(" + operand.ToString() + ")";
            }
            else
            {
                return operand.ToString();
            }
        }


        private string OperationTypeName(OperationType operationType)
        {
            return "+-*/%^".Substring((int)operationType-1,1).ToString();
        }
        #endregion
    }
    public abstract class NativeOperation<T> : NativeOperation, IFunction<T> where T: IConvertible
    {
        public NativeOperation(string name)
            : base(name)
        {

        }
        public NativeOperation(string name, params IConvertible[] operands)
            : base(name)
        {
         
        }
        protected void Initialize() {
            _IsInteger = Utils.IsIntegralType<T>();
        }

        protected override IConvertible GetValue()
        {
            Type type = typeof(T);
            if (type == typeof(long))
            {
                return GetValueLong();
            }
            else if (type == typeof(double))
            {
                return GetValueDouble();
            }
            else
            {
                IConvertible val =  IsInteger ? GetValueLong() : GetValueDouble();
                return (T)Convert.ChangeType(val,typeof(T));
            }
        }

        protected bool _IsInteger;
        public override bool IsInteger
        {
            get
            {
                return _IsInteger;
            }
        }
        IOperand<T> IOperand<T>.Clone()
        {
            return (IOperand<T>)Clone();
        }
        T IOperand<T>.Value
        {
            get
            {
                return (T)base.Value;
            }
        }
    }
}
