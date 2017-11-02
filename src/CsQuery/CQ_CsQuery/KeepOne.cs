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
        /// Given two selectors, shows the content of one, and removes the content of the other, based on
        /// the boolean parameter.
        /// </summary>
        ///
        /// <param name="which">
        /// A boolean value to indicate whether the first or second selector should be used to determine
        /// the elements that are kept. When true, the first is kept and the 2nd removed. When false, the
        /// opposite happens.
        /// </param>
        /// <param name="trueSelector">
        /// The true selector.
        /// </param>
        /// <param name="falseSelector">
        /// The false selector.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>

        public CQ KeepOne(bool which, string trueSelector, string falseSelector)
        {
            return KeepOne(which ? 0 : 1, trueSelector, falseSelector);
        }

        /// <summary>
        /// Given two CQ objects, shows the one, and removes the the other from the document, based on
        /// the boolean parameter.
        /// </summary>
        ///
        /// <param name="which">
        /// A boolean value to indicate whether the first or second selector should be used to determine
        /// the elements that are kept. When true, the first is kept and the 2nd removed. When false, the
        /// opposite happens.
        /// </param>
        /// <param name="trueContent">
        /// The true content.
        /// </param>
        /// <param name="falseContent">
        /// The false content.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>

        public CQ KeepOne(bool which, CQ trueContent, CQ falseContent)
        {
            return KeepOne(which ? 0 : 1, trueContent, falseContent);
        }

        /// <summary>
        /// Removes all but one of a list selectors/objects based on the zero-based index of the first
        /// parameter. The remaining one is explicitly shown.
        /// </summary>
        ///
        /// <param name="which">
        /// An integer representing the zero-based index of the content from the list of items passed
        /// which should be kept and shown.
        /// </param>
        /// <param name="content">
        /// A variable-length parameters list containing content.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object
        /// </returns>

        public CQ KeepOne(int which, params string[] content)
        {
            CQ[] arr = new CQ[content.Length];
            for (int i = 0; i < content.Length; i++)
            {
                arr[i] = Select(content[i]);
            }
            return KeepOne(which, arr);
        }

        /// <summary>
        /// Removes all but one of a list selectors/objects based on the zero-based index of the first
        /// parameter. The remaining one is explicitly shown.
        /// </summary>
        ///
        /// <param name="which">
        /// An integer representing the zero-based index of the content from the list of items passed
        /// which should be kept and shown.
        /// </param>
        /// <param name="content">
        /// A variable-length parameters list containing content.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>

        public CQ KeepOne(int which, params CQ[] content)
        {
            for (int i = 0; i < content.Length; i++)
            {
                if (i == which)
                {
                    content[i].Show();
                }
                else
                {
                    content[i].Remove();
                }
            }
            return this;
        }
    }
}
