using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.StringScanner;
using CsQuery.StringScanner.Patterns;

namespace CsQuery.Engine
{
    /// <summary>
    /// Base class for any pseudoselector that implements validation of min/max parameter values, and
    /// argument validation. When implementing a pseudoselector, you must also implement an interface for the type
    /// of pseudoselector
    /// </summary>

    public abstract class PseudoSelector : IPseudoSelector
    {
        #region private properties

        private string _Arguments;
        
        /// <summary>
        /// Gets or sets criteria (or parameter) data passed with the pseudoselector
        /// </summary>

        protected virtual string[] Parameters {get;set;}

        /// <summary>
        /// A value to determine how to parse the string for a parameter at a specific index.
        /// </summary>
        ///
        /// <param name="index">
        /// Zero-based index of the parameter.
        /// </param>
        ///
        /// <returns>
        /// NeverQuoted to treat quotes as any other character; AlwaysQuoted to require that a quote
        /// character bounds the parameter; or OptionallyQuoted to accept a string that can (but does not
        /// have to be) quoted. The default abstract implementation returns NeverQuoted.
        /// </returns>

        protected virtual QuotingRule ParameterQuoted(int index)
        {
            return QuotingRule.NeverQuoted;
        }

        #endregion

        #region public properties

        /// <summary>
        /// This method is called before any validations are called against this selector. This gives the
        /// developer an opportunity to throw errors based on the configuration outside of the validation
        /// methods.
        /// </summary>
        ///
        /// <value>
        /// The arguments.
        /// </value>

        public virtual string Arguments
        {
            get
            {
                return _Arguments;
            }
            set
            {

                string[] parms=null;
                if (!String.IsNullOrEmpty(value))
                {
                    if (MaximumParameterCount > 1 || MaximumParameterCount < 0)
                    {
                        parms = ParseArgs(value);
                    }
                    else
                    {
                        parms = new string[] { ParseSingleArg(value) };
                    }

                    
                }
                ValidateParameters(parms);
                _Arguments = value;
                Parameters = parms;
                
            }
        }

        /// <summary>
        /// The minimum number of parameters that this selector requires. If there are no parameters, return 0
        /// </summary>
        ///
        /// <value>
        /// An integer
        /// </value>

        public virtual int MinimumParameterCount { get { return 0; } }

        /// <summary>
        /// The maximum number of parameters that this selector can accept. If there is no limit, return -1.
        /// </summary>
        ///
        /// <value>
        /// An integer
        /// </value>

        public virtual int MaximumParameterCount { get { return 0; } }

        /// <summary>
        /// Return the properly cased name of this selector (the class name in non-camelcase)
        /// </summary>

        public virtual string Name
        {
            get
            {
                return Utility.Support.FromCamelCase(this.GetType().Name);
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Parse the arguments using the rules returned by the ParameterQuoted method.
        /// </summary>
        ///
        /// <param name="value">
        /// The arguments
        /// </param>
        ///
        /// <returns>
        /// An array of strings
        /// </returns>

        protected string[] ParseArgs(string value)
        {
            List<string> parms = new List<string>();
            int index = 0;


            IStringScanner scanner = Scanner.Create(value);
           
            while (!scanner.Finished)
            {
                var quoting = ParameterQuoted(index);
                switch (quoting)
                {
                    case QuotingRule.OptionallyQuoted:
                        scanner.Expect(MatchFunctions.OptionallyQuoted(","));
                        break;
                    case QuotingRule.AlwaysQuoted:
                        scanner.Expect(MatchFunctions.Quoted());
                        break;
                    case QuotingRule.NeverQuoted:
                        scanner.Seek(',', true);
                        break;
                    default:
                        throw new NotImplementedException("Unimplemented quoting rule");
                }

                parms.Add(scanner.Match);
                if (!scanner.Finished)
                {
                    scanner.Next();
                    index++;
                }
                
            }
            return parms.ToArray();
        }

        /// <summary>
        /// Parse single argument passed to a pseudoselector
        /// </summary>
        ///
        /// <exception cref="ArgumentException">
        /// Thrown when one or more arguments have unsupported or illegal values.
        /// </exception>
        /// <exception cref="NotImplementedException">
        /// Thrown when the requested operation is unimplemented.
        /// </exception>
        ///
        /// <param name="value">
        /// The arguments.
        /// </param>
        ///
        /// <returns>
        /// The parsed string
        /// </returns>

        protected string ParseSingleArg(string value)
        {
            IStringScanner scanner = Scanner.Create(value);

            var quoting = ParameterQuoted(0);
            switch (quoting)
            {
                case QuotingRule.OptionallyQuoted:
                    scanner.Expect(MatchFunctions.OptionallyQuoted());
                    if (!scanner.Finished)
                    {
                        throw new ArgumentException(InvalidArgumentsError());
                    }
                    return scanner.Match;
                case QuotingRule.AlwaysQuoted:

                    scanner.Expect(MatchFunctions.Quoted());
                    if (!scanner.Finished)
                    {
                        throw new ArgumentException(InvalidArgumentsError());
                    }
                    return scanner.Match;
                case QuotingRule.NeverQuoted:
                    return value;
                default:
                    throw new NotImplementedException("Unimplemented quoting rule");
            }
        
        }

        /// <summary>
        /// Validates a parameter array against the expected number of parameters.
        /// </summary>
        ///
        /// <exception cref="ArgumentException">
        /// Thrown when the wrong number of parameters is passed.
        /// </exception>
        ///
        /// <param name="parameters">
        /// Criteria (or parameter) data passed with the pseudoselector.
        /// </param>

        protected virtual void ValidateParameters(string[] parameters) {

            if (parameters == null)
            {
                 if (MinimumParameterCount != 0) {
                     throw new ArgumentException(ParameterCountMismatchError());
                 } else {
                     return;
                 }
            }

            if ((parameters.Length < MinimumParameterCount ||
                    (MaximumParameterCount >= 0 &&
                        (parameters.Length > MaximumParameterCount))))
            {
                throw new ArgumentException(ParameterCountMismatchError());
            }

        }

        /// <summary>
        /// Gets the string for a parameter count mismatch error.
        /// </summary>
        ///
        /// <returns>
        /// A string to be used as an exception message.
        /// </returns>

        protected string ParameterCountMismatchError()
        {
            if (MinimumParameterCount == MaximumParameterCount )
            {
                if (MinimumParameterCount == 0)
                {
                    return String.Format("The :{0} pseudoselector cannot have arguments.",
                        Name);
                }
                else
                {
                    return String.Format("The :{0} pseudoselector must have exactly {1} arguments.",
                     Name,
                     MinimumParameterCount);
                }
            } else if (MaximumParameterCount >= 0)
            {
                return String.Format("The :{0} pseudoselector must have between {1} and {2} arguments.",
                    Name,
                    MinimumParameterCount,
                    MaximumParameterCount);
            }
            else
            {
                return String.Format("The :{0} pseudoselector must have between {1} and {2} arguments.",
                     Name,
                     MinimumParameterCount,
                     MaximumParameterCount);
            }
        }

        /// <summary>
        /// Get a string for an error when there are invalid arguments
        /// </summary>
        ///
        /// <returns>
        /// A string to be used as an exception message.
        /// </returns>

        protected string InvalidArgumentsError()
        {
            return String.Format("The :{0} pseudoselector has some invalid arguments.",
                        Name);
        }

        #endregion

    }
}
