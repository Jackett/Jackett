using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// TODO this should be fully commented; however it's not part of the main public API

#pragma warning disable 1591
#pragma warning disable 1570


namespace CsQuery.EquationParser.Implementation
{
    /// <summary>
    /// An equation that returns a particular type.
    /// </summary>
    ///
    /// <typeparam name="T">
    /// The type of value returned.
    /// </typeparam>

    public class Equation<T> : Equation, IEquation<T> where T : IConvertible
    {
        public Equation()
        {

        }
        public Equation(IConvertible operand)
        {
            Operand = Utils.EnsureOperand(operand);
        }
        #region public methods

        public new IEquation<T> Clone()
        {
            return (IEquation<T>)CopyTo(GetNewInstance());
        }
        public IEquation<U> CloneAs<U>() where U : IConvertible
        {
            return (IEquation<U>)CloneAsImpl<U>();
        }


        public new T GetValue(params IConvertible[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                SetVariable(i, values[i]);
            }
            return Value;
        }
        public bool TryGetValue(out T result)
        {

            IConvertible untypedResult;
            if (base.TryGetValue(out untypedResult))
            {
                result = (T)Convert.ChangeType(untypedResult, typeof(T));
                return true;
            }
            else
            {
                result = default(T);
                return false;
            }

        }

        public virtual bool TryGetValue(out T result, params IConvertible[] values)
        {
            try
            {
                for (int i = 0; i < values.Length; i++)
                {
                    SetVariable(i, values[i]);
                }
                result = (T)Convert.ChangeType(Value, typeof(T));
                return true;
            }
            catch
            {
                result = default(T);
                return false;
            }
        }


        public new T Value
        {
            get
            {
                return (T)Convert.ChangeType(Operand.Value, typeof(T));
            }
        }

        public override string ToString()
        {
            return Operand == null ? "" : Operand.ToString();
        }
        #endregion
        #region private methods
        protected IOperand<U> CloneAsImpl<U>() where U : IConvertible
        {
            Equation<U> clone = new Equation<U>();
            CopyTo(clone);
            return clone;
        }

        protected override IOperand GetNewInstance()
        {
            return new Equation<T>();
        }
        protected override IOperand CopyTo(IOperand operand)
        {
            CopyTo((IEquation)operand);
            return operand;
        }
        #endregion

        #region Interface members
        IConvertible IEquation.GetValue(params IConvertible[] values)
        {
            return GetValue(values);
        }
        bool IEquation.TryGetValue(out IConvertible value)
        {
            T typedValue;
            if (TryGetValue(out typedValue))
            {
                value = typedValue;
                return true;
            }
            else
            {
                value = default(T);
                return false;
            }
        }
        bool IEquation.TryGetValue(out IConvertible value, params IConvertible[] variableValues)
        {
            T typedValue;
            if (TryGetValue(out typedValue, variableValues))
            {
                value = typedValue;
                return true;
            }
            else
            {
                value = default(T);
                return false;
            }
        }

        IOperand<T> IOperand<T>.Clone()
        {
            return Clone();
        }
        IEquation IEquation.Clone()
        {
            return Clone();
        }
        #endregion
    }
}
