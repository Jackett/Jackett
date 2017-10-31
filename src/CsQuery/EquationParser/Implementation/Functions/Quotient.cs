using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// TODO this should be fully commented; however it's not part of the main public API

#pragma warning disable 1591
#pragma warning disable 1570


namespace CsQuery.EquationParser.Implementation.Functions
{

    public class Quotient : NativeOperation, IOperation
    {
        public Quotient()
            : base("quotient")
        {

        }
        public Quotient(params IConvertible[] operands)
            : base("quotient", operands)
        {
        }

        public override AssociationType AssociationType
        {
            get { return AssociationType.Multiplicaton; }
        }
        protected override IOperand GetNewInstance()
        {
            return new Quotient();
        }
        protected override OperationType PrimaryOperator
        {
            get { return OperationType.Division; }
        }
        protected override OperationType ComplementaryOperator
        {
            get { return OperationType.Multiplication ; }
        }
    }
    public class Quotient<T> : Quotient, IFunction<T> where T : IConvertible
    {
        public Quotient()
            : base()
        { }
        public Quotient(params IConvertible[] operands)
            : base(operands)
        { }

        public new T Value
        {
            get { return (T)GetValue(); }
        }

        protected override IOperand GetNewInstance()
        {
            return new Quotient();
        }
        public new Quotient<T> Clone()
        {
            return (Quotient<T>)Clone();
        }

        IOperand<T> IOperand<T>.Clone()
        {
            return Clone();
        }
    }

}
