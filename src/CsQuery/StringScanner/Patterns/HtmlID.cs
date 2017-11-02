using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.StringScanner.Implementation;

namespace CsQuery.StringScanner.Patterns
{
    /// <summary>
    /// ID can contain any character other than a space.
    /// </summary>
    public class HtmlID: EscapedString
    {
        /// <summary>
        /// Default constructor.
        /// </summary>

        public HtmlID(): 
            base(IsValidID)
        {

        }

        /// <summary>
        /// Match a pattern for a valid HTML ID.
        /// </summary>
        ///
        /// <param name="index">
        /// The index to match
        /// </param>
        /// <param name="character">
        /// The character to match
        /// </param>
        ///
        /// <returns>
        /// true if valid identifier, false if not.
        /// </returns>

        private static bool IsValidID(int index, char character)
        {

            return !CharacterData.IsType(character, CharacterType.HtmlSpace);
        }
    }
}
