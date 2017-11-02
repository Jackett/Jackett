using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Engine
{
    /// <summary>
    /// A pseudoselector that filters a list of elements. Most jQuery extensions fall within this
    /// category.
    /// </summary>

    public interface IPseudoSelectorFilter: IPseudoSelector
    {
        /// <summary>
        /// Filter only the elements matching this result-list position type selector.
        /// </summary>
        ///
        /// <param name="selection">
        /// The sequence of elements prior to this filter being applied.
        /// </param>
        ///
        /// <returns>
        /// A sequence of matching elements.
        /// </returns>

        IEnumerable<IDomObject> Filter(IEnumerable<IDomObject> selection);

    }

}
