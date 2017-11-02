using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// TODO this should be fully commented; however it's not part of the main public API

#pragma warning disable 1591
#pragma warning disable 1570


namespace CsQuery.EquationParser.Implementation
{
    public class Literal: Operand, ILiteral
    {
        #region constructors
        public Literal(): base()
        {

        }
        public Literal(IConvertible value)
            : base()
        {
            Set(value);
        }

        //public static implicit operator Literal(IConvertible value)
        //{
        //    return new Literal(value);
        //}
        #endregion

        #region private properties
        protected IConvertible _Value;
        #endregion
     
        #region public methods
        public new ILiteral Clone()
        {
            return (ILiteral)base.Clone();
        }

        protected override IOperand GetNewInstance()
        {
            return new Literal();
        }
        protected override IOperand CopyTo(IOperand operand)
        {
            ((Literal)operand).Set(GetValue());
            return operand;
        }
        protected override IConvertible GetValue()
        {
            return _Value;
        }
        public override string ToString()
        {
            return Value.ToString();
        }
        #endregion

        #region private methods

        public virtual void Set(IConvertible value)
        {
            _Value = value;
        }

        #endregion

        #region interface members
        //void ILiteral.Set(IConvertible value)
        //{
        //    Set((T)Convert.ChangeType(value, typeof(T)));
        //}
        #endregion
    }

    public class Literal<T>: Literal, ILiteral<T> where T : IConvertible
    {
        #region constructors
        public Literal(): base()
        {

        }
        public Literal(IConvertible value)
            : base()
        {
            SetConvert(value);
        }

        public static implicit operator Literal<T>(int value)
        {
            return new Literal<T>(value);
        }
        public static implicit operator Literal<T>(double value)
        {
            return new Literal<T>(value);
        }
        public static implicit operator Literal<T>(string value)
        {
            return new Literal<T>(value);
        }
        #endregion

        #region private properties
        
        #endregion

        #region public properties
        public new T Value {
            get
            {
                return (T)_Value;
            }
        }

        public new ILiteral<T> Clone()
        {
            return (ILiteral<T>)CopyTo(GetNewInstance());
        }
        public void Set(T value)
        {
            _Value = value;
        }
        #endregion

        #region private methods
        protected override IOperand GetNewInstance()
        {
            return new Literal<T>(Value);
        }

        /// <summary>
        /// This is static so it can be used by the constructors -- sets the value of the strongly typed
        /// instance.
        /// </summary>
        ///
        /// <param name="value">
        /// The value to set
        /// </param>

        private void SetConvert(IConvertible value)
        {

            Set((T)Convert.ChangeType(value, typeof(T)));
        }

        #endregion

        #region interface members
        IOperand<T> IOperand<T>.Clone()
        {
            return Clone();
        }
        void ILiteral.Set(IConvertible value)
        {
            Set((T)Convert.ChangeType(value,typeof(T)));
        }
        #endregion
    }
}
