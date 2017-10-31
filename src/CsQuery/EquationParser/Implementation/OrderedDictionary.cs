using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// TODO this should be fully commented; however it's not part of the main public API

#pragma warning disable 1591
#pragma warning disable 1570

namespace CsQuery.EquationParser
{
    /// <summary>
    /// Default implementation of OrderedDictionary-T,TKey,TValue-
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class OrderedDictionary<TKey, TValue> : OrderedDictionary<Dictionary<TKey, TValue>, TKey, TValue>, IOrderedDictionary<TKey, TValue>
    {

    }

    /// <summary>
    /// A dictionary that also maintains the order added.
    /// </summary>
    /// <typeparam name="T">The concrete type of dictionary to use for the inner dictionary</typeparam>
    /// <typeparam name="TKey">The key type</typeparam>
    /// <typeparam name="TValue">The value type</typeparam>
    public class OrderedDictionary<T, TKey, TValue> : IDictionary<TKey, TValue>, IList<KeyValuePair<TKey, TValue>>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>> where T : IDictionary<TKey, TValue>, new()
    {
        #region private members
        private IDictionary<TKey, TValue> _InnerDictionary;
        private List<KeyValuePair<TKey, TValue>> InnerList;
        private int AutoKeys = 0;

        protected IDictionary<TKey, TValue> InnerDictionary
        {
            get
            {
                if (_InnerDictionary == null)
                {
                    _InnerDictionary = new T();
                    InnerList = new List<KeyValuePair<TKey, TValue>>();
                }
                return _InnerDictionary;
            }
        }
        #endregion

        #region public properties

        public IList<TKey> Keys
        {
            get
            {
                return (IList<TKey>)InnerList.Select(item => item.Key).ToList().AsReadOnly();
            }
        }
        public IList<TValue> Values
        {
            get
            {
                return GetValuesOrdered().ToList().AsReadOnly();
            }
        }
        public int Count
        {
            get { return InnerList.Count; }
        }

        public TValue this[int index]
        {
            get
            {
                return InnerDictionary[InnerList[index].Key];
            }
            set
            {
                var curItem = InnerList[index];
                InnerDictionary[curItem.Key] = value;
                InnerList[index] = curItem;
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        #endregion

        #region public methods

        public void Add(TKey key, TValue value)
        {
            if (!InnerDictionary.ContainsKey(key))
            {
                InnerList.Add(new KeyValuePair<TKey, TValue>(key, value));
            }
            InnerDictionary[key] = value;
        }

        public bool ContainsKey(TKey key)
        {
            return InnerDictionary.ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            if (key.Equals(default(TKey)))
            {
                return false;
            }
            TValue value;
            if (InnerDictionary.TryGetValue(key, out value))
            {
                InnerDictionary.Remove(key);
                var existingItem = InnerList.FirstOrDefault(item => item.Key.Equals(key));
                InnerList.Remove(existingItem);
                return true;
            }
            return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return InnerDictionary.TryGetValue(key, out value);
        }
        /// <summary>
        /// Setting uses indexOf - not optimized.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TValue this[TKey key]
        {
            get
            {
                return InnerDictionary[key];
            }
            set
            {
                TValue current;
                KeyValuePair<TKey, TValue> newItem = new KeyValuePair<TKey, TValue>(key, value);
                if (InnerDictionary.TryGetValue(key, out current))
                {
                    InnerList[IndexOf(key)] = newItem;
                }
                else
                {
                    InnerList.Add(newItem);
                }
                InnerDictionary[key] = value;
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            // add throws exception for existing items - this is safe
            InnerDictionary.Add(item);
            InnerList.Add(item);
        }

        public void Clear()
        {
            InnerDictionary.Clear();
            InnerList.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return InnerDictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            InnerDictionary.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {

            if (InnerDictionary.Remove(item))
            {
                InnerList.Remove(item);
                return true;
            }
            return false;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return InnerDictionary.GetEnumerator();
        }
        /// <summary>
        /// This class is optimized for access by numeric index, or accessing an object by key. If there's a frequent 
        /// need to obtain the numeric index from the key then this should have another dictionary added to xref.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public int IndexOf(TKey key)
        {
            for (int i = 0; i < InnerList.Count; i++)
            {
                if (InnerList[i].Key.Equals(key))
                {
                    return i;
                }
            }
            return -1;
        }
        public int IndexOf(KeyValuePair<TKey, TValue> item)
        {
            return InnerList.IndexOf(item);
        }
        protected TKey GetKey(TValue item)
        {
            var dictItem = InnerDictionary.FirstOrDefault(val => val.Value.Equals(item));
            if (dictItem.Equals(default(KeyValuePair<TKey, TValue>)))
            {
                return default(TKey);
            }
            else
            {
                return dictItem.Key;
            }
        }

        /// <summary>
        /// Insert an item by value only. Dup values are possible this way, it will have a key equal to the string of its index.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(int index, TValue item)
        {
            if (typeof(TKey) != typeof(string) && typeof(TKey) != typeof(int))
            {
                throw new InvalidOperationException("I can only autogenerate keys for string & int key types.");
            }
            else
            {
                TKey key;
                key = (TKey)Convert.ChangeType(AutoKeys++.ToString(), typeof(TKey));
                Insert(index, new KeyValuePair<TKey, TValue>(key, item));
            }
        }
        public void Insert(int index, KeyValuePair<TKey, TValue> item)
        {
            if (!InnerDictionary.ContainsKey(item.Key))
            {
                InnerList.Insert(index, item);
                InnerDictionary.Add(item.Key, item.Value);
            }
        }
        public void RemoveAt(int index)
        {
            var item = InnerList[index];
            InnerDictionary.Remove(item);
            InnerList.RemoveAt(index);
        }

        public void Add(TValue value)
        {

            if (typeof(TKey) != typeof(string) && typeof(TKey) != typeof(int))
            {
                throw new InvalidOperationException("I can only autogenerate keys for string & int key types.");
            }
            else
            {
                TKey key;
                int newIndex = InnerList.Count;
                key = (TKey)Convert.ChangeType(newIndex, typeof(TKey));
                if (!InnerDictionary.ContainsKey(key))
                {
                    InnerList.Insert(newIndex, new KeyValuePair<TKey, TValue>(key, value));
                    InnerDictionary.Add(key, value);
                }
            }
        }

        public bool Contains(TValue item)
        {
            return !GetKey(item).Equals(default(TKey));
        }

        public void CopyTo(TValue[] array, int arrayIndex)
        {
            Values.CopyTo(array, arrayIndex);
        }


        public bool Remove(TValue item)
        {
            return Remove(GetKey(item));
        }

        public override string ToString()
        {
            return InnerDictionary.ToString();
        }
        #endregion

        #region private methods
        protected IEnumerable<TValue> GetValuesOrdered()
        {
            for (int i = 0; i < InnerList.Count; i++)
            {
                yield return InnerDictionary[InnerList[i].Key];
            }
        }
        #endregion

        #region interface only
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return InnerList.GetEnumerator();
        }

        ICollection<TKey> IDictionary<TKey, TValue>.Keys
        {
            get { return Keys; }
        }
        ICollection<TValue> IDictionary<TKey, TValue>.Values
        {
            get { return Values; }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return InnerDictionary.GetEnumerator();
        }
        KeyValuePair<TKey, TValue> IList<KeyValuePair<TKey, TValue>>.this[int index]
        {
            get
            {
                return InnerList[index];
            }
            set
            {
                var curKey = InnerList[index];
                InnerDictionary[curKey.Key] = value.Value;
                InnerList[index] = curKey;
            }
        }

        #endregion
    }
}
