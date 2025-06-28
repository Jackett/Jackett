using System;
using Jackett.Common.Models.DTO.Anilibria;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jackett.Common.Serializer
{
    public class AnilibriaTopTorrentInfoConverter : JsonConverter<AnilibriaTorrentInfo>
    {
        public override AnilibriaTorrentInfo ReadJson(JsonReader reader, Type objectType, AnilibriaTorrentInfo existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);

            return new AnilibriaTorrentInfo
            {
                Id = (long?)obj["id"] ?? 0,
                Hash = (string)obj["hash"],
                Size = (long?)obj["size"] ?? 0,
                Magnet = (string)obj["magnet"],
                Seeders = (long?)obj["seeders"] ?? 0,
                Leechers = (long?)obj["leechers"] ?? 0,
                Label = (string)obj["label"],
                NameMain = (string)obj["release"]?["name"]?["main"],
                NameEnglish = (string)obj["release"]?["name"]?["english"],
                Alias = (string)obj["release"]?["alias"],
                PosterSrc = (string)obj["release"]?["poster"]?["src"],
                CreatedAt = (DateTime?)obj["created_at"] ?? DateTime.MinValue,
                Year = (int?)obj["year"] ?? 0,
                Grabs = (long?)obj["completed_times"] ?? 0,
                Category = (string)obj["release"]?["type"]?["value"],
            };
        }

        public override void WriteJson(JsonWriter writer, AnilibriaTorrentInfo value, JsonSerializer serializer) => throw new NotImplementedException("Serialization not implemented.");
    }
}
