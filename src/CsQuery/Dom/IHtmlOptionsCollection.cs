using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;

namespace CsQuery
{
    /// <summary>
    /// Interface to a collection of HTML options.
    /// </summary>
    ///
    /// <url>
    /// https://developer.mozilla.org/en/DOM/HTMLOptionsCollection
    /// </url>

    public interface IHtmlOptionsCollection: IEnumerable<IDomObject>
    {
        /// <summary>
        /// Returns the specific node at the given zero-based index (gives null if out of range)
        /// </summary>
        ///
        /// <param name="index">
        /// The zero-based index of the option element.
        /// </param>
        ///
        /// <returns>
        /// An HTML Option element.
        /// </returns>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/HTMLOptionsCollection
        /// </url>

        IDomElement Item(int index);

        /// <summary>
        /// Returns the specific node at the given zero-based index (gives null if out of range)
        /// </summary>
        ///
        /// <param name="index">
        /// The zero-based index of the option element.
        /// </param>
        ///
        /// <returns>
        /// An HTML Option element.
        /// </returns>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/HTMLOptionsCollection
        /// </url>

        [IndexerName("Indexer")]
        IDomElement this[int index] { get; }

        /// <summary>
        /// Returns the specific node with the given DOMString (i.e., string) id. Returns null if no such named node exists.
        /// </summary>
        ///
        /// <param name="name">
        /// The zero-based index of the option element.
        /// </param>
        ///
        /// <returns>
        /// An HTML Option element.
        /// </returns>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/HTMLOptionsCollection
        /// </url>
        
        IDomElement NamedItem(string name);

        
        /// <summary>
        /// Returns the specific node with the given DOMString (i.e., string) id. Returns null if no such named node exists.
        /// </summary>
        ///
        /// <param name="name">
        /// The zero-based index of the option element.
        /// </param>
        ///
        /// <returns>
        /// An HTML Option element.
        /// </returns>
        ///
        /// <url>
        /// https://developer.mozilla.org/en/DOM/HTMLOptionsCollection
        /// </url>

        [IndexerName("Indexer")]
        IDomElement this[string name] {get;}
        
    }
}
