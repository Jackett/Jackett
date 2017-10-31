using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// TODO this should be fully commented; however it's not part of the main public API

#pragma warning disable 1591
#pragma warning disable 1570


namespace CsQuery.StringScanner
{
    /// <summary>
    /// Match functions. These are used with StringScanner to parse out expected strings. A basic
    /// match function accepts an int and a char, and is eand returns true as long as the character
    /// is valid for that position in the string. Many patterns have different valid first characters
    /// versus later characters. The function will be called beginning with index zero, and continue
    /// to be called until it returns false, indicating that the end of a pattern that matches that
    /// concept has been reached.
    /// 
    /// More complex patterns require a memory of the previous state, for example, to know whether
    /// quoting is in effect. the IExpectPattern interface describes a class to match more complex
    /// patterns.
    /// </summary>

    public static class MatchFunctions
    {
        /// <summary>
        /// Return true while the string is alphabetic, e.g. contains only letters.
        /// </summary>
        ///
        /// <param name="index">
        /// Zero-based index of the current position in the string.
        /// </param>
        /// <param name="character">
        /// The character at the current position.
        /// </param>
        ///
        /// <returns>
        /// True if the current character is valid for this pattern, false if not.
        /// </returns>

        public static bool Alpha(int index, char character)
        {
            return CharacterData.IsType(character, CharacterType.Alpha); 
        }

        /// <summary>
        /// Returns a pattern that matches numbers.
        /// </summary>
        ///
        /// <param name="requireWhitespaceTerminator">
        /// (optional) when true, only whitespace can terminate this number. When false, any non-numeric character will succesfully terminate the pattern.
        /// </param>
        ///
        /// <returns>
        /// The total number of ber.
        /// </returns>

        public static IExpectPattern Number(bool requireWhitespaceTerminator = false)
        {
            var pattern = new Patterns.Number();
            pattern.RequireWhitespaceTerminator = requireWhitespaceTerminator;
            return pattern;
        }


        public static bool Alphanumeric(int index, char character)
        {
            return CharacterData.IsType(character, CharacterType.Alpha | CharacterType.NumberPart);
        }
        public static IExpectPattern HtmlIDValue()
        {
            // The requirements are different for HTML5 vs. older HTML specs but basically we don't want to be
            // too rigorous on this one -- the tagname spec is about right and includes underscores & colons

            return new Patterns.HtmlIDSelector();
        }

        /// <summary>
        /// Gets an expect pattern for a string that's an HTML attribte name
        /// </summary>
        ///
        /// <returns>
        /// An expect pattern
        /// </returns>

        public static IExpectPattern HTMLAttribute()
        {
            return new Patterns.HTMLAttributeName();
          
        }

        /// <summary>
        /// Gets an expect pattern for a string that's a valid  HTML tag selector.
        /// </summary>
        ///
        /// <returns>
        /// An expect pattern
        /// </returns>

        public static IExpectPattern HTMLTagSelectorName()
        {
            return new Patterns.HTMLTagSelectorName();
        }

        /// <summary>
        /// Gets an expect pattern for a string that's bounded by the provided values.
        /// </summary>
        ///
        /// <param name="boundStart">
        /// (optional) the bound start.
        /// </param>
        /// <param name="boundEnd">
        /// (optional) the bound end.
        /// </param>
        /// <param name="honorInnerQuotes">
        /// (optional) the honor inner quotes.
        /// </param>
        ///
        /// <returns>
        /// An expect pattern
        /// </returns>

        public static IExpectPattern BoundedBy(string boundStart=null, string boundEnd=null, bool honorInnerQuotes=false)
        {
            
            var pattern = new Patterns.Bounded();
            if (!String.IsNullOrEmpty(boundStart))
            {
                pattern.BoundStart = boundStart;
            }
            if (!String.IsNullOrEmpty(boundEnd))
            {
                pattern.BoundEnd= boundEnd;
            }
            pattern.HonorInnerQuotes = honorInnerQuotes;
            return pattern;
        }

        public static IExpectPattern Bounded
        {
            get
            {
                var pattern = new Patterns.Bounded();
                pattern.HonorInnerQuotes = false;
                return pattern;
            }
        }

        /// <summary>
        /// Gets an expect pattern for a string that's bounded by known bounding characters, and has
        /// quoted content.
        /// </summary>

        public static IExpectPattern BoundedWithQuotedContent
        {
            get
            {
                var pattern = new Patterns.Bounded();
                pattern.HonorInnerQuotes = true;
                return pattern;
            }
        }

        /// <summary>
        /// Test whether the character is whitespace.
        /// </summary>
        ///
        /// <param name="index">
        /// Zero-based index of the current position in the string. Not used for this test.
        /// </param>
        /// <param name="character">
        /// The character at the current position.
        /// </param>
        ///
        /// <returns>
        /// true if it is whitespace, false if it fails.
        /// </returns>

        public static bool NonWhitespace(int index, char character)
        {
            return !CharacterData.IsType(character, CharacterType.Whitespace); 
        }

        /// <summary>
        /// Test whether the character is a quote character.
        /// </summary>
        ///
        /// <param name="index">
        /// Zero-based index of the current position in the string.
        /// </param>
        /// <param name="character">
        /// The character at the current position.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public static bool QuoteChar(int index, char character)
        {
            return CharacterData.IsType(character, CharacterType.Quote); 
        }

        /// <summary>
        /// Test whether the character is a bound character.
        /// </summary>
        ///
        /// <param name="index">
        /// Zero-based index of the current position in the string.
        /// </param>
        /// <param name="character">
        /// The character at the current position.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public static bool BoundChar(int index, char character)
        {
            return CharacterData.IsType(character, CharacterType.Enclosing | CharacterType.Quote); 
        }

        /// <summary>
        /// Gets an expect patter for a quoted string.
        /// </summary>
        ///
        /// <returns>
        /// An expect pattern
        /// </returns>

        public static IExpectPattern Quoted()
        {
                return new Patterns.Quoted();
        }

        /// <summary>
        /// A matching function that validates 
        /// </summary>
        ///
        /// <param name="index">
        /// Zero-based index of the.
        /// </param>
        /// <param name="character">
        /// The character.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public static bool PseudoSelector(int index, char character)
        {
            return index == 0 ? CharacterData.IsType(character, CharacterType.Alpha) :
               CharacterData.IsType(character, CharacterType.Alpha) || character == '-';
        }

        /// <summary>
        /// Matches a valid CSS class: http://www.w3.org/TR/CSS21/syndata.html#characters Does not
        /// currently deal with escaping though.
        /// </summary>
        ///
        /// <value>
        /// The name of the CSS class.
        /// </value>

        public static IExpectPattern CssClassName
        {
            get {
                return new Patterns.CssClassName();
            }
        }

        /// <summary>
        /// Returns a pattern matching a string that is optionally quoted. If terminators are passed, any
        /// character in that string will terminate seeking.
        /// </summary>
        ///
        /// <param name="terminators">
        /// (optional) the terminators.
        /// </param>
        ///
        /// <returns>
        /// An expect pattern
        /// </returns>

        public static IExpectPattern OptionallyQuoted(string terminators=null)
        {
            return new Patterns.OptionallyQuoted(terminators);
        }

        /// <summary>
        /// Test whether the character is an operator.
        /// </summary>
        ///
        /// <param name="index">
        /// Zero-based index of this character's position. Not used for this test.
        /// </param>
        /// <param name="character">
        /// The character.
        /// </param>
        ///
        /// <returns>
        /// true if it is an operator, false if it fails.
        /// </returns>

        public static bool Operator(int index, char character)
        {
            return CharacterData.IsType(character, CharacterType.Operator);
        }

    }

}
