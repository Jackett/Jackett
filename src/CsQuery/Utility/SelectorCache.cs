using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.Implementation;
using CsQuery.Engine;

namespace CsQuery.Utility
{
    /// <summary>
    /// Class to cache selectors on a DOM. NOT YET IMPLEMENTED.
    /// </summary>

    public class SelectorCache
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        ///
        /// <param name="cqSource">
        /// The cq source.
        /// </param>

        public SelectorCache(CQ cqSource)
        {
            CqSource = cqSource;
        }


        private CQ  CqSource;

        private IDictionary<Selector, IList<IDomObject>> _SelectionCache;

        /// <summary>
        /// Gets the selection cache.
        /// </summary>

        protected IDictionary<Selector, IList<IDomObject>> SelectionCache
        {
            get
            {
                if (_SelectionCache == null)
                {
                    _SelectionCache = new Dictionary<Selector, IList<IDomObject>>();
                }
                return _SelectionCache;

            }
        }

        /// <summary>
        /// Run the selector.
        /// </summary>
        ///
        /// <param name="selector">
        /// The selector.
        /// </param>
        ///
        /// <returns>
        /// .
        /// </returns>

        public CQ Select(string selector)
        {
            IList<IDomObject> selection;

            var sel = new Selector(selector);
            if (SelectionCache.TryGetValue(sel, out selection)) {
                return new CQ(selection);
            } else {
                var result = CqSource.Select(sel);
                SelectionCache.Add(sel, result.Selection.ToList());
                return result;
            }   


        }
    }
}
