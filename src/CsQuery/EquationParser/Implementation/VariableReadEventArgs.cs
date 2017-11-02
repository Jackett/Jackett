using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// TODO this should be fully commented; however it's not part of the main public API

#pragma warning disable 1591
#pragma warning disable 1570

namespace CsQuery.EquationParser.Implementation
{
    public class VariableReadEventArgs : EventArgs
    {
        public VariableReadEventArgs(string name)
        {
            Name = name;
        }
        public IConvertible Value
        {
            get
            {
                return _Value;
            }
            set
            {
                _Value = value;
            }
        }
        protected IConvertible _Value;

        public Type Type
        {
            get;
            set;
        }
        public string Name
        {
            get;
            protected set;
        }
    }
}
