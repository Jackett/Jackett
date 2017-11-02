using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.EquationParser
{
    /// <summary>
    /// Values that represent OperationType for an arithmetic operator.
    /// </summary>

    public enum OperationType
    {
        /// <summary>
        /// Addition or +
        /// </summary>
        Addition=1,
        /// <summary>
        /// Subtraction or -.
        /// </summary>
        Subtraction=2,
        /// <summary>
        /// Multiplication or *.
        /// </summary>
        Multiplication=3,
        /// <summary>
        /// Division or /.
        /// </summary>
        Division=4,
        /// <summary>
        /// Modulus or %.
        /// </summary>
        Modulus = 5,
        /// <summary>
        /// Power or ^.
        /// </summary>
        Power=6
    }
}
