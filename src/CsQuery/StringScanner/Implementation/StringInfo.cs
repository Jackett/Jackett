using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.StringScanner.Implementation
{
    /// <summary>
    /// A StringInfo object: provides methods to test a string for certain properties.
    /// </summary>

    public class StringInfo : IStringInfo
    {
        /// <summary>
        /// Default constructor.
        /// </summary>

        public StringInfo()
        {

        }

        /// <summary>
        /// Constructor using the string passed
        /// </summary>
        ///
        /// <param name="text">
        /// The target of the new StringInfo object.
        /// </param>

        public StringInfo(string text)
        {
            Target = text;
        }

        /// <summary>
        /// Create a new StringInfo for the string passed
        /// </summary>
        ///
        /// <param name="text">
        /// The target of the new StringInfo object.
        /// </param>

        public static implicit operator StringInfo(string text)
        {
            return new StringInfo(text);
        }

        /// <summary>
        /// Creates a new StringInfo for the string passed
        /// </summary>
        ///
        /// <param name="text">
        /// The target of the new StringInfo object
        /// </param>
        ///
        /// <returns>
        /// A new StringInfo object
        /// </returns>

        public static StringInfo Create(string text)
        {
            return new StringInfo(text);
        }

        /// <summary>
        /// Information describing the character.
        /// </summary>

        protected CharacterInfo charInfo = new CharacterInfo();

        /// <summary>
        /// Tests each character in the current target against a function
        /// </summary>
        ///
        /// <param name="function">
        /// The function.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        protected bool CheckFor(Func<CharacterInfo, bool> function)
        {
            foreach (char current in Target)
            {
                charInfo.Target = current;
                if (!function(charInfo))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// The string which is being tested
        /// </summary>

        public string Target
        {
            get;
            set;
        }

        IConvertible IValueInfo.Target
        {
            get
            {
                return Target;
            }
            set
            {
                Target = (string)value;
            }
        }

        /// <summary>
        /// Test whether a character is alphabetic
        /// </summary>

        protected Func<CharacterInfo, bool> isAlpha = new Func<CharacterInfo, bool>(item => item.Alpha);

        /// <summary>
        /// The value is alphabetic.
        /// </summary>

        public bool Alpha
        {
            get { return Exists && CheckFor(isAlpha); }
        }

        private static Func<CharacterInfo, bool> isNumeric = new Func<CharacterInfo, bool>(item => item.Numeric);

        /// <summary>
        /// The value is numeric.
        /// </summary>

        public bool Numeric
        {
            get { return Exists && CheckFor(isNumeric); }
        }

        private static Func<CharacterInfo, bool> isNumericExtended = new Func<CharacterInfo, bool>(item => item.NumericExtended);

        /// <summary>
        /// The value is numeric, or characters that can be parts of numbers (+,-,.)
        /// </summary>

        public bool NumericExtended
        {
            get { return Exists && CheckFor(isNumericExtended); }
        }

        private static Func<CharacterInfo, bool> isLower = new Func<CharacterInfo, bool>(item => !item.Alpha || item.Lower);

        /// <summary>
        /// The value is all lowercase.
        /// </summary>

        public bool Lower
        {
            get { return Exists && HasAlpha && CheckFor(isLower); }
        }

        private static Func<CharacterInfo, bool> isUpper = new Func<CharacterInfo, bool>(item => !item.Alpha || item.Upper);

        /// <summary>
        /// Gets a value indicating whether the cvale upper.
        /// </summary>

        public bool Upper
        {
            get { return Exists && HasAlpha && CheckFor(isUpper); }
        }

        private static Func<CharacterInfo, bool> isWhitespace = new Func<CharacterInfo, bool>(item => item.Whitespace);

        /// <summary>
        /// The value is whitespace.
        /// </summary>

        public bool Whitespace
        {
            get { return Exists && CheckFor(isWhitespace); }
        }

        private static Func<CharacterInfo, bool> isAlphanumeric = new Func<CharacterInfo, bool>(item => item.Alpha || item.Numeric);

        /// <summary>
        /// The value is alphanumeric.
        /// </summary>

        public bool Alphanumeric
        {
            get { return Exists && CheckFor(isAlphanumeric); }
        }

        /// <summary>
        /// Test whether a character is an operator
        /// </summary>

        protected Func<CharacterInfo, bool> isOperator = new Func<CharacterInfo, bool>(item => item.Operator);

        /// <summary>
        /// The value is a math operator.
        /// </summary>

        public bool Operator
        {
            get { return Exists && CheckFor(isOperator); }
        }

        /// <summary>
        /// The string contains alpha characters.
        /// </summary>

        public bool HasAlpha
        {
            get
            {
                foreach (char current in Target)
                {
                    charInfo.Target = current;
                    if (charInfo.Alpha)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// The string is a valid HTML attribute name.
        /// </summary>

        public bool HtmlAttributeName
        {
            get
            {
                if (!Exists) return false;
                charInfo.Target = Target[0];

                if (!(charInfo.Alpha || charInfo.Target == ':' || charInfo.Target == '_'))
                {
                    return false;
                }
                for (int i = 1; i < Target.Length; i++)
                {
                    charInfo.Target = Target[i];
                    if (!charInfo.Alphanumeric && !("_:.-".Contains(charInfo.Target)))
                    {
                        return false;
                    }

                }
                return true;
            }
        }

        /// <summary>
        /// The is alpha ISO 10646.
        /// </summary>

        protected Func<CharacterInfo, bool> isAlphaISO10646 = new Func<CharacterInfo, bool>(item => item.AlphaISO10646);

        /// <summary>
        /// Indicates that a character is alphabetic-like character defined as a-z, A-Z, hyphen,
        /// underscore, and ISO 10646 code U+00A1 and higher. (per characters allowed in CSS identifiers)
        /// </summary>

        public bool AlphaISO10646
        {
            get
            {
                return Exists && CheckFor(isAlphaISO10646);
            }
        }

        /// <summary>
        /// Returns the target of this StringInfo object
        /// </summary>
        ///
        /// <returns>
        /// A string 
        /// </returns>

        public override string ToString()
        {
            return Target;
        }

        /// <summary>
        /// Gets a value indicating whether the target is non-null and non-empty.
        /// </summary>

        protected bool Exists
        {
            get
            {
                return !String.IsNullOrEmpty(Target);
            }
        }
    }
}
