using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.ExtensionMethods;
using CsQuery.ExtensionMethods.Internal;

namespace CsQuery.Implementation
{
    /// <summary>
    /// A list of DOM elements. The default order is the order added to this construct; the Order
    /// property can be changed to return the contents in a different order.
    /// </summary>
    ///
    /// <typeparam name="T">
    /// The type of element represented by this set.
    /// </typeparam>

    public class SelectionSet<T>: ISet<T>, IList<T>, ICollection<T>,IEnumerable<T>, IEnumerable 
        where T: IDomObject
    {
        #region constructor

        /// <summary>
        /// Create an initially empty instance whose results are returned in the order specified.
        /// </summary>
        ///
        /// <param name="outputOrder">
        /// The output order.
        /// </param>

        public SelectionSet(SelectionSetOrder outputOrder)
        {
            OriginalOrder = SelectionSetOrder.OrderAdded;
            OutputOrder = SelectionSetOrder.OrderAdded;
            OriginalList = EmptyList();
           
        }

        /// <summary>
        /// Create an instance based on an existing sequence. The order passed defines the order of the
        /// original list; if the output order should be different than change it.
        /// 
        /// The sequence is bound directly as the source of this selection set; it is not enumerated.
        /// Therefore it's possible to create "live" sets that will reflect the same contents as their
        /// original source at any point in time. If a client alters the selection set, however, it
        /// becomes static as the set at that point is copied in order to permit alterations. The
        /// original source sequence is never altered, even if it is a list type that can be altered.
        /// 
        /// Because of this care is required. If using an IEnumerable source that is not a basic data
        /// structure, but instead refers to a computationally-intensive process, it might be desirable
        /// to copy it to a list first. The output from the HTML parser and selector engine do this
        /// automatically to prevent accidental misuse. It is conceivable that some future function might
        /// want to provide direct access the the selector engine's IEnumerable output instead of a List
        /// copy to provide a live CSS selector; in this case the engine's Select method would need to be
        /// altered to return the enumerator directly.
        /// </summary>
        ///
        /// <param name="elements">
        /// The sequence to source this selection set.
        /// </param>
        /// <param name="inputOrder">
        /// The list order.
        /// </param>
        /// <param name="outputOrder">
        /// The output order.
        /// </param>

        public SelectionSet(IEnumerable<T> elements, SelectionSetOrder inputOrder, SelectionSetOrder outputOrder)
        {

            OriginalOrder = inputOrder ==0 ? 
                SelectionSetOrder.OrderAdded : 
                inputOrder;
            OutputOrder = outputOrder == 0 ?
                OriginalOrder : 
                outputOrder;
            OriginalList = elements ?? EmptyList();
        }
        #endregion

        #region private properties

        /// <summary>
        /// Cached count
        /// </summary>

        private bool _IsDirty;

        private SelectionSetOrder OriginalOrder;

        // We maintain both a List<T> and a HashSet<T> for selections because set operations are performance-critical
        // for many selectors, but we cannot depend on HashSet<T> to maintain order. If the list is accessed in a sorted
        // order, the sorted version is cached additionally using sortedList.


        /// <summary>
        /// The immutable list as set by a client; can be obsolete if MutableList is non-null
        /// </summary>

        private IEnumerable<T> OriginalList;

        /// <summary>
        /// Cached reference to the list in the output order
        /// </summary>

        private IEnumerable<T> _OrderedList;

        /// <summary>
        /// The active list, if changes are made after set by the client
        /// </summary>

        private HashSet<T> _MutableList;
        private List<T> _MutableListOrdered;

        /// <summary>
        /// The list, if it has been changed from the value with which it was created
        /// </summary>

        protected HashSet<T> MutableList
        {
            get
            {
                if (!IsAltered)
                {
                    ConvertToMutable();
                }
                     
                return _MutableList;
            }
            
        }

        private List<T> MutableListOrdered
        {
            get
            {
                if (!IsAltered)
                {
                    ConvertToMutable();
                }
                return _MutableListOrdered;
            }
        }

        /// <summary>
        /// The selection set in the output order.
        /// </summary>

        protected IEnumerable<T> OrderedList
        {
            get
            {
                if (IsDirty || _OrderedList == null)
                {
                    if (!IsDirty && OriginalOrder == OutputOrder)
                    {
                        _OrderedList = OriginalList;
                    }

                    // If the output isn't in the same order as the input, then convert to a hashset anyway - it
                    // doesn't really take any more time than sorting. 

                    else
                    {
                        switch (OutputOrder)
                        {
                            case SelectionSetOrder.Ascending:
                                _OrderedList = MutableList.OrderBy(item => item.NodePath, PathKeyComparer.Comparer);
                                break;
                            case SelectionSetOrder.Descending:
                                _OrderedList = MutableList.OrderByDescending(item => item.NodePath, PathKeyComparer.Comparer);
                                break;
                            case SelectionSetOrder.OrderAdded:
                                _OrderedList = MutableListOrdered;
                                break;
                        }
                        Clean();
                    }
                }
                return _OrderedList;
            }
        }

        /// <summary>
        /// The output (sorted) list is dirty because changes have been made since it was created. Update the cache.
        /// </summary>

        protected bool IsDirty 
        {
            get {
                return _IsDirty;
            }
            
        }

        /// <summary>
        /// The list is altered from its original state using "Add" or "Remove".
        /// </summary>

        protected bool IsAltered
        {
            get
            {
                return _MutableList != null;
            }
        }

        #endregion

        #region public properties

        /// <summary>
        /// The order in which elements in the set are returned.
        /// </summary>

        public SelectionSetOrder OutputOrder {get;set;}

        /// <summary>
        /// Gets the number of items in the SelectionSet
        /// </summary>

        public int Count
        {
            get
            {
                if (IsAltered)
                {
                    return MutableList.Count;
                } else {
                    return OriginalList.Count();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this object is read only. For SelectionSet objects, this is always false.
        /// </summary>

        public bool IsReadOnly
        {
            get { return false; }
        }

        #endregion

        #region public methods

        /// <summary>
        /// Adds a new item to the SelectionSet
        /// </summary>
        ///
        /// <param name="item">
        /// The item to add.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public bool Add(T item)
        {
            if (MutableList.Add(item))
            {
                MutableListOrdered.Add(item);
                Touch();
                return true;
            } else {
                return false;
            }        
        }

        /// <summary>
        /// Clears this SelectionSet
        /// </summary>

        public void Clear()
        {
            OriginalList = EmptyList();
            _OrderedList = null;
            _MutableList = null;
            _MutableListOrdered = null;
        }

        /// <summary>
        /// Makes a clone of this SelectionSet
        /// </summary>
        ///
        /// <returns>
        /// A copy of this object.
        /// </returns>

        public SelectionSet<T> Clone()
        {
            var clone = new SelectionSet<T>(CloneImpl(),OutputOrder,OutputOrder);
            return clone;
        }

        /// <summary>
        /// Enumerates clone objects in this collection.
        /// </summary>
        ///
        /// <returns>
        /// An enumerator that allows foreach to be used to process clone objects in this collection.
        /// </returns>

        protected IEnumerable<T> CloneImpl()
        {
            foreach (var item in OrderedList)
            {
                yield return (T)item.Clone();
            }
        }

        /// <summary>
        /// Test whether the item is present in the SelectionSet
        /// </summary>
        ///
        /// <param name="item">
        /// The item to test for containment.
        /// </param>
        ///
        /// <returns>
        /// true if the object is in this collection, false if not.
        /// </returns>

        public bool Contains(T item)
        {
            return IsAltered ?
                MutableList.Contains(item) :
                OriginalList.Contains(item);
        }

        /// <summary>
        /// Copy the contents of this SelectionSet to an array
        /// </summary>
        ///
        /// <param name="array">
        /// The target array.
        /// </param>
        /// <param name="arrayIndex">
        /// Zero-based index of the starting position in the array to begin copying.
        /// </param>

        public void CopyTo(T[] array, int arrayIndex)
        {
            int index=0;
            foreach (var item in OrderedList) {
                array[index+arrayIndex]=item;
                index++;
            }
        }

        /// <summary>
        /// Removes the given item from the SelectionSet
        /// </summary>
        ///
        /// <param name="item">
        /// The item to remove.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public bool Remove(T item)
        {
            if (MutableList.Remove(item))
            {
                MutableListOrdered.Remove(item);
                Touch();
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Removes all elements in the specified collection from the current SelectionSet&lt;T&gt;
        /// object.
        /// </summary>
        ///
        /// <param name="other">
        /// The collection of items to remove from the SelectionSet&lt;T&gt; object.
        /// </param>

        public void ExceptWith(IEnumerable<T> other)
        {
            MutableList.ExceptWith(other);
            SynchronizeOrderedList();
            Touch();
        }

        /// <summary>
        /// Modifies the current SelectionSet&lt;T&gt; object to contain only elements that are present
        /// in that object and in the specified collection.
        /// </summary>
        ///
        /// <param name="other">
        /// The collection to compare to the current SelectionSet&lt;T&gt;
        /// object.
        /// </param>

        public void IntersectWith(IEnumerable<T> other)
        {

            MutableList.IntersectWith(other);
            SynchronizeOrderedList();
            Touch();
        }

        /// <summary>
        /// Determines whether a SelectionSet&lt;T&gt; object is a proper subset of the specified
        /// collection.
        /// </summary>
        ///
        /// <param name="other">
        /// The collection to compare to the current SelectionSet&lt;T&gt; object.
        /// </param>
        ///
        /// <returns>
        /// true if it is a proper subset, false if not.
        /// </returns>

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return MutableList.IsProperSubsetOf(other);
        }

        /// <summary>
        /// Determines whether a SelectionSet&lt;T&gt; object is a proper superset of the specified
        /// collection.
        /// </summary>
        ///
        /// <param name="other">
        /// The collection to compare to the current SelectionSet&lt;T&gt; object.
        /// </param>
        ///
        /// <returns>
        /// true if is is a proper superset, false if not.
        /// </returns>

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return MutableList.IsProperSupersetOf(other);
        }

        /// <summary>
        /// Determines whether a SelectionSet&lt;T&gt; object is a subset of the specified collection.
        /// </summary>
        ///
        /// <param name="other">
        /// The collection to compare to the current SelectionSet&lt;T&gt; object.
        /// </param>
        ///
        /// <returns>
        /// true if it is a proper subset, false if not.
        /// </returns>

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return MutableList.IsSubsetOf(other);
        }

        /// <summary>
        /// Determines whether a SelectionSet&lt;T&gt; object is a superset of the specified collection.
        /// </summary>
        ///
        /// <param name="other">
        /// The collection to compare to the current SelectionSet&lt;T&gt; object.
        /// </param>
        ///
        /// <returns>
        /// true if is is a proper superset, false if not.
        /// </returns>

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return MutableList.IsSupersetOf(other);
        }

        /// <summary>
        /// Determines whether the current SelectionSet&lt;T&gt; object and a specified collection share
        /// common elements.
        /// </summary>
        ///
        /// <param name="other">
        /// The collection to compare to the current System.Collections.Generic.HashSet&lt;T&gt;
        /// object.
        /// </param>
        ///
        /// <returns>
        /// true if the sets share at least one common element; , false if not.
        /// </returns>

        public bool Overlaps(IEnumerable<T> other)
        {
            return MutableList.Overlaps(other);
        }

        /// <summary>
        /// Determines whether a SelectionSet&lt;T&gt; object and the specified collection contain the
        /// same elements.
        /// </summary>
        ///
        /// <param name="other">
        /// The collection to compare to the current SelectionSet&lt;T&gt; object.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public bool SetEquals(IEnumerable<T> other)
        {
            return MutableList.SetEquals(other);
        }

        /// <summary>
        /// Modifies the current SelectionSet&lt;T&gt; object to contain only elements that are present
        /// either in that object or in the specified collection, but not both.
        /// </summary>
        ///
        /// <param name="other">
        /// The collection to compare to the current SelectionSet&lt;T&gt; object.
        /// </param>

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            MutableList.SymmetricExceptWith(other);
            SynchronizeOrderedList();
            Touch();
        }

        /// <summary>
        /// Modifies the current SelectionSet&lt;T&gt; object to contain all elements that are present in
        /// itself, the specified collection, or both.
        /// </summary>
        ///
        /// <param name="other">
        /// The collection to compare to the current SelectionSet&lt;T&gt; object.
        /// </param>

        public void UnionWith(IEnumerable<T> other)
        {
            // The hashset maintains uniqueness; we can just try to add everything.

            this.AddRange(other);
        }

        /// <summary>
        /// Return the zero-based index of item in a sequence.
        /// </summary>
        ///
        /// <param name="item">
        /// The item.
        /// </param>
        ///
        /// <returns>
        /// The zero-based position in the list where the item was found, or -1 if it was not found.
        /// </returns>

        public int IndexOf(T item)
        {
            return OrderedList.IndexOf(item);
        }

        /// <summary>
        /// Inserts an item at the specified index
        /// </summary>
        ///
        /// <param name="index">
        /// Zero-based index of the position to insert the item
        /// </param>
        /// <param name="item">
        /// The item to insert.
        /// </param>

        public void Insert(int index, T item)
        {

            if (MutableList.Add(item))
            {
                MutableListOrdered.Insert(index, item);
                Touch();
            }
        }

        /// <summary>
        /// Removes the item at the specified index.
        /// </summary>
        ///
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when the index is outside the bound of the current set.
        /// </exception>
        ///
        /// <param name="index">
        /// Zero-based index of the item to remove.
        /// </param>

        public void RemoveAt(int index)
        {
            if (index >= Count || Count==0)
            {
                throw new IndexOutOfRangeException("Index out of range");
            }

            T item = OrderedList.ElementAt(index);
            MutableList.Remove(item);
            MutableListOrdered.Remove(item);
            Touch();
        }

        /// <summary>
        /// Indexer to get or set items within this collection using array index syntax.
        /// </summary>
        ///
        /// <param name="index">
        /// Zero-based index of the entry to access.
        /// </param>
        ///
        /// <returns>
        /// The indexed item.
        /// </returns>

        public T this[int index]
        {
            get
            {
                return OrderedList.ElementAt(index);

            }
            set
            {
                
                T item = OrderedList.ElementAt(index);

                MutableList.Remove(item);
                MutableList.Add(value);

                int i = MutableListOrdered.IndexOf(item);

                MutableListOrdered[i]=value;
                Touch();
            }
        }

        /// <summary>
        /// Gets the enumerator for the SelectionSet
        /// </summary>
        ///
        /// <returns>
        /// The enumerator.
        /// </returns>

        public IEnumerator<T> GetEnumerator()
        {
            return OrderedList.GetEnumerator();
        }

        #endregion

        #region private methods

        /// <summary>
        /// When an operation changes the original list, configures this object to track changes and deal
        /// with altered lists.
        /// </summary>

        private void ConvertToMutable()
        {
            _MutableList = OriginalList == null ?
                        new HashSet<T>() :
                        new HashSet<T>(OriginalList);

            _MutableListOrdered = new List<T>();
            _MutableListOrdered.AddRange(OriginalList);

            Touch();
        }

        private IEnumerable<T> EmptyList()
        {
            yield break;
        }
        private void Touch()
        {
            _IsDirty = true;
        }

        private void Clean()
        {
            _IsDirty = false;
        }
        /// <summary>
        /// Use after set operations that alter the list
        /// </summary>
        private void SynchronizeOrderedList()
        {
            int index = 0;
            while (index < MutableListOrdered.Count)
            {
                if (!MutableList.Contains(MutableListOrdered[index]))
                {
                    MutableListOrdered.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        void ICollection<T>.Add(T item)
        {
            Add(item);
        }

        #endregion

    }
}
