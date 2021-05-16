using System;
using Newtonsoft.Json;

namespace Jackett.Common.Converters
{
    /// <summary>
    /// converts a string value to a long and vice-versa.
    /// </summary>
    public sealed class StringToLongConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            => writer.WriteValue(value.ToString());

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
            {
                return null;
            }

            if (reader.Value is long)
            {
                return reader.Value;
            }

            return long.TryParse((string)reader.Value, out var foo)
                ? foo
                : (long?)null;
        }

        public override bool CanConvert(Type objectType) => objectType == typeof(string);
    }
}
