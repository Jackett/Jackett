using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.StringScanner
{
    /// <summary>
    /// Interface for a strongly typed IValueInfo
    /// </summary>
    ///
    /// <typeparam name="T">
    /// The type of value
    /// </typeparam>

    public interface IValueInfo<T> : IValueInfo where T : IConvertible
    {
        /// <summary>
        /// The target of the tests.
        /// </summary>

        new T Target { get; set; }
    }
}
