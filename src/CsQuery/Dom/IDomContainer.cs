using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery
{
    
    /// <summary>
    /// Interface for objects that can contain other objects. Note that to allow some consistency with how DOM
    /// objects are used in the browser DOM, many methods are part of the base IDomObject interface so that they
    /// can be used (and return null/missing data) on elements to which they don't apply. So in actuality the only 
    /// unique methods are nonstandard ones.
    /// </summary>
    public interface IDomContainer : IDomObject
    {
        /// <summary>
        /// An enumeration of clones of the chilren of this object
        /// </summary>
        ///
        /// <returns>
        /// An enumerator 
        /// </returns>

        IEnumerable<IDomObject> CloneChildren();
    }
    
}
