using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Engine
{
    /// <summary>
    /// General interface for a pseudoselector filter.
    /// </summary>

    public interface IPseudoSelector
    {
        /// <summary>
        /// This method is called before any validations are called against this selector. This gives the
        /// developer an opportunity to throw errors based on the configuration outside of the validation
        /// methods.
        /// </summary>

        string Arguments { get; set; }

        /// <summary>
        /// The minimum number of parameters that this selector requires. If there are no parameters, return 0
        /// </summary>
        ///
        /// <value>
        /// An integer
        /// </value>

        int MinimumParameterCount { get; }

        /// <summary>
        /// The maximum number of parameters that this selector can accept. If there is no limit, return -1.
        /// </summary>
        ///
        /// <value>
        /// An integer
        /// </value>

        int MaximumParameterCount { get; }

        /// <summary>
        /// Gets CSS name of the pseudoselector
        /// </summary>

        string Name { get; }
    }

}