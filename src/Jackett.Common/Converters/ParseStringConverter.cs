using System;
using Newtonsoft.Json;

namespace Jackett.Common.Converters
{
    public sealed class ParseStringConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            => writer.WriteValue(value.ToString());

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
            {
                return null;
            }
            if (long.TryParse((string)reader.Value, out var foo))
            {
                return foo;
            }
            else
            {
                return null;
            }
        }

        public override bool CanConvert(Type objectType) => objectType == typeof(string);
    }
}
