using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using CsQuery.StringScanner;
using CsQuery.EquationParser.Implementation.Functions;

// TODO this should be fully commented; however it's not part of the main public API

#pragma warning disable 1591
#pragma warning disable 1570


namespace CsQuery.EquationParser.Implementation
{

    public class EquationParserEngine: IEquationParser
    {
        public EquationParserEngine()
        {
            
        }
        #region private members
        protected bool IsTyped { get; set; }
        protected HashSet<IVariable> _UniqueVariables;
        protected int CurPos;
        protected bool ParseEnd;
        protected IStringScanner scanner;

        protected HashSet<IVariable> UniqueVariables
        {
            get
            {
                if (_UniqueVariables == null)
                {
                    _UniqueVariables = new HashSet<IVariable>();
                }
                return _UniqueVariables;
            }
        }
       
        #endregion

        #region public properties
        protected IOperation Clause;
       
        /// <summary>
        /// Error (if any) that occurred while parsing
        /// </summary>
        public string Error { get; set; }
        
        
        #endregion

        #region public methods

        public bool TryParse(string text, out IOperand operand)
        {
            try
            {
                operand = Parse(text);
                return true;
            }
            catch (Exception e)
            {
                operand = null;
                Error = e.Message;
                Clause = null;
                return false;
            }
        }
        public IOperand Parse(string text) 
        {
            return Parse<IConvertible>(text);
        }
        public IOperand Parse<T>(string text) where T: IConvertible
        {
            IsTyped = typeof(T) != typeof(IConvertible);
            scanner = Scanner.Create(text);

            Clause = IsTyped ? 
                new Sum<T>() : 
                new Sum();

            // it could have just one operand
            IOperand lastOperand = GetOperand<T>();

            Clause.AddOperand(lastOperand);
            IOperation working = Clause;
            IOperand nextOperand = null;

            while (!ParseEnd)
            {
                IOperator op= GetOperation();
                
                nextOperand = GetOperand<T>();
                IOperation newOp;

                if (op.AssociationType == working.AssociationType)
                {
                    // working can only be sum/product
                    working.AddOperand(nextOperand, op.IsInverted);
                }
                else
                {
                    switch (op.AssociationType)
                    {
                        case AssociationType.Addition:
                            // always return to the root when adding
                            if (!ReferenceEquals(working, Clause))
                            {
                                working = Clause;
                            }
                            working.AddOperand(nextOperand, op.IsInverted);
                            break;
                        case AssociationType.Multiplicaton:
                            //"steal" last operand from Clause, and change working to the new op
                            newOp = op.GetFunction();
                            
                            newOp.AddOperand(lastOperand);
                            newOp.AddOperand(nextOperand, op.IsInverted);
                            Clause.ReplaceLastOperand(newOp);
                            working = newOp;
                            break;
                        case AssociationType.Power:
                            // Similar to Multiplication, but does not change the active chain to the new operation. It can never be added to.
                            newOp = op.GetFunction();

                            newOp.AddOperand(lastOperand);
                            newOp.AddOperand(nextOperand, op.IsInverted);
                            Clause.ReplaceLastOperand(newOp);
                            break;
                        case AssociationType.Function:
                            // Similar to Multiplication, but does not change the active chain to the new operation. It can never be added to.
                            newOp = op.GetFunction();
                            newOp.AddOperand(nextOperand, op.IsInverted);
                            Clause.ReplaceLastOperand(newOp);
                            break;
                        default:
                            throw new NotImplementedException("Unknown association type.");
                    }
                }
                lastOperand = nextOperand;
                
            }
            Error = "";
            
            return (IOperand)Clause;
        }
        #endregion

        #region private methods
        protected IOperand GetOperand<T>() where T: IConvertible
        {
            string text="";
            IOperand output=null;
            scanner.SkipWhitespace();
            
            if (scanner.Current == '-')
            {
                // convert leading - to "-1" if it precedes a variable, otherwise 
                // just add it to the output stream

                scanner.Next();
                if (scanner.Finished)
                {
                    throw new ArgumentException("Unexpected end of string found, expected an operand (a number or variable name)");
                }
                if (CharacterData.IsType(scanner.Current,CharacterType.Number)) {
                    text+="-";
                } else {
                    output = new Literal<T>(-1);
                }

            } 
            else if (scanner.Current == '+')
            {
                // ignore leading +

                scanner.Next();
            }

            if (output==null)
            {
                if (scanner.Info.Numeric)
                {
                    text += scanner.Get(MatchFunctions.Number());
                    double num;
                    if (Double.TryParse(text, out num))
                    {
                        output = IsTyped ? new Literal<T>(num) : new Literal(num);
                    }
                    else
                    {
                        throw new InvalidCastException("Unable to parse number from '" + text + "'");
                    }
                }
                else if (scanner.Info.Alpha)
                {
                    text += scanner.GetAlpha();
                    if (scanner.CurrentOrEmpty == "(")
                    {
                        IFunction func = Utils.GetFunction<T>(text);

                        var inner = scanner.ExpectBoundedBy('(', true).ToNewScanner("{0},");

                        while (!inner.Finished)
                        {
                            string parm = inner.Get(MatchFunctions.BoundedBy(boundEnd: ","));
                            EquationParserEngine innerParser = new EquationParserEngine();

                            IOperand innerOperand = innerParser.Parse<T>(parm);
                            func.AddOperand(innerOperand);
                        }
                        CacheVariables(func);
                        output = func;

                    }
                    else
                    {
                        IVariable var = GetVariable<T>(text);
                        output = var;
                    }
                }
                else if (scanner.Current == '(')
                {
                    string inner = scanner.Get(MatchFunctions.BoundedBy("("));
                    var parser = new EquationParserEngine();
                    parser.Parse<T>(inner);
                    output = parser.Clause;
                    CacheVariables(output);
                }
                else
                {
                    throw new ArgumentException("Unexpected character '" + scanner.Match + "' found, expected an operand (a number or variable name)");
                }
            }

            scanner.SkipWhitespace();
            ParseEnd = scanner.Finished;

            return output;

        }

        protected IOperator GetOperation()
        {

            IOperator output;
            if (scanner.Info.Alpha || scanner.Current == '(')
            {
                output = new Operator("*");
            }
            else
            {
                output = new Operator(scanner.Get(MatchFunctions.Operator));
            }
            return output;
        }
        protected IVariable GetVariable<T>(string name) where T: IConvertible
        {
            IVariable output;
            output = UniqueVariables.FirstOrDefault(item => item.Name == name);

            if (output==null)
            {
                var variable = IsTyped ? new Variable<T>(name) : new Variable(name);
                output = variable;
                CacheVariables(output);
            }

            return output;
        }

        protected void CacheVariables(IOperand oper)
        {
            if (oper is IVariableContainer)
            {
                foreach (var item in ((IVariableContainer)oper).Variables)
                {
                    UniqueVariables.Add(item);
                }
            }
        }
        #endregion

        #region interface members


        #endregion
    }
}
