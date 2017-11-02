using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery
{
    /// <summary>
    /// Values allowable for the Rel attribute
    /// </summary>

    public enum RelAnchor
    {
        /// <summary>
        /// Gives alternate representations of the current document.
        /// </summary>

        Alternate = 1,

        /// <summary>
        /// Gives a link to the current document's author.
        /// </summary>

        Author = 2,

        /// <summary>
        /// Gives the permalink for the nearest ancestor section.
        /// </summary>
        Bookmark = 3,

        /// <summary>
        /// Provides a link to context-sensitive help.
        /// </summary>

        Help = 4,

        ///// <summary>
        ///// Imports an icon to represent the current document.
        ///// </summary>

        //Icon = 5,

        /// <summary>
        /// Indicates that the main content of the current document is covered by the copyright license described by the referenced document
        /// </summary>

        License = 6,

        /// <summary>
        /// Indicates that the current document is a part of a series, and that the next document in the series is the referenced document.
        /// </summary>

        Next = 7,

        /// <summary>
        /// Indicates that the current document's original author or publisher does not endorse the referenced document.
        /// </summary>

        Nofollow = 8,

        /// <summary>
        /// Requires that the user agent not send an HTTP Referer (sic) header if the user follows the hyperlink.
        /// </summary>

        Noreferrer=9,

        /// <summary>
        /// Specifies that the target resource should be preemptively cached.
        /// </summary>

        Prefetch = 10,

        /// <summary>
        /// Indicates that the current document is a part of a series, and that the previous document in the series is the referenced document.
        /// </summary>

        Prev = 11,

        /// <summary>
        /// Gives a link to a resource that can be used to search through the current document and its related pages.
        /// </summary>

        Search = 12,

        ///// <summary>
        ///// Imports a stylesheet.
        ///// </summary>

        //Stylesheet = 13,

        /// <summary>
        /// Gives a tag (identified by the given address) that applies to the current document.
        /// </summary>

        Tag = 14
    }
}
