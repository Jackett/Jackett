using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jackett.Common.Serializer
{
    public class BooleanConverter : JsonConverter<bool>
    {
        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.Number => reader.GetInt64() switch
                {
                    1 => true,
                    0 => false,
                    _ => throw new JsonException()
                },
                _ => throw new JsonException()
            };
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        {
            writer.WriteBooleanValue(value);
        }
    }

}
