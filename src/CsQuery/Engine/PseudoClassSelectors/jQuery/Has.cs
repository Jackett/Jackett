using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Engine.PseudoClassSelectors
{
    /// <summary>
    /// Return only the last element from a selection
    /// </summary>

    
    public class Has : PseudoSelector, IPseudoSelectorFilter
    {
        /// <summary>
        /// Arguments for the "has" selector.
        /// </summary>
        ///
        /// <value>
        /// The arguments.
        /// </value>

        public override string Arguments
        {
            get
            {
                return base.Arguments;
            }
            set
            {
                base.Arguments = value;

                // Parameter count is guaranteed to be 1
                ChildSelector = new Selector(Parameters[0]).ToContextSelector();
            }
        }

        /// <summary>
        /// The child selector
        /// </summary>

        protected Selector ChildSelector;

        /// <summary>
        /// Return only the elements in the sequence whose children match the ChildSelector
        /// </summary>
        ///
        /// <param name="selection">
        /// The sequence of elements prior to this filter being applied.
        /// </param>
        ///
        /// <returns>
        /// A sequence of elements
        /// </returns>

        public IEnumerable<IDomObject> Filter(IEnumerable<IDomObject> selection)
        {
            var first = selection.FirstOrDefault();

            if (first!=null) {
                var subSel = new HashSet<IDomObject>(ChildSelector.Select(first.Document, selection));
                
                foreach (var item in selection) {

                    foreach (var descendant in Descendants(item))
                    {
                        if (subSel.Contains(descendant))
                        {

                            yield return item;
                            goto BreakDescendants;
                        }
                    }
                BreakDescendants:
                    continue;
                }
            }
        }

        /// <summary>
        /// Enumerates descendants in this collection.
        /// </summary>
        ///
        /// <param name="parent">
        /// The parent.
        /// </param>
        ///
        /// <returns>
        /// An enumerator that allows foreach to be used to process descendants in this collection.
        /// </returns>

        protected IEnumerable<IDomObject> Descendants(IDomObject parent) {
            if (parent.HasChildren)
            {
                foreach (var child in parent.ChildNodes)
                {
                    foreach (var descendant in Descendants(child))
                    {
                        yield return descendant;
                    }
                    yield return child;
                }

            }

        }

        ///// <summary>
        ///// Adds a matched element to the cache. I'm not quite sure if this makes sense. The idea is that
        ///// you will often be checking children of something you just checked; probably we would have to
        ///// iterate through the results of the first subselector and add all the node trees to the root
        ///// for each result to the cache. Might not be worth it.
        ///// </summary>
        /////
        ///// <value>
        ///// The number of maximum parameters.
        ///// </value>

        //protected void AddToCache(IDomObject item)
        //{
        //    IDomObject next= item;
        //    while (next != item.Document && next != null)
        //    {
        //        Cache.Add(next);
        //        next = next.ParentNode;
        //    }
        //}

        /// <summary>
        /// The maximum number of parameters that this selector can accept (1)
        /// </summary>
        ///
        /// <value>
        /// An integer.
        /// </value>

        public override int MaximumParameterCount
        {
            get
            {
                return 1;
            }
        }

        /// <summary>
        /// The minimum number of parameters that this selector requires (1)
        /// </summary>
        ///
        /// <value>
        /// An integer.
        /// </value>

        public override int MinimumParameterCount
        {
            get
            {
                return 1;
            }
        }

     
    }
}
