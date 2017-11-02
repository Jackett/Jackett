using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery
{
    /// <summary>
    /// Interface for methods to access the attributes on a DOM element.
    /// </summary>

    public interface IAttributeCollection: IEnumerable<KeyValuePair<string,string>>
    {
        /// <summary>
        /// Get the value of a named attribute
        /// </summary>
        /// <param name="name">The attribute name</param>
        /// <returns>The attribute value</returns>
        string GetAttribute(string name);

        /// <summary>
        /// Set the value of a named attribute
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        void SetAttribute(string name, string value);

        /// <summary>
        /// Get or set the value of a named attribute
        /// </summary>
        /// <param name="attributeName">The attribute name</param>
        /// <returns>The attribute value</returns>
        /// <returntype>string</returntype>
        string this[string attributeName] { get; set; }

        /// <summary>
        /// The number of attributes in this attribute collection. This includes special attributes such as
        /// "class", "id", and "style"
        /// </summary>
        /// <returntype>int</returntype>
        int Length { get; }
    }
}
