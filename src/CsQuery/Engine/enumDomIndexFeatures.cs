using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Engine
{
    /// <summary>
    /// Flags specifying the features that a given IndexProvider offers
    /// </summary>

    [Flags]
    public enum DomIndexFeatures
    {
        /// <summary>
        /// Index is capable of returning a sequence of elements matching a key
        /// </summary>
        Lookup = 1,
        /// <summary>
        /// Index is capable of returning a range of elements matching a subkey.
        /// </summary>
        Range = 2,
        /// <summary>
        /// Indexes implementing this feature can queue changes to improve performance. When this is true, the QueueChanges method must be implemented.
        /// </summary>
        Queue = 4
    }
}
