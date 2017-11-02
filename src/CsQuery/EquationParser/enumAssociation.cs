using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.EquationParser
{
    /// <summary>
    /// Values that represent AssociationType; which determines how to group adjacent operands when
    /// parsing an equation.
    /// </summary>

    public enum AssociationType
    {
        /// <summary>
        ///  Associate with other Addition operands
        /// </summary>
        Addition= 1,          
        /// <summary>
        ///  associate with other Multiplcation operands
        /// </summary>
        Multiplicaton =2,     
        /// <summary>
        /// never associate, associate only directly adjacent operands.
        /// </summary>
        Power = 3,           
        /// <summary>
        /// never associate, and use parenthesized operands.
        /// </summary>
        Function = 4      

    }
}
