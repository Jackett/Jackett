using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.StringScanner.ExtensionMethods;

namespace CsQuery.StringScanner.Patterns
{
    /// <summary>
    /// Match an attribute value that is optionally quoted with a quote character ' or ".
    /// </summary>

    public class OptionallyQuoted : Quoted
    {
        /// <summary>
        /// Create in instance of the pattern matcher using default terminators
        /// </summary>

        public OptionallyQuoted()
        {
            SetDefaultTerminators();
        }

        /// <summary>
        /// Create in instance of the pattern matcher using any character in the string as a terminator.
        /// A closing quote (when the string is quoted) is always a terminator.
        /// </summary>
        ///
        /// <param name="terminators">
        /// A string containing characters, each of which will terminate seeking (when not inside a quote
        /// block)
        /// </param>

        public OptionallyQuoted(IEnumerable<char> terminators)
        {
            if (terminators != null && terminators.Any())
            {
                Terminators = terminators;
            }
            else
            {
                SetDefaultTerminators();
            }
        }

        private void SetDefaultTerminators()
        {
            Terminators = "])}";
        }
        bool isQuoted;

        /// <summary>
        /// Initializes this object from a character array.
        /// </summary>
        ///
        /// <param name="startIndex">
        /// The start index.
        /// </param>
        /// <param name="sourceText">
        /// Source text.
        /// </param>

        public override void Initialize(int startIndex, char[] sourceText)
        {
            base.Initialize(startIndex, sourceText);
            isQuoted = false;
        }

        /// <summary>
        /// When unquoted, this will terminate the string.
        /// </summary>

        public IEnumerable<char> Terminators
        {
            get;
            set;
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
            isQuoted = CharacterData.IsType(Source[StartIndex], CharacterType.Quote);
            return base.Validate();
        }

        /// <summary>
        /// Finishes a validation.
        /// </summary>
        ///
        /// <returns>
        /// true if the string matched the pattern defined by this instance, false if not.
        /// </returns>

        protected override bool FinishValidate()
        {
            if (isQuoted)
            {
                return base.FinishValidate();
            }
            else
            {
                Result = GetOuput(StartIndex, EndIndex, false);
                return true;
            }
           
        }

        /// <summary>
        /// Override the default Expect for a quoted string to also terminate upon finding one of the
        /// terminators (if not quoted).
        /// </summary>
        ///
        /// <param name="index">
        /// The current index.
        /// </param>
        /// <param name="current">
        /// The current character.
        /// </param>
        ///
        /// <returns>
        /// true to continue seeking.
        /// </returns>

        protected override bool Expect(ref int index, char current)
        {

            if (isQuoted)
            {
                return base.Expect(ref index, current);
            }
            else
            {
                bool isEscaped = index>0 && Source[index - 1] == '\\';
                if (!isEscaped && Terminators.Contains(current)) {
                    return false;
                }
                index++;
                return true;
            }
                
        }

    }
}
