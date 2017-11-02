using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CsQuery.Utility
{
    /// <summary>
    /// Interface fora  JSON serializer.
    /// </summary>

    public interface IJsonSerializer
    {
        /// <summary>
        /// Serializes an object to JSON
        /// </summary>
        ///
        /// <param name="value">
        /// The object to serialize
        /// </param>
        ///
        /// <returns>
        /// A JSON string
        /// </returns>

        string Serialize(object value);

        /// <summary>
        /// Deserializes a string of JSON to a CLR object
        /// </summary>
        ///
        /// <param name="json">
        /// The JSON.
        /// </param>
        /// <param name="type">
        /// The type of object to create
        /// </param>
        ///
        /// <returns>
        /// An object
        /// </returns>

        object Deserialize(string json, Type type);

        /// <summary>
        /// Deserializes a string of JSON to a strongly-typed object
        /// </summary>
        ///
        /// <typeparam name="T">
        /// The type of object
        /// </typeparam>
        /// <param name="json">
        /// The JSON.
        /// </param>
        ///
        /// <returns>
        /// A new object of type T
        /// </returns>

        T Deserialize<T>(string json);
    }
}
