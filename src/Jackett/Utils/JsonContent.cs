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
    public class JsonContent : HttpContent
    {
        private readonly object _value;

        public JsonContent(object value)
        {
            _value = value;
            Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }

        protected override async Task SerializeToStreamAsync(Stream stream,
            TransportContext context)
        {
            var json = JsonConvert.SerializeObject(_value, Formatting.Indented, new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore});
            var writer = new StreamWriter(stream);
            writer.Write(json);
            await writer.FlushAsync();
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }
    }
}
