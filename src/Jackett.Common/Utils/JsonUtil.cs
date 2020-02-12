using System;
using System.Text;
using Newtonsoft.Json;

namespace Jackett.Common.Utils
{
    public class EncodingJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => true;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var obj = value as Encoding;
            writer.WriteValue(obj.WebName);
        }

        public override bool CanRead => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) =>
            throw new NotImplementedException();
    }
}
