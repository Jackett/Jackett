using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using CsQuery.StringScanner.Implementation;

// TODO this should be fully commented; however it's not part of the main public API

#pragma warning disable 1591
#pragma warning disable 1570

namespace CsQuery.StringScanner.Patterns
{
    /// <summary>
    /// Match a string pattern against a particular character validation function, but allow the backslash to escape 
    /// any character.
    /// </summary>
    public class EscapedString: ExpectPattern
    {
        /// <summary>
        /// Default constructor -- simply parses escapes until the end of the string
        /// </summary>

        public EscapedString(): this(AlwaysValid)
        {
            
        }
        public EscapedString(Func<int,char, bool> validCharacter)
        {
            ValidCharacter = validCharacter;
        }
        private static bool AlwaysValid(int index, char character)
        {
            return true;
        }
        protected Func<int, char, bool> ValidCharacter;
        protected bool Escaped;

        public override bool Validate()
        {
            int index = StartIndex;
            int relativeIndex = 0;
            bool done=false;
            Result="";

            while (index<Source.Length && !done)
            {
                char character = Source[index];
                if (!Escaped && character == '\\')
                {
                    Escaped = true;
                }
                else
                {
                    if (Escaped)
                    {
						// process unicode char code point, if presented
						int tempIndex = index;
						StringBuilder sb = new StringBuilder();

                        while (tempIndex < Source.Length // end of string?
                            && tempIndex - index < 6     // only 6 hexadecimal digits are allowed
                            && CharacterData.IsType(Source[tempIndex],CharacterType.Hexadecimal))
						{
							sb.Append(Source[tempIndex]);

							tempIndex++;
						}

                        if (sb.Length >= 1)
                        {
                            int value = 0;
                            if (Int32.TryParse(sb.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
                            {
                                character = (char)value;
                                index = tempIndex;
                            }

                            // If the escape sequence is <6 characters and was terminated by whitespace, then skip the whitespace.

                            if (sb.Length < 6
                                && index < Source.Length
                                && CharacterData.IsType(Source[index], CharacterType.Whitespace))
                            {
                                index++;
                                relativeIndex++;
                            }

                            // decrement because it will be incremented again outside this part of the loop
                            index--;
                            relativeIndex--;
                        }
                        else
                        {

                            // means the escaped character wasn't hex, so just pass it through
                            sb.Append(Source[tempIndex]);

                        }
												
                        Escaped = false;
                    }
                    else
                    {
                        if (!ValidCharacter(relativeIndex,character))
                        {
                            done = true;
                            continue;
                        }
                    }
                    Result += character;
                }
                index++;
                relativeIndex++;
            }
            bool failed = Escaped;
            EndIndex = index;
    
            // should not have passed the end
            if (EndIndex > Length || EndIndex == StartIndex || failed)
            {
                Result = "";
                return false;
            }
         
            return true;
        }

        protected bool failed = false;
       
    }
}
