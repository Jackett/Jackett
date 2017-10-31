using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery
{
    /// <summary>
    /// DOCTYPE node
    /// </summary>
    public interface IDomDocumentType : IDomSpecialElement
    {
        /// <summary>
        /// Gets the type of the document.
        /// </summary>

        DocType DocType { get; }
    }
}
