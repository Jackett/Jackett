using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Implementation
{
    /// <summary>
    /// Alternate implementation of the IDictionary for attributes that uses no objects to see if
    /// this is important for performance. (It doesn't seem to be). Not used as of 6/15/2012.
    /// </summary>

    public class AttributeDictionary : IDictionary<ushort, string>
    {
        private const int arrayIncrements =1;
        private int maxArraySize;

        protected ushort[] InnerKeys;
        protected string[] InnerValues;

        protected int _Count;

        public AttributeDictionary()
        {
            maxArraySize = arrayIncrements;
            InnerKeys = new ushort[maxArraySize];
            InnerValues = new string[maxArraySize];
        }

        private int IndexOf(ushort key)
        {
            for (int i = 0; i < _Count; i++)
            {
                if (InnerKeys[i]==key) {
                    return i;
                }
            }
            return -1;
        }

        // Also identifies a hole in the index
        private int IndexOf(ushort key, out int holeIndex)
        {
            int _holeIndex=-1;
            for (int i = 0; i < _Count; i++)
            {

                if (InnerKeys[i]==key)
                {
                    holeIndex = _holeIndex;
                    return i;
                } else if (InnerKeys[i] == 0)
                {
                    _holeIndex = i;
                }
            }

            holeIndex = _holeIndex;
            return -1;
        }
        public bool ContainsKey(ushort key)
        {
            if (_Count==0)
            {
                return false;
            }
            else
            {
                return InnerKeys.Contains(key);
            }
        }

        public ICollection<ushort> Keys
        {
            get
            {
                return InnerKeys.ToList();
            }
        }

        public bool Remove(ushort key)
        {
            int index = IndexOf(key);
            if (index < 0)
            {
                return false;
            }
            else
            {
                InnerKeys[index] = default(int);
                return true;
            }
        }

        public bool TryGetValue(ushort key, out string value)
        {
            int index = IndexOf( key);
            if (index < 0)
            {
                value = null;
                return false;
            }
            else
            {
                value = InnerValues[index];
                return true;
            }
        }

        public ICollection<string> Values
        {
            get
            {

                List<string> values = new List<string>();

                for (int i = 0; i < _Count; i++)
                {
                    if (InnerKeys[i] != default(int))
                    {
                        values.Add(InnerValues[i]);
                    }
                }
                return values;
            }
        }

        public string this[ushort key]
        {
            get
            {

                string value;
                if (TryGetValue(key, out value))
                {
                    return value;
                }
                else
                {
                    throw new KeyNotFoundException("The value was not found.");
                }
            }
            set
            {
                int newIndex;
                int index = IndexOf(key, out newIndex);
                if (index < 0)
                {
                    if (newIndex < 0)
                    {
                        if (_Count == maxArraySize)
                        {
                            maxArraySize += arrayIncrements;
                            ushort[] temp = new ushort[maxArraySize];
                            Array.Copy(InnerKeys, temp, _Count);
                            InnerKeys = temp;

                            string[] temp2 = new string[maxArraySize];
                            Array.Copy(InnerValues, temp2, _Count);
                            InnerValues = temp2;
                            newIndex = _Count;
                            _Count++;
                        }
                        else
                        {
                            newIndex = _Count++;
                        }
                    }
                    InnerKeys[newIndex] = key;
                    InnerValues[newIndex] = value;

                }
                else
                {
                    InnerValues[index] = value;
                }
            }
        }

        public void Add(KeyValuePair<ushort, string> item)
        {
            int index = IndexOf(item.Key);
            if (index >= 0 && item.Value != InnerValues[index])
            {
                throw new InvalidOperationException("The key already exists with a different value.");
            }
            
        }
        public void Add(ushort key, string value)
        {
            this[key] = value;
        }
        public void Clear()
        {
            _Count = 0;
        }

        public bool Contains(KeyValuePair<ushort, string> item)
        {
            if (_Count == 0)
            {
                return false;
            }
            else
            {
                for (int i = 0; i < _Count; i++)
                {
                    if (InnerKeys[i] == item.Key)
                    {
                        return InnerValues[i] == item.Value;
                    }
                }
                return false;
            }
        }

        public void CopyTo(KeyValuePair<ushort, string>[] array, int arrayIndex)
        {
            int index = 0;
            array = new KeyValuePair<ushort, string>[Count];

            for (int i=0;i<_Count;i++)
            {
                if (InnerKeys[i] != 0)
                {
                    array[arrayIndex + index++] = new KeyValuePair<ushort, string>(InnerKeys[i], InnerValues[i]);
                }

            }
        }

        public int Count
        {
            get { return _Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<ushort, string> item)
        {
            if (Contains(item))
            {
                return Remove(item.Key);
            }
            return false;
        }
        public override string ToString()
        {
            string output="";
            for (int i = 0; i < _Count; i++)
            {

                if (InnerKeys[i] > 0)
                {
                    output += (output == "" ? "" : ",") +
                        "[" + InnerKeys[i] + ",\"" + InnerValues[i] + "\"]";
                }
            }
            return output;
        }
        #region interface members
        public IEnumerator<KeyValuePair<ushort, string>> GetEnumerator()
        {
            return GetEnumerable().GetEnumerator();
        }

        protected IEnumerable<KeyValuePair<ushort, string>> GetEnumerable()
        {
            for (int i = 0; i < _Count; i++)
            {
                if (InnerKeys[i] > 0)
                {
                    yield return new KeyValuePair<ushort, string>(InnerKeys[i], InnerValues[i]);
                }
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion
    }
}
