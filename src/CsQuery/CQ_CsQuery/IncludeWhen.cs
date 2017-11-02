using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.Utility;
using CsQuery.ExtensionMethods;
using CsQuery.ExtensionMethods.Internal;
using CsQuery.Engine;
using CsQuery.Implementation;

namespace CsQuery
{
    public partial class CQ
    {


        /// <summary>
        /// Conditionally includes a selection. This is the equivalent of calling Remove() only when
        /// "include" is false.
        /// </summary>
        ///
        /// <param name="include">
        /// true to include, false to exclude.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object
        /// </returns>

        public CQ IncludeWhen(bool include)
        {
            if (!include)
            {
                Remove();
            }
            return this;
        }
    }
}
