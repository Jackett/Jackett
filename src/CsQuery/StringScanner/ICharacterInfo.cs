using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.StringScanner
{
    /// <summary>
    /// An interface that describes characterstics of a single character.
    /// </summary>

    public interface ICharacterInfo : IValueInfo<char>
    {
        /// <summary>
        /// The character is an opening or closing parenthesis.
        /// </summary>

        bool Parenthesis { get; }

        /// <summary>
        /// The character is an enclosing type such as a parenthesis or curly brace (anything which has a
        /// matching close that's not the same as the opening; this specifically excludes single and
        /// double-quote characters).
        /// </summary>

        bool Enclosing { get; }

        /// <summary>
        /// Gets a value indicating whether the character is any bounding type (includes all Enclosing types, plus quotes).
        /// </summary>

        bool Bound { get; }

        /// <summary>
        /// Gets a value indicating whether the character is a quote.
        /// </summary>

        bool Quote { get; }

        /// <summary>
        /// Gets a value indicating whether the character is a separator (a space, or pipe)
        /// </summary>

        bool Separator { get; }
    }
}
