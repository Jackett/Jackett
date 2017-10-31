using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections;
using System.Linq;
using System.Text;
using System.Diagnostics;
using CsQuery.StringScanner.ExtensionMethods;

// TODO: This code should be fully commented at some point. It's not part of the main public API though.

#pragma warning disable 1591
#pragma warning disable 1570

namespace CsQuery.StringScanner.Implementation
{
    /// <summary>
    /// String scanner engine. A lexical scanner to match complex patterns.
    /// </summary>

    public class StringScannerEngine: IStringScanner
    {
        #region constructors

        /// <summary>
        /// Create a new StringScannerEngine with no configuration
        /// </summary>

        public StringScannerEngine()
        {
            Init();
        }

        /// <summary>
        /// Create a new StringScannerEngine for a string
        /// </summary>
        ///
        /// <param name="text">
        /// The string to scan
        /// </param>

        public StringScannerEngine(string text)
        {
            Text = text;
            Init();
        }

        /// <summary>
        /// Create a new StringScannerEngine for a string
        /// </summary>
        ///
        /// <param name="text">
        /// The string to scan.
        /// </param>

        public static implicit operator StringScannerEngine(string text) {
            return new StringScannerEngine(text);
        }

        /// <summary>
        /// Common configuration tasks for all constructors.
        /// </summary>

        protected void Init()
        {
            IgnoreWhitespace = true;
            Reset();
        }
        #endregion

        #region private properties

        private string _Text;
        private string _CurrentMatch;
        private string _LastMatch;
        private int cachedPos;
        private string cachedMatch;
        bool cached = false;
        private CharacterInfo _characterInfo;
        
       
        /// <summary>
        /// When true, the next seek should honor quotes
        /// </summary>
        protected bool QuotingActive
        { get; set; }
        protected char QuoteChar
        { get; set; }
        protected CharacterInfo characterInfo
        {
            get
            {
                if (_characterInfo == null)
                {
                    _characterInfo = new CharacterInfo();
                }
                return _characterInfo;
            }
        }
        protected bool SuppressErrors { get; set; }
        protected char[] _Chars;
        #endregion

        #region public properties

        /// <summary>
        /// Gets or sets the text that the scanner acts upon.
        /// </summary>
        ///
        /// <seealso cref="Chars"/>

        public string Text
        {
            get
            {
                if (_Text == null)
                {
                    _Text = new string(_Chars);
                }
                return _Text;
            }
            set
            {
                _Text = value ?? "";
                _Chars = null;
                Length = _Text.Length;
                Reset();
            }
        }
        public char[] Chars
        {
            get
            {
                if (_Chars == null)
                {
                    _Chars = _Text.ToCharArray();
                }
                return _Chars;
            }
            set
            {
                _Chars = value;
                _Text=null;
                Length = value.Length;
                Reset();
            }
        }
        /// <summary>
        /// Causes the next action to permit quoting -- if the first character is a quote character, stop characters between there
        /// and the next matching quote character will be ignored.
        /// </summary>
        public bool AllowQuoting()
        {
            if (IgnoreWhitespace)
            {
                NextNonWhitespace();
            }
            if (CharacterData.IsType(Peek(),CharacterType.Whitespace))
            {
                Next();
                QuotingActive = true;
                QuoteChar = Current;
            }
            return QuotingActive;
        }
        public bool IgnoreWhitespace { get; set; }

        /// <summary>
        /// Gets or sets the length of the text bound to this scanner.
        /// </summary>

        public int Length
        { get; protected set; }

        /// <summary>
        /// Gets or sets the current zero-based position of the scanner.
        /// </summary>

        public int Index
        {
            get;
            protected set;
        }
        public int LastIndex
        {
            get;
            protected set;
        }
        
        /// <summary>
        /// Return the character at the current scanning position without advancing the pointer. Throw an error
        /// if the pointer is at the end of the string.
        /// </summary>
        public char Current
        {
            get
            {
                return Text[Index];
            }
        }

        /// <summary>
        /// Return the character at the current scanning position without advancing the pointer. If the pointer is
        /// at the end of the string, return an empty string.
        /// </summary>
        public string CurrentOrEmpty
        {
            get
            {
                return Finished ? null : Current.ToString();
            }
        }

        /// <summary>
        /// The string or character that has been matched.
        /// </summary>

        public string Match
        {
            get
            {
                return _CurrentMatch;
            }
            protected set
            {
                _LastMatch = _CurrentMatch;
                _CurrentMatch = value;
            }
        }
        /// <summary>
        /// The string or character matched prior to last operation
        /// </summary>
        public string LastMatch
        {
            get
            {
                return _LastMatch;
            }
            protected set
            {
                _LastMatch = value;
            }
        }
        /// <summary>
        /// The current position is after the last character
        /// </summary>
        public bool Finished
        {
            get
            {
                return Index >= Length || Length == 0;
            }
        }
        /// <summary>
        /// The current position is on the last character
        /// </summary>
        public bool AtEnd
        {
            get
            {
                return Index == Length - 1;
            }
        }
        public string LastError
        {
            get;
            protected set;
        }
        public bool Success
        {
            get;
            protected set;
        }
        /// <summary>
        /// The character at the current position is alphabetic
        /// </summary>
        public ICharacterInfo Info
        {
            get
            {
                characterInfo.Target = Finished ? (char)0 : Current;
                return characterInfo;
            }
        }
        #endregion
        
        #region public methods
        /// <summary>
        /// Creates a new stringscanner instance from the current match
        /// </summary>
        /// <returns></returns>
        public IStringScanner ToNewScanner()
        {
            if (!Success)
            {
                throw new InvalidOperationException("The last operation was not successful; a new string scanner cannot be created.");
            }
            return Scanner.Create(Match);
        }

        /// <summary>
        /// Creates a new StringScanner instance from a string that is formatted using the current match
        /// as the single format argument.
        /// </summary>
        ///
        /// <exception cref="InvalidOperationException">
        /// Thrown when the prior operation failed.
        /// </exception>
        ///
        /// <param name="template">
        /// The string to use as a template
        /// </param>
        ///
        /// <returns>
        /// A new StringScanner
        /// </returns>

        public IStringScanner ToNewScanner(string template)
        {
            if (!Success)
            {
                throw new InvalidOperationException("The last operation was not successful; a new string scanner cannot be created.");
            }
            return Scanner.Create(String.Format(template,Match));
        }

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

        public bool Matches(string text)
        {
            return text.Length + Index <= Length && Text.Substring(Index, text.Length) == text;
        }

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

        public bool MatchesAny(IEnumerable<string> text)
        {
            foreach (string val in text)
            {
                if (Matches(val))
                {
                    return true;
                }
            }
            return false;
        }

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

        public IStringScanner Seek(char character, bool orEnd)
        {
            AssertNotFinished();
            CachePos();
            while (!Finished && Current!=character)
            {
                Next();
            }
            if (!orEnd)
            {
                AssertNotFinished();
            }
            Match = Text.Substring(cachedPos,Index-cachedPos);
            NewPos();
            return this;
        }

        protected void SkipWhitespaceImpl()
        {
            if (Finished)
            {
                return;
            }
            if (CharacterData.IsType(Current,CharacterType.Whitespace))
            {
                while (!Finished && CharacterData.IsType(Current,CharacterType.Whitespace))
                {
                    Next();
                }
            }
        }

        /// <summary>
        /// If the current character is whitespace, advances to the next non whitespace. Otherwise, nothing happens.
        /// </summary>
        public void SkipWhitespace()
        {
            CachePos();
            AutoSkipWhitespace();
            NewPos();
        }

        /// <summary>
        /// Advances to the next non-whitespace character
        /// </summary>
        public void NextNonWhitespace()
        {
            CachePos();
            NextNonWhitespaceImpl();
            NewPos();
        }
       
        public char Peek()
        {
            if (Index < Length - 1)
            {
                return Text[Index + 1];
            }
            else
            {
                return (char)0;
            }
        }
        /// <summary>
        /// Moves pointer forward one character, or to the position after the next match.
        /// </summary>
        /// <returns></returns>

        public bool Next()
        {
            return Move(1);
        }

        public bool Previous()
        {
            return Move(-1);
        }

        public bool Move(int offset)
        {
            if (Index+offset > Length)
            {
                ThrowException("Cannot advance beyond end of string.");
            }
            else if (Index + offset < 0)
            {
                ThrowException("Cannot reverse beyond beginning of string");
            }

            Index += offset;
            return Index < Length && Index>0;
        }
        /// <summary>
        /// Returns to the state before the last Expect. This is not affected by manual Next/Prev operations
        /// </summary>
        /// <returns></returns>
        public void Undo()
        {
            if (LastIndex < 0)
            {
                ThrowException("Can't undo - there's nothing to undo");
            }
            Index = LastIndex;
            Match = LastMatch;
            LastMatch = "";
            LastIndex = -1;

            NewPos();
        }


        public void AssertFinished(string errorMessage=null)
        {
            if (!Finished)
            {
                if (String.IsNullOrEmpty(errorMessage))
                {
                    ThrowUnexpectedCharacterException();
                }
                else
                {
                    ThrowException(errorMessage);
                }
            }
        }
        public void AssertNotFinished(string errorMessage=null)
        {
            if (Finished)
            {
                if (String.IsNullOrEmpty(errorMessage))
                {
                    ThrowUnexpectedCharacterException();
                }
                else
                {
                    ThrowException(errorMessage);
                }
            }
        }
        public void Reset()
        {
            Index = 0;
            LastIndex = -1;
            Match = "";
            LastError = "";
            Success = true;
        }

        /// <summary>
        /// Moves the pointer past the last character postion.
        /// </summary>

        public void End()
        {
            CachePos();
            NewPos(Length);
        }


        public IStringScanner Expect(string text)
        {
            AssertNotFinished();
            CachePos();
            AutoSkipWhitespace();
            if (Matches(text))
            {
                Match = Text.Substring(Index, text.Length);
                NewPos(Index+text.Length);
            }
            else
            {
                ThrowUnexpectedCharacterException();
            }
            return this;
        }
        public string Get(params string[] values)
        {
            Expect((string[])values);
            return Match;
        }
        public void Expect(params string[] values)
        {
            Expect((IEnumerable<string>)values);
        }
        
        public string Get(IEnumerable<string> stringList)
        {
            Expect(stringList);
            return Match;
        }
        public bool TryGet(IEnumerable<string> stringList, out string result)
        {
            return TryWrapper(()=> {
                Expect(stringList);
            },out result);
        }

        public void Expect(IEnumerable<string> stringList)
        {
            AssertNotFinished();
            CachePos();
            AutoSkipWhitespace();
            string startChars = "";
            foreach (string expected in stringList)
            {
                startChars+=expected[0];
            }
            if (ExpectCharImpl(startChars))
            {
                foreach (string expected in stringList)
                {
                    if (Matches(expected))
                    {
                        Match = Text.Substring(Index, expected.Length);
                        NewPos(Index + expected.Length);
                        return;
                    }
                }
            }
            ThrowUnexpectedCharacterException();
        }

        public char GetChar(char character) {
            ExpectChar(character);
            return Match[0];
        }
        public bool TryGetChar(char character, out string result)
        {
            return TryWrapper(() =>
            {
                ExpectChar(character);
            }, out result);
        }
        /// <summary>
        /// If current character (or next non-whitespace character) is not the expected value, then an error is thrown
        /// </summary>
        /// <param name="character"></param>
        /// <returns></returns>
        /// 
        public IStringScanner ExpectChar(char character)
        {
            AssertNotFinished();
            CachePos();
            AutoSkipWhitespace();

            if (Current == character)
            {
                Match = Current.ToString();
                Next();
                NewPos();
            }
            else
            {
                ThrowUnexpectedCharacterException();
            }
            return this;
        }
        public char GetChar(string characters)
        {
            return GetChar(characters.ToCharArray());
        }
        public bool TryGetChar(string characters, out string result)
        {
            return TryWrapper(() =>
            {
                Expect(characters);
            }, out result);
        }


        public IStringScanner ExpectChar(params char[] characters)
        {
            return ExpectChar((IEnumerable<char>)characters);
        }
        public char GetChar(IEnumerable<char> characters)
        {
            ExpectChar((IEnumerable<char>)characters);
            return Match[0];
        }
        public bool TryGetChar(IEnumerable<char> characters, out string result)
        {
            return TryWrapper(() =>
            {
                ExpectChar((IEnumerable<char>)characters);
            }, out result);
        }
        /// If one of the current characters (or next non-whitespace character) is not the expected value, then an error is thrown
        public IStringScanner ExpectChar(IEnumerable<char> characters)
        {
            AssertNotFinished();
            CachePos();
            AutoSkipWhitespace();
            if (ExpectCharImpl(characters))
            {
                Match = Current.ToString();
                Next();
                NewPos();
            }
            else
            {
                ThrowUnexpectedCharacterException();
            }
            return this;
        }
        protected bool ExpectCharImpl(IEnumerable<char> characters)
        {
            //HashSet<char> expected = new HashSet<char>(characters);
            foreach (char item in characters) {
                if (item==Current) {
                    return true;
                }
            }
            return false;

        }
        

        public string GetNumber()
        {
            ExpectNumber();
            return Match;
        }
        public bool TryGetNumber(out string result)
        {
            return TryWrapper(() =>
            {
                ExpectNumber();
            }, out result);
        }
        public bool TryGetNumber<T>(out T result) where T:IConvertible 
        {
            string stringResult;
            bool gotNumber = TryWrapper(() =>
            {
                ExpectNumber();
            }, out stringResult);

            if (gotNumber)
            {
                result = (T)Convert.ChangeType(stringResult, typeof(T));
                return true;
            }
            else
            {
                result = default(T);
                return false;
            }
        }
        public bool TryGetNumber(out int result)
        {
            double doubleResult;
            if (TryGetNumber(out doubleResult))
            {
                result = Convert.ToInt32(doubleResult);
                return true;
            }
            else
            {
                result = 0;
                return false;
            }
        }

        /// <summary>
        /// Starting with the current character, treats text as a number, seeking until the next
        /// character that would terminate a valid number.
        /// </summary>
        ///
        /// <param name="requireWhitespaceTerminator">
        /// (optional) the require whitespace terminator.
        /// </param>
        ///
        /// <returns>
        /// .
        /// </returns>

        public IStringScanner ExpectNumber(bool requireWhitespaceTerminator = false)
        {
            return Expect(MatchFunctions.Number(requireWhitespaceTerminator));
        }
        public bool TryGetAlpha(out string result)
        {
            return TryWrapper(() =>
            {
                GetAlpha();
            }, out result);
        }
        public string GetAlpha()
        {
            ExpectAlpha();
            return Match;
        }

        /// <summary>
        /// Starting with the current character, seeks until a non-alpha character is found
        /// </summary>
        /// <returns></returns>
        public IStringScanner ExpectAlpha()
        {
            return Expect(MatchFunctions.Alpha);
            
        }

        public string Get(IExpectPattern pattern)
        {
            ExpectImpl(pattern);
            return Match;
        }
        public bool TryGet(IExpectPattern pattern, out string result)
        {
            return TryWrapper(() =>
            {
                Expect(pattern);
            }, out result);
        }

        /// <summary>
        /// Continue seeking as long as the delegate returns true.
        /// </summary>
        ///
        /// <param name="pattern">
        /// A class specifying the pattern to match.
        /// </param>
        ///
        /// <returns>
        /// The string scanner.
        /// </returns>

        public IStringScanner Expect(IExpectPattern pattern)
        {
            ExpectImpl(pattern);
            return this;
        }
        public string Get(Func<int, char, bool> validate)
        {
            Expect(validate);
            return Match;
        }
        public bool TryGet(Func<int, char, bool> validate, out string result)
        {
            return TryWrapper(() =>
            {
                Expect(validate);
            }, out result);
        }

        /// <summary>
        /// Continue seeking as long as the delegate returns True.
        /// </summary>
        ///
        /// <param name="validate">
        /// A pattern matching function
        /// </param>
        ///
        /// <returns>
        /// This IStringScanner instance
        /// </returns>

        public IStringScanner Expect(Func<int, char, bool> validate)
        {
            AssertNotFinished();
            CachePos();
            AutoSkipWhitespace();
            int startPos = Index;
            int index = 0;
            while (!Finished && validate(index, Current))
            {
                Index++;
                index++;
            }
            if (Index > startPos)
            {
                Match = Text.SubstringBetween(startPos, Index);
                NewPos();
            }
            else
            {
                ThrowUnexpectedCharacterException();
            }
            return this;
        }

        /// <summary>
        /// Expects a string bounded by the character at the current postion. If the current character is
        /// a bounding character, then the pattern will match until the matching closing bound character
        /// is found, e.g. () [] {} &lt;&gt;. For non-bounding characters, the pattern will match until
        /// the same character is found again.
        /// </summary>
        ///
        /// <param name="start">
        /// The position to start scanning.
        /// </param>
        /// <param name="end">
        /// The last position.
        /// </param>
        /// <param name="allowQuoting">
        /// (optional) the allow quoting.
        /// </param>
        ///
        /// <returns>
        /// The bounded by.
        /// </returns>

        public string GetBoundedBy(string start, string end, bool allowQuoting=false)
        {
            ExpectBoundedBy(start, end);
            return Match;
        }
        public bool TryGetBoundedBy(string start, string end, bool allowQuoting, out string result)
        {
            return TryWrapper(() =>
            {
                ExpectBoundedBy(start,end,allowQuoting);
            }, out result);
        }
        public IStringScanner ExpectBoundedBy(string start, string end, bool allowQuoting = false)
        {
            var boundedBy = new Patterns.Bounded();
            boundedBy.BoundStart = start;
            boundedBy.BoundEnd = end;
            boundedBy.HonorInnerQuotes=allowQuoting;
            return Expect(boundedBy);
        }

        /// <summary>
        /// The single character bound will be matched with a closing char for () [] {} &lt;&gt; or the
        /// same char for anything else.
        /// </summary>
        ///
        /// <param name="bound">
        /// .
        /// </param>
        /// <param name="allowQuoting">
        /// (optional) the allow quoting.
        /// </param>
        ///
        /// <returns>
        /// The bounded by.
        /// </returns>

        public string GetBoundedBy(char bound, bool allowQuoting=false)
        {
            ExpectBoundedBy(bound, allowQuoting);
            return Match;
        }

        /// <summary>
        /// Require that the text starting at the current position matches a pattern which is bounded by
        /// a specific character, with the inner value opotionally quoted with a quote character ' or ".
        /// </summary>
        ///
        /// <param name="bound">
        /// The bounding character.
        /// </param>
        /// <param name="allowQuoting">
        /// (optional) the allow quoting.
        /// </param>
        ///
        /// <returns>
        /// The current string scanner.
        /// </returns>

        public IStringScanner ExpectBoundedBy(char bound, bool allowQuoting = false)
        {
            var boundedBy = new Patterns.Bounded();
            boundedBy.BoundStart = bound.ToString();
            boundedBy.HonorInnerQuotes = allowQuoting;
            return Expect(boundedBy);
        }
        public override string ToString()
        {
            return Text;
        }



        private StringScannerEngine ExpectImpl(IExpectPattern pattern)
        {
            AssertNotFinished();
            CachePos();
            AutoSkipWhitespace();
            int startPos = Index;

            pattern.Initialize(Index, Chars);
            // call the function one more time after the end of the string - this determines outcome
            if (pattern.Validate())
            {
                Match = pattern.Result;
                NewPos(pattern.EndIndex);
            }
            else
            {
                Index = pattern.EndIndex; // for error report to be accurate - will be undone at end
                ThrowUnexpectedCharacterException();
            }
            return this;
        }
        protected bool TryWrapper(Action action, out string result)
        {
            SuppressErrors = true;
            action();
            SuppressErrors = false;
            if (Success)
            {
                result = Match;
            }
            else
            {
                result = "";
            }
            return Success;
        }

        [DebuggerStepThrough]
        protected void ThrowUnexpectedCharacterException()
        {
            if (Index >= Length)
            {
                ThrowUnexpectedEndOfStringException();
            }
            else
            {
                ThrowException("Unexpected character found",Index);
            }
        }
        [DebuggerStepThrough]
        protected void ThrowUnexpectedEndOfStringException()
        {
            ThrowException("The string unexpectedly ended",Index);
        }
        [DebuggerStepThrough]
        protected void ThrowException(string message)
        {
            ThrowException(message, -1);
        }
        //[DebuggerStepThrough]
        protected void ThrowException(string message, int errPos)
        {
            string error = message;
            int pos = -1;
            if (String.IsNullOrEmpty(Text))
            {
                error = " -- the string is empty.";
            }
            else
            {
                pos = Math.Min(errPos + 1, Length - 1);
            }

            RestorePos();

            if (pos >= 0)
            {
                error += " at position " + pos + ": \"";

                if (Index != pos)
                {
                    if (Index > 0 && Index < Length)
                    {
                        error += ".. ";
                    }
                    error += Text.SubstringBetween(Math.Max(Index - 10, 0), pos) + ">>" + Text[pos] + "<<";
                    if (pos < Length - 1)
                    {
                        error += Text.SubstringBetween(pos + 1, Math.Min(Length, pos + 30));
                    }
                    error += "\"";
                }
            }
            
            LastError = error;

            if (SuppressErrors)
            {
                Success = false;
            }
            else
            {
                throw new ArgumentException(error);
            }
        }
        #endregion
        
        #region private methods

        protected void AutoSkipWhitespace()
        {
            if (IgnoreWhitespace)
            {
                SkipWhitespaceImpl();
            }
        }
        protected void NextNonWhitespaceImpl()
        {
            Next();
            SkipWhitespaceImpl();
        }

        /// <summary>
        /// Cache the last pos before an attempted operation,.
        /// </summary>
        ///
        /// <exception cref="InvalidOperationException">
        /// Thrown when there is already something cached.
        /// </exception>

        protected void CachePos()
        {
            LastError = "";
            Success = true;
            if (cached)
            {
                throw new InvalidOperationException("Internal error: already cached");
            }
            cached = true;
            cachedPos = Index;
            cachedMatch = Match;
        }
        /// <summary>
        /// Sets the current position, updates the last pos from cache, and clears any current match. If the cached position is the same
        /// as the current position, nothing is done.
        /// </summary>
        protected void NewPos(int pos)
        {
            Index = pos;
            NewPos();
        }
        protected void NewPos()
        {
            if (Index != cachedPos)
            {
                LastIndex = cachedPos;
                LastMatch = cachedMatch;
            }
            cached = false;
            
        }
        /// <summary>
        /// Restores position from cache
        /// </summary>
        protected void RestorePos()
        {
            if (cached)
            {
                Index = cachedPos;
                Match= cachedMatch;
                cached = false;
            }
        }
        protected string LookupsToString(HashSet<char> list)
        {
            string charString = String.Empty;
            foreach (char item in list)
            {
                charString += item;
            }
            return charString;
        }

      
        
        #endregion
    }


}