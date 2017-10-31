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
        /// Map properties of inputObjects to target. If target is an expando object, it will be updated.
        /// If not, a new one will be created including the properties of target and inputObjects.
        /// </summary>
        ///
        /// <param name="target">
        /// The target of the mapping, or null to create a new target.
        /// </param>
        /// <param name="sources">
        /// One or more objects that are the source of the mapping.
        /// </param>
        ///
        /// <returns>
        /// The target object itself, if non-null, or a new dynamic object, if the target is null.
        /// </returns>

        public static object Extend(object target, params object[] sources)
        {
            return Objects.Extend(false, target, sources);
        }

        /// <summary>
        /// Map properties of inputObjects to target. If target is an expando object, it will be updated.
        /// If not, a new one will be created including the properties of target and inputObjects.
        /// </summary>
        ///
        /// <param name="deep">
        /// When true, will clone properties that are objects.
        /// </param>
        /// <param name="target">
        /// The target of the mapping, or null to create a new target.
        /// </param>
        /// <param name="sources">
        /// One or more objects that are the source of the mapping.
        /// </param>
        ///
        /// <returns>
        /// The target object itself, if non-null, or a new dynamic object, if the target is null.
        /// </returns>

        public static object Extend(bool deep, object target, params object[] sources)
        {
            return Objects.Extend(deep, target, sources);
        }
    }
}
