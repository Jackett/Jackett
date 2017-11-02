using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.IO;
using CsQuery.ExtensionMethods.Internal;
using CsQuery.Utility;
using CsQuery.Engine;
using CsQuery.Promises;
using CsQuery.HtmlParser;
using CsQuery.Implementation;

namespace CsQuery
{
    public partial class CQ
    {
        #region CsQuery global options

        /// <summary>
        /// DEPRECATED. Please use CsQuery.Config.DomRenderingOptions. 
        /// </summary>
        
        [Obsolete]
        public static DomRenderingOptions DefaultDomRenderingOptions 
        {
            get {
                return Config.DomRenderingOptions;
            }
            set {
                Config.DomRenderingOptions = value;
            }
        }

        /// <summary>
        /// DEPRECATED. Please use CsQuery.Config.DocType
        /// </summary>
        
        [Obsolete]
        public static DocType DefaultDocType {
            get
            {
                return Config.DocType;
            }
            set
            {
                Config.DocType = value;
            }
        }

        #endregion 

        #region private properties
        
        #endregion

        #region static utility methods

        /// <summary>
        /// Convert an object to JSON.
        /// </summary>
        ///
        /// <param name="json">
        /// The obect to serialize.
        /// </param>
        ///
        /// <returns>
        /// A JSON formatted string.
        /// </returns>

        public static string ToJSON(object json)
        {
            return Utility.JSON.ToJSON(json);

        }

        /// <summary>
        /// Parse JSON into a typed object.
        /// </summary>
        ///
        /// <typeparam name="T">
        /// The target type of the object to create.
        /// </typeparam>
        /// <param name="json">
        /// The JSON string to deserialize.
        /// </param>
        ///
        /// <returns>
        /// A new object of type T
        /// </returns>

        public static T ParseJSON<T>(string json)
        {
            return Utility.JSON.ParseJSON<T>(json);
        }

        /// <summary>
        /// Parse a JSON string into an expando object, or a json value into a primitive type.
        /// </summary>
        ///
        /// <param name="json">
        /// The JSON string to deserialize.
        /// </param>
        ///
        /// <returns>
        /// A new object of type T
        /// </returns>

        public static object ParseJSON(string json)
        {
            return Utility.JSON.ParseJSON(json);
        }

        /// <summary>
        /// Parse a JSON string into an expando object, or a json value into a primitive type.
        /// </summary>
        ///
        /// <param name="json">
        /// The JSON string to deserialize.
        /// </param>
        /// <param name="type">
        /// The type of object to create
        /// </param>
        ///
        /// <returns>
        /// A new object of the specified type
        /// </returns>

        public static object ParseJSON(string json, Type type)
        {
            return Utility.JSON.ParseJSON(json, type);
        }

        /// <summary>
        /// Convert a dictionary to a dynamic object. Use to get another expando object from a sub-
        /// object of an expando object, e.g. as returned from JSON data.
        /// </summary>
        ///
        /// <param name="obj">
        /// The object.
        /// </param>
        ///
        /// <returns>
        /// obj as a JsObject.
        /// </returns>

        public static JsObject ToExpando(object obj)
        {
            JsObject result;


            if (obj is IDictionary<string, object>)
            {
                result = Objects.Dict2Dynamic<JsObject>((IDictionary<string, object>)obj);
            }
            else
            {
                return Objects.ToExpando(obj);
            }
            return result;
        }

        /// <summary>
        /// Converts an object to a dynamic object of type T.
        /// </summary>
        ///
        /// <typeparam name="T">
        /// The type of object to create. This must be an IDynamicMetaObjectProvider that also implements
        /// IDictionary&lt;string,object&gt;
        /// </typeparam>
        /// <param name="obj">
        /// The object.
        /// </param>
        ///
        /// <returns>
        /// A new object of type T.
        /// </returns>

        public static T ToDynamic<T>(object obj) where T : IDynamicMetaObjectProvider, IDictionary<string,object>,new()
        {
            if (obj is IDictionary<string, object>)
            {
                return Objects.Dict2Dynamic<T>((IDictionary<string, object>)obj);
            }
            else
            {
                return Objects.ToExpando<T>(obj);
            }
        }
        
        #endregion

    }
}
