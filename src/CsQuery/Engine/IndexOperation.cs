using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Engine
{
    internal enum IndexOperationType
    {
        /// <summary>
        /// Adds to the index
        /// </summary>
        Add=1,
        /// <summary>
        /// Remove from the index.
        /// </summary>
        Remove=2,
        /// <summary>
        /// Change the value only.
        /// </summary>
        Change =3
    }

    internal struct IndexOperation
    {
        public IndexOperationType IndexOperationType;
        public ushort[] Key;
        public IDomObject Value;
    }
}
