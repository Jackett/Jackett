using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;

namespace CsQuery.Implementation
{
    /// <summary>
    /// A list of nodes representing the children of a DOM element.
    /// </summary>

    public class ChildNodeList : INodeList
    {

        #region constructor

        /// <summary>
        /// Constructor binding this list to its owner
        /// </summary>
        ///
        /// <param name="owner">
        /// The object that owns this list (the parent)
        /// </param>

        public ChildNodeList(IDomContainer owner)
        {
            Owner = owner;
        }

        #endregion

        #region private properties

        /// <summary>
        /// The inner list of objects.
        /// </summary>

        protected List<IDomObject> InnerList
        {
            get
            {
                if (_InnerList == null)
                {
                    _InnerList = new List<IDomObject>();
                }
                return _InnerList;
            }
        }

        private List<IDomObject> _InnerList = null;

        #endregion

        #region public properties

        /// <summary>
        /// Event raised when the NodeList changes.
        /// </summary>

        public event EventHandler<NodeEventArgs> OnChanged;

        /// <summary>
        /// Gets or sets the object that owns this list (the parent)
        /// </summary>

        public IDomContainer Owner { get; set; }

        /// <summary>
        /// Get the item at the specified index.
        /// </summary>
        ///
        /// <param name="index">
        /// Zero-based index of the item.
        /// </param>
        ///
        /// <returns>
        /// An item.
        /// </returns>

        public IDomObject Item(int index)
        {
            return this[index];
        }
        #endregion

        #region INodeList<T> members


        #endregion

        #region IList<T> Members

        /// <summary>
        /// The zero-based index of the item in this list
        /// </summary>
        ///
        /// <param name="item">
        /// The element to add.
        /// </param>
        ///
        /// <returns>
        /// The zero-based index of the item, or -1 if it was not found.
        /// </returns>

        public int IndexOf(IDomObject item)
        {
            return _InnerList == null ? -1 : InnerList.IndexOf(item);
        }

        /// <summary>
        /// Add a child to this element.
        /// </summary>
        ///
        /// <param name="item">
        /// The element to add
        /// </param>
        
        public void Add(IDomObject item)
        {
 
            if (item.ParentNode != null)
            {
                item.Remove();
            }

            // Ensure ID uniqueness - remove ID if same-named object already exists
            
            if (!String.IsNullOrEmpty(item.Id)
                && !Owner.IsFragment
                && Owner.Document.GetElementById(item.Id) != null)
            {
                item.Id = null;
            }

            InnerList.Add(item);

            AddParent(item, InnerList.Count - 1);
            RaiseChangedEvent(item);
        }

        /// <summary>
        /// Add a child without validating that a node is a member of this DOM already or that the ID is
        /// unique.
        /// </summary>
        ///
        /// <param name="item">
        /// The item to add
        /// </param>

        public void AddAlways(IDomObject item)
        {
            InnerList.Add(item);
            AddParent(item, InnerList.Count - 1);
        }

        /// <summary>
        /// Adds a child element at a specific index.
        /// </summary>
        ///
        /// <param name="index">
        /// The index at which to insert the element
        /// </param>
        /// <param name="item">
        /// The element to insert
        /// </param>

        public void Insert(int index, IDomObject item)
        {
            if (item.ParentNode != null)
            {

                item.Remove();
            }

            // This must come BEFORE AddParent - otherwise the index entry will be present already at this position 

            ReindexFromRight(index); 

            if (index == InnerList.Count)
            {
                InnerList.Add(item);
            }
            else
            {
                InnerList.Insert(index, item);
            }
            

            AddParent(item, index);

            RaiseChangedEvent(item);
        }

        /// <summary>
        /// Remove an item from this list and update index.
        /// </summary>
        ///
        /// <param name="index">
        /// The ordinal index at which to remove the node
        /// </param>

        public void RemoveAt(int index)
        {
            IDomObject item = InnerList[index];
            InnerList.RemoveAt(index);
            RemoveParent(item);
            ReindexFromLeft(index);
            RaiseChangedEvent(item);
        }

        /// <summary>
        /// Remove an element from this element's children.
        /// </summary>
        ///
        /// <param name="item">
        /// The item to remove.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if the item was not found in the children.
        /// </returns>

        public bool Remove(IDomObject item)
        {
            if (item.ParentNode != this.Owner)
            {
                return false;
            }
            else
            {
                RemoveAt(item.Index);
                return true;
            }
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

        [IndexerName("Indexer")]
        public IDomObject this[int index]
        {
            get
            {
                return InnerList[index];
            }
            set
            {
                RemoveAt(index);
                if (index < InnerList.Count)
                {
                    Insert(index, value);
                }
                else
                {
                    Add(value);
                }

            }
        }


        #endregion

        #region private methods

        private void RaiseChangedEvent(IDomObject node)
        {
            var onChanged = OnChanged;
            if (onChanged != null)
            {
                onChanged(this, new NodeEventArgs(node));
            }
        }

        private void RemoveParent(IDomObject element)
        {
            if (element.ParentNode != null)
            {
                DomObject item = element as DomObject;
                if (!element.IsDisconnected && element.IsIndexed)
                {
                    item.Document.DocumentIndex.RemoveFromIndex((IDomIndexedNode)element);
                }
                item.ParentNode = null;
            }


        }

        private void AddParent(IDomObject element, int index)
        {
            DomObject item = element as DomObject;

            item.ParentNode = Owner;
            item.Index = index;
            if (element.IsIndexed)
            {
                item.Document.DocumentIndex.AddToIndex((IDomIndexedNode)element);
            }
        }

        /// <summary>
        /// Reindex a range of elements starting at index, through the end
        /// </summary>
        ///
        /// <param name="index">
        /// The index at which to insert the element.
        /// </param>

        private void ReindexFromLeft(int index)
        {
            if (index < InnerList.Count)
            {
                bool isDisconnected = Owner.IsDisconnected;


                int end = InnerList.Count;
                for (int i = index; i < end; i++)
                {
                    if (!isDisconnected && InnerList[i].IsIndexed)
                    {
                        var el = (DomElement)InnerList[i];

                        // This would get assigned anyway but this is much faster since we already know the index
                        Owner.Document.DocumentIndex.RemoveFromIndex(el);
                        el.Index = i;

                        Owner.Document.DocumentIndex.AddToIndex(el);
                    }
                    else
                    {
                        // this should run for disconnected nodes & for n
                        ((DomObject)InnerList[i]).Index = i;
                    }
                }
            }

        }
        /// <summary>
        /// Reindex all documents starting the last index through index, from right to left.
        /// </summary>
        ///
        /// <param name="index">
        /// The index at which to insert the element.
        /// </param>

        private void ReindexFromRight(int index)
        {
            if (index < InnerList.Count)
            {
                bool isDisconnected = Owner.IsDisconnected;

                int end = index - 1;
                for (int i = InnerList.Count-1; i > end; i--)
                {
                    if (!isDisconnected && InnerList[i].IsIndexed)
                    {
                        var el = (DomElement)InnerList[i];

                        // This would get assigned anyway but this is much faster since we already know the index
                        Owner.Document.DocumentIndex.RemoveFromIndex(el);
                        el.Index = i + 1;

                        Owner.Document.DocumentIndex.AddToIndex(el);
                    }
                    else
                    {
                        // this should run for disconnected nodes & for n
                        ((DomObject)InnerList[i]).Index = i+1;
                    }
                }
            }
        }

        
        #endregion

        #region ICollection<IDomObject> Members

        /// <summary>
        /// Adds a range of elements as children of this list.
        /// </summary>
        ///
        /// <param name="elements">
        /// An IEnumerable&lt;IDomObject&gt; of items to append to this.
        /// </param>

        public void AddRange(IEnumerable<IDomObject> elements)
        {
            // because elements will be removed from their parent while adding, we need to copy the
            // enumerable first since it will change otherwise
            List<IDomObject> copy = new List<IDomObject>(elements);
            foreach (IDomObject e in copy)
            {
                Add(e);
            }
        }
        /// <summary>
        /// Remove all children of this node
        /// </summary>
        public void Clear()
        {
            if (_InnerList != null)
            {
                for (int i = InnerList.Count - 1; i >= 0; i--)
                {
                    Remove(InnerList[i]);
                }
            }
        }

        /// <summary>
        /// Query if this object contains the given item.
        /// </summary>
        ///
        /// <param name="item">
        /// The item to look for.
        /// </param>
        ///
        /// <returns>
        /// true if the object is in this collection, false if not.
        /// </returns>

        public bool Contains(IDomObject item)
        {
            return _InnerList==null ? false : InnerList.Contains(item);
        }

        /// <summary>
        /// Copies this list to an array.
        /// </summary>
        ///
        /// <param name="array">
        /// The array.
        /// </param>
        /// <param name="arrayIndex">
        /// Zero-based index of the starting point in the array to copy to.
        /// </param>

        public void CopyTo(IDomObject[] array, int arrayIndex)
        {
            InnerList.CopyTo(array,arrayIndex);
        }

        /// <summary>
        /// Gets the number of items in this list.
        /// </summary>

        public int Count
        {
            get { return _InnerList==null ? 0 : InnerList.Count; }
        }

        /// <summary>
        /// The number of nodes in this INodeList.
        /// </summary>

        public int Length
        {
            get { return Count; }
        }

        /// <summary>
        /// Gets a value indicating whether this object is read only. For ChildNodeList collections, this
        /// is always false.
        /// </summary>

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }


        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region IEnumerable<T> Members

        IEnumerator<IDomObject> IEnumerable<IDomObject>.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        ///
        /// <returns>
        /// The enumerator.
        /// </returns>

        public IEnumerator<IDomObject> GetEnumerator()
        {
            return InnerList.GetEnumerator();
        }

        #endregion

    }
}
