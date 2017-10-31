using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.Engine;

namespace CsQuery
{
    /// <summary>
    /// The default DomIndexProvider.
    /// </summary>

    public static class DomIndexProviders
    {
        /// <summary>
        /// Static constructor.
        /// </summary>

        static DomIndexProviders()
        {
            _RangedDomIndexProvider = new RangedDomIndexProvider();
            _SimpleDomIndexProvider = new SimpleDomIndexProvider();
            _NoDomIndexProvider = new NoDomIndexProvider();
        }

        private static IDomIndexProvider _RangedDomIndexProvider;
        private static IDomIndexProvider _SimpleDomIndexProvider;
        private static IDomIndexProvider _NoDomIndexProvider;
        
        /// <summary>
        /// Return a SimpleDomIndex provider
        /// </summary>
        ///
        /// <returns>
        /// The DomIndex instance
        /// </returns>

        public static IDomIndexProvider Simple
        {
            get
            {
                return _SimpleDomIndexProvider;
            }
        }

        /// <summary>
        /// Returns a RangedDomIndex provider
        /// </summary>

        public static IDomIndexProvider Ranged
        {
            get
            {
                return _RangedDomIndexProvider;
            }
        }

        /// <summary>
        /// Returns a NoDomIndex provider
        /// </summary>

        public static IDomIndexProvider None
        {
            get
            {
                return _NoDomIndexProvider;
            }
        }
    }

    /// <summary>
    ///  DomIndexProvider returning a SimpleDomIndex
    /// </summary>

    public class SimpleDomIndexProvider : IDomIndexProvider
    {
        /// <summary>
        /// Return an instance of a DomIndex class.
        /// </summary>
        ///
        /// <returns>
        /// The DomIndex instance
        /// </returns>

        public IDomIndex GetDomIndex()
        {
            return new DomIndexSimple();
        }


    }

    /// <summary>
    /// DomIndexProvider returning a RangedDomIndex
    /// </summary>

    public class RangedDomIndexProvider : IDomIndexProvider
    {
        /// <summary>
        /// Return an instance of a DomIndex class.
        /// </summary>
        ///
        /// <returns>
        /// The DomIndex instance
        /// </returns>

        public IDomIndex GetDomIndex()
        {
            return new DomIndexRanged();
        }
    }

    /// <summary>
    /// DomIndexProvider returning a RangedDomIndex
    /// </summary>

    public class NoDomIndexProvider : IDomIndexProvider
    {
        /// <summary>
        /// Return an instance of a DomIndex class.
        /// </summary>
        ///
        /// <returns>
        /// The DomIndex instance
        /// </returns>

        public IDomIndex GetDomIndex()
        {
            return new DomIndexNone();
        }
    }
}
