using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// TODO this should be fully commented; however it's not part of the main public API

#pragma warning disable 1591
#pragma warning disable 1570


namespace CsQuery.EquationParser
{
    public interface IEquationParser
    {
        bool TryParse(string text, out IOperand operand);
        IOperand Parse(string text);
        IOperand Parse<T>(string text) where T : IConvertible;
        string Error { get; }
    }
    public interface IEquationParser<T> : IEquationParser where T : IConvertible
    {
        bool TryParse(string text, out IOperand<T> operand);
        new IOperand<T> Parse(string text);
    }
}
