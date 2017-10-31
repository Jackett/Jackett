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
        /// Create a new, empty CsQuery object bound to this domain.
        /// </summary>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>

        public CQ NewCqInDomain()
        {
            CQ csq = NewCqUnbound();
            csq.CsQueryParent = this;
            
            return csq;
        }

        /// <summary>
        /// Creates a new instance of the CQ object. This should be used inside CQ to create a new object
        /// under all circumstances so it can be overridden by derived classes.
        /// </summary>
        ///
        /// <returns>
        /// A new CQ object
        /// </returns>

        protected virtual CQ NewCqUnbound()
        {
            return new CQ();
        }
    }
}
