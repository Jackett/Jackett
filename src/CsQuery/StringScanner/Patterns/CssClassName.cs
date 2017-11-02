using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.StringScanner.Implementation;

namespace CsQuery.StringScanner.Patterns
{
    /// <summary>
    /// A pattern that matches a valid CSS class name
    /// </summary>

    public class CssClassName: EscapedString
    {
        /// <summary>
        /// Default constructor.
        /// </summary>

        public CssClassName() : 
            base(IsValidClassName)
        {

        }
        /// <summary>
        /// Match a pattern for a CSS class name selector
        /// TODO - doesn't validate hyphen-digit combo.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="character"></param>
        /// <returns></returns>
        protected static bool IsValidClassName(int index, char character)
        {
            
            if (index == 0)
            {
                return CharacterData.IsType(character, CharacterType.AlphaISO10646);
            }
            else
            {
                return CharacterData.IsType(character, CharacterType.AlphaISO10646 | CharacterType.Number);
            }

        }
    }
}
