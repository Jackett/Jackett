using System;
using System.Collections.Generic;
using System.Collections;
using System.Text.RegularExpressions;
using System.Linq;
using System.Dynamic;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using CsQuery;

namespace CsQuery.StringScanner.ExtensionMethods
{
    /// <summary>
    /// Extension methods used by CsQuery but not specialized enough to be considered useful for clients; therefore
    /// in a separate namespace.
    /// </summary>
    public static class ExtensionMethods
    {

        /// <summary>
        /// Returns the text between startIndex and endIndex (exclusive of endIndex)
        /// </summary>
        /// <param name="text"></param>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        /// <returns></returns>
        public static string SubstringBetween(this string text, int startIndex, int endIndex)
        {
            if (endIndex > text.Length || endIndex < 0)
            {
                return "";
            }
            return (text.Substring(startIndex, endIndex - startIndex));
        }

        /// <summary>
        /// Returns the text between startIndex and endIndex (exclusive of endIndex)
        /// </summary>
        ///
        /// <param name="text">
        /// The source text
        /// </param>
        /// <param name="startIndex">
        /// The start index
        /// </param>
        /// <param name="endIndex">
        /// The end index
        /// </param>
        ///
        /// <returns>
        /// The substring, or an empty string if the range was not within the string.
        /// </returns>

        public static string SubstringBetween(this char[] text, int startIndex, int endIndex)
        {
            int len = endIndex - startIndex + 1;
            string result = "";
            for (int i = startIndex; i < endIndex; i++)
            {
                result += text[i];
            }
            return result;
        }
    }
}
