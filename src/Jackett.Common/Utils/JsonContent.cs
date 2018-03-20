using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Jackett.Common.Utils
{
    public class JsonContent : StringContent
    {
        public JsonContent(object value)
            : this(value, Encoding.UTF8)
        {
            Headers.ContentType.CharSet = "utf-8";
        }

        public JsonContent(object value, Encoding encoding)
            : base(JsonConvert.SerializeObject(value, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }), encoding, "application/json")
        {
        }
    }
}
