using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using CsQuery;
using CsQuery.EquationParser;
using CsQuery.StringScanner;

// TODO this should be fully commented; however it's not part of the main public API

#pragma warning disable 1591
#pragma warning disable 1570


namespace CsQuery.EquationParser.Implementation
{
    /// <summary>
    /// An equation.
    /// </summary>

    public class Equation : Operand, IEquation
    {
        #region constructors

        public Equation()
        {
            Initialize();
        }
        public Equation(IOperand operand)
        {
            Initialize();
            Operand = operand;
        }
        protected virtual void Initialize()
        {
            _VariableValues = new OrderedDictionary<string, IConvertible>();
        }
        #endregion

        #region private properties
        private IOperand _Operand;
        private OrderedDictionary<string, IConvertible> _VariableValues;
        //xref of names in the order they appear

        /// <summary>
        /// The names of the variables in the order added. For functions (where the parameters are passed only by order)
        /// this is important. Probably could move this to the Function implementation
        /// but it requires overriding everything, almost easier to keep it here.
        /// </summary>

        #endregion

        #region public properties

        public IOrderedDictionary<string, IConvertible> VariableValues
        {
            get
            {
                return _VariableValues;
            }
        }

        /// <summary>
        /// The root operand for the equation. The equation must not be changed once set, or variables
        /// will not be bound.
        /// </summary>

        public IOperand Operand
        {
            get
            {
                return _Operand;
            }
            set
            {
                _Operand = value;
                VariableValues.Clear();
                if (value != null && value is IVariableContainer)
                {
                    foreach (IVariable variable in ((IVariableContainer)value).Variables)
                    {
                        AddVariable(variable);
                    }
                }
            }
        }

        /// <summary>
        /// The values set (on order that each variable appears first in the equation) for each varaiable
        /// </summary>
        
        public IEnumerable<IVariable> Variables
        {
            get
            {
                if (Operand is IVariableContainer)
                {
                    return ((IVariableContainer)Operand).Variables;
                }
                else
                {
                    return Utils.EmptyEnumerable<IVariable>();
                }
            }
        }


        #endregion

        #region public methods

        /// <summary>
        /// Makes a deep copy of this object.
        /// </summary>
        ///
        /// <returns>
        /// A copy of this object.
        /// </returns>

        public new IEquation Clone()
        {
            return (IEquation)CopyTo(GetNewInstance());
        }

        /// <summary>
        /// Compiles the equation.
        /// </summary>

        public void Compile()
        {
            if (Operand is IFunction)
            {
                ((IFunction)Operand).Compile();
            }
        }

        /// <summary>
        /// Execute the equation using existing variable data; if any errors occur, return false.
        /// </summary>
        ///
        /// <param name="result">
        /// [out] The result.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public bool TryGetValue(out IConvertible result)
        {
            try
            {
                result = Value;
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        /// <summary>
        /// Execute the equation using the values passed; if any errors occur, return false.
        /// </summary>
        ///
        /// <param name="result">
        /// [out] The result.
        /// </param>
        /// <param name="values">
        /// A variable-length parameters list containing values.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public virtual bool TryGetValue(out IConvertible result, params IConvertible[] values)
        {
            try
            {
                for (int i = 0; i < values.Length; i++)
                {
                    SetVariable(i, values[i]);
                }
                result = Value;
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        /// <summary>
        /// Sets the value used for a variable when the function is next run.
        /// </summary>
        ///
        /// <param name="name">
        /// The variable name
        /// </param>
        /// <param name="value">
        /// The value
        /// </param>

        public virtual void SetVariable(string name, IConvertible value)
        {
            // Setting a variable doesn't do anything directly, instead, it stores the value for use when it's accessed by the equation.
            // Each entity that makes up an equation has its own variable list - the objects used for "x" each time it appears are not the
            // same instance. This makes construction easier (otherwise, there would have to be an "owner" for each operand so they could
            // access an existing instance of same-named variable). So we get variable values from an event, rather than assigning them
            // to the objects. 
            //Variables.Where(item=> item.Name==name).Do(item=> {
            //    item.Clear();
            //});

            VariableValues[name] = value;
        }

        /// <summary>
        /// Sets the value used for a variable when the function is next run.
        /// </summary>
        ///
        /// <param name="index">
        /// Zero-based index of the.
        /// </param>
        /// <param name="value">
        /// .
        /// </param>

        public virtual void SetVariable(int index, IConvertible value)
        {
            if (VariableValues.Count == index)
            {
                SetVariable(VariableValues.Count.ToString(), value);
            }
            else
            {
                SetVariable(_VariableValues.Keys[index], value);
            }

        }

        /// <summary>
        /// Sets the value of a strongly-typed named variable.
        /// </summary>
        ///
        /// <typeparam name="U">
        /// The type of the variable.
        /// </typeparam>
        /// <param name="name">
        /// The name.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>

        public virtual void SetVariable<U>(string name, U value) where U : IConvertible
        {
            SetVariable(name, (IConvertible)value);
        }

        /// <summary>
        /// Get the value of this operand.
        /// </summary>
        ///
        /// <returns>
        /// The value.
        /// </returns>

        protected override IConvertible GetValue()
        {
            return Operand.Value;
        }

        /// <summary>
        /// Set the paramenters in order to the values passed, and returns the result of the equation
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        
        public IConvertible GetValue(params IConvertible[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                SetVariable(i, values[i]);
            }
            return Value;
        }

        public override string ToString()
        {
            return Operand.ToString();
        }
        #endregion

        #region protected methods
        protected override IOperand GetNewInstance()
        {
            return new Equation();
        }
        protected override IOperand CopyTo(IOperand operand)
        {
            IEquation target = (IEquation)operand;
            target.Operand = Operand.Clone();

            return operand;
        }

        protected void Variable_OnGetValue(object sender, VariableReadEventArgs e)
        {
            IConvertible value;
            if (VariableValues.TryGetValue(e.Name, out value))
            {
                e.Value = VariableValues[e.Name];
            }
            else
            {
                throw new InvalidOperationException("The value for variable '" + e.Name + "' was not set.");
            }

        }
        protected void AddVariable(IVariable variable)
        {
            if (!VariableValues.ContainsKey(variable.Name))
            {
                _VariableValues[variable.Name] = null;
            }
            variable.OnGetValue += new EventHandler<VariableReadEventArgs>(Variable_OnGetValue);
        }

        #endregion

        #region interface members
        IEnumerable<IVariable> IVariableContainer.Variables
        {
            get
            {
                return Variables;
            }
        }
        #endregion
    }
   
}
