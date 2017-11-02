using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Engine.PseudoClassSelectors
{
    /// <summary>
    /// Test whether an element appears at the specified position with the list.
    /// </summary>

    public class Eq : Indexed
    {
        /// <summary>
        /// Filter a sequence of elements, returning only the element at the specified position
        /// </summary>
        ///
        /// <param name="selection">
        /// A sequence of elements
        /// </param>
        ///
        /// <returns>
        /// A sequence containing one or zero elements
        /// </returns>

        public override IEnumerable<IDomObject> Filter(IEnumerable<IDomObject> selection)
        {
            IDomObject el =  ElementAtIndex(selection, Index);
            if (el != null)
            {
                yield return el;
            }
        }



        private IDomObject ElementAtIndex(IEnumerable<IDomObject> list, int index)
        {
            if (index < 0)
            {
                index = list.Count() + index;
            }
            bool ok = true;
            IEnumerator<IDomObject> enumerator = list.GetEnumerator();
            for (int i = 0; i <= index && ok; i++)
            {
                ok = enumerator.MoveNext();
            }
            if (ok)
            {
                return enumerator.Current;
            }
            else
            {
                return null;
            }
        }


    }
}
