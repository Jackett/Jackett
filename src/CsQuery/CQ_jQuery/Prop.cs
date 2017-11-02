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
using CsQuery.HtmlParser;

namespace CsQuery
{
    public partial class CQ
    {
        #region public methods

        /// <summary>
        /// Set one or more properties for the set of matched elements.
        /// </summary>
        ///
        /// <param name="name">
        /// The property to set
        /// </param>
        /// <param name="value">
        /// The value
        /// </param>
        ///
        /// <returns>
        /// The current CQ object
        /// </returns>

        public CQ Prop(string name, IConvertible value)
        {
            // Prop actually works on things other than boolean - e.g. SelectedIndex. For now though only use prop for booleans

            if (HtmlData.IsBoolean(name))
            {
                SetProp(name, value);
            }
            else
            {
                Attr(name, value);
            }
            return this;
        }

        /// <summary>
        /// Test whether the named property is set for the first element in the selection set.
        /// </summary>
        ///
        /// <remarks>
        /// When used to test the "selected" property of options in option groups, and none are
        /// explicitly marked as "selected", this will return "true" for the first option in the group,
        /// per browser DOM behavior.
        /// </remarks>
        ///
        /// <param name="name">
        /// The property name.
        /// </param>
        ///
        /// <returns>
        /// true if it is set, false if not.
        /// </returns>

        public bool Prop(string name)
        {
            name = name.ToLower();
            if (Length > 0 && HtmlData.IsBoolean(name))
            {
                bool has = this[0].HasAttribute(name);

                // if there is nothing with the "selected" attribute, in non-multiple select lists, 
                // the first one is selected by default by Sizzle. We will return that same information 
                // when using prop.
                
                // TODO: this won't work for the "selected" selector. Need to move this logic into DomElement 
                // and use selected property instead to make this work. I am not sure I agree with the jQuery
                // implementation anyway since querySelectorAll does NOT return this.
                
                if (name == "selected" && !has)
                {
                    var owner = First().Closest("select");
                    string ownerSelected = owner.Val();
                    if (ownerSelected == String.Empty && !owner.Prop("multiple"))
                    {
                        return ReferenceEquals(owner.Find("option").FirstOrDefault(), this[0]);
                    }
                }

                return has;
            }
            return false;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Helper function for Attr &amp; Prop. Sets a property to true or false for an object that is
        /// "truthy" or not.
        /// </summary>
        ///
        /// <param name="name">
        /// The property name.
        /// </param>
        /// <param name="value">
        /// .The value.
        /// </param>

        protected void SetProp(string name, object value)
        {
            bool state = Objects.IsTruthy(value);
            foreach (IDomElement e in Elements)
            {
                if (state)
                {
                    e.SetAttribute(name);
                }
                else
                {
                    e.RemoveAttribute(name);
                }
            }
        }

        #endregion

    }
}
