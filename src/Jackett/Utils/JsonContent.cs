using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Utils
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
