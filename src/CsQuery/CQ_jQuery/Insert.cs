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
        /// Insert every element in the set of matched elements after the target.
        /// </summary>
        ///
        /// <summary>
        /// Inserts an after described by target.
        /// </summary>
        ///
        /// <param name="target">
        /// The target to insert after.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/insertAfter/
        /// </url>

        public CQ InsertAfter(IDomObject target)
        {
            return InsertAtOffset(target, 1);
        }

        /// <summary>
        /// Insert every element in the set of matched elements after each element in the target sequence.
        /// </summary>
        ///
        /// <remarks>
        /// If there is a single element in the target, the elements in the selection set will be moved
        /// before the target (not cloned). If there is more than one target element, however, cloned
        /// copies of the inserted element will be created for each target after the first, and that new
        /// set (the original element plus clones) is returned.
        /// </remarks>
        ///
        /// <param name="target">
        /// A sequence of elements or a CQ object.
        /// </param>
        ///
        /// <returns>
        /// The set of elements inserted, including the original elements and any clones made if there
        /// was more than one target.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/insertAfter/
        /// </url>

        public CQ InsertAfter(IEnumerable<IDomObject> target)
        {
            CQ output;
            InsertAtOffset(EnsureCsQuery(target), 1, out output);
            return output;
        }

        /// <summary>
        /// Insert every element in the set of matched elements after the target.
        /// </summary>
        ///
        /// <remarks>
        /// If there is a single element in the resulting set of the selection created by the parameter
        /// selector, then the original elements in this object's selection set will be moved before it.
        /// If there is more than one target element, however, cloned copies of the inserted element will
        /// be created for each target after the first, and that new set (the original element plus
        /// clones) is returned.
        /// </remarks>
        ///
        /// <param name="selectorTarget">
        /// A selector identifying the target elements after which each element in the current set will
        /// be inserted.
        /// </param>
        ///
        /// <returns>
        /// The set of elements inserted, including the original elements and any clones made if there
        /// was more than one target.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/insertAfter/
        /// </url>

        public CQ InsertAfter(string selectorTarget)
        {
            return InsertAfter(Select(selectorTarget));
        }


        /// <summary>
        /// Insert every element in the set of matched elements before each elemeent in the selection set
        /// created from the target selector.
        /// </summary>
        ///
        /// <remarks>
        /// If there is a single element in the resulting set of the selection created by the parameter
        /// selector, then the original elements in this object's selection set will be moved before it.
        /// If there is more than one target element, however, cloned copies of the inserted element will
        /// be created for each target after the first, and that new set (the original element plus
        /// clones) is returned.
        /// </remarks>
        ///
        /// <param name="selector">
        /// A selector. The matched set of elements will be inserted before the element(s) specified by
        /// this selector.
        /// </param>
        ///
        /// <returns>
        /// The set of elements inserted, including the original elements and any clones made if there
        /// was more than one target.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/insertBefore/
        /// </url>

        public CQ InsertBefore(string selector)
        {
            return InsertBefore(Select(selector));
        }

        /// <summary>
        /// Insert every element in the set of matched elements before the target.
        /// </summary>
        ///
        /// <param name="target">
        /// The element to which the elements in the current selection set should inserted after.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/insertBefore/
        /// </url>

        public CQ InsertBefore(IDomObject target)
        {
            return InsertAtOffset(target, 0);
        }

        /// <summary>
        /// Insert every element in the set of matched elements before the target.
        /// </summary>
        ///
        /// <remarks>
        /// If there is a single element in the target, the elements in the selection set will be moved
        /// before the target (not cloned). If there is more than one target element, however, cloned
        /// copies of the inserted element will be created for each target after the first, and that new
        /// set (the original element plus clones) is returned.
        /// </remarks>
        ///
        /// <param name="target">
        /// A sequence of elements or a CQ object that is the target; each element in the selection set
        /// will be inserted after each element in the target.
        /// </param>
        ///
        /// <returns>
        /// The set of elements inserted, including the original elements and any clones made if there
        /// was more than one target.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/insertBefore/
        /// </url>

        public CQ InsertBefore(IEnumerable<IDomObject> target)
        {
            CQ output;
            InsertAtOffset(EnsureCsQuery(target), 0, out output);
            return output;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Support for InsertAfter and InsertBefore. An offset of 0 will insert before the current
        /// element. 1 after.
        /// </summary>
        ///
        /// <param name="target">
        /// The target object
        /// </param>
        /// <param name="offset">
        /// The offset from the targe object to insert
        /// </param>
        ///
        /// <returns>
        /// The current CQ object
        /// </returns>

        private CQ InsertAtOffset(IDomObject target, int offset)
        {
            int index = target.Index;

            // must enumerate the list since it can be altered by the loop
            var list = SelectionSet.ToList();
            foreach (var item in list)
            {
                target.ParentNode.ChildNodes.Insert(index + offset, item);
                index++;
            }
            return this;
        }

        #endregion

    }
}
