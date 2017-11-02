using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// TODO this should be fully commented; however it's not part of the main public API

#pragma warning disable 1591
#pragma warning disable 1570


namespace CsQuery.EquationParser
{
    public interface IVariableContainer
    {
        IEnumerable<IVariable> Variables {get;}
    }
}
