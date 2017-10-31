using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.StringScanner.Implementation;

namespace CsQuery.StringScanner.Patterns
{
    /// <summary>
    /// A pattern that matches a number
    /// </summary>

    public class Number: ExpectPattern
    {
        /// <summary>
        /// Normally true, indicates that only legal whitespace can successfully terminate the number;
        /// other non-numeric characters will cause failure. If false, any non-numeric character will
        /// terminate successfuly.
        /// </summary>

        public bool RequireWhitespaceTerminator
        {
            get;
            set;
        }

        /// <summary>
        /// Initializes the pattern. This is called before any scanning begins.
        /// </summary>
        ///
        /// <param name="startIndex">
        /// The index within the source string to begin scanning.
        /// </param>
        /// <param name="sourceText">
        /// The source string.
        /// </param>

        public override void Initialize(int startIndex, char[] sourceText)
        {
             base.Initialize(startIndex, sourceText);
             decimalYet = false;
        }

        /// <summary>
        /// Run the validation against the passed string.
        /// </summary>
        ///
        /// <returns>
        /// Returns true if the pattern defined by this class is successfully matched, and false if not.
        /// </returns>

        public override bool Validate()
        {
            int index = StartIndex;
            while (index<Source.Length && Expect(ref index, Source[index]))
            {
                ;
            }
            EndIndex = index;
    
            // should not have passed the end
            if (EndIndex > Length || EndIndex == StartIndex || failed)
            {
                Result = "";
                return false;
            }
         
            Result = GetOuput(StartIndex, EndIndex, false,false);
            return true;
        }

        /// <summary>
        /// Internal flag that validation has failed
        /// </summary>

        protected bool failed = false;

        /// <summary>
        /// Internal flag indicating that a decimal point has appeared already and another would indicate
        /// failure or termination.
        /// </summary>

        protected bool decimalYet = false;

        /// <summary>
        /// Assert that the character at the current position matches the pattern
        /// </summary>
        ///
        /// <param name="index">
        /// [in,out] Zero-based index of the position
        /// </param>
        /// <param name="current">
        /// The current character
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        protected virtual bool Expect(ref int index, char current)
        {
            info.Target = current;
            if (index == StartIndex)
            {
                if (!info.Numeric && current!='-' && current!='+')
                {
                    failed=true;
                    return false;
                }
            }
            else
            {
                if (info.Whitespace || info.Operator)
                {
                    return false;
                } else if (current == '.') {
                    if (decimalYet)
                    {
                        failed = true;
                        return false;
                    }
                    else
                    {
                        decimalYet = true;
                    }
                } 
                else if (!info.Numeric)
                {
                    failed = RequireWhitespaceTerminator;
                    return false;
                } 
            }
            index++;
            return true;
        }
    }
}
