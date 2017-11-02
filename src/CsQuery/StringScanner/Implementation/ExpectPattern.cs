using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.StringScanner.Implementation
{
    /// <summary>
    /// Abstract base class for IExpectPattern. This implements some helper functions that are commonly used by patterns.
    /// </summary>

    public abstract class ExpectPattern: IExpectPattern 
    {
        /// <summary>
        /// ICharacterInfo wrapper arond the current character. This class provides methods to test a
        /// character for certain properties.
        /// </summary>

        protected ICharacterInfo info = CharacterData.CreateCharacterInfo();

        /// <summary>
        /// The source string being scanned
        /// </summary>

        protected char[] Source;

        /// <summary>
        /// The starting index within the source string
        /// </summary>

        protected int StartIndex;

        /// <summary>
        /// The total length of the source string
        /// </summary>

        protected int Length;

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

        public virtual void Initialize(int startIndex, char[] sourceText)
        {
            Source = sourceText;
            StartIndex = startIndex;
            Length = Source.Length;
        }

        /// <summary>
        /// Run the validation against the passed string
        /// </summary>
        ///
        /// <returns>
        /// Returns true if the pattern defined by this class is successfully matched, and false if not.
        /// </returns>

        public virtual bool Validate()
        {
            if (EndIndex > StartIndex)
            {
                Result = GetOuput(StartIndex,EndIndex, false);
                return true;
            } else {
                Result = "";
                return false;
            }
        }

        /// <summary>
        /// Gets or sets zero-based index of the ending postion. This is one position after the last
        /// matching character.
        /// </summary>
        ///
        /// <value>
        /// The end index.
        /// </value>

        public virtual int EndIndex
        {
            get;
            protected set;
        }

        /// <summary>
        /// When a valid string was found, the string.
        /// </summary>
        ///
        /// <value>
        /// A string.
        /// </value>

        public virtual string Result
        {
            get;
            protected set;
        }

        /// <summary>
        /// Test if a string matches a substring in the source
        /// </summary>
        ///
        /// <param name="startIndex">
        /// The index within the source string to begin scanning.
        /// </param>
        /// <param name="substring">
        /// The substring to match
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        protected bool MatchSubstring(int startIndex, string substring)
        {
            if (startIndex + substring.Length <= Source.Length)
            {
                for (int i = 0; i < substring.Length; i++)
                {
                    if (Source[startIndex + i] != substring[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Copy the source to an output string between startIndex and endIndex (exclusive), optionally
        /// unescaping part of it.
        /// </summary>
        ///
        /// <param name="startIndex">
        /// The starting index to begin copying.
        /// </param>
        /// <param name="endIndex">
        /// The ending index
        /// </param>
        /// <param name="honorQuotes">
        /// true to honor quotes within the output string, false to treat them as any other characer.
        /// </param>
        ///
        /// <returns>
        /// The ouput.
        /// </returns>

        protected string GetOuput(int startIndex, int endIndex, bool honorQuotes)
        {
            return GetOuput(startIndex, endIndex, honorQuotes, false);
        }

        /// <summary>
        /// Copy the source to an output string between startIndex and endIndex (exclusive), optionally
        /// unescaping part of it.
        /// </summary>
        ///
        /// <exception cref="InvalidOperationException">
        /// Thrown when the requested operation is invalid.
        /// </exception>
        ///
        /// <param name="startIndex">
        /// The starting index to begin copying.
        /// </param>
        /// <param name="endIndex">
        /// The ending index.
        /// </param>
        /// <param name="honorQuotes">
        /// true to honor quotes within the output string, false to treat them as any other characer.
        /// </param>
        /// <param name="stripQuotes">
        /// true to strip quotes.
        /// </param>
        ///
        /// <returns>
        /// The ouput.
        /// </returns>

        protected string GetOuput(int startIndex, int endIndex, bool honorQuotes, bool stripQuotes)
        {
            bool quoted = false;

            char quoteChar=(char)0;
            StringBuilder sb = new StringBuilder();
            int index=startIndex;
            if (endIndex <= index) {
                return "";
            }
            if (stripQuotes && CharacterData.IsType(Source[index], CharacterType.Quote))
            {
                quoted = true;
                quoteChar = Source[index];
                index++;
                endIndex--;
            }
            while (index<endIndex)
            {
                char current = Source[index];
                info.Target = current;
                if (honorQuotes)
                {
                    if (!quoted)
                    {
                        if (info.Quote)
                        {
                            quoted = true;
                            quoteChar = current;
                        }
                    }
                    else
                    {
                        if (current == quoteChar)
                        {
                            quoted = false;
                        }

                        // Do not handle escaping here ever - leave this to the user to handle as needed.
                        
                        //if (current == '\\')
                        //{
                        //    char newChar;
                        //    index++;
                        //    if (TryParseEscapeChar(Source[index], out newChar))
                        //    {
                        //        current = newChar;
                        //    }
                        //    else
                        //    {
                        //        throw new InvalidOperationException("Invalid escape character found in quoted string: '" + Source[index] + "'");
                        //    }
                        //}
                    }
                }
                sb.Append(current);
                index++;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Try parse escape character.
        /// </summary>
        ///
        /// <param name="character">
        /// The character.
        /// </param>
        /// <param name="newValue">
        /// [out] The new value.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        protected bool TryParseEscapeChar(char character, out char newValue)
        {
            switch (character)
            {
                case '\\':
                case '"':
                case '\'':
                    newValue = character;
                    break;
                case 'n':
                    newValue = '\n';
                    break;
                default:
                    newValue = ' ';
                    return false;
            }
            return true;
        }
    }
}
