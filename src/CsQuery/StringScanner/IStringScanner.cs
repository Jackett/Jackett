using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections;
using System.Linq;
using System.Text;
using System.Diagnostics;

// TODO: This code should be fully commented at some point. It's not part of the main public API though.

#pragma warning disable 1591
#pragma warning disable 1570

namespace CsQuery.StringScanner
{
    /// <summary>
    /// Interface defining a StringScanner - a lexical scanner
    /// </summary>

    public interface IStringScanner
    {
        /// <summary>
        /// Gets or sets the text that the scanner acts upon
        /// </summary>
        /// <seealso cref="Chars"/>

        string Text {get;set;}

        /// <summary>
        /// Gets or sets the text that this scanner acts upon.
        /// </summary>
        ///
        /// <seealso cref="Text"/>

        char[] Chars {get;set;}

        /// <summary>
        /// Gets or sets a value indicating whether the scanner should ignore whitespace. When true, it
        /// is skipped automatically.
        /// </summary>

        bool IgnoreWhitespace { get; set; }

        /// <summary>
        /// Gets the length of the text bound to this scanner.
        /// </summary>

        int Length {get;}

        /// <summary>
        /// Gets or sets the current zero-based position of the scanner.
        /// </summary>

        int Index {get;}

        /// <summary>
        /// Gets the zero-based index of the scanner before the last operation.
        /// </summary>

        int LastIndex { get; }

        /// <summary>
        /// Gets the current character.
        /// </summary>

        char Current {get;}

        /// <summary>
        /// Returns the character after the current character
        /// </summary>
        ///
        /// <returns>
        /// A character
        /// </returns>

        char Peek();

        /// <summary>
        /// Gets the next character, or an empty string if the pointer is at the end of the string.
        /// </summary>

        string CurrentOrEmpty {get;}

        /// <summary>
        /// Gets the current match string (usually, the text between the prior pointer position and the
        /// current pointer position, possibly excluding whitespace. This depends on the last operation).
        /// </summary>

        string Match {get;}

        /// <summary>
        /// Gets the match prior to the curren one.
        /// </summary>

        string LastMatch {get;}

        /// <summary>
        /// Gets a value indicating whether the pointer is after the end of the string.
        /// </summary>

        bool Finished {get;}

        /// <summary>
        /// Gets a value indicating whether at the last character of the string.
        /// </summary>

        bool AtEnd {get;}

        /// <summary>
        /// Gets a value indicating whether the last operation succeeded. Since failure throws an error,
        /// this is generally useful only if errors are trapped.
        /// </summary>

        bool Success {get;}

        /// <summary>
        /// Gets the error message when the prior operation failed.
        /// </summary>

        string LastError {get;}

        /// <summary>
        /// Causes the next action to permit quoting -- if the first character is a quote character, stop
        /// characters between there and the next matching quote character will be ignored.
        /// </summary>
        ///
        /// <returns>
        /// true if the next value is quoted, false if not
        /// </returns>

        bool AllowQuoting();

        /// <summary>
        /// CharacterInfo object bound to the character at the current index.
        /// </summary>

        ICharacterInfo Info {get; }

        /// <summary>
        /// If the pointer is current on whitespace, advance to the next non-whitespace character. If the
        /// pointer is not on whitespace, do nothing.
        /// </summary>

        void SkipWhitespace();

        /// <summary>
        /// Advance the pointer to the next character that is not whitespace. This differes from
        /// SkipShitespace in that this always advances the pointer.
        /// </summary>

        void NextNonWhitespace();

        /// <summary>
        /// Advance the pointer by one character.
        /// </summary>
        ///
        /// <returns>
        /// true if the pointer can be advanced again, false if it is after the last position.
        /// </returns>

        bool Next();

        /// <summary>
        /// Move the pointer back one position.
        /// </summary>
        ///
        /// <returns>
        /// true if the pointer can be moved back again, false if it is at the origin.
        /// </returns>

        bool Previous();

        /// <summary>
        /// Moves the pointer by a specific number of characters, forward or reverse.
        /// </summary>
        ///
        /// <param name="count">
        /// A positive or negative integer.
        /// </param>
        ///
        /// <returns>
        /// true if the pointer is not at the origin or after the end of the string, false otherwise.
        /// </returns>

        bool Move(int count);

        /// <summary>
        /// Undo the last operation
        /// </summary>

        void Undo();

        /// <summary>
        /// Moves the pointer past the last character postion.
        /// </summary>

        void End();

        /// <summary>
        /// Throw an error if the current scanner is not finished.
        /// </summary>
        ///
        /// <param name="errorMessage">
        /// (optional) message describing the error.
        /// </param>

        void AssertFinished(string errorMessage=null);

        /// <summary>
        /// Throw an error if the current scanner is finished.
        /// </summary>
        ///
        /// <param name="errorMessage">
        /// (optional) message describing the error.
        /// </param>

        void AssertNotFinished(string errorMessage=null);

        /// <summary>
        /// Resets the pointer to the origin and clear any state information about the scanner. This sets
        /// the internal state as if it had just been created.
        /// </summary>

        void Reset();


        /// <summary>
        /// Test that the text starting at the current position matches the passed text.
        /// </summary>
        ///
        /// <param name="text">
        /// The text to match
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        bool Matches(string text);

        /// <summary>
        /// Test that the text starting at the current position is any of the strings passed.
        /// </summary>
        ///
        /// <param name="text">
        /// A sequence of strings to match
        /// </param>
        ///
        /// <returns>
        /// true if one of, false if not.
        /// </returns>

        bool MatchesAny(IEnumerable<string> text);

        /// <summary>
        /// Seeks until a specific character is found. The Match string becomes everything from the
        /// current position, through the position before the matched character. If the scanner is
        /// already at the end, an exception is thrown.
        /// </summary>
        ///
        /// <param name="character">
        /// The character to seek.
        /// </param>
        /// <param name="orEnd">
        /// When true, the end of the string is a valid match. When false, the end of the string will
        /// cause an exception.
        /// </param>
        ///
        /// <returns>
        /// The current string scanner.
        /// </returns>

        IStringScanner Seek(char character, bool orEnd);

        /// <summary>
        /// Creates a new scanner from the current match.
        /// </summary>
        ///
        /// <returns>
        /// A new IStringScanner
        /// </returns>

        IStringScanner ToNewScanner();

        /// <summary>
        /// Creates a new scanner from the current match.
        /// </summary>
        ///
        /// <param name="template">
        /// The template.
        /// </param>
        ///
        /// <returns>
        /// A new IStringScanner.
        /// </returns>

        IStringScanner ToNewScanner(string template);

        /// <summary>
        /// Assert that the text matches the string starting at the current position. The pointer is
        /// advanced to the first position beyond the matching text. If it does not, an ArgumentException
        /// is thrown.
        /// </summary>
        ///
        /// <param name="text">
        /// The text to match.
        /// </param>
        ///
        /// <returns>
        /// The current StringScanner.
        /// </returns>

        IStringScanner Expect(string text);

        /// <summary>
        /// Assert that the text matches the pattern defined by an IExpectPattern object. The pointer is
        /// advanced until the pattern stops matching. If it does not, an ArgumentException is thrown.
        /// </summary>
        ///
        /// <param name="pattern">
        /// A pattern specifying the.
        /// </param>
        ///
        /// <returns>
        /// The current StringScanner.
        /// </returns>

        IStringScanner Expect(IExpectPattern pattern);

        /// <summary>
        /// Assert that at least one character starting at the current position validates using a
        /// function delegate. The pointer advances until the delegate returns false. If it does not, an
        /// ArgumentException is thrown.
        /// </summary>
        ///
        /// <param name="validate">
        /// The validate.
        /// </param>
        ///
        /// <returns>
        /// The current StringScanner.
        /// </returns>

        IStringScanner Expect(Func<int, char, bool> validate);

        /// <summary>
        /// Assert that the current character matches the character passed. The pointer is advanced by
        /// one position. If it does not, an ArgumentException is thrown.
        /// </summary>
        ///
        /// <param name="character">
        /// The character to seek.
        /// </param>
        ///
        /// <returns>
        /// .
        /// </returns>

        IStringScanner ExpectChar(char character);

        /// <summary>
        /// Assert that the current character matches any of the characters passed. The pointer is
        /// advanced by one position. If it does not, an ArgumentException is thrown.
        /// </summary>
        ///
        /// <param name="characters">
        /// The characters to match
        /// </param>
        ///
        /// <returns>
        /// The current string scanner.
        /// </returns>

        IStringScanner ExpectChar(IEnumerable<char> characters);

        /// <summary>
        /// Assert that there is a pattern that matches a number starting at the current position. The
        /// pointer is advanced to the position after the end of the number. If it does not, an
        /// ArgumentException is thrown.
        /// </summary>
        ///
        /// <param name="requireWhitespaceTerminator">
        /// (optional) Indicates if whitespace is the only valid terminator. If true, an
        /// ArgumentException will be thrown if the first character that terminates the number is not
        /// whitespace. If false, any character that is invalid as part of a number will stop matching
        /// with no error.
        /// </param>
        ///
        /// <returns>
        /// The current string scanner.
        /// </returns>

        IStringScanner ExpectNumber(bool requireWhitespaceTerminator = false);

        /// <summary>
        /// Assert that the current pattern is alphabetic until the next whitespace.
        /// </summary>
        ///
        /// <returns>
        /// The current string scanner.
        /// </returns>

        IStringScanner ExpectAlpha();

        /// <summary>
        /// Asser that the current pattern is bounded by the start and end characters passed
        /// </summary>
        ///
        /// <param name="start">
        /// The start bound character
        /// </param>
        /// <param name="end">
        /// The end bound character
        /// </param>
        /// <param name="allowQuoting">
        /// (optional) True if the contents of the bounds can be quoted
        /// </param>
        ///
        /// <returns>
        /// The current string scanner
        /// </returns>

        IStringScanner ExpectBoundedBy(string start, string end, bool allowQuoting=false);
        IStringScanner ExpectBoundedBy(char bound, bool allowQuoting = false);

        bool TryGet(IEnumerable<string> stringList, out string result);
        bool TryGet(IExpectPattern pattern, out string result);
        bool TryGet(Func<int, char, bool> validate, out string result);
        bool TryGetChar(char character, out string result);
        bool TryGetChar(string characters, out string result);
        bool TryGetChar(IEnumerable<char> characters, out string result);
        bool TryGetNumber(out string result);
        bool TryGetNumber<T>(out T result) where T : IConvertible;
        bool TryGetNumber(out int result);
        bool TryGetAlpha(out string result);
        bool TryGetBoundedBy(string start, string end, bool allowQuoting, out string result);

        string Get(params string[] values);
        string Get(IEnumerable<string> stringList);
        string Get(IExpectPattern pattern);
        string Get(Func<int, char, bool> validate);
        string GetNumber();
        string GetAlpha();
        string GetBoundedBy(string start, string end, bool allowQuoting=false);
        string GetBoundedBy(char bound, bool allowQuoting=false);

        char GetChar(char character);
        char GetChar(string characters);
        char GetChar(IEnumerable<char> characters);

        void Expect(params string[] values);
        void Expect(IEnumerable<string> stringList);
    }


}