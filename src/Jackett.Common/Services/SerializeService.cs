using Jackett.Common.Services.Interfaces;
using Newtonsoft.Json;

namespace Jackett.Common.Services
{

    class SerializeService : ISerializeService
    {
        public string Serialise(object obj)
        {
            return JsonConvert.SerializeObject(obj,Formatting.Indented);
        }

        public T DeSerialise<T>(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {
                return default(T);
            }
        }
    }
}
