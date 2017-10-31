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
        /// <summary>
        /// Set a specific item, identified by the 2nd parameter, of a named option group, identified by
        /// the first parameter, as selected.
        /// </summary>
        ///
        /// <param name="groupName">
        /// The value of the name attribute identifying this option group.
        /// </param>
        /// <param name="value">
        /// The option value to set as selected
        /// </param>
        ///
        /// <returns>
        /// The current CQ object
        /// </returns>

        public CQ SetSelected(string groupName, IConvertible value)
        {
            var group = this.Find("input[name='" + groupName + "']");
            var item = group.Filter("[value='" + value + "']");
            if (group.Length == 0)
            {
                item = this.Find("#" + groupName);
            }
            if (item.Length > 0)
            {
                ushort nodeNameID = group[0].NodeNameID;
                string type = group[0]["type"].ToUpper();
                if (nodeNameID == HtmlData.tagOPTION)
                {
                    var ownerMultiple = group.Closest("select").Prop("multiple");
                    if (Objects.IsTruthy(ownerMultiple))
                    {
                        item.Prop("selected", true);
                    }
                    else
                    {
                        group.Prop("selected", false);
                        item.Prop("selected", true);
                    }
                }
                else if (nodeNameID == HtmlData.tagINPUT && 
                    (type == "radio" || type == "checkbox"))
                {
                    if (type == "radio")
                    {
                        group.Prop("checked", false);
                    }
                    item.Prop("checked", true);
                }
            }
            return this;
        }
    }
}
