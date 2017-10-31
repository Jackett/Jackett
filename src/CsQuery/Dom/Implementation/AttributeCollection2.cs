using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CsQuery.ExtensionMethods;
using CsQuery.ExtensionMethods.Internal;
using CsQuery.HtmlParser;
using CsQuery.Utility;

namespace CsQuery.Implementation
{
    /// <summary>
    /// This was a hybrid dictionary implementation to see how much we could gain by not creating a
    /// new dictionary object except when there were more than x attributes. As it turns out this
    /// doesn't gain us much.
    /// </summary>

    public class AttributeCollection2 : IDictionary<string, string>, IEnumerable<KeyValuePair<string, string>>
    {

        #region constructors

        /// <summary>
        /// will use the dictionary that its constructed with. This way bound attribute collections can use a common
        /// dictionary and avoid the expensive process of creating a dictionary object for each element
        /// </summary>
        public AttributeCollection2()
        {
            InnerKeys = new ushort[maxArraySize];
            InnerValues = new string[maxArraySize];
        }

        #endregion

        #region private properties

        private const int maxArraySize=3;

        private bool UseDict;
        private ushort[] InnerKeys;
        private string[] InnerValues;
        private IDictionary<ushort, string> InnerDictionary;
        
        internal string this[ushort nodeId]
        {
            get
            {
                return Get(nodeId);
            }
            set
            {
                Set(nodeId, value);
            }
        }

        #endregion

        #region public properties
        public bool HasAttributes
        {
            get
            {
                return Count > 0;
            }
        }

        private int _Count;
        public int Count
        {
            get
            {
                return UseDict ? InnerDictionary.Count : _Count;
            }
            protected set 
            {
                if (UseDict)
                {
                    throw new Exception("Shouldn't be setting Count in dictionary mode.");
                }
                _Count = value;
            }
        }

        #endregion

        #region public methods

        public void Clear()
        {
            Count=0;
        }

        public AttributeCollection2 Clone()
        {
            AttributeCollection2 clone = new AttributeCollection2();
           
            if (HasAttributes)
            {
                foreach (var id in GetAttributeIds())
                {
                    clone.SetRaw(id, Get(id));
                }
            }
            return clone;
        }

        public void Add(string name, string value)
        {
            Set(name, value);
        }

        public bool Remove(string name)
        {
            return Unset(name);
        }
        public bool Remove(ushort tokenId)
        {
            return Unset(tokenId);
        }
        public string this[string name]
        {
            get
            {
                return Get(name);
            }
            set
            {
                Set(name, value);
            }
        }
        public bool ContainsKey(string key)
        {
            if (Count == 0)
            {
                return false;
            }
            else if (!UseDict)
            {
                return InnerKeys.IndexOf(HtmlData.Tokenize(key),Count) >= 0;
            }
            else
            {
                return InnerDictionary.ContainsKey(HtmlData.Tokenize(key));
            }
        }
        public bool ContainsKey(ushort tokenId)
        {
            if (Count == 0)
            {
                return false;
            }
            else if (!UseDict)
            {
                return InnerKeys.IndexOf(tokenId,Count) >= 0;
            }
            else
            {
                return InnerDictionary.ContainsKey(tokenId);
            }
        }
        public ICollection<string> Keys
        {
            get
            {
                List<string> keys = new List<string>();
                if (!UseDict)
                {
                    for (int i = 0; i < Count; i++)
                    {
                        keys.Add(HtmlData.TokenName(InnerKeys[i]));
                    }
                }
                else
                {
                    foreach (var id in InnerDictionary.Keys)
                    {
                        keys.Add(HtmlData.TokenName(id).ToLower());
                    }
                }
                return keys;
            }
        }
        public ICollection<string> Values
        {
            get 
            {
                List<string> keys = new List<string>();
                if (!UseDict)
                {
                    for (int i = 0; i < Count; i++)
                    {
                        keys.Add(InnerValues[i]);
                    }
                }
                else
                {
                    foreach (var value in InnerDictionary.Values)
                    {
                        keys.Add(value);
                    }
                }
                return keys;

            
            }
        }
        public bool TryGetValue(string key, out string value)
        {
            // do not use trygetvalue from dictionary. We need default handling in Get
            
            value = Get(key);
            return value != null ||
                ContainsKey(HtmlData.Tokenize(key));
        }
        public bool TryGetValue(ushort key, out string value)
        {
            // do not use trygetvalue from dictionary. We need default handling in Get
            value = Get(key);
            return value != null ||
                ContainsKey(key);
        }
        /// <summary>
        /// Sets a boolean only attribute having no value
        /// </summary>
        /// <param name="name"></param>
        public void SetBoolean(string name)
        {
            ushort tokenId = HtmlData.Tokenize(name);

            SetBoolean(tokenId);
        }
        public void SetBoolean(ushort tokenId)
        {

            if (!UseDict)
            {
                int index = InnerKeys.IndexOf(tokenId,Count);
                if (index < 0)
                {
                    index = GetNextIndex();
                }
                if (!UseDict)
                {

                    InnerKeys[index] = tokenId;
                    InnerValues[index] = null;
                    return;
                }
            }
            InnerDictionary[tokenId] = null;
        }


        /// <summary>
        /// Removing an attribute implementation
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool Unset(string name)
        {
            return Unset(HtmlData.Tokenize(name));
        }
        public bool Unset(ushort tokenId)
        {
            
            if (!UseDict)
            {
                int index = InnerKeys.IndexOf(tokenId, Count);
                if (index < 0)
                {
                    return false;
                }

                for (int i = index; i < Count - 1; i++)
                {
                    InnerKeys[i] = InnerKeys[i + 1];
                    InnerValues[i] = InnerValues[i + 1];
                }
                Count--;
                return true;

            }
            else
            {
                return InnerDictionary.Remove(tokenId);
            }
        }
        #endregion

        #region private methods

        /// <summary>
        /// Return the next index, and convert to a dictionary if the non-object limit is exceeded
        /// </summary>
        /// <returns></returns>
        private int GetNextIndex()
        {

            if (!UseDict)
            {
                Count++;
                if (Count > maxArraySize)
                {
                    UseDict = true;
                    InnerDictionary = new Dictionary<ushort, string>();
                    for (int i = 0; i < maxArraySize; i++)
                    {
                        InnerDictionary[InnerKeys[i]] = InnerValues[i];
                    }
                }
            }
            else
            {
                throw new Exception("Can't access GetNextIndex in dictionary mode.");
            }
            return Count-1;
        }

        protected string Get(string name)
        {
            name = name.CleanUp();
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            return Get(HtmlData.Tokenize(name));
        }
        protected string Get(ushort tokenId)
        {
            string value;
            if (!UseDict)
            {
                int index = InnerKeys.IndexOf(tokenId,Count);
                return index < 0 ? 
                    null :
                    InnerValues[index];

            }
            else
            {
                return InnerDictionary.TryGetValue(tokenId, out value) ?
                    value : 
                    null;
            }

        }
        /// <summary>
        /// Adding an attribute implementation
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        protected void Set(string name, string value)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Cannot set an attribute with no name.");
            }
            name = name.CleanUp();
            Set(HtmlData.Tokenize(name), value);
        }
        /// <summary>
        /// Second to last line of defense -- will call back to owning Element for attempts to set class, style, or ID, which are 
        /// managed by Element.
        /// </summary>
        /// <param name="tokenId"></param>
        /// <param name="value"></param>
        protected void Set(ushort tokenId, string value)
        {
            if (value == null)
            {
                Unset(tokenId);
            }
            else
            {
                SetRaw(tokenId, value);
            }
        }
        /// <summary>
        /// Used by DomElement to (finally) set the ID value
        /// </summary>
        /// <param name="tokenId"></param>
        /// <param name="value"></param>
        internal void SetRaw(ushort tokenId, string value)
        {
            
            if (!UseDict)
            {
                int newIndex = InnerKeys.IndexOf(tokenId, Count);
                if (newIndex < 0)
                {
                    newIndex = GetNextIndex();
                }
                if (!UseDict)
                {
                    InnerKeys[newIndex] = tokenId;
                    InnerValues[newIndex] = value;
                    return;

                }
            }
                
            InnerDictionary[tokenId] = value;
                
            
        }
        
       


        protected IEnumerable<KeyValuePair<string, string>> GetAttributes()
        {
            if (!UseDict)
            {
                for (int i = 0; i < Count; i++)
                {
                    yield return new KeyValuePair<string, string>(HtmlData.TokenName(InnerKeys[i]), InnerValues[i]);
                }
            }
            else
            {
                foreach (var kvp in InnerDictionary)
                {
                    yield return new KeyValuePair<string, string>(HtmlData.TokenName(kvp.Key).ToLower(), kvp.Value);
                }
            }
        }

        internal IEnumerable<ushort> GetAttributeIds()
        {
            if (!UseDict)
            {
                for (int i = 0; i < Count; i++)
                {
                    yield return InnerKeys[i];
                }
            }
            else
            {
                foreach (var key in InnerDictionary.Keys)
                {
                    yield return key;
                }
            }
        }

        #endregion

        #region interface implementation


        bool ICollection<KeyValuePair<string, string>>.IsReadOnly
        {
            get { return false; }
        }

        void ICollection<KeyValuePair<string, string>>.Add(KeyValuePair<string, string> item)
        {
            Add(item.Key, item.Value);
        }


        bool ICollection<KeyValuePair<string, string>>.Contains(KeyValuePair<string, string> item)
        {
            return ContainsKey(item.Key)
                && Get(item.Key) == item.Value;
        }

        void ICollection<KeyValuePair<string, string>>.CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            array = new KeyValuePair<string, string>[Count];
            int index = 0;
            foreach (var kvp in GetAttributes())
            {
                array[index++] = new KeyValuePair<string, string>(kvp.Key, kvp.Value);
            }
        }

        bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> item)
        {
            if (ContainsKey(item.Key)
                && Get(item.Key) == item.Value)
            {
                return Remove(item.Key);
            }
            else
            {
                return false;
            }
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return GetAttributes().GetEnumerator();
        }


        IEnumerator<KeyValuePair<string, string>> IEnumerable<KeyValuePair<string, string>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}
