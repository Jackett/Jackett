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
    /// A collection of attributes.
    /// </summary>

    public class AttributeCollection : IDictionary<string, string>, IEnumerable<KeyValuePair<string, string>>
    {
        #region constructors

        /// <summary>
        /// Default constructor.
        /// </summary>

        public AttributeCollection()
        {

        }
        #endregion

        #region private properties

        private IDictionary<ushort, string> Attributes = new Dictionary<ushort, string>();

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

        /// <summary>
        /// Test whether there are any attributes in this collection.
        /// </summary>

        public bool HasAttributes
        {
            get
            {
                return 
                    Attributes.Count > 0;
            }
        }

        /// <summary>
        /// The number of attributes in this collection
        /// </summary>

        public int Count
        {
            get { return Attributes.Count; }
        }

        #endregion

        #region public methods

        /// <summary>
        /// Removes all attributes from this collection.
        /// </summary>

        public void Clear()
        {
            Attributes.Clear();
        }

        /// <summary>
        /// Makes a deep copy of the attribute collection.
        /// </summary>
        ///
        /// <returns>
        /// A copy of this object.
        /// </returns>

        public AttributeCollection Clone()
        {
            AttributeCollection clone = new AttributeCollection();
           
            if (HasAttributes)
            {
                foreach (var kvp in Attributes)
                {
                    clone.Attributes.Add(kvp.Key, kvp.Value);
                }
            }
            return clone;
        }

        /// <summary>
        /// Adds a new name/value pair to the collection
        /// </summary>
        ///
        /// <param name="name">
        /// The name of the attribute.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>

        public void Add(string name, string value)
        {
            Set(name, value);
        }

        /// <summary>
        /// Removes the named attribute from the collection.
        /// </summary>
        ///
        /// <param name="name">
        /// The name to remove.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public bool Remove(string name)
        {
            return Unset(name);
        }

        /// <summary>
        /// Removes an attribute identified by its token ID from the collection
        /// </summary>
        ///
        /// <param name="tokenId">
        /// The unique token ID for the attribute name.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public bool Remove(ushort tokenId)
        {
            return Unset(tokenId);
        }

        /// <summary>
        /// Get or set an attribute value by name
        /// </summary>
        ///
        /// <param name="name">
        /// The name of the attribute.
        /// </param>
        ///
        /// <returns>
        /// The value.
        /// </returns>

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

        /// <summary>
        /// Test whether the named attribute exists in the collection.
        /// </summary>
        ///
        /// <param name="key">
        /// The attribute name.
        /// </param>
        ///
        /// <returns>
        /// true if it exists, false if not.
        /// </returns>

        public bool ContainsKey(string key)
        {
            return Attributes.ContainsKey(HtmlData.Tokenize(key));
        }

        /// <summary>
        /// Test whether the attribute identified by its unique token ID exists in the collection.
        /// </summary>
        ///
        /// <param name="tokenId">
        /// The unique token ID for the attribute name.
        /// </param>
        ///
        /// <returns>
        /// true if it exists, false if not.
        /// </returns>

        public bool ContainsKey(ushort tokenId)
        {
            return Attributes.ContainsKey(tokenId);
        }

        /// <summary>
        /// Get a sequence of all attribute names in this collection.
        /// </summary>

        public ICollection<string> Keys
        {
            get
            {
                List<string> keys = new List<string>();
                foreach (var id in Attributes.Keys)
                {
                    keys.Add(HtmlData.TokenName(id).ToLower());
                }
                return keys;
            }
        }

        /// <summary>
        /// A collection of all the values in this attribute collection
        /// </summary>

        public ICollection<string> Values
        {
            get { return Attributes.Values; }
        }

        /// <summary>
        /// Try to get a value for the specified attribute name.
        /// </summary>
        ///
        /// <param name="name">
        /// The key.
        /// </param>
        /// <param name="value">
        /// [out] The value.
        /// </param>
        ///
        /// <returns>
        /// true if the key was present, false if it fails.
        /// </returns>

        public bool TryGetValue(string name, out string value)
        {
            // do not use trygetvalue from dictionary. We need default handling in Get
            value = Get(name);
            return value != null ||
                Attributes.ContainsKey(HtmlData.Tokenize(name));
        }

        /// <summary>
        /// Try to get a value for the specified attribute identified by its unique token ID.
        /// </summary>
        ///
        /// <param name="tokenId">
        /// The attribute's token ID.
        /// </param>
        /// <param name="value">
        /// [out] The value.
        /// </param>
        ///
        /// <returns>
        /// true if the key was present, false if not.
        /// </returns>

        public bool TryGetValue(ushort tokenId, out string value)
        {
            // do not use trygetvalue from dictionary. We need default handling in Get
            value = Get(tokenId);
            return value != null ||
                Attributes.ContainsKey(tokenId);
        }

        /// <summary>
        /// Sets a boolean only attribute having no value.
        /// </summary>
        ///
        /// <param name="name">
        /// The attribute to set
        /// </param>

        public void SetBoolean(string name)
        {
            ushort tokenId = HtmlData.Tokenize(name);

            SetBoolean(tokenId);
        }

        /// <summary>
        /// Sets a boolean only attribute having no value.
        /// </summary>
        ///
        /// <param name="tokenId">
        /// The attribute's unique token ID
        /// </param>

        public void SetBoolean(ushort tokenId)
        {
            Attributes[tokenId] = null;
        }

        /// <summary>
        /// Remove an attribute.
        /// </summary>
        ///
        /// <param name="name">
        /// The attribute name
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public bool Unset(string name)
        {
            return Unset(HtmlData.Tokenize(name));
        }

        /// <summary>
        /// Remove an attribute.
        /// </summary>
        ///
        /// <param name="tokenId">
        /// The unique token ID for the attribute name.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public bool Unset(ushort tokenId)
        {
            bool result = Attributes.Remove(tokenId);
            return result;
        }

        #endregion

        #region private methods

        private string Get(string name)
        {
            name = name.CleanUp();
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }
            return Get(HtmlData.Tokenize(name));
        }

        private string Get(ushort tokenId)
        {
            string value;

            if (Attributes.TryGetValue(tokenId, out value))
            {
                return value;
            }
            else
            {
                return null;
            }

        }

        /// <summary>
        /// Adding an attribute implementation
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        private void Set(string name, string value)
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
        private void Set(ushort tokenId, string value)
        {
            SetRaw(tokenId, value);
        }
        /// <summary>
        /// Used by DomElement to (finally) set the ID value
        /// </summary>
        /// <param name="tokenId"></param>
        /// <param name="value"></param>
        internal void SetRaw(ushort tokenId, string value)
        {
            if (value == null)
            {
                Unset(tokenId);
            }
            else
            {
                Attributes[tokenId] = value;
            }
        }

        /// <summary>
        /// Enumerates the attributes in this collection as a sequence of KeyValuePairs.
        /// </summary>
        ///
        /// <returns>
        /// A sequence of KeyValuePair&lt;string,string&gt; objects.
        /// </returns>

        protected IEnumerable<KeyValuePair<string, string>> GetAttributes()
        {
            foreach (var kvp in Attributes)
            {
                yield return new KeyValuePair<string, string>(HtmlData.TokenName(kvp.Key).ToLower(), kvp.Value);
            }
        }
        internal IEnumerable<ushort> GetAttributeIds()
        {
            return Attributes.Keys;
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
                && Attributes[HtmlData.Tokenize(item.Key)] == item.Value;
        }

        void ICollection<KeyValuePair<string, string>>.CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
            array = new KeyValuePair<string, string>[Attributes.Count];
            int index = 0;
            foreach (var kvp in Attributes)
            {
                array[index++] = new KeyValuePair<string, string>(HtmlData.TokenName(kvp.Key), kvp.Value);
            }
        }

        bool ICollection<KeyValuePair<string, string>>.Remove(KeyValuePair<string, string> item)
        {
            if (ContainsKey(item.Key)
                && Attributes[HtmlData.Tokenize(item.Key)] == item.Value)
            {
                return Remove(item.Key);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the enumerator for this AttributeCollection
        /// </summary>
        ///
        /// <returns>
        /// The enumerator.
        /// </returns>

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
