using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// TODO this should be fully commented; however it's not part of the main public API

#pragma warning disable 1591
#pragma warning disable 1570


namespace CsQuery.EquationParser.Implementation
{

    public class Variable : Operand, IVariable
    {
        #region constructors
        public Variable()
        {

        }
        public Variable(string name)
        {
            Name=name;
        }
        #endregion

        #region private members
        /// <summary>
        /// The value has been obtained. When true, the cached value will be used instead of requerying. Clear() resets this.
        /// </summary>
        //protected bool isGotValue=false;
        protected IConvertible _Value;
        #endregion

        #region public properties
        public event EventHandler<VariableReadEventArgs> OnGetValue;

        public string Name { get; set; }

        public Type Type
        {
            get
            {
                return _Value == null ? typeof(IConvertible) : _Value.GetType();
            }
        }

        public new IConvertible Value
        {
            get
            {
                return GetValue();
            }
            set
            {
                _Value = value;
            }
        }
        protected override IConvertible GetValue()
        {
           // if (!isGotValue)
           // {
                if (OnGetValue == null)
                {
                    throw new InvalidOperationException("This variable is not bound to a formula, so it's value cannot be read.");
                }
                VariableReadEventArgs args = new VariableReadEventArgs(Name);
                args.Type = Type;
                OnGetValue(this, args);

                if (Type != typeof(IConvertible))
                {
                    _Value = (IConvertible)Convert.ChangeType(args.Value, Type);
                }
                else
                {
                    _Value = args.Value;
                }
              //  isGotValue = true;
         //   }
            return _Value;
            
        }

        #endregion

        #region public methods
        public new IVariable Clone()
        {
            return (IVariable)CopyTo(GetNewInstance());
        }

        protected override IOperand GetNewInstance()
        {
            return new Variable(Name);
        }
        protected override IOperand CopyTo(IOperand operand)
        {
            return operand;   
        }

        //public void Clear()
        //{
        //    isGotValue = false;
        //}

        public override string ToString()
        {
            return Name;
            //return Name + (Value!=null ? "="+Value.ToString() : "");

        }
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
        public override bool Equals(object obj)
        {  
            IVariable var = obj as IVariable;
            return obj == null ? false : var.Name == Name;
        }
        public IEnumerable<IVariable> Variables
        {
            get
            {
                yield return this;
            }
        }
        #endregion

        #region interface members
        IConvertible IOperand.Value
        {
            get { return Value; }
        }

        IVariable IVariable.Clone()
        {
            return Clone();
        }
        #endregion

    }
    public class Variable<T> : Variable, IVariable<T> where T: IConvertible 
    {
        #region constructors
        public Variable()
        {

        }
        public Variable(string name)
        {
            Name = name;
        }
        #endregion

        public new IVariable<T> Clone()
        {
            return (IVariable<T>)base.Clone();
        }

        public IVariable<U> CloneAs<U>() where U : IConvertible
        {
            throw new NotImplementedException();
        }

        public new T Value
        {
            get
            {
                return (T)base.Value;
            }
            set
            {
                base.Value = value;
            }
        }

        IOperand<T> IOperand<T>.Clone()
        {
            return Clone();
        }
    }
}
