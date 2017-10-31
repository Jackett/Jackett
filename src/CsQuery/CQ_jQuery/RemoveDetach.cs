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
        /// Remove all selected elements from the Document.
        /// </summary>
        ///
        /// <param name="selector">
        /// A selector expression that filters the set of matched elements to be removed.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/remove/
        /// </url>

        public CQ Remove(string selector = null)
        {
            var list = !String.IsNullOrEmpty(selector) ?
                Filter(selector).SelectionSet :
                SelectionSet;

            // We need to copy first because selection can change
            List<IDomObject> removeList = list.ToList();

            List<bool> disconnected = list.Select(item => item.IsDisconnected ||
                item.Document != Document).ToList();

            
            for (int index = 0; index < removeList.Count; index++)
            {
                var el = removeList[index];
                if (disconnected[index])
                {
                    list.Remove(el);
                }
                if (el.ParentNode != null)
                {
                    el.Remove();
                }
            }
            return this;
        }

        /// <summary>
        /// This is synonymous with Remove in CsQuery, since there's nothing associated with an element
        /// that is not rendered. It is included for compatibility.
        /// </summary>
        ///
        /// <remarks>
        /// CsQuery does not maintain data such as initial visibility state when using Show/Hide, or an
        /// internal data structure when using Data methods. There is no data associated with an element
        /// that is not represented entirely through the markup that it will render. In the future, it's
        /// possible we may add such functionality for certain features, so it may be desirable to use
        /// Detach instead of Remove in those situations. This ensures forward compatibility.
        /// </remarks>
        ///
        /// <param name="selector">
        /// A selector expression that filters the set of matched elements to be removed.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object.
        /// </returns>

        public CQ Detach(string selector = null)
        {
            return Remove(selector);
        }
        

    }
}
