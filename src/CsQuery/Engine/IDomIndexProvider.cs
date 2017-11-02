using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Engine
{
    /// <summary>
    /// Interface for a service locator providing in instance of a DomIndex
    /// </summary>

    public interface IDomIndexProvider
    {
        /// <summary>
        /// Return an instance of a DomIndex class
        /// </summary>

        IDomIndex GetDomIndex();
    }
}
