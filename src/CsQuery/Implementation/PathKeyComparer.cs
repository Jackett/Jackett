using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace CsQuery.Implementation
{
    /// <summary>
    /// A string comparer that is not concerned with anything other than the raw value of the characters. No encoding, no culture.
    /// </summary>

    public class PathKeyComparer : IComparer<ushort[]>, IEqualityComparer<ushort[]>
    {
        static PathKeyComparer()
        {
            _Comparer = new PathKeyComparer();
        }


        private static readonly PathKeyComparer _Comparer;

        /// <summary>
        /// Gets an instance of TrueStringComparer
        /// </summary>

        public static PathKeyComparer Comparer
        {
            get
            {
                return _Comparer;
            }
        }

        /// <summary>
        /// Compares two string objects to determine their relative ordering.
        /// </summary>
        ///
        /// <param name="x">
        /// String to be compared.
        /// </param>
        /// <param name="y">
        /// String to be compared.
        /// </param>
        ///
        /// <returns>
        /// Negative if 'x' is less than 'y', 0 if they are equal, or positive if it is greater.
        /// </returns>


        public int Compare(ushort[] x, ushort[] y)
        {
            int xlen = x.Length;
            int ylen = y.Length;

            int ilen = xlen < ylen ? xlen :  ylen;
            int ipos = 0;

            while (ipos < ilen && x[ipos] == y[ipos])
                ++ipos;

            return ipos < ilen
                        ? (x[ipos] < y[ipos] ? -1 : 1)
                        : (xlen <ylen ? -1 : xlen > ylen ? 1 : 0);
        }


        /// <summary>
        /// Marginally faster when just testing equality than using Compare
        /// </summary>
        ///
        /// <param name="x">
        /// String to be compared.
        /// </param>
        /// <param name="y">
        /// String to be compared.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        protected bool CompareEqualLength(ushort[] x, ushort[] y)
        {
            int len = x.Length;
            for (int pos = 0; pos < len; pos++)
            {
                if (x[pos] != y[pos])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Tests if two string objects are considered equal.
        /// </summary>
        ///
        /// <param name="x">
        /// String to be compared.
        /// </param>
        /// <param name="y">
        /// String to be compared.
        /// </param>
        ///
        /// <returns>
        /// true if the objects are considered equal, false if they are not.
        /// </returns>

        public bool Equals(ushort[] x, ushort[] y)
        {
            int len = x.Length;

            if (len != y.Length)
            {
                return false;
            }

            while (len-->0)
            {
                if (x[len] != y[len])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Calculates the hash code for this object.
        /// </summary>
        ///
        /// <param name="obj">
        /// The object.
        /// </param>
        ///
        /// <returns>
        /// The hash code for this object.
        /// </returns>
        
        

        public int GetHashCode(ushort[] obj)
        {
            unchecked
            {
                const int hashP = 16777619;
                int hash = (int)2166136261;

                for (int i = 0; i < obj.Length; i++)
                    hash = (hash ^ obj[i]) * hashP;
                
                return ((((hash + (hash << 13))
                    ^ (hash >> 7))
                    + (hash << 3))
                    ^ (hash >> 17))
                    + (hash << 5);
            }
            
        }

    }
}
