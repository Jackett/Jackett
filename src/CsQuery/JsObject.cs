 using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using CsQuery.Utility;


namespace CsQuery
{
 
    /// <summary>
    /// A dynamic object implementation that differs from ExpandoObject in two ways:
    /// 
    /// 1) Missing property values always return null (or a specified value)
    /// 2) Allows case-insensitivity
    /// 
    /// </summary>
    public class JsObject  : DynamicObject, 
        IDictionary<string, object>, 
        IEnumerable<KeyValuePair<string, object>>, IEnumerable
    {
        /// <summary>
        /// Default constructor.
        /// </summary>

        public JsObject()
        {
            Initialize(null, null);
        }

        /// <summary>
        /// Create in instance using a comparer and a particular value for missing properties
        /// </summary>
        ///
        /// <param name="comparer">
        /// The comparer.
        /// </param>
        /// <param name="missingPropertyValue">
        /// The missing property value.
        /// </param>

        public JsObject(StringComparer comparer = null,object missingPropertyValue=null)
        {
            Initialize(comparer,missingPropertyValue);

        }

        /// <summary>
        /// Initializes this object to its default state.
        /// </summary>
        ///
        /// <param name="comparer">
        /// The comparer.
        /// </param>
        /// <param name="missingPropertyValue">
        /// The missing property value.
        /// </param>

        protected void Initialize(StringComparer comparer, object missingPropertyValue)
        {
            AllowMissingProperties = true;
            MissingPropertyValue = missingPropertyValue;
            InnerProperties = new Dictionary<string, object>(comparer ?? StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Convert this object into a JSON string.
        /// </summary>
        ///
        /// <returns>
        /// This object as a string.
        /// </returns>

        public override string ToString()
        {
            return CQ.ToJSON(this);
        }

        /// <summary>
        /// Enumerates the property/value pairs
        /// </summary>
        ///
        /// <typeparam name="T">
        /// Generic type parameter.
        /// </typeparam>
        ///
        /// <returns>
        /// An enumerator that allows foreach to be used to process enumerate&lt; t&gt; in this
        /// collection.
        /// </returns>

        public IEnumerable<T> Enumerate<T>()
        {
            return Objects.EnumerateProperties<T>(this);
        }

        /// <summary>
        /// When true, accessing missing properties will return MissingPropertyValue instead of throwing
        /// an error.
        /// </summary>

        protected bool AllowMissingProperties
        {
            get;
            set;
        }

        /// <summary>
        /// An object or value to be returned when missing properties are accessed (assuming they are allowed)
        /// </summary>

        protected object MissingPropertyValue
        {
            get;
            set;
        }

        /// <summary>
        /// When true, the property names will not be case sensitive
        /// </summary>

        public bool IgnoreCase
        {
            get;
            set;
        }

        /// <summary>
        /// The dictionary of properties
        /// </summary>

        protected IDictionary<string, object> InnerProperties
        {
            get;
            set;
        }

        /// <summary>
        /// Return the value of a named property
        /// </summary>
        ///
        /// <param name="name">
        /// The name.
        /// </param>
        ///
        /// <returns>
        /// The indexed item.
        /// </returns>

        public object this[string name]
        {
            get
            {
                object result;
                TryGetMember(name, typeof(object), out result);
                return result;
            }
            set
            {
                TrySetMember(name, value);
            }
        }

        /// <summary>
        /// Gets the strongly-typed value of a property
        /// </summary>
        ///
        /// <typeparam name="T">
        /// Generic type parameter.
        /// </typeparam>
        /// <param name="name">
        /// The property name
        /// </param>
        ///
        /// <returns>
        /// The value, or null if the value does not exist.
        /// </returns>

        public T Get<T>(string name)
        {
            object value;
            TryGetMember(name, typeof(T), out value);
            return (T)value;
        }

        /// <summary>
        /// Return the value of a property as a strongly-typed sequence
        /// </summary>
        ///
        /// <exception cref="ArgumentException">
        /// Thrown when one or more arguments have unsupported or illegal values.
        /// </exception>
        ///
        /// <typeparam name="T">
        /// The type of value expected in the property
        /// </typeparam>
        /// <param name="name">
        /// The name of the property
        /// </param>
        ///
        /// <returns>
        /// A sequence of values of type T
        /// </returns>

        public IEnumerable<T> GetList<T>(string name)
        {
            IEnumerable list = Get(name) as IEnumerable;
            if (list != null)
            {
                foreach (object item in list)
                {
                    yield return (T)item;
                }
            }
            else
            {
                throw new ArgumentException("The property '" + name + "' is not an array.");
            }
        }

        /// <summary>
        /// Gets a value for a named property
        /// </summary>
        ///
        /// <param name="name">
        /// The property name.
        /// </param>
        ///
        /// <returns>
        /// The value
        /// </returns>

        public object Get(string name)
        {
            object value;
            TryGetMember(name, typeof(object), out value);
            return value;

        }

        /// <summary>
        /// Provides the implementation for operations that get member values. Classes derived from the
        /// <see cref="T:System.Dynamic.DynamicObject" /> class can override this method to specify
        /// dynamic behavior for operations such as getting a value for a property.
        /// </summary>
        ///
        /// <param name="binder">
        /// Provides information about the object that called the dynamic operation. The binder.Name
        /// property provides the name of the member on which the dynamic operation is performed. For
        /// example, for the Console.WriteLine(sampleObject.SampleProperty) statement, where sampleObject
        /// is an instance of the class derived from the <see cref="T:System.Dynamic.DynamicObject" />
        /// class, binder.Name returns "SampleProperty". The binder.IgnoreCase property specifies whether
        /// the member name is case-sensitive.
        /// </param>
        /// <param name="result">
        /// The result of the get operation. For example, if the method is called for a property, you can
        /// assign the property value to <paramref name="result" />.
        /// </param>
        ///
        /// <returns>
        /// true if the operation is successful; otherwise, false. If this method returns false, the run-
        /// time binder of the language determines the behavior. (In most cases, a run-time exception is
        /// thrown.)
        /// </returns>

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return TryGetMember(binder.Name, binder.ReturnType, out result);
        }

        /// <summary>
        /// Provides the implementation for operations that get member values. Classes derived from the
        /// <see cref="T:System.Dynamic.DynamicObject" /> class can override this method to specify
        /// dynamic behavior for operations such as getting a value for a property.
        /// </summary>
        ///
        /// <exception cref="KeyNotFoundException">
        /// Thrown when a key not found error condition occurs.
        /// </exception>
        ///
        /// <param name="name">
        /// .
        /// </param>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <param name="result">
        /// The result of the get operation. For example, if the method is called for a property, you can
        /// assign the property value to <paramref name="result" />.
        /// </param>
        ///
        /// <returns>
        /// true if the operation is successful; otherwise, false. If this method returns false, the run-
        /// time binder of the language determines the behavior. (In most cases, a run-time exception is
        /// thrown.)
        /// </returns>

        protected bool TryGetMember(string name, Type type, out object result)
        {
            object value = null;
            bool success = String.IsNullOrEmpty(name) ?
                false : 
                InnerProperties.TryGetValue(name, out value);

            if (!success)
            {
                if (AllowMissingProperties)
                {
                    if (type == typeof(object))
                    {
                        result = MissingPropertyValue;
                    }
                    else
                    {
                        result = Objects.DefaultValue(type);
                    }
                    success = true;
                }
                else
                {
                    throw new KeyNotFoundException("There is no property named \"" + name + "\".");
                }
            }
            else
            {
                if (type == typeof(object))
                {
                    result = value;
                }
                else
                {
                    result = Objects.Convert(value, type);
                }

            }
            return success;
        }

        /// <summary>
        /// Provides the implementation for operations that set member values. Classes derived from the
        /// <see cref="T:System.Dynamic.DynamicObject" /> class can override this method to specify
        /// dynamic behavior for operations such as setting a value for a property.
        /// </summary>
        ///
        /// <param name="binder">
        /// Provides information about the object that called the dynamic operation. The binder.Name
        /// property provides the name of the member to which the value is being assigned. For example,
        /// for the statement sampleObject.SampleProperty = "Test", where sampleObject is an instance of
        /// the class derived from the <see cref="T:System.Dynamic.DynamicObject" /> class, binder.Name
        /// returns "SampleProperty". The binder.IgnoreCase property specifies whether the member name is
        /// case-sensitive.
        /// </param>
        /// <param name="value">
        /// The value to set to the member. For example, for sampleObject.SampleProperty = "Test", where
        /// sampleObject is an instance of the class derived from the
        /// <see cref="T:System.Dynamic.DynamicObject" /> class, the <paramref name="value" /> is "Test".
        /// </param>
        ///
        /// <returns>
        /// true if the operation is successful; otherwise, false. If this method returns false, the run-
        /// time binder of the language determines the behavior. (In most cases, a language-specific run-
        /// time exception is thrown.)
        /// </returns>

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            return TrySetMember(binder.Name, value);
            
        }

        /// <summary>
        /// Provides the implementation for operations that set member values. Classes derived from the
        /// <see cref="T:System.Dynamic.DynamicObject" /> class can override this method to specify
        /// dynamic behavior for operations such as setting a value for a property.
        /// </summary>
        ///
        /// <param name="name">
        /// .
        /// </param>
        /// <param name="value">
        /// The value to set to the member. For example, for sampleObject.SampleProperty = "Test", where
        /// sampleObject is an instance of the class derived from the
        /// <see cref="T:System.Dynamic.DynamicObject" /> class, the <paramref name="value" /> is "Test".
        /// </param>
        ///
        /// <returns>
        /// true if the operation is successful; otherwise, false. If this method returns false, the run-
        /// time binder of the language determines the behavior. (In most cases, a language-specific run-
        /// time exception is thrown.)
        /// </returns>

        protected bool TrySetMember(string name, object value)
        {
            try
            {
                if (String.IsNullOrEmpty(name))
                {
                    return false;
                }

                if (value is IDictionary<string, object> && !(value is JsObject))
                {
                    InnerProperties[name] = ToJsObject((IDictionary<string, object>)value);
                }
                else
                {
                    InnerProperties[name] = value;
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Test if a named property exists
        /// </summary>
        ///
        /// <param name="name">
        /// The property name
        /// </param>
        ///
        /// <returns>
        /// true if the property exists, false if not.
        /// </returns>

        public bool HasProperty(string name)
        {
            return InnerProperties.ContainsKey(name);
        }

        /// <summary>
        /// Deletes a named property.
        /// </summary>
        ///
        /// <param name="name">
        /// The property to delete.
        /// </param>
        ///
        /// <returns>
        /// true if it succeeds, false if it fails.
        /// </returns>

        public bool Delete(string name)
        {
            return InnerProperties.Remove(name);
        }

        /// <summary>
        /// Returns a new JsObject from a dictionary of key/value paris
        /// </summary>
        ///
        /// <param name="value">
        /// The value to set to the member. For example, for sampleObject.SampleProperty = "Test", where
        /// sampleObject is an instance of the class derived from the
        /// <see cref="T:System.Dynamic.DynamicObject" /> class, the <paramref name="value" /> is "Test".
        /// </param>
        ///
        /// <returns>
        /// value as a JsObject.
        /// </returns>

        protected JsObject ToJsObject(IDictionary<string, object> value)
        {
            JsObject obj = new JsObject();
            foreach (KeyValuePair<string, object> kvp in value)
            {
                obj[kvp.Key] = kvp.Value;
            }
            return obj;
        }

        /// <summary>
        /// Returns the enumeration of all dynamic member names.
        /// </summary>
        ///
        /// <returns>
        /// A sequence that contains dynamic member names.
        /// </returns>

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return InnerProperties.Keys;
        }

        /// <summary>
        /// The enumerator
        /// </summary>
        ///
        /// <returns>
        /// The enumerator.
        /// </returns>

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return InnerProperties.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        #region explicit interface members

        void IDictionary<string, object>.Add(string key, object value)
        {
            TrySetMember(key, value);
        }

        bool IDictionary<string, object>.ContainsKey(string key)
        {
            return InnerProperties.ContainsKey(key);
        }

        ICollection<string> IDictionary<string, object>.Keys
        {
            get { return InnerProperties.Keys; }
        }

        bool IDictionary<string, object>.Remove(string key)
        {
            return InnerProperties.Remove(key);
        }

        bool IDictionary<string, object>.TryGetValue(string key, out object value)
        {
            if (HasProperty(key))
            {
                return TryGetMember(key, typeof(object), out value);
            }
            else
            {
                value = null;
                return false;
            }
        }

        ICollection<object> IDictionary<string, object>.Values
        {
            get { return InnerProperties.Values; }
        }

        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
        {
            TrySetMember(item.Key, item.Value);
        }

        void ICollection<KeyValuePair<string, object>>.Clear()
        {
            InnerProperties.Clear();
        }

        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
        {
            return InnerProperties.Contains(item);
        }

        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            InnerProperties.CopyTo(array, arrayIndex);
        }

        int ICollection<KeyValuePair<string, object>>.Count
        {
            get { return InnerProperties.Count; }
        }

        bool ICollection<KeyValuePair<string, object>>.IsReadOnly
        {
            get { return false; }
        }

        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
        {
            return InnerProperties.Remove(item);
        }

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion

    }
}
