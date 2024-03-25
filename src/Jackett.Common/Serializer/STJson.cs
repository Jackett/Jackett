using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jackett.Common.Serializer
{
    public static class STJson
    {
        private static readonly JsonSerializerOptions _SerializerSettings = GetSerializerSettings();

        public static JsonSerializerOptions GetSerializerSettings()
        {
            var settings = new JsonSerializerOptions();
            ApplySerializerSettings(settings);
            return settings;
        }

        public static void ApplySerializerSettings(JsonSerializerOptions serializerSettings)
        {
            serializerSettings.AllowTrailingCommas = true;
            serializerSettings.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            serializerSettings.PropertyNameCaseInsensitive = true;
            serializerSettings.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            serializerSettings.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            serializerSettings.WriteIndented = true;

            serializerSettings.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, true));
            serializerSettings.Converters.Add(new BooleanConverter());
        }

        public static T Deserialize<T>(string json)
            where T : new()
        {
            return JsonSerializer.Deserialize<T>(json, _SerializerSettings);
        }

        public static bool TryDeserialize<T>(string json, out T result)
            where T : new()
        {
            try
            {
                result = Deserialize<T>(json);
                return true;
            }
            catch (JsonException)
            {
                result = default(T);
                return false;
            }
        }

        public static string ToJson(object obj)
        {
            return JsonSerializer.Serialize(obj, _SerializerSettings);
        }
    }
}
