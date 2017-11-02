using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// TODO this should be fully commented; however it's not part of the main public API

#pragma warning disable 1591
#pragma warning disable 1570


namespace CsQuery.EquationParser
{
    public interface IOperator 
    {
        void Set(string value);
        bool TrySet(string value);
        OperationType OperationType { get; }
        AssociationType AssociationType { get; }
        bool IsInverted { get; }
        IOperator Clone();

        IOperation GetFunction();
    }
}
