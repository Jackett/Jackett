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
        /// Replace each element in the set of matched elements with the provided new content.
        /// </summary>
        ///
        /// <param name="content">
        /// The HTML string of the content to insert.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/replaceWith/
        /// </url>

        public CQ ReplaceWith(params string[] content)
        {
            CQ output = this;
            if (Length > 0)
            {
                // Before allows adding of new content to an empty selector. To ensure consistency with jQuery
                // implentation, do not do this if called on an empty selector. 

                // The logic here is tricky because we can do a replace on disconnected selection sets. This has to
                // track what was orignally scheduled for removal in case the set changes in "Before" b/c it's disconnected.

                CQ newContent = EnsureCsQuery(MergeContent(content));
                CQ replacing = NewInstance(this);

                output = Before(newContent);
                output.SelectionSet.ExceptWith(replacing);
                replacing.Remove();

            }
            return output;
        }

        /// <summary>
        /// Replace each element in the set of matched elements with the element passed by parameter.
        /// </summary>
        ///
        /// <param name="element">
        /// The element to replace the content with.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/replaceWith/
        /// </url>

        public CQ ReplaceWith(IDomObject element)
        {
            return ReplaceWith(Objects.Enumerate(element));
        }

        /// <summary>
        /// Replace each element in the set of matched elements with the sequence of elements or CQ
        /// object provided.
        /// </summary>
        ///
        /// <param name="elements">
        /// The new conent to replace the selection set content with.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/replaceWith/
        /// </url>

        public CQ ReplaceWith(IEnumerable<IDomObject> elements)
        {
            return Before(elements).Remove();
        }
        

    }
}
