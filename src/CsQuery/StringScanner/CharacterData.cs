using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using CsQuery.StringScanner;
using CsQuery.StringScanner.Implementation;

namespace CsQuery.StringScanner
{
    /// <summary>
    /// A static class to provide attribute information about characters, e.g. determining whether or
    /// not it belongs to a number of predefined classes. This creates an array of every possible
    /// character with a uint that is a bitmap (of up to 32 possible values)
    /// This permits very fast access to this information since it only needs to be looked up via an
    /// index. Uses an array of 65536 uints = 256K of memory.
    /// </summary>
    
    public static class CharacterData
    {
        #region constructor

        /// <summary>
        /// Configuration of the xref of character info. This sets bitflags in the "characterFlags" array
        /// for every unicode value that defines its attributes. This creates a lookup table allowing
        /// very rapid access to metadata about a single character, useful during string-parsing and
        /// scanning.
        /// </summary>

        static CharacterData()
        {
            charsHtmlSpaceArray = charsHtmlSpace.ToArray<char>();

            characterFlags = new uint[65536];
            setBit(charsWhitespace, (uint)CharacterType.Whitespace);
            setBit(charsAlpha, (uint)CharacterType.Alpha);
            setBit(charsNumeric, (uint)CharacterType.Number);
            setBit(charsNumericExtended, (uint)CharacterType.NumberPart);
            setBit(charsLower, (uint)CharacterType.Lower);
            setBit(charsUpper, (uint)CharacterType.Upper);
            setBit(charsQuote, (uint)CharacterType.Quote);
            setBit(charsOperator, (uint)CharacterType.Operator);
            setBit(charsEnclosing, (uint)CharacterType.Enclosing);
            setBit(charsEscape, (uint)CharacterType.Escape);
            setBit(charsSeparators, (uint)CharacterType.Separator);
            setBit(charsHtmlSpace, (uint)CharacterType.HtmlSpace);
            setBit(charsHex, (uint)CharacterType.Hexadecimal);

            // html tag start

            SetHtmlTagNameStart((uint)CharacterType.HtmlTagNameStart);
            SetHtmlTagNameExceptStart((uint)CharacterType.HtmlTagNameExceptStart);

            SetHtmlTagNameStart((uint)CharacterType.HtmlTagSelectorStart);
            SetHtmlTagSelectorExceptStart((uint)CharacterType.HtmlTagSelectorExceptStart);

            SetHtmlAttributeName((uint)CharacterType.HtmlAttributeName);

            // html tag end
            setBit(charsHtmlSpace + "/>", (uint)CharacterType.HtmlTagOpenerEnd);

            // html tag any
            setBit(charsHtmlTagAny, (uint)CharacterType.HtmlTagAny);

            setBit(charsHtmlMustBeEncoded, (uint)CharacterType.HtmlMustBeEncoded);

            setBit(charsHtmlSpace + ">", (uint)CharacterType.HtmlAttributeValueTerminator);

            SetAlphaISO10646((uint)CharacterType.AlphaISO10646);
            SetSelectorTerminator((uint)CharacterType.SelectorTerminator);
        }

        #endregion

        #region private properties/constants

        // HTML spec for a space: http://www.w3.org/TR/html-markup/terminology.html#space
        //
        // U+0020 SPACE
        // U+0009 CHARACTER TABULATION (tab)
        // U+000A LINE FEED (LF)
        // U+000C FORM FEED (FF)
        // U+000D CARRIAGE RETURN (CR).

        const string charsHtmlSpace = "\x0020\x0009\x000A\x000C\x000D";
        
        // Add a couple more for non-HTML spec whitespace
        const string charsWhitespace = charsHtmlSpace + "\x00A0\x00C0";

        const string charsNumeric = "0123456789";
        const string charsHex = charsNumeric + "abcdefABCDEF";
        const string charsNumericExtended = "0123456789.-+";
        const string charsLower = "abcdefghijklmnopqrstuvwxyz";
        const  string charsUpper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string charsAlpha = charsLower + charsUpper;
        const string charsQuote = "\"'";
        const string charsOperator = "!+-*/%<>^=~";
        const string charsEnclosing = "()[]{}<>`´“”«»";
        const string charsEscape = "\\";
        const string charsSeparators = ", |";
        const string charsHtmlTagAny = "<>/";
        const string charsHtmlMustBeEncoded = "<>&";

        private static uint[] characterFlags;

        #endregion

        #region public properties/methods

        /// <summary>
        /// An array of all HTML "space" characters.
        /// </summary>

        public static readonly char[] charsHtmlSpaceArray;

        /// <summary>
        /// Creates a new instance of the CharacterInfo class
        /// </summary>
        ///
        /// <returns>
        /// The new character information.
        /// </returns>

        public static ICharacterInfo CreateCharacterInfo()
        {
            return new CharacterInfo();
        }

        /// <summary>
        /// Creates a new instance of the CharacterInfo class.
        /// </summary>
        ///
        /// <param name="character">
        /// The character to bind to the new instance.
        /// </param>
        ///
        /// <returns>
        /// A new CharacterInfo instance.
        /// </returns>

        public static ICharacterInfo CreateCharacterInfo(char character)
        {
            return new CharacterInfo(character);
        }

        /// <summary>
        /// Creates a new StringInfo instance
        /// </summary>
        ///
        /// <returns>
        /// The new StringInfo instance
        /// </returns>

        public static IStringInfo CreateStringInfo()
        {
            return new StringInfo();
        }

        /// <summary>
        /// Creates a new StringInfo instance bound to a string
        /// </summary>
        ///
        /// <param name="text">
        /// The string to bind.
        /// </param>
        ///
        /// <returns>
        /// The new StringInfo instance.
        /// </returns>

        public static IStringInfo CreateStringInfo(string text)
        {
            return new StringInfo(text);
        }

        /// <summary>
        /// Test whether a character matches a set of flags defined by the paramter
        /// </summary>
        ///
        /// <param name="character">
        /// The character to test
        /// </param>
        /// <param name="type">
        /// The type to which to compare the character
        /// </param>
        ///
        /// <returns>
        /// true if the character matches the flags in the test type, false if not
        /// </returns>

        public static bool IsType(char character, CharacterType type)
        {
            return (characterFlags[character] & (uint)type) > 0;
        }

        /// <summary>
        /// Gets a type with all flags set for the types implemented by this character
        /// </summary>
        ///
        /// <param name="character">
        /// The character to test
        /// </param>
        ///
        /// <returns>
        /// The type.
        /// </returns>

        public static CharacterType GetType(char character)
        {
            return (CharacterType)characterFlags[character];
        }

        /// <summary>
        /// Return the closing character for a set of known opening enclosing characters (including
        /// single and double quotes)
        /// </summary>
        ///
        /// <exception cref="InvalidOperationException">
        /// Thrown when the character is not a known opening bound
        /// </exception>
        ///
        /// <param name="character">
        /// The opening bound character
        /// </param>
        ///
        /// <returns>
        /// The closing bound character
        /// </returns>

        public static char Closer(char character)
        {
            char result = CloserImpl(character);
            if (result == (char)0)
            {
                throw new InvalidOperationException("The character '" + character + "' is not a known opening bound.");
            }
            return result;
        }

        /// <summary>
        /// Return the matching bound for known opening and closing bound characters (same as Closer, but
        /// accepts closing tags and returns openers)
        /// </summary>
        ///
        /// <exception cref="InvalidOperationException">
        /// Thrown when the requested operation is invalid.
        /// </exception>
        ///
        /// <param name="character">
        /// The opening bound character
        /// </param>
        ///
        /// <returns>
        /// The matching close character
        /// </returns>

        public static char MatchingBound(char character)
        {
            
            switch (character)
            {
                case ']':
                    return '[';
                case ')':
                    return '(';
                case '}':
                    return '{';
                case '>':
                    return '<';
                case '´':
                    return '`';
                case '”':
                    return '“';
                case '»':
                    return '«';
                default:
                    char result =  CloserImpl(character);
                    if (result == (char)0)
                    {
                        throw new InvalidOperationException("The character '" + character + "' is not a bound.");
                    };
                    return result;
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Sets the bits for ISO 10646.
        /// </summary>
        ///
        /// <param name="hsb">
        /// the target
        /// </param>

        private static void SetAlphaISO10646(uint hsb)
        {

            setBit(charsAlpha, hsb);
            setBit('-', hsb);
            setBit('_', hsb);
            // 161 = A1
            SetRange(hsb, 0x00A1, 0xFFFF);
        }
        private static void SetSelectorTerminator(uint hsb)
        {
            setBit(charsWhitespace, hsb);
            setBit(",:[>~+.#", hsb);
        }
        /// <summary>
        /// Matches anything but the first character for a valid HTML attribute name.
        /// </summary>
        ///
        /// <param name="hsb">
        /// the target
        /// </param>

        private static void SetHtmlAttributeName(uint hsb)
        {
            SetAlphaISO10646(hsb);
            setBit(charsNumericExtended, hsb);
            setBit("_:.-", hsb);
        }

        /// <summary>
        /// We omit ":" as a valid name start character because it makes pseudoselectors impossible to parse.
        /// </summary>
        /// <param name="hsb"></param>
        private static void SetHtmlTagSelectorStart(uint hsb)
        {
            //  | [#xF900-#xFDCF] | [#xFDF0-#xFFFD] | [#x10000-#xEFFFF]

            setBit(charsAlpha, hsb);
            setBit("_", hsb);
            SetRange(hsb, 0xC0, 0xD6);
            SetRange(hsb, 0xD8, 0xF6);
            SetRange(hsb, 0xF8, 0x2FF);
            SetRange(hsb, 0x370, 0x37D);
            SetRange(hsb, 0x37F, 0x1FFF);
            SetRange(hsb, 0x200C, 0x200D);
            SetRange(hsb, 0x2070, 0x218F);
            SetRange(hsb, 0x2C00, 0x2FEF);
            SetRange(hsb, 0x3001, 0xD7FF);
            SetRange(hsb, 0xF900, 0xFDCF);
            SetRange(hsb, 0xFDF0, 0xFFFD);

            // what the heck is this? How can a unicode character be 32 bits?
            //SetRange(hsb, 0x10000, 0xEFFFF);
        }
        /// <summary>
        /// Similar to above, we omit "." as a valid in-name char because it breaks chained CSS selectors.
        /// </summary>
        private static void SetHtmlTagSelectorExceptStart(uint hsb)
        {
            SetHtmlTagSelectorStart(hsb);
            setBit(charsNumeric, hsb);
            setBit("-", hsb);
            setBit((char)0xB7, hsb);
            SetRange(hsb, 0x0300, 0x036F);
            SetRange(hsb, 0x203F, 0x2040);
        }
        /// <summary>
        /// Add the : back in when actually parsing html
        /// </summary>
        /// <param name="hsb"></param>
        private static void SetHtmlTagNameStart(uint hsb)
        {
            SetHtmlTagSelectorStart(hsb);
            setBit(":", hsb);
        }

        /// <summary>
        /// Add the . back in when actually parsing html
        /// </summary>
        /// <param name="hsb"></param>
        private static void SetHtmlTagNameExceptStart(uint hsb)
        {
            SetHtmlTagSelectorExceptStart(hsb);
            setBit(":", hsb);
            setBit(".", hsb);
        }
        private static void SetRange(uint flag, ushort start, ushort end)
        {
            for (int i = start; i <= end; i++)
            {
                setBit((char)i, flag);
            }
        }
        private static void setBit(string forCharacters, uint bit)
        {
            for (int i = 0; i < forCharacters.Length; i++)
            {
                setBit(forCharacters[i], bit);
            }
        }
        private static void setBit(char character, uint bit)
        {
            characterFlags[(ushort)character] |= bit;
        }

        private static char CloserImpl(char character)
        {
            switch (character)
            {
                case '"':
                    return '"';
                case '\'':
                    return '\'';
                case '[':
                    return ']';
                case '(':
                    return ')';
                case '{':
                    return '}';
                case '<':
                    return '>';
                case '`':
                    return '´';
                case '“':
                    return '”';
                case '«':
                    return '»';
                case '»':
                    return '«';
                default:
                    return (char)0;
            }
        }
        #endregion
    }
}
