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
        /// Given a table header or cell, returns all members of the same column in the table. This will
        /// most likely not work as you would expect if there are colspan cells.
        /// </summary>
        ///
        /// <returns>
        /// A new CQ object containing all the th and td cells in the specified column.
        /// </returns>

        public CQ GetTableColumn()
        {
            var els = this.Filter("th,td");
            CQ result = NewCqInDomain();
            foreach (var el in els)
            {
                var elCq = el.Cq();
                int colIndex = elCq.Index();
                result.AddSelection(elCq.Closest("table").GetTableColumn(colIndex));
            }
            return result;
        }

        /// <summary>
        /// Selects then zero-based nth cells  (th and td) from all rows in any matched tables. This will
        /// most likely no do what you expect if the table has colspan cells.
        /// </summary>
        ///
        /// <param name="column">
        /// The zero-based index of the column to target.
        /// </param>
        ///
        /// <returns>
        /// A new CQ object containing all the th and td cells in the specified column.
        /// </returns>

        public CQ GetTableColumn(int column)
        {
            CQ result = NewCqInDomain();
            foreach (var el in FilterElements(this, "table"))
            {

                result.AddSelection(el.Cq().Find(String.Format("tr>th:eq({0}), tr>td:eq({0})", column)));

            }
            return result;
        }
    }
}
