using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Engine.PseudoClassSelectors
{
    /// <summary>
    /// Return elements that don't match a selector.
    /// </summary>
    ///
    /// <url>
    /// http://reference.sitepoint.com/css/pseudoclass-not
    /// </url>

    public class Not : PseudoSelector, IPseudoSelectorFilter 
    {
        /// <summary>
        /// Return all elements that do not match the selector passed as a parameter to the :not()
        /// selector.
        /// </summary>
        ///
        /// <param name="selection">
        /// The sequence of elements prior to this filter being applied.
        /// </param>
        ///
        /// <returns>
        /// A sequence of matching elements.
        /// </returns>

        public IEnumerable<IDomObject> Filter(IEnumerable<IDomObject> selection)
        {
         
            var first = selection.FirstOrDefault();
            if (first != null)
            {
                var subSel = SubSelector().Select(first.Document, selection);
                return selection.Except(subSel);
            }
            else
            {
                return Enumerable.Empty<IDomObject>();
            }
        }

        private Selector SubSelector()
        {
            return new Selector(String.Join(",", Parameters))
               .ToFilterSelector();
            
        }

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
