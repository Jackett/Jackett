using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.StringScanner.Implementation;

namespace CsQuery.StringScanner.Patterns
{
    /// <summary>
    /// A pattern that expects a quoted string. Allows any characters inside the quoted text,
    /// including backslashed escape characters, and terminates upon a matching closing quote.
    /// </summary>

    public class Quoted: ExpectPattern
    {

        /// <summary>
        /// The quote character that was used to open the string.
        /// </summary>
        
        char quoteChar;

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
            if (EndIndex > Length || EndIndex == StartIndex)
            {
                Result = "";
                return false;
            }
            return FinishValidate();
        }

        /// <summary>
        /// Finishes a validation
        /// </summary>
        ///
        /// <returns>
        /// true if the string matched the pattern defined by this instance, false if not.
        /// </returns>

        protected virtual bool FinishValidate(){ 
            //return the substring excluding the quotes
            Result = GetOuput(StartIndex, EndIndex, true, true);
            return true;
        }

        /// <summary>
        /// Assert that the current character matches the pattern defined by this object.
        /// </summary>
        ///
        /// <param name="index">
        ///  Zero-based index of the current position.
        /// </param>
        /// <param name="current">
        /// The current character.
        /// </param>
        ///
        /// <returns>
        /// true if the pattern matches at this position, false if not.
        /// </returns>

        protected virtual bool Expect(ref int index, char current)
        {
            info.Target = current;
            if (index == StartIndex)
            {
                quoteChar = current;
                if (!info.Quote)
                {
                    return false;
                }
            }
            else
            {

                bool isEscaped = Source[index - 1] == '\\';
                if (current == quoteChar && !isEscaped)
                {
                    index++;
                    return false;
                }
            }
            index++;
            return true;
        }
    }
}
