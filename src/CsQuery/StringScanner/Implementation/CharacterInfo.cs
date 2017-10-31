using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace CsQuery.StringScanner.Implementation
{
    /// <summary>
    /// A class that provides methods with metadata about a character.
    /// </summary>

    public class CharacterInfo : ICharacterInfo
    {
        /// <summary>
        /// Create a new unbound CharacterInfo class
        /// </summary>

        public CharacterInfo()
        {

        }

        /// <summary>
        /// Create a new CharacterInfo class bound to a character.
        /// </summary>
        ///
        /// <param name="character">
        /// The character.
        /// </param>

        public CharacterInfo(char character)
        {
            Target = character;
        }

        /// <summary>
        /// CharacterInfo casting operator: creates a new instance from a single character
        /// </summary>
        ///
        /// <param name="character">
        /// The character to bind to the new CharacterInfo class
        /// </param>

        public static implicit operator CharacterInfo(char character) {
            return new CharacterInfo(character);
        }

        /// <summary>
        /// Creates a new CharacterInfo instance from a character
        /// </summary>
        ///
        /// <param name="character">
        /// The character to bind to this instance.
        /// </param>
        ///
        /// <returns>
        /// A new CharacterInfo object
        /// </returns>

        public static ICharacterInfo Create(char character)
        {
            return new CharacterInfo(character);
        }

        /// <summary>
        /// Gets or sets bound character for this instance. This is the character against which all tests
        /// are performed.
        /// </summary>

        public char Target { get; set; }

        IConvertible IValueInfo.Target
        {
            get
            {
                return Target;
            }
            set
            {
                Target = (char)value;
            }
        }

        /// <summary>
        /// Flags indicating the use of this character.
        /// </summary>

        public CharacterType Type
        {
            get
            {
                return CharacterData.GetType(Target);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the character is alphabetic, e.g. a-z, A-Z
        /// </summary>

        public bool Alpha
        {
            get
            {
                return CharacterData.IsType(Target,CharacterType.Alpha);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the bound character is numeric only, e.g. 0-9
        /// </summary>

        public bool Numeric
        {
            get
            {
                return CharacterData.IsType(Target,CharacterType.Number);
            }
        }

        /// <summary>
        /// Test whether the character is numeric or part of a complete number, e.g. also includes '+', '-
        /// ' and '.'.
        /// </summary>

        public bool NumericExtended
        {
            get
            {
                return CharacterData.IsType(Target, CharacterType.NumberPart);
            }
        }

        /// <summary>
        /// Test whether the character is lower-case
        /// </summary>

        public bool Lower
        {
            get
            {
                return CharacterData.IsType(Target, CharacterType.Lower);
            }
        }

        /// <summary>
        /// Test whether the character is upper-case
        /// </summary>

        public bool Upper
        {
            get
            {
                return CharacterData.IsType(Target, CharacterType.Upper);
            }
        }

        /// <summary>
        /// Test whether the character is whitespace. This is really HTML5 "space" and not ANSI
        /// whitespace which. HTML5 space is much more restrictive; this is generally used to test
        /// whether a character delimits an entity during HTML/CSS/HTML-related parsing.
        /// </summary>

        public bool Whitespace
        {
            get
            {
                return CharacterData.IsType(Target,CharacterType.Whitespace);
            }
        }

        /// <summary>
        /// The value is alphanumeric.
        /// </summary>

        public bool Alphanumeric
        {
            get
            {
                return CharacterData.IsType(Target, CharacterType.Alpha | CharacterType.Number);
            }
        }

        /// <summary>
        /// The value is a math operator.
        /// </summary>

        public bool Operator
        {
            get
            {
                return CharacterData.IsType(Target, CharacterType.Operator);
            }
        }
        /// <summary>
        /// Enclosing, plus double and single quotes
        /// </summary>
        public bool Bound
        {
            get
            {
                return CharacterData.IsType(Target, CharacterType.Enclosing | CharacterType.Quote);
            }
        }

        /// <summary>
        /// Tests whether the character is an enclosing/bounding type, one of:
        /// ()[]{}&lt;&gt;`´“”«».
        /// </summary>

        public bool Enclosing
        {
            get
            {
                return CharacterData.IsType(Target, CharacterType.Enclosing);
            }
        }

        /// <summary>
        /// Tests whether the bound character is a single- or double-quote
        /// </summary>

        public bool Quote
        {
            get
            {
                return CharacterData.IsType(Target, CharacterType.Quote);
            }
        }

        /// <summary>
        /// Tests whether the bound character is an opening or closing parenthesis.
        /// </summary>

        public bool Parenthesis
        {
            get
            {
                return Target == '(' || Target == ')';
            }
        }

        /// <summary>
        /// Gets a value indicating whether the character is a separator (a space, or pipe)
        /// </summary>

        public bool Separator
        {
            get
            {
                return CharacterData.IsType(Target, CharacterType.Separator);
            }
        }

        /// <summary>
        /// Indicates that a character is alphabetic-like character defined as a-z, A-Z, hyphen,
        /// underscore, and ISO 10646 code U+00A1 and higher. (per characters allowed in CSS identifiers)
        /// </summary>

        public bool AlphaISO10646
        {
            get
            {
                return CharacterData.IsType(Target,CharacterType.AlphaISO10646);
            }
        }

        /// <summary>
        /// Returns a string that is the current target
        /// </summary>
        ///
        /// <returns>
        /// The current target as a string
        /// </returns>

        public override string ToString()
        {
            return Target.ToString();
        }
    }

}


