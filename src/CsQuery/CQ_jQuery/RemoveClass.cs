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
        /// Remove all classes from each element in the set of matched elements.
        /// </summary>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/removeClass/
        /// </url>

        public CQ RemoveClass()
        {
            Elements.ForEach(item =>
            {
                item.ClassName = "";
            });
            return this;
        }

        /// <summary>
        /// Remove one or more classess from each element in the set of matched elements.
        /// </summary>
        ///
        /// <param name="className">
        /// One or more space-separated classes to be removed from the class attribute of each matched
        /// element.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>

        public CQ RemoveClass(string className)
        {

            foreach (IDomElement e in Elements)
            {
                if (!String.IsNullOrEmpty(className))
                {
                    e.RemoveClass(className);
                }
            }
            return this;
        }
        

    }
}
