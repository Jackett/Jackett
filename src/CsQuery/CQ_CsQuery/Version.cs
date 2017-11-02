using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
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
        /// Return the current assembly's version.
        /// </summary>
        ///
        /// <returns>
        /// A string
        /// </returns>

        public static string Version()
        {
            return typeof(CQ).GetTypeInfo().Assembly.GetName().Version.ToString();
        }
    }
}
