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
        /// Adds the specified class, or each class in a space-separated list, to each of the set of
        /// matched elements.
        /// </summary>
        ///
        /// <param name="className">
        /// One or more class names to be added to the class attribute of each matched element.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/addclass/
        /// </url>

        public CQ AddClass(string className)
        {
            foreach (var item in Elements)
            {
                item.AddClass(className);
            }
            return this;
        }

        /// <summary>
        /// Add or remove one or more classes from each element in the set of matched elements, depending
        /// on either the class's presence.
        /// </summary>
        ///
        /// <param name="classes">
        /// One or more class names (separated by spaces) to be toggled for each element in the matched
        /// set.
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/toggleClass/
        /// </url>

        public CQ ToggleClass(string classes)
        {
            IEnumerable<string> classList = classes.SplitClean(' ');
            foreach (IDomElement el in Elements)
            {
                foreach (string cls in classList)
                {
                    if (el.HasClass(cls))
                    {
                        el.RemoveClass(cls);
                    }
                    else
                    {
                        el.AddClass(cls);
                    }
                }
            }
            return this;
        }

        /// <summary>
        /// Add or remove one or more classes from each element in the set of matched elements, depending
        /// on the value of the switch argument.
        /// </summary>
        ///
        /// <param name="classes">
        /// One or more class names (separated by spaces) to be toggled for each element in the matched
        /// set.
        /// </param>
        /// <param name="addRemoveSwitch">
        /// a boolean value that determine whether the class should be added (true) or removed (false).
        /// </param>
        ///
        /// <returns>
        /// The current CQ object.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/toggleClass/
        /// </url>

        public CQ ToggleClass(string classes, bool addRemoveSwitch)
        {
            IEnumerable<string> classList = classes.SplitClean(' ');
            foreach (IDomElement el in Elements)
            {
                foreach (string cls in classList)
                {
                    if (addRemoveSwitch)
                    {
                        el.AddClass(cls);
                    }
                    else
                    {
                        el.RemoveClass(cls);
                    }
                }
            }
            return this;
        }

        //public CQ ToggleClass(bool addRemoveSwitch)
        //{

        //}

        /// <summary>
        /// Determine whether any of the matched elements are assigned the given class.
        /// </summary>
        ///
        /// <param name="className">
        /// The class name to search for.
        /// </param>
        ///
        /// <returns>
        /// true if the class exists on any of the elements, false if not.
        /// </returns>
        ///
        /// <url>
        /// http://api.jquery.com/hasclass/
        /// </url>

        public bool HasClass(string className)
        {

            IDomElement el = FirstElement();

            return el == null ? false :
                el.HasClass(className);
        }
        

    }
}
