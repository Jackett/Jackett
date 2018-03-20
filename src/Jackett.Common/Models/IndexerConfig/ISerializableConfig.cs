using Newtonsoft.Json.Linq;

namespace Jackett.Common.Models.IndexerConfig
{
    public interface ISerializableConfig
    {
        JObject Serialize();
        ISerializableConfig Deserialize(JObject jobj);
    }
}
