using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.IndexerConfig
{
    public interface ISerializableConfig
    {
        JObject Serialize();
        ISerializableConfig Deserialize(JObject jobj);
    }
}
