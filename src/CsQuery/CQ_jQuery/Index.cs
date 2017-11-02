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
        #region public methods

        /// <summary>
        /// Search for a given element from among the matched elements.
        /// </summary>
        ///
        /// <returns>
        /// The index of the element, or -1 if it was not found.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/index/
        /// </url>

        public int Index()
        {
            IDomObject el = SelectionSet.FirstOrDefault();
            if (el != null)
            {
                return GetElementIndex(el);
            }
            return -1;
        }

        /// <summary>
        /// Returns the position of the current selection within the new selection defined by "selector".
        /// </summary>
        ///
        /// <param name="selector">
        /// The selector string.
        /// </param>
        ///
        /// <returns>
        /// The zero-based index of the selection within the new selection
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/index/
        /// </url>

        public int Index(string selector)
        {
            var selection = Select(selector);
            return selection.Index(SelectionSet);
        }

        /// <summary>
        /// Returns the position of the element passed in within the selection set.
        /// </summary>
        ///
        /// <param name="element">
        /// The element to exclude.
        /// </param>
        ///
        /// <returns>
        /// The zero-based index of "element" within the selection set, or -1 if it was not a member of
        /// the current selection.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/index/
        /// </url>

        public int Index(IDomObject element)
        {
            int index = -1;
            if (element != null)
            {
                int count = 0;
                foreach (IDomObject el in SelectionSet)
                {
                    if (ReferenceEquals(el, element))
                    {
                        index = count;
                        break;
                    }
                    count++;
                }
            }
            return index;
        }

        /// <summary>
        /// Returns the position of the first element in the sequence passed by parameter within the
        /// current selection set..
        /// </summary>
        ///
        /// <param name="elements">
        /// The element to look for.
        /// </param>
        ///
        /// <returns>
        /// The zero-based index of the first element in the sequence within the selection.
        /// </returns>

        public int Index(IEnumerable<IDomObject> elements)
        {
            return Index(elements.FirstOrDefault());

        }
        #endregion

        #region private methods

        /// <summary>
        /// Return the relative position of an element among its Element siblings (non-element nodes excluded)
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        protected int GetElementIndex(IDomObject element)
        {
            int count = 0;
            IDomContainer parent = element.ParentNode;
            if (parent == null)
            {
                count = -1;
            }
            else
            {
                foreach (IDomElement el in parent.ChildElements)
                {
                    if (ReferenceEquals(el, element))
                    {
                        break;
                    }
                    count++;
                }
            }
            return count;
        }

        #endregion

    }
}
