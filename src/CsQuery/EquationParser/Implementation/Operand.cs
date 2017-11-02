using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// TODO this should be fully commented; however it's not part of the main public API

#pragma warning disable 1591
#pragma warning disable 1570


namespace CsQuery.EquationParser.Implementation
{
    public abstract class Operand: IOperand
    {
        protected int intValue;
        protected double doubleValue;
        protected bool? _IsNumber;
        protected bool? _IsInt;
        protected bool? _IsText;
        protected bool? _IsBoolean;

        /// <summary>
        /// Abstract hooks for cloning. To allow more flexibility in inheriting part of the process
        /// (e.g. part of the code to copy the instance may be shared, but not instance-making code)
        /// it's split into two parts.
        /// </summary>
        /// <returns></returns>
        public IOperand Clone()
        {
            return CopyTo(GetNewInstance());
        }
        protected abstract IOperand GetNewInstance();
        protected abstract IOperand CopyTo(IOperand operand);

        /// <summary>
        /// Since it isn't possible to have compile-time type checking for the generic implementation beyond
        /// IConvertible, allow implementations to define the types that are valid
        /// </summary>
        protected virtual bool IsValidType(Type type)
        {
            return Utils.IsNumericType(type);
        }
        protected virtual bool AllowNullValues(Type type)
        {
            return Utils.IsNumericType(type);
        }
        //public IOperand<U> CloneAs<U>() where U: IConvertible
        //{
        //    return (IOperand<U>)CloneAsImpl<U>();
        //}
        //protected abstract IOperand<U> CloneAsImpl<U>() where U : IConvertible;

        public IConvertible Value
        {
            get
            {
                return GetValue();
            }
        }

        /// <summary>
        /// Get the value of this operand
        /// </summary>
        ///
        /// <returns>
        /// The value.
        /// </returns>

        protected abstract IConvertible GetValue();

        /// <summary>
        /// Indicates that this operand is either an integral type or contains an integral value. 
        /// That is, non-integral types containing integral values will still report true
        /// </summary>
        public virtual bool IsInteger
        {
            get
            {
                if (_IsInt != null)
                {
                    return (bool)_IsInt;
                }
                else
                {
                    return Utils.IsIntegralValue(Value);
                }
            }
        }
        public bool IsFloatingPoint
        {
            get
            {
                return IsNumber && !IsInteger;
            }
        }
        public bool IsNumber
        {
            get
            {
                if (_IsNumber != null)
                {
                    return (bool)_IsNumber;
                }
                else
                {
                    return Utils.IsNumericType(Value);
                }
            }
        }
        public bool IsText
        {
            get
            {
                if (_IsText != null)
                {
                    return (bool)_IsText;
                }
                else
                {
                    return Value is string;
                }
            }
        }
        public bool IsBoolean
        {
            get
            {
                if (_IsBoolean != null)
                {
                    return (bool)_IsBoolean;
                }
                else
                {
                    return Value is bool;
                }
            }
        }

        #region interface members
        
        public virtual TypeCode GetTypeCode()
        {
            return Value.GetTypeCode();
        }

        public virtual bool ToBoolean(IFormatProvider provider)
        {
            return intValue != 0;
        }

        public virtual byte ToByte(IFormatProvider provider)
        {
            if (intValue >= 0 || intValue < 255)
            {
                return (byte)intValue;
            }
            return ConversionException<byte>();
        }

        public virtual char ToChar(IFormatProvider provider)
        {
            if (intValue < 0 || intValue > 65535)
            {
                return (char)intValue;
            }
            return ConversionException<char>();
        }

        public virtual DateTime ToDateTime(IFormatProvider provider)
        {
            return ConversionException<DateTime>();
        }

        public virtual decimal ToDecimal(IFormatProvider provider)
        {
            return (decimal)doubleValue;
        }

        public virtual double ToDouble(IFormatProvider provider)
        {
            return doubleValue;
        }

        public virtual short ToInt16(IFormatProvider provider)
        {
            if (intValue < Int16.MinValue || intValue > Int16.MaxValue)
            {
                return ConversionException<Int16>();
            }
            return (Int16)intValue;
        }

        public virtual int ToInt32(IFormatProvider provider)
        {
            return intValue;
        }

        public virtual long ToInt64(IFormatProvider provider)
        {
            return (Int64)intValue;
        }

        public virtual sbyte ToSByte(IFormatProvider provider)
        {
            return (sbyte)Convert.ChangeType(intValue, typeof(sbyte));
        }

        public virtual float ToSingle(IFormatProvider provider)
        {
            return (float)Convert.ChangeType(doubleValue, typeof(sbyte));
        }

        public virtual string ToString(IFormatProvider provider)
        {
            return Value.ToString();
        }

        public virtual object ToType(Type conversionType, IFormatProvider provider)
        {
            return Convert.ChangeType(Value, conversionType);
        }

        public virtual ushort ToUInt16(IFormatProvider provider)
        {
            return (ushort)Convert.ChangeType(intValue, typeof(ushort));
        }

        public virtual uint ToUInt32(IFormatProvider provider)
        {
            return (uint)Convert.ChangeType(intValue, typeof(uint));
        }

        public virtual ulong ToUInt64(IFormatProvider provider)
        {
            return (ulong)Convert.ChangeType(intValue, typeof(ulong));
        }
        #endregion
        protected U ConversionException<U>()
        {
            throw new InvalidCastException("Cannot convert value '" + Value + "' to type " + typeof(U).ToString());
        }
    }

    public abstract class Operand<T> : Operand, IOperand<T> where T : IConvertible
    {
        public Operand()
        {
        }

        public new T Value { 
            get {
                return (T)GetValue(); 
            } 
        }

        public new abstract IOperand<T> Clone();

        public override string ToString()
        {
            return Value.ToString();
        }

        #region iconvertible members
        
        #endregion

        #region interface members
        IConvertible IOperand.Value
        {
            get
            {
                return Value;
            }
        }

        #endregion

    }
}
