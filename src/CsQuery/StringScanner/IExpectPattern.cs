using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.StringScanner
{
    /// <summary>
    /// An interface for pattern matching.
    /// 
    /// Something implementing this interface will be used as follows:
    /// 
    /// First, Initialize is called, passing in the source and the starting index where scanning
    /// should begin.
    /// 
    /// The Validate function then scans the string, and returns true if a valid match is found, and
    /// false if not.
    /// 
    /// The Result property should be populated by the function with the matching string, and the
    /// EndIndex property should be populated with the last position scanned (one after the last
    /// valid character that was returned). If no valid string was matched, EndIndex should equal the
    /// original StartIndex.
    /// </summary>

    public interface IExpectPattern
    {
        /// <summary>
        /// Initializes the pattern
        /// </summary>
        ///
        /// <param name="startIndex">
        /// The start index.
        /// </param>
        /// <param name="source">
        /// Source for the.
        /// </param>

        void Initialize(int startIndex, char[] source);

        /// <summary>
        /// Validate the string and try to match something.
        /// </summary>
        ///
        /// <returns>
        /// true if a matching string was found, false if not.
        /// </returns>

        bool Validate();

        /// <summary>
        /// When a valid string was found, the string.
        /// </summary>
        ///
        /// <value>
        /// A string.
        /// </value>

        string Result { get; }

        /// <summary>
        /// Gets zero-based index of the ending postion. This is one position after the last matching
        /// character.
        /// </summary>
        ///
        /// <value>
        /// The end index.
        /// </value>

        int EndIndex { get; }
    }
}
