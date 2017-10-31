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
        /// Insert content, specified by the parameter, to the beginning of each element in the set of
        /// matched elements.
        /// </summary>
        ///
        /// <param name="elements">
        /// One or more elements.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object representing the inserte content.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/prepend/
        /// </url>

        public CQ Prepend(params IDomObject[] elements)
        {
            return Prepend(Objects.Enumerate(elements));
        }

        /// <summary>
        /// Insert content, specified by the parameter, to the beginning of each element in the set of
        /// matched elements.
        /// </summary>
        ///
        /// <param name="selector">
        /// One or more selectors or HTML strings.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/prepend/
        /// </url>

        public CQ Prepend(params string[] selector)
        {
            return Prepend(MergeContent(selector));
        }

        /// <summary>
        /// Insert content, specified by the parameter, to the beginning of each element in the set of
        /// matched elements.
        /// </summary>
        ///
        /// <param name="elements">
        /// The elements to be inserted.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/prepend/
        /// </url>

        public CQ Prepend(IEnumerable<IDomObject> elements)
        {
            CQ ignoredOutput;
            return Prepend(elements, out ignoredOutput);
        }

        /// <summary>
        /// Insert content, specified by the parameter, to the beginning of each element in the set of
        /// matched elements.
        /// </summary>
        ///
        /// <param name="elements">
        /// The elements to be inserted.
        /// </param>
        /// <param name="insertedElements">
        /// A CQ object containing all the elements added.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/prepend/
        /// </url>

        public CQ Prepend(IEnumerable<IDomObject> elements, out CQ insertedElements)
        {
            insertedElements = NewCqInDomain();
            bool first = true;

            // For the first iteration, the elements can be moved. For successive iterations, a clone must
            // be insterted. 

            List<IDomObject> list = elements.ToList();
            
            foreach (var child in Elements)
            {
                int index = 0;

                // Make sure they didn't really mean to add to a tbody or something
                IDomElement target = GetTrueTarget(child );

                foreach (var e in list)
                {
                    IDomObject toInsert = first ? e : e.Clone();

                    target.ChildNodes.Insert(index++, toInsert);
                    insertedElements.SelectionSet.Add(toInsert);
                }
                first = false;
            }
            return this;

        }

    }
}
