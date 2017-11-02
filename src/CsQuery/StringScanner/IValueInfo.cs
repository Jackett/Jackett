using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.StringScanner
{
    /// <summary>
    /// Interface for characterstics of a value, either a single character or a string.
    /// </summary>

    public interface IValueInfo
    {
        /// <summary>
        /// The value is alphabetic
        /// </summary>

        bool Alpha { get; }

        /// <summary>
        /// The value is numeric.
        /// </summary>

        bool Numeric { get; }

        /// <summary>
        /// The value is numeric, or characters that can be parts of numbers (+,-,.)
        /// </summary>

        bool NumericExtended { get; }

        /// <summary>
        ///The value is all lowercase
        /// </summary>

        bool Lower { get; }

        /// <summary>
        /// Gets a value indicating whether the cvale upper.
        /// </summary>

        bool Upper { get; }

        /// <summary>
        /// The value is whitespace.
        /// </summary>

        bool Whitespace { get; }

        /// <summary>
        /// The value is alphanumeric.
        /// </summary>

        bool Alphanumeric { get; }

        /// <summary>
        /// The value is a math operator
        /// </summary>

        bool Operator { get; }

        /// <summary>
        /// Indicates that a character is alphabetic-like character defined as a-z, A-Z, hyphen,
        /// underscore, and ISO 10646 code U+00A1 and higher. (per characters allowed in CSS identifiers)
        /// </summary>

        bool AlphaISO10646 { get; }

        /// <summary>
        /// The bound character or string for this instance. This is the character against which all
        /// tests are performed.
        /// </summary>

        IConvertible Target { get; set; }
    }



}
