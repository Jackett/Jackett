using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using CsQuery.ExtensionMethods.Internal;

namespace CsQuery.Implementation
{
    /// <summary>
    /// An INodeList wrapper for an IList object
    /// </summary>
    ///
    /// <typeparam name="T">
    /// Generic type parameter.
    /// </typeparam>

    public class NodeList<T>: INodeList<T> where T: IDomObject
    {
        #region constructor

        /// <summary>
        /// Wraps a list in a NodeList object
        /// </summary>
        ///
        /// <param name="list">
        /// The list.
        /// </param>

        public NodeList(IList<T> list)
        {
            InnerList = list;
            IsReadOnly = true;
        }

        /// <summary>
        /// Creates a new node list from an enumeration. This will enumerate the sequence at create time
        /// into a new list.
        /// </summary>
        ///
        /// <param name="sequence">
        /// The sequence
        /// </param>

        public NodeList(IEnumerable<T> sequence)
        {
            InnerList = new List<T>(sequence);
            IsReadOnly = true;
        }

        #endregion

        #region private properties

        /// <summary>
        /// The inner list object.
        /// </summary>

        protected IList<T> InnerList;

        #endregion

        /// <summary>
        /// Gets the number of items in this NodeList.
        /// </summary>

        public int Length
        {
            get { return InnerList.Count; }
        }

        /// <summary>
        /// Return the item at the specified index
        /// </summary>
        ///
        /// <param name="index">
        /// Zero-based index of the.
        /// </param>
        ///
        /// <returns>
        /// An item of type T
        /// </returns>

        public T Item(int index)
        {
            return this[index];
        }

        /// <summary>
        /// Get the index of the item in this list
        /// </summary>
        ///
        /// <param name="item">
        /// The item.
        /// </param>
        ///
        /// <returns>
        /// The 0-based index, or -1 if it does not exist in the list
        /// </returns>

        public int IndexOf(T item)
        {
            return InnerList.IndexOf(item);
        }

        /// <summary>
        /// Inserts an item at the specified position in the list
        /// </summary>
        ///
        /// <param name="index">
        /// Zero-based index of the insertion point
        /// </param>
        /// <param name="item">
        /// The item.
        /// </param>

        public void Insert(int index, T item)
        {
            InnerList.Insert(index, item);
        }

        /// <summary>
        /// Removes the item at the specified index
        /// </summary>
        ///
        /// <param name="index">
        /// Zero-based index of the item to remove
        /// </param>

        public void RemoveAt(int index)
        {
            InnerList.RemoveAt(index);
        }

        /// <summary>
        /// Get or set the item at the specified index
        /// </summary>
        ///
        /// <param name="index">
        /// Zero-based index of the entry to access.
        /// </param>
        ///
        /// <returns>
        /// The item.
        /// </returns>

        [IndexerName("Indexer")]
        public T this[int index]
        {
            get
            {
                return InnerList[index];

            }
            set
            {
                InnerList[index] = value;
            }
        }

        /// <summary>
        /// Adds the item to the end of the list
        /// </summary>
        ///
        /// <param name="item">
        /// The item to add
        /// </param>

        public void Add(T item)
        {
            InnerList.Add(item);
        }

        /// <summary>
        /// Clears this object to its blank/initial state.
        /// </summary>

        public void Clear()
        {
            InnerList.Clear();
        }

        /// <summary>
        /// Query if this object contains the given item.
        /// </summary>
        ///
        /// <param name="item">
        /// The item.
        /// </param>
        ///
        /// <returns>
        /// true if the object is in this collection, false if not.
        /// </returns>

        public bool Contains(T item)
        {
            return InnerList.Contains(item);
        }

        /// <summary>
        /// Copies the contents of this list to an array
        /// </summary>
        ///
        /// <param name="array">
        /// The array.
        /// </param>
        /// <param name="arrayIndex">
        /// Zero-based index of the starting point in the array to copy
        /// </param>

        public void CopyTo(T[] array, int arrayIndex)
        {
            InnerList.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Gets the number of items in this list
        /// </summary>

        public int Count
        {
            get { return InnerList.Count; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this object is read only.
        /// </summary>

        public bool IsReadOnly
        {
            get;
            protected set;
        }

        /// <summary>
        /// Removes the given item from the list
        /// </summary>
        ///
        /// <param name="item">
        /// The item.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public bool Remove(T item)
        {
            return InnerList.Remove(item);
        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        ///
        /// <returns>
        /// The enumerator.
        /// </returns>

        public IEnumerator<T> GetEnumerator()
        {
            return InnerList.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Converts this object to an IList&lt;T&gt;
        /// </summary>
        ///
        /// <returns>
        /// This object as an IList&lt;T&gt;
        /// </returns>

        public IList<T> ToList()
        {
            return new List<T>(this).AsReadOnly();
        }
    }
}
