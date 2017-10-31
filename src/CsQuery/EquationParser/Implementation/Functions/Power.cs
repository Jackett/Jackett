using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// TODO this should be fully commented; however it's not part of the main public API

#pragma warning disable 1591
#pragma warning disable 1570


namespace CsQuery.EquationParser.Implementation.Functions
{

    public class Power : NativeOperation, IOperation
    {
        public Power()
            : base("power")
        {

        }
        public Power(IOperand operand1, IOperand operand2)
            : base("power")
        {
            AddOperand(operand1);
            AddOperand(operand2);
        }
        
        public override int RequiredParmCount
        {
            get { return 2; }
        }

        public override int MaxParmCount
        {
            get { return 2; }
        }

        public override AssociationType AssociationType
        {
            get { return AssociationType.Power; }
        }

        protected override IOperand GetNewInstance()
        {
            return new Power();
        }



        protected override OperationType ComplementaryOperator
        {
            get { return 0; }
        }

        protected override OperationType PrimaryOperator
        {
            get { return OperationType.Power; }
        }
    }

}
